using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace InCleanHome.API.Payments.Infrastructure.ExternalServices.Izipay;

public interface IIzipayService
{
    /// <summary>
    /// True si la integración está conectada a Izipay real (no es simulación).
    /// El frontend lo usa para decidir qué SDK cargar (Krypton vs el dialog simulado).
    /// </summary>
    bool IsRealMode { get; }
    string PublicKey { get; }
    string Endpoint { get; }
    string Currency { get; }

    Task<IzipayChargeResult> CreatePaymentAsync(
        decimal amount, string orderId, string customerEmail);

    bool VerifyIpnSignature(string payload, string signature);
}

public record IzipayChargeResult(
    bool Success,
    string? FormToken,
    string? PublicKey,
    string? Endpoint,
    string? OrderId,
    string? Error,
    bool Simulated);

/// <summary>
/// Cliente para Izipay.
///
/// Endpoint principal (modo real):
///   POST {Endpoint}/api-payment/V4/Charge/CreatePayment
///   Authorization: Basic base64(ShopId:Password)
///   Body: { amount (centavos), currency, orderId, customer: { email } }
///   Respuesta: { status: "SUCCESS", answer: { formToken: "..." } }
///
/// IPN/webhook (modo real):
///   Izipay POSTea kr-answer (JSON) y kr-hash (HMAC-SHA256 hex) cuando se
///   completa un pago. Verificamos kr-hash con HmacSha256Key.
/// </summary>
public class IzipayService : IIzipayService
{
    private readonly IzipaySettings _settings;
    private readonly HttpClient _http;

    public IzipayService(IOptions<IzipaySettings> options, IHttpClientFactory httpFactory)
    {
        _settings = options.Value;
        _http = httpFactory.CreateClient("izipay");
    }

    public bool IsRealMode =>
        !_settings.Simulation && !string.IsNullOrWhiteSpace(_settings.ShopId);

    public string PublicKey =>
        IsRealMode ? _settings.PublicKey : "SIMULATED_PUBLIC_KEY";

    public string Endpoint => _settings.Endpoint;
    public string Currency => _settings.Currency;

    public async Task<IzipayChargeResult> CreatePaymentAsync(
        decimal amount, string orderId, string customerEmail)
    {
        if (amount <= 0)
            return new IzipayChargeResult(false, null, null, null, orderId,
                "Amount must be greater than zero", false);

        // ── Modo simulación ──────────────────────────────────────────────────
        if (!IsRealMode)
        {
            // formToken sintético reconocible por el frontend.
            // Limita a 80 chars para evitar problemas si Izipay validase tamaño.
            var raw = $"SIMULATED-{orderId}-{(long)(amount * 100)}-{Guid.NewGuid():N}";
            var simToken = raw.Length > 80 ? raw.Substring(0, 80) : raw;
            Console.WriteLine($"[Izipay:SIM] formToken creado order={orderId} amount={amount} {_settings.Currency} email={customerEmail}");
            return new IzipayChargeResult(true, simToken, PublicKey, _settings.Endpoint, orderId, null, true);
        }

        // ── Modo real ────────────────────────────────────────────────────────
        try
        {
            var url = $"{_settings.Endpoint.TrimEnd('/')}/api-payment/V4/Charge/CreatePayment";
            var bodyObj = new
            {
                amount   = (long)Math.Round(amount * 100m), // Izipay espera centavos
                currency = _settings.Currency,
                orderId  = orderId,
                customer = new { email = customerEmail }
            };
            var json = JsonSerializer.Serialize(bodyObj);

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            var basic = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_settings.ShopId}:{_settings.Password}"));
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await _http.SendAsync(req);
            var respText = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Izipay] HTTP {(int)resp.StatusCode}: {respText}");
                return new IzipayChargeResult(false, null, null, _settings.Endpoint, orderId,
                    $"HTTP {(int)resp.StatusCode}", false);
            }

            using var doc = JsonDocument.Parse(respText);
            var status = doc.RootElement.TryGetProperty("status", out var s) ? s.GetString() : null;
            if (status != "SUCCESS")
            {
                Console.WriteLine($"[Izipay] non-SUCCESS response: {respText}");
                return new IzipayChargeResult(false, null, null, _settings.Endpoint, orderId,
                    "Izipay returned non-SUCCESS status", false);
            }

            var formToken = doc.RootElement
                .GetProperty("answer")
                .GetProperty("formToken")
                .GetString();
            return new IzipayChargeResult(true, formToken, _settings.PublicKey,
                _settings.Endpoint, orderId, null, false);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Izipay] Exception calling API: {e.Message}");
            return new IzipayChargeResult(false, null, null, _settings.Endpoint, orderId, e.Message, false);
        }
    }

    /// <summary>
    /// Verifica la firma HMAC-SHA256 de un IPN entrante.
    /// payload   = exactamente el string del campo "kr-answer".
    /// signature = el valor del campo "kr-hash" (hex).
    /// </summary>
    public bool VerifyIpnSignature(string payload, string signature)
    {
        if (!IsRealMode) return true; // En sim aceptamos cualquier cosa.
        if (string.IsNullOrEmpty(_settings.HmacSha256Key) || string.IsNullOrEmpty(signature)) return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.HmacSha256Key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var hex = Convert.ToHexString(hash).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hex),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));
    }
}
