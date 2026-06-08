using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace InCleanHome.API.Payments.Infrastructure.ExternalServices.PayPal;

public interface IPayPalService
{
    bool IsEnabled { get; }
    string ClientIdPublic { get; }
    string Currency { get; }
    string Environment { get; }

    /// <summary>Crea una orden y devuelve el approveLink al que redirigir al cliente.</summary>
    Task<PayPalCreateOrderResult> CreateOrderAsync(decimal amount, string currency, string reference, string description);

    /// <summary>Captura una orden previamente aprobada por el cliente.</summary>
    Task<PayPalCaptureResult> CaptureOrderAsync(string orderId);
}

public record PayPalCreateOrderResult(
    bool Success,
    string? OrderId,
    string? ApproveLink,
    string? Error);

public record PayPalCaptureResult(
    bool Success,
    string? OrderId,
    string? CaptureId,
    string? Status,
    decimal? Amount,
    string? Currency,
    string? Error);

/// <summary>
/// Cliente HTTP para PayPal Orders API v2.
///
/// Flujo:
///   1) CreateOrderAsync -> POST /v2/checkout/orders con intent=CAPTURE.
///      Devuelve un orderId y un link de aprobación (approve URL).
///   2) Frontend redirige al cliente a la approve URL.
///   3) Cliente paga en sandbox.paypal.com.
///   4) PayPal redirige a ReturnUrl?token={orderId}&PayerID={payerId}.
///   5) Frontend llama a CaptureOrderAsync con el orderId.
///   6) CaptureOrderAsync -> POST /v2/checkout/orders/{id}/capture.
///      Confirma el pago. Devuelve status=COMPLETED si todo OK.
///
/// Autenticación:
///   PayPal usa OAuth 2.0 Client Credentials. Para cada operación pedimos un
///   access_token con Basic Auth (clientId:clientSecret) -> /v1/oauth2/token.
///   El token dura ~9h. Se cachea en memoria entre llamadas.
/// </summary>
public class PayPalService : IPayPalService
{
    private readonly PayPalSettings _settings;
    private readonly HttpClient _http;

    // Cache simple del access_token entre requests (no queremos pedirlo cada vez).
    private static string? _cachedToken;
    private static DateTime _tokenExpiresAt = DateTime.MinValue;
    private static readonly SemaphoreSlim _tokenLock = new(1, 1);

    public PayPalService(IOptions<PayPalSettings> options, IHttpClientFactory httpFactory)
    {
        _settings = options.Value;
        _http = httpFactory.CreateClient("paypal");
    }

    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(_settings.ClientId) &&
        !string.IsNullOrWhiteSpace(_settings.ClientSecret);

    public string ClientIdPublic => _settings.ClientId;
    public string Currency => _settings.Currency;
    public string Environment => _settings.Environment;

    // ── Auth: access_token con caché ─────────────────────────────────────────

    private async Task<string?> GetAccessTokenAsync()
    {
        if (_cachedToken is not null && _tokenExpiresAt > DateTime.UtcNow.AddMinutes(2))
            return _cachedToken;

        await _tokenLock.WaitAsync();
        try
        {
            if (_cachedToken is not null && _tokenExpiresAt > DateTime.UtcNow.AddMinutes(2))
                return _cachedToken;

            var url = $"{_settings.BaseUrl}/v1/oauth2/token";
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            var basic = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}"));
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
            req.Content = new StringContent("grant_type=client_credentials",
                Encoding.UTF8, "application/x-www-form-urlencoded");

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                Console.WriteLine($"[PayPal] Auth failed: HTTP {(int)resp.StatusCode} {body}");
                return null;
            }

