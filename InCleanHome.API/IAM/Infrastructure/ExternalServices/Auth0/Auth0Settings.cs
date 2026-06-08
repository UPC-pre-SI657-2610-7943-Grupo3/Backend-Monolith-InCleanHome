namespace InCleanHome.API.IAM.Infrastructure.ExternalServices.Auth0;

/// <summary>
/// Configuración de Auth0 leída desde appsettings.json -> sección "Auth0".
///
/// Si Enabled = false, el endpoint /api/auth/auth0/login responde 503 y el frontend
/// debe ocultar el botón "Continuar con Auth0".
///
/// En producción/prod-like:
///   - Domain  : tenant Auth0   (ej: incleanhome.us.auth0.com)
///   - Audience: identifier de la API en Auth0 (ej: https://incleanhome-api)
///   - ClientId: opcional; sirve para validaciones adicionales si se quisiera.
/// </summary>
public class Auth0Settings
{
    public bool   Enabled  { get; set; } = false;
    public string Domain   { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
}
