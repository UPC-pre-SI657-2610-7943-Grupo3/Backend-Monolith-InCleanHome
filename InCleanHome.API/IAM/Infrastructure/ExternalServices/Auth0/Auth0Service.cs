using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InCleanHome.API.IAM.Infrastructure.ExternalServices.Auth0;

public interface IAuth0Service
{
    bool IsEnabled { get; }
    Task<Auth0UserInfo?> ValidateAndGetUserInfoAsync(string accessToken);
}

/// <summary>
/// Datos del usuario obtenidos de /userinfo. "Sub" es el identificador único de Auth0
/// (tipo "auth0|abc123" o "google-oauth2|xxx"). "Email" puede venir vacío si el
/// scope "email" no fue solicitado.
/// </summary>
public record Auth0UserInfo(string Sub, string Email, string Name, string? Picture);

/// <summary>
/// Cliente de Auth0.
///
/// Pasos del flujo:
///   1. El frontend hace login en Auth0 Universal Login y obtiene un access_token (JWT firmado por Auth0 con RS256).
///   2. El frontend manda ese token a POST /api/auth/auth0/login.
///   3. Acá validamos la firma del JWT contra las JWKS públicas del tenant
///      (https://{domain}/.well-known/jwks.json) — esto es la única forma segura.
///   4. Si pasa la validación, llamamos a /userinfo para obtener email/name/picture.
///   5. El controlador crea o recupera el usuario interno y emite NUESTRO JWT.
/// </summary>
public class Auth0Service : IAuth0Service
{
    private readonly Auth0Settings _settings;
    private readonly HttpClient _http;
    private static readonly JwtSecurityTokenHandler _jwtHandler = new();

    // Cache simple del JWKS (no queremos golpear Auth0 en cada login).
    private static JsonWebKeySet? _jwksCache;
    private static DateTime _jwksCacheExpiresAt = DateTime.MinValue;
    private static readonly SemaphoreSlim _jwksLock = new(1, 1);

    public Auth0Service(IOptions<Auth0Settings> options, IHttpClientFactory httpFactory)
    {
        _settings = options.Value;
        _http = httpFactory.CreateClient("auth0");
    }

    public bool IsEnabled => _settings.Enabled && !string.IsNullOrWhiteSpace(_settings.Domain);

    public async Task<Auth0UserInfo?> ValidateAndGetUserInfoAsync(string accessToken)
    {
        if (!IsEnabled)
        {
            Console.WriteLine("[Auth0] Disabled — refusing to validate token.");
            return null;
        }
        if (string.IsNullOrWhiteSpace(accessToken)) return null;

        // 1) Validar firma contra JWKS.
        try
        {
            var jwks = await GetJwksAsync();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = $"https://{_settings.Domain}/",
                ValidateAudience = !string.IsNullOrWhiteSpace(_settings.Audience),
                ValidAudience = _settings.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = jwks.Keys,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2)
            };
            _jwtHandler.ValidateToken(accessToken, validationParameters, out _);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Auth0] Token validation failed: {e.Message}");
            return null;
        }

        // 2) Pedir /userinfo (devuelve email/name/picture si los scopes lo permiten).
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"https://{_settings.Domain}/userinfo");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Auth0] /userinfo returned {(int)resp.StatusCode}");
                return null;
            }
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            var sub   = json.GetProperty("sub").GetString() ?? string.Empty;
            var email = json.TryGetProperty("email", out var e1)   ? e1.GetString() ?? string.Empty : string.Empty;
            var name  = json.TryGetProperty("name", out var e2)    ? e2.GetString() ?? email        : email;
            var pic   = json.TryGetProperty("picture", out var e3) ? e3.GetString() : null;

            if (string.IsNullOrWhiteSpace(email))
            {
                Console.WriteLine("[Auth0] /userinfo did not return an email — make sure the SPA requests scope=\"openid profile email\".");
                return null;
            }
            return new Auth0UserInfo(sub, email, name, pic);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Auth0] /userinfo failed: {e.Message}");
            return null;
        }
    }

    private async Task<JsonWebKeySet> GetJwksAsync()
    {
        if (_jwksCache is not null && _jwksCacheExpiresAt > DateTime.UtcNow)
            return _jwksCache;

        await _jwksLock.WaitAsync();
        try
        {
            if (_jwksCache is not null && _jwksCacheExpiresAt > DateTime.UtcNow)
                return _jwksCache;

            var url = $"https://{_settings.Domain}/.well-known/jwks.json";
            var jwksJson = await _http.GetStringAsync(url);
            _jwksCache = new JsonWebKeySet(jwksJson);
            _jwksCacheExpiresAt = DateTime.UtcNow.AddHours(6);
            return _jwksCache;
        }
        finally { _jwksLock.Release(); }
    }
}
