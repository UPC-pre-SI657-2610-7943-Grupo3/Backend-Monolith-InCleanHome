namespace InCleanHome.API.Profiles.Domain.Model.Queries;

public record GetClientProfileByUserIdQuery(int UserId);
public record GetWorkerProfileByUserIdQuery(int UserId);
public record GetWorkerProfileByIdQuery(int Id);
public record GetAllWorkerProfilesQuery;

public record SearchWorkersQuery(
    string? ServiceType,
    string? Zone,
    string? Gender,
    int? MinAge,
    int? MaxAge,
    decimal? MaxHourlyRate,
    decimal? MinRating);
