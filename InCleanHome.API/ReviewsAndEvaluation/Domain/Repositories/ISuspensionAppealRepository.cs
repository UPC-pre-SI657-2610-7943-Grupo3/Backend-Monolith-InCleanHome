using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Aggregates;
using InCleanHome.API.Shared.Domain.Repositories;

namespace InCleanHome.API.ReviewsAndEvaluation.Domain.Repositories;

public interface ISuspensionAppealRepository : IBaseRepository<SuspensionAppeal>
{
    /// <summary>Devuelve el reclamo activo (pending) del usuario, si existe.</summary>
    Task<SuspensionAppeal?> FindActiveByUserIdAsync(int userId);

    /// <summary>Devuelve todos los reclamos (historial) del usuario, más reciente primero.</summary>
    Task<IEnumerable<SuspensionAppeal>> FindAllByUserIdAsync(int userId);

    /// <summary>Devuelve los reclamos pendientes de revisión, más antiguos primero (FIFO).</summary>
    Task<IEnumerable<SuspensionAppeal>> FindPendingAsync();
}
