namespace InCleanHome.API.Profiles.Domain.Model.Commands;

public record CreateClientProfileCommand(int UserId, string Name, string Phone);

public record CreateWorkerProfileCommand(
    int UserId,
    string Name,
    string Phone,
    int Age,
    string Gender,
    List<string> ServiceTypes,
    List<string> Zones,
    decimal HourlyRate,
    decimal HourlyRateSunday,
    int ExperienceYears,
    string Bio);

public record UpdateClientProfileCommand(int UserId, string Name, string Phone);

public record UpdateWorkerProfileCommand(
    int UserId,
    string Name,
    string Phone,
    int Age,
    List<string> ServiceTypes,
    List<string> Zones,
    decimal HourlyRate,
    decimal HourlyRateSunday,
    int ExperienceYears,
    string Bio);

public record RegisterWorkerCompletedServiceCommand(int UserId, int Rating);

public record UpdateWorkerPhotoCommand(int UserId, string? PhotoUrl);
public record UpdateClientPhotoCommand(int UserId, string? PhotoUrl);
