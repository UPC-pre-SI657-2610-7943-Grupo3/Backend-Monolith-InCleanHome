namespace InCleanHome.API.Payments.Domain.Model.ValueObjects;

public static class PaymentMethodType
{
    public const string Cash         = "cash";
    // "Card" es el tipo legacy usado en bookings creados antes de la integración
    // con pasarelas. No se usa para registrar nuevos métodos — el cliente elige
    // entre IzipayCard o PayPalCard ahora.
    public const string Card         = "card";
    public const string Yape         = "yape";
    public const string Plin         = "plin";
    public const string BankTransfer = "bank_transfer";
    // Tarjeta procesada por Izipay (sandbox Krypton). Distinto de "card" legacy.
    // Cuando el cliente eligió este método al reservar, "Pagar Servicio" abre
    // el modal Izipay sandbox después de que la trabajadora complete el servicio.
    public const string IzipayCard   = "izipay_card";
    // Tarjeta / cuenta procesada por PayPal (sandbox redirect flow). Cuando el
    // cliente eligió este método al reservar, "Pagar Servicio" inicia el
    // redirect a sandbox.paypal.com después de que se complete el servicio.
    public const string PayPalCard   = "paypal_card";

    public static readonly string[] All = { Cash, Card, Yape, Plin, BankTransfer, IzipayCard, PayPalCard };

    public static bool IsValid(string t) => All.Contains(t);
}
