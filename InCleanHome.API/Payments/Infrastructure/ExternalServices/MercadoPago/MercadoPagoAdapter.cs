using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using InCleanHome.API.Payments.Domain.Services.External;
using Microsoft.Extensions.Options;

namespace InCleanHome.API.Payments.Infrastructure.ExternalServices.MercadoPago;

/// <summary>
///     Adapter concreto que implementa <see cref="IPaymentGatewayProvider"/>
///     para Mercado Pago Perú.
/// </summary>
/// <remarks>
///     <para>
///     Esta clase es la ÚNICA que conoce los detalles de la API de Mercado Pago:
///     URL base, formato de payload, headers de autenticación, mapeo de estados.
///     El resto del proyecto solo conoce <see cref="IPaymentGatewayProvider"/>.
///     </para>
///     <para>
///     Si la API de MP cambia (nueva versión, nuevo endpoint, etc.), el cambio
///     vive únicamente acá. Si se decidiera cambiar a otra pasarela, basta con
///     crear otro adapter que implemente la misma interfaz.
///     </para>
///     <para>Documentación oficial:
///     <a href="https://www.mercadopago.com.pe/developers/es/reference/preferences/_checkout_preferences/post">
///     Crear preferencia</a> y
///     <a href="https://www.mercadopago.com.pe/developers/es/reference/payments/_payments_id/get">
///     Consultar pago</a>.
///     </para>
/// </remarks>
public class MercadoPagoAdapter : IPaymentGatewayProvider
{
    private readonly HttpClient _httpClient;
    private readonly MercadoPagoSettings _settings;
    private readonly ILogger<MercadoPagoAdapter> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public MercadoPagoAdapter(
        HttpClient httpClient,
        IOptions<MercadoPagoSettings> settings,
        ILogger<MercadoPagoAdapter> logger)
    {
        _httpClient = httpClient;
        _settings   = settings.Value;
        _logger     = logger;

        // El Access Token va siempre como Bearer en todas las llamadas REST de MP.
        if (!string.IsNullOrWhiteSpace(_settings.AccessToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.AccessToken);
        }

        // La BaseAddress queda configurable para poder apuntar a stubs en tests.
        if (_httpClient.BaseAddress is null && !string.IsNullOrWhiteSpace(_settings.BaseApiUrl))
        {
            _httpClient.BaseAddress = new Uri(_settings.BaseApiUrl);
        }
    }

    public string GetPublicKey() => _settings.PublicKey;

    public async Task<CreatePaymentIntentResult> CreatePaymentIntentAsync(CreatePaymentIntentRequest request)
    {
        if (!_settings.IsEnabled)
            throw new InvalidOperationException(
                "Mercado Pago no está configurado. Verifica AccessToken y PublicKey en appsettings.");

        // Payload mínimo para crear una "preference" (checkout pro) en MP.
        // El cliente luego es redirigido a `init_point` (producción) o
        // `sandbox_init_point` (sandbox), y al volver MP nos llama a back_urls.

        // auto_return = "approved" hace que MP redirija al cliente automáticamente
        // tras pagar. PERO MP rechaza la preferencia con 400 si auto_return está
        // presente y las back_urls apuntan a localhost. Por eso solo lo incluimos
        // cuando las URLs son HTTPS públicas. En dev (localhost) el cliente verá
        // un botón "Volver al sitio" en MP que tiene que apretar manualmente.
        var isLocalhost = request.SuccessUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase)
                          || request.SuccessUrl.Contains("127.0.0.1");

        // Construimos el payload como Dictionary porque algunos campos son
        // opcionales (auto_return). Con un anonymous type tendríamos que
        // siempre incluir todos los campos, y MP rechaza null en auto_return.
        var payload = new Dictionary<string, object?>
        {
            ["items"] = new[]
            {
                new {
                    title       = request.Description,
                    quantity    = 1,
                    unit_price  = (double)request.Amount,
                    currency_id = "PEN",
                }
            },
            ["payer"] = new { email = request.PayerEmail },
            ["back_urls"] = new
            {
                success = request.SuccessUrl,
                failure = request.FailureUrl,
                pending = request.PendingUrl,
            },
            ["external_reference"]   = request.BookingId.ToString(),
            ["statement_descriptor"] = "InCleanHome",
        };
        if (!isLocalhost)
        {
            payload["auto_return"] = "approved";
        }

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/checkout/preferences", payload, JsonOpts);
            if (!response.IsSuccessStatusCode)
            {
                // Loguea el cuerpo de error que devuelve MP para diagnóstico real.
                // MP típicamente devuelve {"message": "...", "error": "...", "cause": [...]}
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("[MP] Preference creation failed ({StatusCode}): {Body}",
                    (int)response.StatusCode, errorBody);
                throw new InvalidOperationException(
                    $"Mercado Pago rechazó la preferencia ({(int)response.StatusCode}): {errorBody}");
            }
            var dto = await response.Content.ReadFromJsonAsync<MpPreferenceDto>(JsonOpts);
            if (dto is null || string.IsNullOrEmpty(dto.Id))
                throw new InvalidOperationException("Mercado Pago devolvió una preferencia inválida.");

