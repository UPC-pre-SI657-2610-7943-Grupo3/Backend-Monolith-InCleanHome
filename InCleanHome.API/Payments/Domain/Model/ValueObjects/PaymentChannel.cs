namespace InCleanHome.API.Payments.Domain.Model.ValueObjects;

/// <summary>
///     Canal por el que el cliente pagó un servicio completado.
///     <para>
///     <c>MercadoPago</c> pasa por la pasarela oficial Mercado Pago Perú (sandbox /
///     producción) usando el SDK Bricks en el frontend y la API REST de MP en
///     el backend. El resto son flujos manuales: el cliente paga "por fuera"
///     (Yape/Plin/transferencia bancaria) y confirma en la app que ya pagó.
///     </para>
///     <para>
///     La comisión del 10% se aplica a TODOS los canales: aunque Yape/Plin/Bank
///     no estén intermediados por la plataforma, la plataforma registra la
///     comisión virtualmente (la trabajadora la verá descontada del pago
///     simulado al solicitar cobro).
///     </para>
///     <para>
///     <c>Efectivo</c> fue removido del sistema: no permite cobrar comisión ni
///     emitir comprobante trazable, y rompe el modelo de monetización.
///     </para>
/// </summary>
public static class PaymentChannel
{
    public const string MercadoPago = "mercadopago";
    public const string Yape        = "yape";
    public const string Plin        = "plin";
    public const string Bank        = "bank_transfer";

    public static readonly string[] All = { MercadoPago, Yape, Plin, Bank };

    public static bool IsValid(string c) => All.Contains(c);

    /// <summary>
    ///     True si este canal está intermediado por una pasarela (hoy solo MP).
    ///     Útil para mostrar el ícono correcto del canal y para distinguir
    ///     auditoría de transacciones reales vs. confirmaciones manuales.
    /// </summary>
    public static bool IsGatewayMediated(string c) => c == MercadoPago;

    /// <summary>
    ///     Todos los canales restantes generan comisión 10% (Efectivo fue eliminado).
    /// </summary>
    public static bool ChargesPlatformFee(string _) => true;
}
