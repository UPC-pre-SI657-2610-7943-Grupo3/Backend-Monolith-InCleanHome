using InCleanHome.API.IAM.Domain.Model.Aggregates;
using InCleanHome.API.IAM.Domain.Model.Commands;

namespace InCleanHome.API.IAM.Domain.Services;

/// <summary>
/// Comandos del bounded context IAM.
///
/// NOTA: los comandos clásicos de autenticación (SignIn, SignUp, ForgotPassword,
/// ResetPassword) ya no aparecen acá. Auth0 los reemplaza por completo: el login
/// y el alta corren a través del flujo Universal Login → /api/auth/auth0/* y la
/// recuperación de contraseña la maneja Auth0 directamente desde su Universal Login.
/// </summary>
public interface IUserCommandService
{
    Task Handle(VerifyUserCommand command);
    Task Handle(ApproveWorkerDocumentsCommand command);
    Task Handle(RejectWorkerDocumentsCommand command);
    Task Handle(SuspendUserCommand command);
    Task Handle(ClearUserSuspensionCommand command);
    Task Handle(UploadWorkerDocumentCommand command);
    Task<User> Handle(UpdateUserEmailCommand command);
    Task Handle(DeleteUserCommand command);
    Task Handle(RegisterDeviceTokenCommand command);
}
