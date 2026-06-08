namespace InCleanHome.API.Payments.Infrastructure.ExternalServices.Izipay;

/// <summary>
/// Configuración de Izipay (alias "micuentaweb / PayZen Perú").
///
/// Leído desde appsettings.json -> sección "Izipay".
///
/// MODO SIMULATION = true (por defecto):
///   - No se llama a la API real de Izipay. El service devuelve un formToken
///     sintético con prefijo "SIMULATED-...". El frontend reconoce ese prefijo
///     y abre un dialog de "pago simulado" con botones aprobar/rechazar.
///   - Sirve para clases/demos y para desarrollar el frontend sin tener una
///     cuenta de Izipay activa.
///   - El endpoint /api/payments/izipay/confirm-simulation se habilita.
///
/// MODO SIMULATION = false (producción / TEST real):
///   - Se llama a POST {Endpoint}/api-payment/V4/Charge/CreatePayment con
///     HTTP Basic auth (ShopId : Password).
///   - El formToken devuelto se entrega al frontend, que lo renderiza con el
///     SDK Krypton (kr-payment-form).
///   - Izipay confirma el pago vía webhook firmado HMAC-SHA256, que llega a
///     /api/payments/izipay/ipn.
///
/// Conseguir credenciales: https://secure.micuentaweb.pe -> Configuración ->
/// Tienda -> Claves de API REST. Anotar ShopId (usuario), Password TEST,
/// Clave pública TEST (para el JS del navegador) y Clave HMAC-SHA256 TEST.
/// </summary>
public class IzipaySettings
{
    public bool   Simulation     { get; set; } = true;
    public string ShopId         { get; set; } = string.Empty;
    public string Password       { get; set; } = string.Empty;
    public string PublicKey      { get; set; } = string.Empty;
    public string HmacSha256Key  { get; set; } = string.Empty;
    public string Endpoint       { get; set; } = "https://api.micuentaweb.pe";
    public string Currency       { get; set; } = "PEN";
}
