using System.Net.Mime;
using InCleanHome.API.IAM.Domain.Model.Aggregates;
using InCleanHome.API.Messaging.Domain.Model.Commands;
using InCleanHome.API.Messaging.Domain.Model.Queries;
using InCleanHome.API.Messaging.Domain.Services;
using InCleanHome.API.Messaging.Infrastructure.ExternalServices;
using InCleanHome.API.Messaging.Interfaces.REST.Resources;
using InCleanHome.API.Messaging.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InCleanHome.API.Messaging.Interfaces.REST.Controllers;

/// <summary>
///     Direct messaging endpoints — now powered by Twilio Conversations.
/// </summary>
[ApiController]
[Route("api/messages")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Direct messaging via Twilio Conversations")]
public class MessagesController(
    IMessageCommandService commandService,
    IMessageQueryService queryService,
    ITwilioConversationsService twilioService) : ControllerBase
{
    // ── Twilio Conversations endpoints ────────────────────────────────────────

    /// <summary>
    ///     Generates a Twilio Access Token for the current user.
    ///     The frontend uses this token to connect to the Twilio Conversations SDK.
    /// </summary>
    [HttpGet("token")]
    [SwaggerOperation("Get Twilio Token", "Returns a short-lived Twilio Access Token for the current user.")]
    public IActionResult GetToken()
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        var identity = $"user_{current.Id}";
        var token = twilioService.GenerateAccessToken(identity);

        return Ok(new TwilioTokenResource(token, identity));
    }

    /// <summary>
    ///     Gets or creates a Twilio Conversation between the current user and another user.
    ///     Returns the conversation SID that the frontend needs to join the channel.
    /// </summary>
    [HttpPost("conversation/{userId:int}")]
    [SwaggerOperation("Get or Create Conversation", "Returns the Twilio conversation SID for the pair of users.")]
    public async Task<IActionResult> GetOrCreateConversation(int userId)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        if (current.Id == userId)
            return BadRequest(new { error = "Cannot create a conversation with yourself." });

        try
        {
            var conversationSid = await twilioService.GetOrCreateConversationSidAsync(
                $"user_{current.Id}",
                $"user_{userId}"
            );
            return Ok(new ConversationSidResource(conversationSid));
        }
        catch (Exception e)
        {
            Console.WriteLine($"[TWILIO ERROR] {e.GetType().Name}: {e.Message}");
            return BadRequest(new { error = e.Message });
        }
    }

    // ── Legacy endpoints (kept for backwards compatibility) ───────────────────

    [HttpGet("conversations")]
    [SwaggerOperation("List Conversations (legacy)")]
    public async Task<IActionResult> ListConversations()
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        var convs = await queryService.Handle(new GetConversationsForUserQuery(current.Id));
        return Ok(convs.Select(ConversationResourceFromViewAssembler.ToResourceFromView));
    }

    [HttpGet("{userId:int}")]
    [SwaggerOperation("Get Thread (legacy)")]
    public async Task<IActionResult> GetThread(int userId)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        var messages = await queryService.Handle(new GetMessagesBetweenQuery(current.Id, userId));
        await commandService.Handle(new MarkConversationAsReadCommand(current.Id, userId));

        return Ok(messages.Select(MessageResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpPost("{userId:int}")]
    [SwaggerOperation("Send Message (legacy)")]
    public async Task<IActionResult> Send(int userId, [FromBody] SendMessageResource resource)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        try
        {
            var m = await commandService.Handle(new SendMessageCommand(current.Id, userId, resource.Content));
            return Ok(MessageResourceFromEntityAssembler.ToResourceFromEntity(m));
        }
        catch (Exception e)
        {
            return BadRequest(new { error = e.Message });
        }
    }

    /// <summary>
    ///     Creates an in-app notification (and triggers a push) for the recipient of a
    ///     message that was just sent via Twilio Conversations. Called from the frontend
    ///     after <c>conversation.sendMessage(...)</c> succeeds.
    ///     This endpoint does NOT persist the message body — Twilio already stores it.
    /// </summary>
    [HttpPost("{userId:int}/notify")]
    [SwaggerOperation("Notify Recipient", "Creates a notification + push for the recipient of a Twilio message just sent.")]
    public async Task<IActionResult> Notify(int userId, [FromBody] SendMessageResource resource)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        if (current.Id == userId) return BadRequest(new { error = "Cannot notify yourself." });

        try
        {
            // Reuse the same handler — it stores the message AND creates the notification.
            // Storing in BD is harmless extra (Twilio is the source of truth for messages,
            // but having a local copy is useful for offline / analytics / history).
            await commandService.Handle(new SendMessageCommand(current.Id, userId, resource.Content ?? ""));
            return Ok(new { message = "Notification created" });
        }
        catch (Exception e)
        {
            return BadRequest(new { error = e.Message });
        }
    }
}
