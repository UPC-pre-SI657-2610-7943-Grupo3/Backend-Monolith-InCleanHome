using InCleanHome.API.IAM.Domain.Model.Aggregates;
using InCleanHome.API.IAM.Domain.Model.Commands;
using InCleanHome.API.IAM.Domain.Model.ValueObjects;
using InCleanHome.API.IAM.Domain.Repositories;
using InCleanHome.API.IAM.Domain.Services;
using InCleanHome.API.Shared.Domain.Repositories;

namespace InCleanHome.API.IAM.Application.Internal.CommandServices;

/// <summary>
/// Manejador de comandos del bounded context IAM.
///
/// Solo cubre operaciones administrativas (aprobar/rechazar documentos, suspender,
/// borrar usuarios) y de mantenimiento (subir documentos del worker, actualizar
/// email, registrar device token para FCM). El login y el alta corren ahora por
/// Auth0 (ver <c>Auth0LoginController</c>).
/// </summary>
public class UserCommandService(
    IUserRepository userRepository,
    IWorkerDocumentRepository workerDocumentRepository,
    IUnitOfWork unitOfWork) : IUserCommandService
{
    public async Task Handle(VerifyUserCommand command)
    {
        var user = await userRepository.FindByIdAsync(command.UserId)
            ?? throw new Exception($"User {command.UserId} not found");
        user.Verify();
        userRepository.Update(user);
        await unitOfWork.CompleteAsync();
    }

    public async Task Handle(ApproveWorkerDocumentsCommand command)
    {
        var user = await userRepository.FindByIdAsync(command.UserId)
            ?? throw new Exception($"User {command.UserId} not found");

        if (user.Role != UserRole.Worker)
            throw new Exception("Only worker accounts require document approval");

        user.MarkDocumentsAsVerified();
        userRepository.Update(user);
        await unitOfWork.CompleteAsync();
    }

    public async Task Handle(RejectWorkerDocumentsCommand command)
    {
        var user = await userRepository.FindByIdAsync(command.UserId)
            ?? throw new Exception($"User {command.UserId} not found");

        if (user.Role != UserRole.Worker)
            throw new Exception("Only worker accounts can have their documents rejected");

        user.MarkDocumentsAsRejected();
        userRepository.Update(user);
        await unitOfWork.CompleteAsync();
    }

    public async Task Handle(SuspendUserCommand command)
    {
        var user = await userRepository.FindByIdAsync(command.UserId)
            ?? throw new Exception($"User {command.UserId} not found");
        user.Suspend(command.Duration, command.Reason);
        userRepository.Update(user);
        await unitOfWork.CompleteAsync();
    }

    public async Task Handle(ClearUserSuspensionCommand command)
    {
        var user = await userRepository.FindByIdAsync(command.UserId)
            ?? throw new Exception($"User {command.UserId} not found");
        user.ClearSuspension();
        userRepository.Update(user);
        await unitOfWork.CompleteAsync();
    }

    public async Task Handle(UploadWorkerDocumentCommand command)
    {
        if (!DocumentType.IsValid(command.DocumentType))
            throw new Exception("Invalid document type");

        var user = await userRepository.FindByIdAsync(command.UserId)
            ?? throw new Exception("User not found");

        if (user.Role != UserRole.Worker)
            throw new Exception("Only workers can upload documents");

        // Si la trabajadora ya subió un documento de este mismo tipo antes (sea
        // porque está corrigiendo un PDF subido por error, o porque el admin se
        // lo rechazó y está re-enviándolo), lo borramos para que solo quede el
        // nuevo. Así nunca hay más de 2 documentos por trabajadora (uno de
        // Antecedentes y uno de Experiencia).
        var existing = (await workerDocumentRepository.FindByUserIdAsync(command.UserId)).ToList();
        foreach (var old in existing.Where(d => d.DocumentType == command.DocumentType))
            workerDocumentRepository.Remove(old);

        var doc = new WorkerDocument(command.UserId, command.DocumentType, command.FileName, command.FileBase64);
        await workerDocumentRepository.AddAsync(doc);

        // Calculamos los tipos finales tras el reemplazo: los que sobrevivieron
        // del set anterior (los de OTRO tipo) más el nuevo que acabamos de subir.
        var finalTypes = existing
            .Where(d => d.DocumentType != command.DocumentType)
            .Select(d => d.DocumentType)
            .Append(command.DocumentType)
            .ToHashSet();

        if (finalTypes.Contains(DocumentType.BackgroundCheck) && finalTypes.Contains(DocumentType.Experience))
        {
            // Ambos documentos presentes → cuenta lista para revisión del admin.
            // MarkDocumentsAsUploaded() además limpia DocumentsRejected si estaba en true.
            user.MarkDocumentsAsUploaded();
            userRepository.Update(user);
        }

        await unitOfWork.CompleteAsync();
    }

    public async Task<User> Handle(UpdateUserEmailCommand command)
    {
        var user = await userRepository.FindByIdAsync(command.UserId)
            ?? throw new Exception("User not found");

        if (userRepository.ExistsByEmail(command.Email) && user.Email != command.Email)
            throw new Exception($"Email {command.Email} is already taken");

        user.UpdateEmail(command.Email);
        userRepository.Update(user);
        await unitOfWork.CompleteAsync();
        return user;
    }

    public async Task Handle(DeleteUserCommand command)
    {
        var user = await userRepository.FindByIdAsync(command.UserId)
            ?? throw new Exception("User not found");

        if (user.Role == UserRole.Admin)
            throw new InvalidOperationException("No se puede eliminar una cuenta de administrador.");

        userRepository.Remove(user);
        await unitOfWork.CompleteAsync();
    }

    public async Task Handle(RegisterDeviceTokenCommand command)
    {
        var user = await userRepository.FindByIdAsync(command.UserId)
            ?? throw new Exception("User not found");

        user.UpdateDeviceToken(command.Token);
        userRepository.Update(user);
        await unitOfWork.CompleteAsync();
    }
}
