using System.Collections.ObjectModel;

namespace TutorLocal.Models;

/// <summary>Message échangé dans la messagerie sécurisée entre un élève et un tuteur.</summary>
public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string SenderId { get; set; } = string.Empty;

    public bool IsFromUser { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.Now;

    public bool HasImage { get; set; }

    /// <summary>Double coche bleue une fois le message lu.</summary>
    public bool IsRead { get; set; }

    public string TimeLabel => SentAt.ToString("HH:mm");

    /// <summary>Heure + accusés de réception.</summary>
    public string StatusLabel => TimeLabel;
}

/// <summary>Conversation entre un élève et un tuteur.</summary>
public class Conversation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string StudentId { get; set; } = string.Empty;

    public string TutorId { get; set; } = string.Empty;

    public ObservableCollection<ChatMessage> Messages { get; init; } = new();

    /// <summary>Carte "Séance confirmée" affichée en haut du fil si une séance est programmée.</summary>
    public DateTime? PlannedSessionDate { get; set; }

    public string PlannedSessionPlace { get; set; } = string.Empty;
}
