namespace InCleanHome.API.Messaging.Domain.Services.External;

/// <summary>
///     Puerto del dominio para un proveedor externo de mensajería en tiempo real.
/// </summary>
/// <remarks>
///     <para>
///     El dominio necesita dos cosas de cualquier proveedor de chat:
///     <list type="number">
///         <item><description>
///             Una <i>conversación</i> única entre dos usuarios (idempotente —
///             si ya existe, devolver la misma).
///         </description></item>
///         <item><description>
///             Un <i>token de acceso</i> que el frontend usa para conectarse al
///             SDK del proveedor y enviar/recibir mensajes directamente.
///         </description></item>
///     </list>
///     </para>
///     <para>
///     Hoy la única implementación es <c>TwilioRealtimeMessagingAdapter</c>. Si
///     mañana se cambia a SendBird, Stream Chat, PubNub, basta con escribir otro
///     adapter que implemente esta interfaz y registrarlo en DI.
///     </para>
/// </remarks>
public interface IRealtimeMessagingProvider
{
    /// <summary>
    ///     Devuelve el SID/ID de la conversación entre dos usuarios. La crea si
    ///     no existe; idempotente. El orden de los participantes no importa
    ///     (la implementación debe normalizar).
    /// </summary>
    Task<string> GetOrCreateConversationSidAsync(string participantA, string participantB);

    /// <summary>
    ///     Genera un token de acceso de corta vida que el cliente usa para
    ///     conectarse al SDK del proveedor con la identidad dada.
    /// </summary>
    string GenerateAccessToken(string identity);
}
