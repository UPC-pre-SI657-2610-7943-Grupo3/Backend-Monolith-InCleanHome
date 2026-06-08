namespace InCleanHome.API.IAM.Domain.Model.Commands;

/// <summary>
///     Registers (or clears) the Firebase Cloud Messaging device/browser token
///     associated with the given user. A null/empty Token clears the previous value.
/// </summary>
public record RegisterDeviceTokenCommand(int UserId, string? Token);
