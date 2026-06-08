namespace InCleanHome.API.Payments.Infrastructure.ExternalServices.PayPal;

/// <summary>
/// Configuración de PayPal Orders API v2 (redirect flow).
///
/// Modo Sandbox (Environment = "Sandbox"):
///   Base URL: https://api-m.sandbox.paypal.com
///   Las transacciones no mueven dinero real; se usan cuentas de prueba creadas
///   en developer.paypal.com -> Testing Tools -> Sandbox Accounts.
///
/// Modo Live (Environment = "Live"):
///   Base URL: https://api-m.paypal.com
///   Requiere cuenta de comercio aprobada por PayPal. NO se usa en este proyecto.
///
/// IMPORTANTE: PayPal Perú no acepta PEN nativamente. La moneda configurada
/// es USD y la app hace conversión cosmética 1:1 (S/. 50 -> $50 en sandbox).
/// En producción real habría que aplicar tipo de cambio.
/// </summary>
public class PayPalSettings
{
    public string Environment  { get; set; } = "Sandbox";
    public string ClientId     { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Currency     { get; set; } = "USD";

    /// <summary>URL a la que PayPal redirige después de pago exitoso (con ?token=&PayerID=).</summary>
    public string ReturnUrl    { get; set; } = "http://localhost:5173/client/payment-success";

    /// <summary>URL a la que PayPal redirige si el cliente cancela en el checkout.</summary>
    public string CancelUrl    { get; set; } = "http://localhost:5173/client/payment-cancel";

    /// <summary>Base URL calculada según Environment.</summary>
    public string BaseUrl => Environment?.ToLowerInvariant() == "live"
        ? "https://api-m.paypal.com"
        : "https://api-m.sandbox.paypal.com";
}
