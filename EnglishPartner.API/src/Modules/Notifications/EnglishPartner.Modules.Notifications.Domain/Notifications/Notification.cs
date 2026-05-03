using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Notifications.Domain.Notifications;

public sealed class Notification : AggregateRoot
{
    public Guid UserId { get; private set; }
    public string Title { get; private set; }
    public string Message { get; private set; }
    public NotificationType Type { get; private set; }
    public bool IsRead { get; private set; }

    private Notification()
    {
        // EF Core
        Title = default!;
        Message = default!;
    }

    private Notification(Guid id, Guid userId, string title, string message, NotificationType type)
    {
        Id = id;
        UserId = userId;
        Title = title;
        Message = message;
        Type = type;
        IsRead = false;
    }

    public static Notification Create(Guid userId, string title, string message, NotificationType type)
    {
        return new Notification(Guid.NewGuid(), userId, title, message, type);
    }

    public void MarkAsRead()
    {
        if (IsRead) return;
        IsRead = true;
    }
}