            // En sandbox usamos sandbox_init_point; en producción init_point.
            // Detectamos sandbox por el prefijo del access token (TEST-...).
            var isSandbox = _settings.AccessToken.StartsWith("TEST-", StringComparison.OrdinalIgnoreCase);
            var checkoutUrl = isSandbox ? (dto.SandboxInitPoint ?? dto.InitPoint) : (dto.InitPoint ?? dto.SandboxInitPoint);

            return new CreatePaymentIntentResult(dto.Id, checkoutUrl ?? string.Empty);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[MP] Error creando preferencia para booking {BookingId}", request.BookingId);
            throw new InvalidOperationException(
                "No se pudo iniciar el pago con Mercado Pago. Verifica que las credenciales sean válidas.", ex);
        }
    }

    public async Task<PaymentStatusResult> GetPaymentStatusAsync(string paymentId)
    {
        if (!_settings.IsEnabled)
            throw new InvalidOperationException("Mercado Pago no está configurado.");

        try
        {
            var response = await _httpClient.GetAsync($"/v1/payments/{paymentId}");
            response.EnsureSuccessStatusCode();
            var dto = await response.Content.ReadFromJsonAsync<MpPaymentDto>(JsonOpts);
            if (dto is null)
                throw new InvalidOperationException("Mercado Pago devolvió un pago inválido.");

            // Mapeo de estados MP a estados normalizados del dominio:
            // approved -> approved | pending/in_process -> pending | rejected/cancelled -> rejected | refunded -> refunded.
            var normalized = dto.Status?.ToLowerInvariant() switch
            {
                "approved"   => "approved",
                "pending"    => "pending",
                "in_process" => "pending",
                "authorized" => "pending",
                "refunded"   => "refunded",
                "charged_back" => "refunded",
                _            => "rejected", // rejected, cancelled y cualquier otro
            };

            return new PaymentStatusResult(
                PaymentId: dto.Id?.ToString() ?? paymentId,
                Status:    normalized,
                Amount:    dto.TransactionAmount,
                ProviderRawStatus: dto.Status ?? "unknown");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[MP] Error consultando pago {PaymentId}", paymentId);
            throw new InvalidOperationException(
                "No se pudo consultar el estado del pago en Mercado Pago.", ex);
        }
    }

    /// <summary>
    ///     Implementa la búsqueda por external_reference usando la API
    ///     <c>GET /v1/payments/search?external_reference=&lt;bookingId&gt;</c>.
    ///     MP devuelve un array de resultados; quedamos con el primero que esté
    ///     aprobado.
    /// </summary>
    public async Task<PaymentStatusResult?> FindApprovedPaymentByExternalReferenceAsync(string externalReference)
    {
        if (!_settings.IsEnabled)
            throw new InvalidOperationException("Mercado Pago no está configurado.");
        if (string.IsNullOrWhiteSpace(externalReference))
            return null;

        try
        {
            var url = $"/v1/payments/search?external_reference={Uri.EscapeDataString(externalReference)}";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("[MP] /payments/search returned {Status}: {Body}", (int)response.StatusCode, body);
                return null;
            }
            var dto = await response.Content.ReadFromJsonAsync<MpSearchDto>(JsonOpts);
            if (dto?.Results is null || dto.Results.Count == 0)
                return null;

            // Tomamos el primer pago aprobado. Si hay varios intentos (rechazado
            // y luego aprobado), buscamos el approved específicamente.
            var approved = dto.Results.FirstOrDefault(p =>
                string.Equals(p.Status, "approved", StringComparison.OrdinalIgnoreCase));
            if (approved is null) return null;

            return new PaymentStatusResult(
                PaymentId: approved.Id?.ToString() ?? string.Empty,
                Status:    "approved",
                Amount:    approved.TransactionAmount,
                ProviderRawStatus: approved.Status ?? "approved");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[MP] Error buscando pagos por external_reference {Ref}", externalReference);
            return null;
        }
    }

    // ── DTOs internos: solo conocidos por este adapter ────────────────────
    // Mantienen los nombres tal cual MP los retorna (snake_case). No salen
    // hacia el resto del proyecto — el dominio solo ve los records normalizados.

    private record MpPreferenceDto(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("init_point")] string? InitPoint,
        [property: JsonPropertyName("sandbox_init_point")] string? SandboxInitPoint);

    private record MpPaymentDto(
        [property: JsonPropertyName("id")] long? Id,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("transaction_amount")] decimal TransactionAmount);

    private record MpSearchDto(
        [property: JsonPropertyName("results")] List<MpPaymentDto>? Results);
}
