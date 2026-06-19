namespace InCleanHome.API.IAM.Domain.Services.External;

/// <summary>
///     Puerto del dominio para un proveedor externo de identidad (SSO).
/// </summary>
/// <remarks>
///     <para>
///     Este contrato encapsula lo que la plataforma necesita de un proveedor
///     de identidad: <i>verificar un token y obtener los datos del usuario</i>.
///     No conoce JWKS, OIDC, /userinfo, ni ningún detalle de Auth0.
///     </para>
///     <para>
///     Hoy la única implementación es <c>Auth0IdentityProviderAdapter</c>. Si
///     mañana se cambia a Cognito, Okta, Keycloak, basta con escribir otro
///     adapter que implemente esta interfaz y registrarlo en DI — el resto del
///     proyecto (controllers, command services) no cambia.
///     </para>
/// </remarks>
public interface IIdentityProvider
{
    /// <summary>True si el adapter está configurado y operativo.</summary>
    bool IsEnabled { get; }

    /// <summary>
    ///     Valida el access token recibido del frontend (firma, expiración,
    ///     issuer/audience) y devuelve los datos del usuario tal como los
    ///     declara el proveedor. Devuelve null si la validación falla o si
    ///     el adapter está deshabilitado.
    /// </summary>
    Task<IdentityProviderUserInfo?> ValidateAndGetUserInfoAsync(string accessToken);
}

/// <summary>
///     Datos del usuario tal como vienen del proveedor de identidad.
/// </summary>
/// <param name="Subject">
///     Identificador único del usuario en el proveedor (ej. <c>auth0|abc123</c>,
///     <c>google-oauth2|xxx</c>). Inmutable a lo largo del tiempo.
/// </param>
/// <param name="Email">Email principal del usuario.</param>
/// <param name="Name">Nombre completo (puede coincidir con email si no lo proveyó).</param>
/// <param name="PictureUrl">URL de la foto de perfil (opcional).</param>
public record IdentityProviderUserInfo(string Subject, string Email, string Name, string? PictureUrl);
