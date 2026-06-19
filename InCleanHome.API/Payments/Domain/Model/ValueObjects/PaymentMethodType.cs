namespace InCleanHome.API.Payments.Domain.Model.ValueObjects;

/// <summary>
///     Tipos de método de pago que el cliente puede registrar en su perfil.
/// </summary>
/// <remarks>
///     <para>
///     <c>MercadoPago</c> es el único canal con pasarela: el cliente paga con
///     tarjeta, Yape (vía MP), PagoEfectivo (vía MP), etc. desde el checkout de
///     Mercado Pago. <c>Yape</c>, <c>Plin</c> y <c>BankTransfer</c> son flujos
///     manuales (el cliente paga "por fuera" y confirma en la app).
///     </para>
///     <para>
///     <c>Cash</c> fue removido: no permite cobrar comisión ni emitir
///     comprobante trazable.
///     </para>
/// </remarks>
public static class PaymentMethodType
{
    public const string MercadoPago  = "mercadopago";
    public const string Yape         = "yape";
    public const string Plin         = "plin";
    public const string BankTransfer = "bank_transfer";

    public static readonly string[] All = { MercadoPago, Yape, Plin, BankTransfer };

    public static bool IsValid(string t) => All.Contains(t);
}
