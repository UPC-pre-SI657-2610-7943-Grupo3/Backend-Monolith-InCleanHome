using System.ComponentModel.DataAnnotations.Schema;
using EntityFrameworkCore.CreatedUpdatedDate.Contracts;

namespace InCleanHome.API.Payments.Domain.Model.Aggregates;

/// <summary>
///     Payment method aggregate root.
/// </summary>
/// <remarks>
///     Represents the off-platform method a client agreed to use to pay the worker
///     (cash, card, Yape, Plin, bank transfer). InCleanHome does not process payments —
///     it only records the agreed method.
/// </remarks>
public class PaymentMethod : IEntityWithCreatedUpdatedDate
{
    public int Id { get; private set; }
    public int UserId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public string Details { get; private set; } = string.Empty;
    public bool IsDefault { get; private set; }

    [Column("CreatedAt")] public DateTimeOffset? CreatedDate { get; set; }
    [Column("UpdatedAt")] public DateTimeOffset? UpdatedDate { get; set; }

    public PaymentMethod() { }

    public PaymentMethod(int userId, string type, string label, string? details, bool isDefault)
    {
        UserId    = userId;
        Type      = type;
        Label     = label;
        Details   = details ?? string.Empty;
        IsDefault = isDefault;
    }

    public PaymentMethod MarkAsDefault()    { IsDefault = true;  return this; }
    public PaymentMethod UnmarkAsDefault()  { IsDefault = false; return this; }
}
