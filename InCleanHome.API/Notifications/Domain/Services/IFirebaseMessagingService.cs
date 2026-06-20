using System.Collections.Generic;
using System.Threading.Tasks;

namespace InCleanHome.API.Notifications.Domain.Services;

public interface IFirebaseMessagingService
{
    Task<string> SendNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null);
}