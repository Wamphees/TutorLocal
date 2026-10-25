namespace TutorLocal.Models;

/// <summary>Statut d'une séance de tutorat.</summary>
public enum SessionStatus
{
    /// <summary>En attente de confirmation par le tuteur.</summary>
    Pending,

    /// <summary>Confirmée par le tuteur.</summary>
    Confirmed,

    /// <summary>Terminée.</summary>
    Completed,

    /// <summary>Annulée.</summary>
    Cancelled
}

/// <summary>Modalité de la séance.</summary>
public enum SessionModality
{
    InPerson,
    Online
}

/// <summary>Séance de tutorat entre un élève et un tuteur bénévole.</summary>
public class TutoringSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string StudentId { get; set; } = string.Empty;

    public string TutorId { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public SessionStatus Status { get; set; } = SessionStatus.Pending;

    public SessionModality Modality { get; set; } = SessionModality.InPerson;

    /// <summary>Date et heure de début de la séance.</summary>
    public DateTime Date { get; set; }

    /// <summary>Durée en minutes.</summary>
    public int DurationMinutes { get; set; } = 60;

    public string LocationTitle { get; set; } = string.Empty;

    public string LocationSubtitle { get; set; } = string.Empty;

    /// <summary>Indique si l'élève a déjà laissé un avis après une séance terminée.</summary>
    public bool FeedbackLeft { get; set; }

    /// <summary>Sujet de travail évoqué (décrivez votre difficulté).</summary>
    public string Topic { get; set; } = string.Empty;

    public bool IsUpcoming => Status is SessionStatus.Pending or SessionStatus.Confirmed;

    public bool NeedsFeedback => Status == SessionStatus.Completed && !FeedbackLeft;

    /// <summary>Icône selon la modalité : présentiel ou visio.</summary>
    public string ModalityLabel => Modality == SessionModality.Online ? "En visioconférence sécurisée" : "En présentiel";
}

/// <summary>Demande d'aide express publiée par un élève.</summary>
public class HelpRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string StudentId { get; set; } = string.Empty;

    public string TutorId { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Level { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string AttachmentPath { get; set; } = string.Empty;

    public string AttachmentName { get; set; } = string.Empty;

    /// <summary>Urgent (sous 24h) ou planifié (cette semaine).</summary>
    public bool IsUrgent { get; set; } = true;

    public SessionModality Modality { get; set; } = SessionModality.InPerson;

    public string MeetingPlace { get; set; } = string.Empty;

    public DateTime PublishedAt { get; set; } = DateTime.Now;

    public HelpRequestStatus Status { get; set; } = HelpRequestStatus.Published;
}

public enum HelpRequestStatus
{
    Published,
    Accepted,
    Rejected,
    Closed
}

/// <summary>Avis détaillé soumis après une séance.</summary>
public class SessionFeedback
{
    public string SessionId { get; set; } = string.Empty;

    public string TutorId { get; set; } = string.Empty;

    public string StudentId { get; set; } = string.Empty;

    public int Score { get; set; }

    public List<string> Tags { get; set; } = new();

    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
