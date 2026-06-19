namespace InCleanHome.API.Notifications.Domain.Services.External;

/// <summary>
///     Puerto del dominio para un proveedor externo de notificaciones push.
/// </summary>
/// <remarks>
///     <para>
///     El dominio solo necesita una cosa de cualquier proveedor de push:
///     <i>enviar una notificación a un dispositivo identificado por un token</i>.
///     No conoce FCM, APNs, ni nombres específicos de proveedores.
///     </para>
///     <para>
///     Hoy la única implementación es <c>FirebaseCloudMessagingAdapter</c>. Si
///     mañana se cambia a OneSignal, Pusher Beams, AWS SNS, basta con escribir
///     otro adapter que implemente esta interfaz y registrarlo en DI.
///     </para>
/// </remarks>
public interface IPushNotificationProvider
{
    /// <summary>
    ///     Envía una notificación push directa a un dispositivo. Devuelve el ID
    ///     asignado por el proveedor (útil para tracing y logs).
    /// </summary>
    /// <param name="deviceToken">Token único del navegador o celular del usuario.</param>
    /// <param name="title">Título visible de la notificación.</param>
    /// <param name="body">Cuerpo visible de la notificación.</param>
    /// <param name="data">
    ///     Diccionario opcional de datos extra (ej. <c>link</c>, <c>bookingId</c>)
    ///     que el service worker del frontend usa al hacer click en la notificación.
    /// </param>
    Task<string> SendNotificationAsync(string deviceToken, string title, string body,
        Dictionary<string, string>? data = null);
}
