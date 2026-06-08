namespace InCleanHome.API.Payments.Domain.Model.ValueObjects;

/// <summary>
/// Canal por el que el cliente pagó un servicio completado.
///
/// IzipayCard pasa por Izipay sandbox (formulario real con tarjeta de prueba).
/// PayPal pasa por PayPal Orders API v2 con redirect flow (sandbox.paypal.com).
/// El resto son flujos manuales: el cliente paga "por fuera" de la app (Yape al
/// celular de la trabajadora, Plin, transferencia, efectivo) y simplemente confirma
/// en la app que ya pagó.
///
/// IMPORTANTE: aunque solo IzipayCard y PayPal pasen realmente por una pasarela,
/// la comisión del 10% se calcula igual para Yape/Plin/Bank — la plataforma
/// "registra" la comisión virtualmente aunque no la cobre (Opción 2 contable
/// acordada). Cash es el ÚNICO caso donde no se cobra comisión.
/// </summary>
public static class PaymentChannel
{
    public const string IzipayCard = "izipay_card";
    public const string PayPal     = "paypal";
    public const string Yape       = "yape";
    public const string Plin       = "plin";
    public const string Bank       = "bank_transfer";
    public const string Cash       = "cash";

    public static readonly string[] All = { IzipayCard, PayPal, Yape, Plin, Bank, Cash };

    public static bool IsValid(string c) => All.Contains(c);

    /// <summary>
    /// True si este canal está intermediado por una pasarela.
    /// NOTA: ya NO se usa para decidir el flujo de payout — actualmente TODOS los
    /// canales generan un ServicePayment con PayoutStatus=Pending y la trabajadora
    /// puede solicitar cobro para cualquiera de ellos (ver ServicePayment ctor).
    /// El método sigue acá por si alguna parte del código necesita distinguir el
    /// origen real del pago (por ejemplo para mostrar el icono correcto).
    /// </summary>
    public static bool IsGatewayMediated(string c) => c == IzipayCard || c == PayPal;

    /// <summary>
    /// Alias antiguo. Mantener para compatibilidad de código que aún use
    /// IsIzipayMediated. La semántica es la misma: ¿la plataforma debe liberar
    /// fondos al worker?
    /// </summary>
    public static bool IsIzipayMediated(string c) => IsGatewayMediated(c);

    /// <summary>
    /// True si este canal genera comisión 10% para la plataforma.
    /// Cash es el único que NO genera comisión.
    /// </summary>
    public static bool ChargesPlatformFee(string c) => c != Cash;
}
