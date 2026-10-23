using System.Collections.ObjectModel;
using TutorLocal.Models;

namespace TutorLocal.Services;

/// <summary>
/// Gestion des séances de tutorat : planification, confirmation, annulation,
/// suivi et avis de fin de séance.
/// </summary>
public interface ISessionService
{
    ObservableCollection<TutoringSession> Sessions { get; }

    TutoringSession? SessionForRating { get; set; }

    /// <summary>Séances à venir de l'élève (en attente + confirmées), triées par date.</summary>
    IEnumerable<TutoringSession> GetUpcoming(string studentId);

    /// <summary>Séances terminées.</summary>
    IEnumerable<TutoringSession> GetCompleted(string studentId);

    /// <summary>Séances annulées.</summary>
    IEnumerable<TutoringSession> GetCancelled(string studentId);

    TutoringSession? GetNextUpcoming(string studentId);

    /// <summary>Annule une séance à venir.</summary>
    bool Cancel(string sessionId);

    /// <summary>Crée une séance à partir d'une demande d'aide acceptée.</summary>
    TutoringSession CreateFromRequest(HelpRequest request);

    /// <summary>Programme une séance avec un tuteur depuis sa fiche.</summary>
    TutoringSession Schedule(string studentId, Tutor tutor, string subject, DateTime date, SessionModality modality);

    /// <summary>Enregistre l'avis de fin de séance (score + tags + commentaire).</summary>
    SessionFeedback SaveFeedback(TutoringSession session, int score, IEnumerable<string> tags, string comment);
}

public class SessionService : ISessionService
{
    private readonly ITutorService _tutorService;

    /// <summary>Feedback non noté : un seul processus à la fois, suffisant pour la messagerie de séance courante.</summary>
    public TutoringSession? SessionForRating { get; set; }

    private readonly Dictionary<string, SessionFeedback> _feedbacks = new();

    public SessionService(ITutorService tutorService)
    {
        _tutorService = tutorService;
        SeedSessions();
    }

    public ObservableCollection<TutoringSession> Sessions { get; } = new();

    public IEnumerable<TutoringSession> GetUpcoming(string studentId) =>
        Sessions.Where(s => s.StudentId == studentId && s.IsUpcoming).OrderBy(s => s.Date);

    public IEnumerable<TutoringSession> GetCompleted(string studentId) =>
        Sessions.Where(s => s.StudentId == studentId && s.Status == SessionStatus.Completed)
            .OrderByDescending(s => s.Date);

    public IEnumerable<TutoringSession> GetCancelled(string studentId) =>
        Sessions.Where(s => s.StudentId == studentId && s.Status == SessionStatus.Cancelled)
            .OrderByDescending(s => s.Date);

    public TutoringSession? GetNextUpcoming(string studentId) =>
        GetUpcoming(studentId).FirstOrDefault();

    public bool Cancel(string sessionId)
    {
        var session = Sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session == null || !session.IsUpcoming)
            return false;

