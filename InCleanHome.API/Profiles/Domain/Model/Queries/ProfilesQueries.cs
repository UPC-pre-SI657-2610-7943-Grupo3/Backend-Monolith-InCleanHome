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
    decimal? MinRating,
    // ServiceTypes: filtro multi-servicio con AND. Si trae elementos, el worker
    // resultante DEBE ofrecer TODOS los servicios listados (no basta con uno).
    // Se mantiene `ServiceType` (singular) para compatibilidad con clientes viejos.
    List<string>? ServiceTypes = null);
