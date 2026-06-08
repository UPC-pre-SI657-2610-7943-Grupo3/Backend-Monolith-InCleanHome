using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using FirebaseAdmin.Messaging;
using InCleanHome.API.Notifications.Domain.Services;
using System.IO;
using System.Text;
using System.Text.Json;

namespace InCleanHome.API.Notifications.Infrastructure.External.Firebase;

public class FirebaseNotificationService : IFirebaseMessagingService
{
    private const string CredentialsFileName = "firebase-service-account.json";

    public FirebaseNotificationService()
    {
        if (FirebaseApp.DefaultInstance != null) return;

        string jsonContent;

        // Option A: read from environment variable (Render / production)
        var envJson = Environment.GetEnvironmentVariable("FIREBASE_SERVICE_ACCOUNT_JSON");
        if (!string.IsNullOrWhiteSpace(envJson))
        {
            jsonContent = envJson;
            Console.WriteLine("[Firebase] Using credentials from FIREBASE_SERVICE_ACCOUNT_JSON env var.");
        }
        else
        {
            // Option B: read from file (local development)
            // 1. Resolvemos la ruta usando AppContext.BaseDirectory primero (donde
            //    realmente se copia el archivo en compilación: bin/Debug/net9.0/...)
            //    Si no está ahí, probamos con la ruta relativa al working directory
            //    (útil cuando se ejecuta con `dotnet run` desde la raíz del proyecto).
            var candidatePaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, CredentialsFileName),
                Path.Combine(Directory.GetCurrentDirectory(), CredentialsFileName),
                CredentialsFileName
            };

            var credentialsPath = candidatePaths.FirstOrDefault(File.Exists)
                ?? throw new FileNotFoundException(
                    $"No se encontró '{CredentialsFileName}'. Búsqueda en: {string.Join(", ", candidatePaths)}. " +
                    "Alternatively, set the FIREBASE_SERVICE_ACCOUNT_JSON environment variable.");

            jsonContent = File.ReadAllText(credentialsPath);
            Console.WriteLine($"[Firebase] Using credentials from file: {credentialsPath}");
        }

        // 3. Validamos la estructura básica
        var keyData = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
        if (keyData == null || !keyData.ContainsKey("project_id"))
        {
            throw new InvalidOperationException("El archivo de credenciales de Firebase no es válido.");
        }

        // 4. Convertimos el JSON validado en un MemoryStream
        var jsonBytes = Encoding.UTF8.GetBytes(jsonContent);
        using var memoryStream = new MemoryStream(jsonBytes);

        // 5. Silenciamos el warning CS0618 únicamente para esta inicialización limpia en memoria
#pragma warning disable CS0618
        var googleCredential = GoogleCredential.FromStream(memoryStream)
            .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
#pragma warning restore CS0618

        // 6. Inicializamos Firebase Admin SDK
        FirebaseApp.Create(new AppOptions()
        {
            Credential = googleCredential,
            ProjectId = keyData["project_id"]
        });

        Console.WriteLine($"[Firebase] Initialized with project '{keyData["project_id"]}'");
    }

    public async Task<string> SendNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null)
    {
        if (string.IsNullOrWhiteSpace(deviceToken))
            throw new ArgumentException("Device token is required.", nameof(deviceToken));

        var message = new Message()
        {
            Token = deviceToken,
            Notification = new Notification()
            {
                Title = title,
                Body = body
            },
            Data = data,
            // Web push configuration: forces the browser to show a notification when
            // the page is in the background and the service worker is active.
            Webpush = new WebpushConfig()
            {
                Notification = new WebpushNotification()
                {
                    Title = title,
                    Body = body,
                    Icon = "/favicon.svg"
                },
                // FCM exige que Link sea una URL HTTPS completa (ej. https://app.com/bookings).
                // Como guardamos rutas relativas (/client/bookings, /worker/requests), las
                // pasamos por el data payload — el service worker las concatena al origin
                // dentro de notificationclick. Aquí solo enviamos Link cuando viene https://.
                FcmOptions = data != null
                             && data.TryGetValue("link", out var link)
                             && !string.IsNullOrEmpty(link)
                             && (link.StartsWith("https://") || link.StartsWith("http://"))
                    ? new WebpushFcmOptions { Link = link }
                    : null
            }
        };

        var messageId = await FirebaseMessaging.DefaultInstance.SendAsync(message);
        Console.WriteLine($"[Firebase] Push sent. messageId={messageId} to token={deviceToken[..Math.Min(20, deviceToken.Length)]}...");
        return messageId;
    }
}
