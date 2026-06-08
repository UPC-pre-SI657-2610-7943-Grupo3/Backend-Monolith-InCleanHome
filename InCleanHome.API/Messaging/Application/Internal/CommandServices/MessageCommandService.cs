using InCleanHome.API.IAM.Interfaces.ACL;
using InCleanHome.API.Messaging.Domain.Model.Aggregates;
using InCleanHome.API.Messaging.Domain.Model.Commands;
using InCleanHome.API.Messaging.Domain.Repositories;
using InCleanHome.API.Messaging.Domain.Services;
using InCleanHome.API.Notifications.Interfaces.ACL;
using InCleanHome.API.Profiles.Interfaces.ACL;
using InCleanHome.API.Shared.Domain.Repositories;

namespace InCleanHome.API.Messaging.Application.Internal.CommandServices;

public class MessageCommandService(
    IMessageRepository repository,
    IUnitOfWork unitOfWork,
    INotificationsContextFacade notificationsFacade,
    IIamContextFacade iamFacade,
    IProfilesContextFacade profilesFacade) : IMessageCommandService
{
    public async Task<Message> Handle(SendMessageCommand c)
    {
        if (c.SenderId == c.RecipientId)
            throw new Exception("Cannot send a message to yourself.");
        if (string.IsNullOrWhiteSpace(c.Content))
            throw new Exception("Message content cannot be empty.");

        var message = new Message(c.SenderId, c.RecipientId, c.Content);
        await repository.AddAsync(message);
        await unitOfWork.CompleteAsync();

        // 🚀 Notificación in-app + push FCM para el destinatario del mensaje.
        // Envuelto en try/catch para que un fallo aquí (ej. usuario sin perfil
        // visible, Firebase caído) NO rompa el envío del mensaje en sí, que ya
        // se guardó arriba con CompleteAsync. La lógica de Twilio queda intacta.
        try
        {
            // El link depende del rol del destinatario: /client/messages/{senderId}
            // o /worker/messages/{senderId}. Lo resolvemos por su rol en IAM.
            var recipientRole = await iamFacade.FetchRoleByUserId(c.RecipientId);
            var rolePath = recipientRole == "worker" ? "worker" : "client";
            var link = $"/{rolePath}/messages/{c.SenderId}";

            // Nombre del remitente para el título de la notificación.
            var senderName = await profilesFacade.FetchUserNameByUserId(c.SenderId);
            if (string.IsNullOrWhiteSpace(senderName)) senderName = "Alguien";

            // Recortamos el body por si el mensaje es muy largo (Notification body
            // se ve mejor breve; el contenido completo está en el chat).
            var preview = c.Content.Length > 120 ? c.Content[..117] + "..." : c.Content;

            await notificationsFacade.CreateNotification(
                userId: c.RecipientId,
                type:   "message",
                title:  $"{senderName} te envió un mensaje",
                body:   preview,
                link:   link);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Messaging] Could not create notification for message: {ex.Message}");
        }

        return message;
    }

    public async Task Handle(MarkConversationAsReadCommand c)
    {
        await repository.MarkAsReadAsync(c.UserId, c.OtherUserId);
        await unitOfWork.CompleteAsync();
    }
}
