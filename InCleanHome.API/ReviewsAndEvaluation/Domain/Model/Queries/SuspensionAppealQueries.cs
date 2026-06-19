namespace InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Queries;

public record GetSuspensionAppealByIdQuery(int Id);

/// <summary>Trae el reclamo activo (pending) del usuario, si existe.</summary>
public record GetActiveSuspensionAppealByUserIdQuery(int UserId);

/// <summary>Historial completo de reclamos del usuario.</summary>
public record GetSuspensionAppealsByUserIdQuery(int UserId);

/// <summary>Listado de reclamos pendientes para que admin revise.</summary>
public record GetPendingSuspensionAppealsQuery();
