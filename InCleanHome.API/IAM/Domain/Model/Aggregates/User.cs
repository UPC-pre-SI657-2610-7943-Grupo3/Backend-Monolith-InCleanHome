using System.Text.Json.Serialization;
using InCleanHome.API.IAM.Domain.Model.ValueObjects;

namespace InCleanHome.API.IAM.Domain.Model.Aggregates;

/// <summary>
///     User aggregate root for the IAM bounded context.
/// </summary>
public class User
{
    public int Id { get; private set; }
    public string Email { get; private set; } = string.Empty;

    [JsonIgnore] public string PasswordHash { get; private set; } = string.Empty;

    public string Role { get; private set; } = UserRole.Client;
    public bool IsVerified { get; private set; }
    public bool DocumentsVerified { get; private set; }
    /// <summary>
    ///     True once the worker has uploaded both required documents.
    ///     The account remains unverified until an admin explicitly calls ApproveDocuments.
    /// </summary>
    public bool DocumentsUploaded { get; private set; }

    /// <summary>
    ///     True when the admin explicitly rejected the worker's documents and the worker
    ///     has not yet re-uploaded. Used so the worker dashboard can show a banner inviting
    ///     them to re-submit. Cleared automatically the next time the worker uploads both
    ///     required documents again.
    /// </summary>
    public bool DocumentsRejected { get; private set; }

    // Password recovery: a one-time token with an expiry. Stored hashed-in-spirit
    // (opaque GUID) — replaced/cleared once used.
    [JsonIgnore] public string? ResetToken { get; private set; }
    [JsonIgnore] public DateTimeOffset? ResetTokenExpiresAt { get; private set; }

    // Temporary account suspension (applied for late cancellations, etc.).
    // While SuspendedUntil > UtcNow, the user is considered suspended and certain
    // actions (booking, being booked) are blocked. The suspension expires by
    // simply checking the timestamp; no scheduled job is required.
    public DateTimeOffset? SuspendedUntil { get; private set; }
    public string? SuspensionReason { get; private set; }

    // Firebase Cloud Messaging device/browser token. Stored so backend can push
    // notifications to the user's currently registered device. Nullable: a user
    // may not have granted permission yet or may be using a device without push.
    [JsonIgnore] public string? DeviceToken { get; private set; }

    public User() { }

    public User(string email, string passwordHash, string role)
    {
        Email          = email;
        PasswordHash   = passwordHash;
        Role           = UserRole.IsValid(role) ? role : UserRole.Client;
        // Clients are auto-verified on sign-up. Workers must upload documents and be approved.
        IsVerified         = role == UserRole.Client;
        DocumentsVerified  = role == UserRole.Client;
    }

    public User UpdatePasswordHash(string passwordHash) { PasswordHash = passwordHash; return this; }
    public User UpdateEmail(string email) { Email = email; return this; }
    public User Verify()                                { IsVerified = true; return this; }
    /// <summary>Marks that the worker has submitted both documents. Account stays unverified until admin approves. Clears any previous rejection.</summary>
    public User MarkDocumentsAsUploaded()               { DocumentsUploaded = true; DocumentsRejected = false; return this; }
    /// <summary>Called by admin to fully approve a worker's account.</summary>
    public User MarkDocumentsAsVerified()               { DocumentsVerified = true; DocumentsUploaded = true; DocumentsRejected = false; IsVerified = true; return this; }

    /// <summary>
    ///     Called by admin to reject a worker's submitted documents. The cuenta
    ///     stays in DB so the worker can re-upload, but is treated as unverified:
    ///     does not appear in search and cannot receive bookings until re-approval.
    ///     The <see cref="DocumentsRejected"/> flag stays true until the worker
    ///     re-uploads both documents, so the frontend can surface a "rejected"
    ///     banner instead of the generic "pending" state.
    /// </summary>
    public User MarkDocumentsAsRejected()
    {
        DocumentsVerified = false;
        DocumentsUploaded = false;
        DocumentsRejected = true;
        IsVerified = false;
        return this;
    }

    // Issues a password-reset token valid for the given duration.
    public User SetResetToken(string token, DateTimeOffset expiresAt)
    {
        ResetToken = token;
        ResetTokenExpiresAt = expiresAt;
        return this;
    }

    // Returns true if the supplied token matches and has not expired.
    public bool IsResetTokenValid(string token)
        => !string.IsNullOrEmpty(ResetToken)
           && ResetToken == token
           && ResetTokenExpiresAt.HasValue
           && ResetTokenExpiresAt.Value > DateTimeOffset.UtcNow;

    // Clears the reset token after a successful password change.
    public User ClearResetToken()
    {
        ResetToken = null;
        ResetTokenExpiresAt = null;
        return this;
    }

    /// <summary>
    ///     Applies a temporary suspension. While <see cref="SuspendedUntil"/> is
    ///     in the future the account is considered suspended.
    /// </summary>
    public User Suspend(TimeSpan duration, string reason)
    {
        var now = DateTimeOffset.UtcNow;
        var base_ = SuspendedUntil.HasValue && SuspendedUntil.Value > now ? SuspendedUntil.Value : now;
        SuspendedUntil   = base_.Add(duration);
        SuspensionReason = reason;
        return this;
    }

    public User ClearSuspension()
    {
        SuspendedUntil   = null;
        SuspensionReason = null;
        return this;
    }

    /// <summary>
    ///     Registers (or clears) the Firebase Cloud Messaging token associated with the
    ///     user's current device/browser. Pass <c>null</c> or an empty string to clear it.
    /// </summary>
    public User UpdateDeviceToken(string? token)
    {
        DeviceToken = string.IsNullOrWhiteSpace(token) ? null : token;
        return this;
    }

    /// <summary>True if the user is currently within a suspension window.</summary>
    public bool IsCurrentlySuspended()
        => SuspendedUntil.HasValue && SuspendedUntil.Value > DateTimeOffset.UtcNow;
}
