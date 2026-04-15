using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Notifications.Domain;

public static class NotificationErrors
{
    public static readonly Error NotFound =
        new("Notification.NotFound", "Notification was not found.");
}
