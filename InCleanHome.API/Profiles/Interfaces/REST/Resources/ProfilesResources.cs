namespace InCleanHome.API.Profiles.Interfaces.REST.Resources;

// ===== Sign-up payloads coming from the frontend =====

public record RegisterClientResource(string Name, string Email, string Password, string? Phone);

public record RegisterWorkerResource(
    string Name,
    string Email,
    string Password,
    string? Phone,
    int Age,
    string Gender,
    List<string> ServiceTypes,
    List<string> Zones,
    decimal HourlyRate,
    int ExperienceYears,
    string? Bio);

// ===== Output =====

public record AuthResponseResource(UserPayload User, string Token);

public record UserPayload(
    int Id,
    string Email,
    string Role,
    string Name,
    string? Phone,
    bool IsVerified,
    bool DocumentsVerified,
    bool DocumentsUploaded,
    bool DocumentsRejected,
    DateTimeOffset? SuspendedUntil,
    string? SuspensionReason);

public record WorkerResource(
    int Id,                 // userId so the frontend can navigate /worker/{id}
    string Name,
    string? Phone,
    int Age,
    string Gender,
    List<string> ServiceTypes,
    List<string> Zones,
    decimal HourlyRate,
    int ExperienceYears,
    string Bio,
    decimal AverageRating,
    int TotalServices,
    bool DocumentsVerified,
    string? PhotoUrl,
    DateTimeOffset? SuspendedUntil,
    bool HasConfirmedReports,
    int ConfirmedReportsCount);

public record UpdateWorkerProfileResource(
    string Name,
    string? Phone,
    int Age,
    int ExperienceYears,
    decimal HourlyRate,
    List<string> ServiceTypes,
    List<string> Zones,
    string? Bio);

public record WorkerStatsResource(
    decimal NetEarnings,
    decimal PlatformFeeDeducted,
    int CompletedServices,
    decimal AverageRating,
    List<MonthlyEarning> MonthlyEarnings);

public record MonthlyEarning(string Month, decimal Earnings);

public record ClientProfileResource(
    int Id,
    int UserId,
    string Name,
    string? Phone,
    string? PhotoUrl);

public record UpdateClientProfileResource(
    string Name,
    string? Phone);

public record UpdatePhotoResource(string? PhotoUrl);