            using var doc = JsonDocument.Parse(body);
            var token = doc.RootElement.GetProperty("access_token").GetString();
            var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var e) ? e.GetInt32() : 28800;
            _cachedToken = token;
            _tokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
            return token;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[PayPal] Auth exception: {e.Message}");
            return null;
        }
        finally { _tokenLock.Release(); }
    }

    // ── Create Order ─────────────────────────────────────────────────────────

    public async Task<PayPalCreateOrderResult> CreateOrderAsync(
        decimal amount, string currency, string reference, string description)
    {
        if (!IsEnabled)
            return new PayPalCreateOrderResult(false, null, null, "PayPal is not configured");

        var token = await GetAccessTokenAsync();
        if (token is null)
            return new PayPalCreateOrderResult(false, null, null, "PayPal auth failed");

        try
        {
            var url = $"{_settings.BaseUrl}/v2/checkout/orders";
            var bodyObj = new
            {
                intent = "CAPTURE",
                purchase_units = new[]
                {
                    new
                    {
                        reference_id = reference,
                        description  = description,
                        amount = new
                        {
                            currency_code = currency,
                            value         = amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                        }
                    }
                },
                application_context = new
                {
                    return_url = _settings.ReturnUrl,
                    cancel_url = _settings.CancelUrl,
                    user_action = "PAY_NOW",
                    brand_name = "InCleanHome",
                    shipping_preference = "NO_SHIPPING"
                }
            };
            var json = JsonSerializer.Serialize(bodyObj);

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await _http.SendAsync(req);
            var respText = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                Console.WriteLine($"[PayPal] CreateOrder failed: HTTP {(int)resp.StatusCode} {respText}");
                return new PayPalCreateOrderResult(false, null, null, $"HTTP {(int)resp.StatusCode}");
            }

            using var doc = JsonDocument.Parse(respText);
            var orderId = doc.RootElement.GetProperty("id").GetString();

            string? approveLink = null;
            if (doc.RootElement.TryGetProperty("links", out var links))
            {
                foreach (var link in links.EnumerateArray())
                {
                    var rel = link.TryGetProperty("rel", out var r) ? r.GetString() : null;
                    var href = link.TryGetProperty("href", out var h) ? h.GetString() : null;
                    if (rel == "approve" || rel == "payer-action")
                    {
                        approveLink = href;
                        break;
                    }
                }
            }

            if (orderId is null || approveLink is null)
                return new PayPalCreateOrderResult(false, null, null, "Order created but no approve link");

            return new PayPalCreateOrderResult(true, orderId, approveLink, null);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[PayPal] CreateOrder exception: {e.Message}");
            return new PayPalCreateOrderResult(false, null, null, e.Message);
        }
    }

    // ── Capture Order ────────────────────────────────────────────────────────

    public async Task<PayPalCaptureResult> CaptureOrderAsync(string orderId)
    {
        if (!IsEnabled)
            return new PayPalCaptureResult(false, orderId, null, null, null, null, "PayPal is not configured");

        var token = await GetAccessTokenAsync();
        if (token is null)
            return new PayPalCaptureResult(false, orderId, null, null, null, null, "PayPal auth failed");

        try
        {
            var url = $"{_settings.BaseUrl}/v2/checkout/orders/{orderId}/capture";
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            var resp = await _http.SendAsync(req);
            var respText = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                Console.WriteLine($"[PayPal] CaptureOrder failed: HTTP {(int)resp.StatusCode} {respText}");
                return new PayPalCaptureResult(false, orderId, null, null, null, null,
                    $"HTTP {(int)resp.StatusCode}");
            }

            using var doc = JsonDocument.Parse(respText);
            var status = doc.RootElement.TryGetProperty("status", out var s) ? s.GetString() : null;

            string? captureId = null;
            decimal? amount = null;
            string? currency = null;
            if (doc.RootElement.TryGetProperty("purchase_units", out var pus))
            {
                foreach (var pu in pus.EnumerateArray())
                {
                    if (!pu.TryGetProperty("payments", out var pay)) continue;
                    if (!pay.TryGetProperty("captures", out var caps)) continue;
                    foreach (var cap in caps.EnumerateArray())
                    {
                        captureId = cap.TryGetProperty("id", out var cid) ? cid.GetString() : null;
                        if (cap.TryGetProperty("amount", out var amt))
                        {
                            currency = amt.TryGetProperty("currency_code", out var c) ? c.GetString() : null;
                            if (amt.TryGetProperty("value", out var v) &&
                                decimal.TryParse(v.GetString(), System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                                amount = parsed;
                        }
                        break;
                    }
                    break;
                }
            }

            return new PayPalCaptureResult(true, orderId, captureId, status, amount, currency, null);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[PayPal] CaptureOrder exception: {e.Message}");
            return new PayPalCaptureResult(false, orderId, null, null, null, null, e.Message);
        }
    }
}
