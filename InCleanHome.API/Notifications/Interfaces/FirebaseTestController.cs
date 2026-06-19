using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InCleanHome.API.Notifications.Domain.Services;
using InCleanHome.API.Notifications.Domain.Services.External;

namespace InCleanHome.API.Notifications.Interfaces.REST.Controllers;

[ApiController]
[Route("api/v1/firebase-test")] // Cambiamos la ruta base para que no pase por /api/notifications
[AllowAnonymous] // Permitimos el acceso libre de tokens Auth0 a nivel de clase
public class FirebaseTestController(IPushNotificationProvider pushProvider) : ControllerBase
{
    [HttpPost("test-send")]
    public async Task<IActionResult> TestSend([FromQuery] string token)
    {
        try
        {
            var messageId = await pushProvider.SendNotificationAsync(
                deviceToken: token,
                title: "¡Prueba de Conexión Exitosa!",
                body: "Si estás leyendo esto, tu backend de .NET y Firebase están perfectamente integrados."
            );

            return Ok(new { success = true, messageId, details = "Conexión con Google FCM exitosa." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }
}
