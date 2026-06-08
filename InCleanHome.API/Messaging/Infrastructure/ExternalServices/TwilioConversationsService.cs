using Twilio;
using Twilio.Jwt.AccessToken;
using Twilio.Rest.Conversations.V1.Service;
using Twilio.Rest.Conversations.V1.Service.Conversation;
 
namespace InCleanHome.API.Messaging.Infrastructure.ExternalServices;
 
public interface ITwilioConversationsService
{
    Task<string> GetOrCreateConversationSidAsync(string participantA, string participantB);
    string GenerateAccessToken(string identity);
}
 
public class TwilioConversationsService : ITwilioConversationsService
{
    private readonly string _accountSid;
    private readonly string _apiKeySid;
    private readonly string _apiKeySecret;
    private readonly string _conversationServiceSid;
 
    public TwilioConversationsService(IConfiguration configuration)
    {
        _accountSid             = configuration["Twilio:AccountSid"]!;
        _apiKeySid              = configuration["Twilio:ApiKeySid"]!;
        _apiKeySecret           = configuration["Twilio:ApiKeySecret"]!;
        _conversationServiceSid = configuration["Twilio:ConversationServiceSid"]!;
 
        TwilioClient.Init(
            configuration["Twilio:AccountSid"],
            configuration["Twilio:AuthToken"]
        );
    }
 
    public async Task<string> GetOrCreateConversationSidAsync(string participantA, string participantB)
    {
        var ids = new[] { participantA, participantB }.OrderBy(x => x).ToArray();
        var uniqueName = $"incleanhome_{ids[0]}_{ids[1]}";
 
        try
        {
            var createOptions = new CreateConversationOptions(_conversationServiceSid)
            {
                UniqueName   = uniqueName,
                FriendlyName = $"Chat {participantA} - {participantB}"
            };
            var conversation = await ConversationResource.CreateAsync(createOptions);
 
            await AddParticipantIfNotExists(conversation.Sid, participantA);
            await AddParticipantIfNotExists(conversation.Sid, participantB);
 
            return conversation.Sid;
        }
        catch (Twilio.Exceptions.ApiException)
        {
            var readOptions = new ReadConversationOptions(_conversationServiceSid);
            var conversations = await ConversationResource.ReadAsync(readOptions);
            var existing = conversations.FirstOrDefault(c => c.UniqueName == uniqueName);
            if (existing != null) return existing.Sid;
            throw;
        }
    }
 
    public string GenerateAccessToken(string identity)
    {
        var grant = new ChatGrant
        {
            ServiceSid = _conversationServiceSid
        };
 
        var grants = new HashSet<IGrant> { grant };
 
        var token = new Token(
            accountSid:    _accountSid,
            signingKeySid: _apiKeySid,
            secret:        _apiKeySecret,
            identity:      identity,
            expiration:    DateTime.UtcNow.AddHours(24),
            grants:        grants
        );
 
        return token.ToJwt();
    }
 
    private async Task AddParticipantIfNotExists(string conversationSid, string identity)
    {
        try
        {
            var options = new CreateParticipantOptions(_conversationServiceSid, conversationSid)
            {
                Identity = identity
            };
            await ParticipantResource.CreateAsync(options);
        }
        catch
        {
            // Participant already exists — ignore
        }
    }
}
 