namespace InCleanHome.API.Payments.Infrastructure.ExternalServices.MercadoPago;

/// <summary>
///     Binding de configuración para Mercado Pago (sección <c>MercadoPago</c>
///     en <c>appsettings.json</c> / variables de entorno).
/// </summary>
/// <remarks>
///     <para>
///     <c>AccessToken</c> es SECRETO y debe vivir solo en el backend. Nunca se
///     envía al frontend ni se loguea en limpio. En desarrollo local se
///     configura en <c>appsettings.Development.json</c>; en producción se
///     inyecta como variable de entorno (Azure App Service / Render).
///     </para>
///     <para>
///     <c>PublicKey</c> es público por diseño y se entrega al frontend vía un
///     endpoint del adapter (<c>GET /api/payments/mercadopago/public-key</c>)
///     para inicializar el SDK Bricks.
///     </para>
///     <para>
///     <c>FrontendBaseUrl</c> se usa para armar las URLs de retorno
///     (success/failure/pending). En dev: <c>http://localhost:5173</c>; en
///     producción: el dominio de Netlify.
///     </para>
/// </remarks>
public class MercadoPagoSettings
{
    public const string SectionName = "MercadoPago";

    public string AccessToken    { get; set; } = string.Empty;
    public string PublicKey      { get; set; } = string.Empty;
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";
    public string BaseApiUrl     { get; set; } = "https://api.mercadopago.com";

    /// <summary>True si las credenciales están configuradas.</summary>
    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(AccessToken) && !string.IsNullOrWhiteSpace(PublicKey);
}
