namespace InCleanHome.API.Payments.Domain.Model.ValueObjects;

/// <summary>
///     Estado del payout (entrega del dinero a la trabajadora) de un pago.
/// </summary>
/// <remarks>
///     <para>
///     Hoy todos los canales arrancan en <c>Pending</c> y pasan a <c>Completed</c>
///     cuando la trabajadora presiona "Solicitar cobro" desde su panel de pagos.
///     <c>NotApplicable</c> queda como valor disponible por compatibilidad con
///     pagos legacy creados antes de unificar el flujo de payout.
///     </para>
///     <para>
///     Para canales manuales (Yape/Plin/Bank) la transición Pending → Completed
///     es una confirmación de que la trabajadora recibió el dinero por fuera.
///     Para Mercado Pago, simula la liberación de fondos retenidos en la
///     cuenta de la plataforma.
///     </para>
/// </remarks>
public static class PayoutStatus
{
    public const string NotApplicable = "not_applicable";
    public const string Pending       = "pending";
    public const string Completed     = "completed";

    public static readonly string[] All = { NotApplicable, Pending, Completed };

    public static bool IsValid(string s) => All.Contains(s);
}
