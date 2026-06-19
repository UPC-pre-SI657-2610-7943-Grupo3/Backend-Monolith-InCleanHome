using InCleanHome.API.IAM.Domain.Repositories;
using InCleanHome.API.Notifications.Interfaces.ACL;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Aggregates;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Commands;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Repositories;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Services;
using InCleanHome.API.Shared.Domain.Repositories;

namespace InCleanHome.API.ReviewsAndEvaluation.Application.Internal.CommandServices;

/// <summary>
///     Servicio de comandos para apelaciones de suspensión.
/// </summary>
/// <remarks>
///     <para>
///     Cuando un admin acepta el reclamo (<see cref="Handle(AcceptSuspensionAppealCommand)"/>),
///     este servicio cruza el límite del bounded context y modifica directamente el
///     User aggregate en IAM para limpiar la suspensión. Esto es deliberado: el
///     reclamo y la suspensión están conceptualmente acoplados; al aceptar
///     queremos garantizar atomicidad (o se levanta o no se procesa).
///     </para>
///     <para>
///     En una migración futura a microservicios este acoplamiento se resolvería
///     vía un evento de dominio <c>SuspensionAppealAccepted</c> que el
///     <c>identity-service</c> consume para limpiar la suspensión.
///     </para>
/// </remarks>
public class SuspensionAppealCommandService(
    ISuspensionAppealRepository repository,
    IUserRepository userRepository,
    INotificationsContextFacade notificationsFacade,
    IUnitOfWork unitOfWork) : ISuspensionAppealCommandService
{
    public async Task<SuspensionAppeal> Handle(SubmitSuspensionAppealCommand command)
    {
        // Solo permitimos apelar si el usuario está realmente suspendido. Esto
        // evita reclamos "preventivos" y mantiene el dataset limpio.
        var user = await userRepository.FindByIdAsync(command.UserId)
            ?? throw new InvalidOperationException("Usuario no encontrado.");
        if (!user.IsCurrentlySuspended())
            throw new InvalidOperationException(
                "Solo puedes reclamar si estás actualmente suspendido.");

        // No se permite un segundo reclamo pendiente sobre la misma suspensión.
        // Si ya tiene uno activo, lo rechazamos. (Si el reclamo anterior fue
        // rechazado, sí puede enviar otro nuevo — eso lo permitimos.)
        var existing = await repository.FindActiveByUserIdAsync(command.UserId);
        if (existing is not null)
            throw new InvalidOperationException(
                "Ya tienes un reclamo en revisión. Espera la respuesta del equipo de InCleanHome.");

        var appeal = new SuspensionAppeal(command.UserId, command.Reason);
        await repository.AddAsync(appeal);
        await unitOfWork.CompleteAsync();
        return appeal;
    }

    public async Task<SuspensionAppeal?> Handle(AcceptSuspensionAppealCommand command)
    {
        var appeal = await repository.FindByIdAsync(command.AppealId);
        if (appeal is null) return null;
        appeal.Accept(command.AdminUserId, command.Response);
        repository.Update(appeal);

        // ── Cruce de bounded context (IAM) ────────────────────────────────
        // Aceptar el reclamo implica levantar la suspensión. Si el usuario
        // ya no estaba suspendido (caducó por tiempo), igual lo dejamos
        // explícitamente en estado limpio.
        var user = await userRepository.FindByIdAsync(appeal.UserId);
        if (user is not null)
        {
            user.ClearSuspension();
            userRepository.Update(user);
        }

        await unitOfWork.CompleteAsync();

        // Notificación best-effort al usuario (no aborta la transacción).
        try
        {
            await notificationsFacade.CreateNotification(
                userId: appeal.UserId,
                type:   "suspension_appeal_accepted",
                title:  "Tu reclamo fue aceptado",
                body:   "Tu suspensión ha sido levantada. Ya puedes usar la plataforma normalmente.",
                link:   "/");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SuspensionAppeal] Notificación de aceptación no enviada: {ex.Message}");
        }

        return appeal;
    }

    public async Task<SuspensionAppeal?> Handle(RejectSuspensionAppealCommand command)
    {
        var appeal = await repository.FindByIdAsync(command.AppealId);
        if (appeal is null) return null;
        appeal.Reject(command.AdminUserId, command.Response);
        repository.Update(appeal);
        await unitOfWork.CompleteAsync();

        // Notificación best-effort al usuario.
        try
        {
            var bodyExtra = string.IsNullOrWhiteSpace(appeal.AdminResponse)
                ? "Tu reclamo fue revisado pero no se aceptó. La suspensión continúa."
                : $"Tu reclamo fue revisado y no se aceptó. Motivo: {appeal.AdminResponse}";
            await notificationsFacade.CreateNotification(
                userId: appeal.UserId,
                type:   "suspension_appeal_rejected",
                title:  "Tu reclamo no fue aceptado",
                body:   bodyExtra,
                link:   "/");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SuspensionAppeal] Notificación de rechazo no enviada: {ex.Message}");
        }

        return appeal;
    }
}
