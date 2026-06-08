using System.Security.Claims;
using System.Text;
using InCleanHome.API.IAM.Application.Internal.OutboundServices;
using InCleanHome.API.IAM.Domain.Model.Aggregates;
using InCleanHome.API.IAM.Infrastructure.Tokens.JWT.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace InCleanHome.API.IAM.Infrastructure.Tokens.JWT.Services;

public class TokenService(IOptions<TokenSettings> tokenSettings) : ITokenService
{
    private readonly TokenSettings _tokenSettings = tokenSettings.Value;

    public string GenerateToken(User user)
    {
        var key = Encoding.ASCII.GetBytes(_tokenSettings.Secret);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Sid, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var handler = new JsonWebTokenHandler();
        return handler.CreateToken(tokenDescriptor);
    }

    public async Task<int?> ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token)) return null;

        var handler = new JsonWebTokenHandler();
        var key = Encoding.ASCII.GetBytes(_tokenSettings.Secret);
        try
        {
            var result = await handler.ValidateTokenAsync(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            });

            var jwtToken = (JsonWebToken)result.SecurityToken;
            return int.Parse(jwtToken.Claims.First(c => c.Type == ClaimTypes.Sid).Value);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }
}
