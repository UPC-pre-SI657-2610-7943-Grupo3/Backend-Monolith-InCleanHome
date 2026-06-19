using InCleanHome.API.IAM.Domain.Model.Aggregates;
using InCleanHome.API.Profiles.Domain.Model.Aggregates;
using InCleanHome.API.Profiles.Interfaces.REST.Resources;

namespace InCleanHome.API.Profiles.Interfaces.REST.Transform;

public static class WorkerResourceFromEntityAssembler
{
    /// <summary>
    ///     The frontend identifies a worker by user id (used for /worker/{id} links and
    ///     messaging routes), not by WorkerProfile.Id, so we expose <c>UserId</c> as <c>id</c>.
    /// </summary>
    public static WorkerResource ToResourceFromEntity(
        WorkerProfile w,
        bool documentsVerified,
        DateTimeOffset? suspendedUntil = null,
        int confirmedReportsCount = 0,
        bool worksSundays = false)
        => new(
            w.UserId,
            w.Name,
            w.Phone,
            w.Age,
            w.Gender,
            w.ServiceTypes,
            w.Zones,
            w.HourlyRate,
            w.HourlyRateSunday,
            w.ExperienceYears,
            w.Bio,
            w.AverageRating,
            w.TotalServices,
            documentsVerified,
            w.PhotoUrl,
            suspendedUntil,
            confirmedReportsCount > 0,
            confirmedReportsCount,
            worksSundays);
}

public static class UserPayloadFromEntityAssembler
{
    public static UserPayload FromUserAndProfile(User user, string name, string? phone)
        => new(user.Id, user.Email, user.Role, name, phone, user.IsVerified, user.DocumentsVerified,
               user.DocumentsUploaded, user.DocumentsRejected, user.SuspendedUntil, user.SuspensionReason);
}

public static class ClientResourceFromEntityAssembler
{
    public static ClientProfileResource ToResourceFromEntity(ClientProfile c)
        => new(
            c.Id,
            c.UserId,
            c.Name,
            c.Phone,
            c.PhotoUrl);
}
