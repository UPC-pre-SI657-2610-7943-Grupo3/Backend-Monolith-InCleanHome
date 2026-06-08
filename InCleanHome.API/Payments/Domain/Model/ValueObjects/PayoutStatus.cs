namespace InCleanHome.API.Payments.Domain.Model.ValueObjects;

/// <summary>
/// Estado del payout (entrega del dinero al worker) de un pago Izipay.
///
/// Solo aplica a pagos por canal IzipayCard — el resto de canales tienen
/// PayoutStatus = NotApplicable porque el worker ya cobró directamente
/// del cliente sin pasar por la plataforma.
///
/// Flujo: NotApplicable | Pending → Completed
///
///   - NotApplicable: canal manual (Yape/Plin/Bank/Cash) — el worker ya tiene
///     el dinero en su bolsillo (o lo tendrá en cuanto el cliente confirme).
///   - Pending: canal IzipayCard — el dinero está en la cuenta de la plataforma
///     (sandbox) y el worker tiene que pedir el cobro.
///   - Completed: el worker pidió cobro y la plataforma "liberó" los fondos.
/// </summary>
public static class PayoutStatus
{
    public const string NotApplicable = "not_applicable";
    public const string Pending       = "pending";
    public const string Completed     = "completed";

    public static readonly string[] All = { NotApplicable, Pending, Completed };

    public static bool IsValid(string s) => All.Contains(s);
}
