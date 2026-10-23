using System.Collections.ObjectModel;
using TutorLocal.Models;

namespace TutorLocal.Services;

public interface INotificationService
{
    int UnreadCount(string userId);

    void Add(string userId, string title, string message);

    Task ShowUnreadAsync(string userId);
}

public sealed class NotificationService : INotificationService
{
    private readonly LocalDatabase _database;
    private readonly ObservableCollection<AppNotification> _notifications;

    public NotificationService(LocalDatabase database)
    {
        _database = database;
        _notifications = new ObservableCollection<AppNotification>(_database.Load<AppNotification>("notifications"));
    }

    public int UnreadCount(string userId) =>
        _notifications.Count(notification => notification.UserId == userId && !notification.IsRead);

    public void Add(string userId, string title, string message)
    {
        var notification = new AppNotification
        {
            UserId = userId,
            Title = title,
            Message = message
        };
        _notifications.Add(notification);
        Save(notification);
    }

    public async Task ShowUnreadAsync(string userId)
    {
        var unread = _notifications.Where(notification => notification.UserId == userId && !notification.IsRead)
            .OrderByDescending(notification => notification.CreatedAt)
            .ToList();
        var page = Application.Current?.MainPage;
        if (page == null)
            return;

        if (unread.Count == 0)
        {
            await page.DisplayAlert("Notifications", "Vous êtes à jour.", "OK");
            return;
        }

        var visible = unread.Take(8).ToList();
        var message = string.Join("\n\n", visible.Select(item => $"{item.Title}\n{item.Message}"));
        await page.DisplayAlert("Notifications", message, "OK");
        foreach (var notification in visible)
        {
            notification.IsRead = true;
            Save(notification);
        }
    }

    private void Save(AppNotification notification) =>
        _database.Save("notifications", notification.Id, notification, ownerId: notification.UserId,
            searchText: $"{notification.Title} {notification.Message}", status: notification.IsRead ? "Read" : "Unread",
            occurredAt: notification.CreatedAt);
}