        session.Status = SessionStatus.Cancelled;
        return true;
    }

    public TutoringSession CreateFromRequest(HelpRequest request)
    {
        var tutor = _tutorService.GetById(request.TutorId) ?? _tutorService.Tutors.First();
        var date = request.IsUrgent ? DateTime.Today.AddDays(1).AddHours(15) : DateTime.Today.AddDays(4).AddHours(17);

        var session = new TutoringSession
        {
            StudentId = request.StudentId,
            TutorId = tutor.Id,
            Subject = request.Subject,
            SubjectIcon = request.SubjectIcon,
            Status = SessionStatus.Pending,
            Modality = request.Modality,
            Date = date,
            Topic = request.Description,
            LocationTitle = request.Modality == SessionModality.Online ? "En visioconférence sécurisée"
                : "Médiathèque Parmentier",
            LocationSubtitle = request.Modality == SessionModality.Online ? "Lien crypté généré 15 min avant"
                : "Salle d'étude libre (1ᵉʳ étage) · Paris 11e"
        };

        Sessions.Add(session);
        return session;
    }

    public TutoringSession Schedule(string studentId, Tutor tutor, string subject, DateTime date, SessionModality modality)
    {
        var session = new TutoringSession
        {
            StudentId = studentId,
            TutorId = tutor.Id,
            Subject = subject,
            SubjectIcon = tutor.Subjects.FirstOrDefault(s => s.Name.Equals(subject, StringComparison.OrdinalIgnoreCase))?.Icon ?? "📘",
            Status = SessionStatus.Pending,
            Modality = modality,
            Date = date,
            LocationTitle = modality == SessionModality.Online ? "En visioconférence sécurisée" : tutor.LocationLabel,
            LocationSubtitle = modality == SessionModality.Online ? "Lien crypté généré 15 min avant" : tutor.MeetingPlace
        };

        Sessions.Add(session);
        return session;
    }

    public SessionFeedback SaveFeedback(TutoringSession session, int score, IEnumerable<string> tags, string comment)
    {
        session.FeedbackLeft = true;

        var feedback = new SessionFeedback
        {
            SessionId = session.Id,
            TutorId = session.TutorId,
            StudentId = session.StudentId,
            Score = score,
            Tags = tags.ToList(),
            Comment = comment
        };

        _feedbacks[session.Id] = feedback;
        _tutorService.AddRating(session.TutorId, score);

        return feedback;
    }

    /// <summary>
    /// Historique de démonstration : cohérent avec la maquette
    /// (2 à venir, 5 terminées dont 1 sans avis, 1 annulée).
    /// </summary>
    private void SeedSessions()
    {
        var now = DateTime.Now;

        // 1. En attente de confirmation (Thomas, maths) — la carte "Rappel de révision" du tableau de bord.
        Sessions.Add(new TutoringSession
        {
            StudentId = "alex",
            TutorId = "tutor-thomas",
            Subject = "Mathématiques",
            SubjectIcon = "📐",
            Status = SessionStatus.Pending,
            Modality = SessionModality.InPerson,
            Date = now.Date.AddDays(1).Add(new TimeSpan(15, 30, 0)),
            Topic = "Révisions chapitre des dérivées",
            LocationTitle = "Médiathèque Violette Leduc",
            LocationSubtitle = "Salle d'étude 2 (1ᵉʳ étage) · Paris 11e"
        });

        // 2. Confirmée (Sarah, physique-chimie) en visio.
        Sessions.Add(new TutoringSession
        {
            StudentId = "alex",
            TutorId = "tutor-sarah",
            Subject = "Physique-Chimie",
            SubjectIcon = "⚡",
            Status = SessionStatus.Confirmed,
            Modality = SessionModality.Online,
            Date = now.Date.AddDays(4).Add(new TimeSpan(10, 0, 0)),
            Topic = "Exercices de chimie organique",
            LocationTitle = "En visioconférence sécurisée",
            LocationSubtitle = "Lien crypté généré 15 min avant le début"
        });

        // 3. Terminée non évaluée (Lucas, anglais) → déclenche la demande d'avis.
        Sessions.Add(new TutoringSession
        {
            StudentId = "alex",
            TutorId = "tutor-lucas",
            Subject = "Anglais",
            SubjectIcon = "🇬🇧",
            Status = SessionStatus.Completed,
            Modality = SessionModality.InPerson,
            Date = now.Date.AddDays(-13).Add(new TimeSpan(17, 0, 0)),
            Topic = "Séance d'expression orale",
            LocationTitle = "Centre social Parmentier",
            LocationSubtitle = "Paris 11e",
            FeedbackLeft = false
        });

        // Séances passées déjà évaluées (historique).
        var past = new[]
        {
            new { TutorId = "tutor-thomas", Subject = "Mathématiques", Icon = "📐", Days = 17 },
            new { TutorId = "tutor-sarah", Subject = "SVT", Icon = "🧬", Days = 24 },
            new { TutorId = "tutor-thomas", Subject = "Physique-Chimie", Icon = "⚡", Days = 31 },
            new { TutorId = "tutor-mehdi", Subject = "Français", Icon = "✍️", Days = 45 }
        };

        foreach (var p in past)
        {
            Sessions.Add(new TutoringSession
            {
                StudentId = "alex",
                TutorId = p.TutorId,
                Subject = p.Subject,
                SubjectIcon = p.Icon,
                Status = SessionStatus.Completed,
                Modality = SessionModality.InPerson,
                Date = now.Date.AddDays(-p.Days).Add(new TimeSpan(16, 0, 0)),
                FeedbackLeft = true,
                LocationTitle = "Médiathèque Violette Leduc",
                LocationSubtitle = "Paris 11e"
            });
        }

        // 1 annulée.
        Sessions.Add(new TutoringSession
        {
            StudentId = "alex",
            TutorId = "tutor-claire",
            Subject = "Histoire-Géographie",
            SubjectIcon = "🗺️",
            Status = SessionStatus.Cancelled,
            Modality = SessionModality.InPerson,
            Date = now.Date.AddDays(-7).Add(new TimeSpan(14, 0, 0)),
            FeedbackLeft = false,
            LocationTitle = "Médiathèque Violette Leduc",
            LocationSubtitle = "Annulée par le tuteur"
        });
    }
}
