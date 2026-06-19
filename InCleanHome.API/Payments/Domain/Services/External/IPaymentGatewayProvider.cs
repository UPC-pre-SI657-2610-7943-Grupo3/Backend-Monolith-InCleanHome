namespace InCleanHome.API.Payments.Domain.Services.External;

/// <summary>
///     Puerto del dominio para una pasarela de pagos genérica.
/// </summary>
/// <remarks>
///     <para>
///     Este es el contrato que el dominio conoce. Las implementaciones concretas
///     (adapters) viven en <c>Infrastructure/ExternalServices/&lt;Provider&gt;/</c>
///     y traducen este contrato a las llamadas reales del SDK del proveedor.
///     </para>
///     <para>
///     Hoy la única implementación es <c>MercadoPagoAdapter</c>. Si mañana se
///     cambia a Stripe/Niubiz/cualquier otra, basta con agregar un nuevo adapter
///     que implemente esta interfaz y registrarlo en DI — el resto del código
///     (controllers, command services) no cambia.
///     </para>
/// </remarks>
public interface IPaymentGatewayProvider
{
    /// <summary>
    ///     Crea una intención de pago en la pasarela. El frontend usa el resultado
    ///     (id de preferencia + URL de checkout) para redirigir al cliente.
    /// </summary>
    Task<CreatePaymentIntentResult> CreatePaymentIntentAsync(CreatePaymentIntentRequest request);

    /// <summary>
    ///     Consulta el estado actual de un pago en la pasarela. Se usa tras el
    ///     redirect de retorno para confirmar si efectivamente se aprobó.
    /// </summary>
    Task<PaymentStatusResult> GetPaymentStatusAsync(string paymentId);

    /// <summary>
    ///     Busca en la pasarela un pago aprobado para un <c>external_reference</c>
    ///     dado (nuestro bookingId). Devuelve null si no hay ninguno aprobado.
    ///     Sirve para el flujo "Ya pagué, verificar" en localhost, donde el
    ///     cliente no nos pasa el payment_id de regreso porque pagó en otra
    ///     pestaña.
    /// </summary>
    Task<PaymentStatusResult?> FindApprovedPaymentByExternalReferenceAsync(string externalReference);

    /// <summary>
    ///     Devuelve el Public Key (clave pública) que el frontend necesita para
    ///     inicializar el SDK de la pasarela. NUNCA devuelve el Access Token
    ///     (que es secreto y solo vive en el backend).
    /// </summary>
    string GetPublicKey();
}

/// <summary>Petición para crear una intención de pago.</summary>
/// <param name="BookingId">ID del booking que se va a pagar.</param>
/// <param name="Amount">Monto total en soles (con dos decimales).</param>
/// <param name="Description">Texto que aparece al cliente en la pasarela.</param>
/// <param name="PayerEmail">Email del cliente (la pasarela puede pre-rellenarlo).</param>
/// <param name="SuccessUrl">URL absoluta de retorno tras pago exitoso.</param>
/// <param name="FailureUrl">URL absoluta de retorno tras pago fallido.</param>
/// <param name="PendingUrl">URL absoluta de retorno tras pago pendiente.</param>
public record CreatePaymentIntentRequest(
    int BookingId,
    decimal Amount,
    string Description,
    string PayerEmail,
    string SuccessUrl,
    string FailureUrl,
    string PendingUrl);

/// <summary>
///     Resultado de crear una intención de pago.
/// </summary>
/// <param name="PreferenceId">ID interno de la pasarela para esta intención.</param>
/// <param name="CheckoutUrl">URL de redirección donde el cliente paga.</param>
public record CreatePaymentIntentResult(string PreferenceId, string CheckoutUrl);

/// <summary>
///     Resultado normalizado de consultar el estado de un pago.
/// </summary>
/// <param name="PaymentId">ID del pago en la pasarela.</param>
/// <param name="Status">Estado normalizado: approved | pending | rejected | refunded.</param>
/// <param name="Amount">Monto efectivamente cobrado.</param>
/// <param name="ProviderRawStatus">Estado tal como lo devuelve el proveedor (para debug/logs).</param>
public record PaymentStatusResult(string PaymentId, string Status, decimal Amount, string ProviderRawStatus);
