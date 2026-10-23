using System.Collections.ObjectModel;
using TutorLocal.Models;

namespace TutorLocal.Services;

/// <summary>
/// Demandes d'aide express : publication, acceptation par un tuteur,
/// transformation en séance planifiée.
/// </summary>
public interface IHelpRequestService
{
    ObservableCollection<HelpRequest> Requests { get; }

    IEnumerable<HelpRequest> GetIncoming(string tutorId);

    HelpRequest Publish(HelpRequest request);

    /// <summary>Simule l'acceptation de la demande par un tuteur bénévole du quartier.</summary>
    TutoringSession Accept(HelpRequest request, ITutorService tutors);

    bool Reject(HelpRequest request);
}

public class HelpRequestService : IHelpRequestService
{
    private readonly ISessionService _sessions;
    private readonly LocalDatabase _database;
    private readonly INotificationService _notifications;

    public HelpRequestService(ISessionService sessions, LocalDatabase database, INotificationService notifications)
    {
        _sessions = sessions;
        _database = database;
        _notifications = notifications;
        foreach (var request in _database.Load<HelpRequest>("requests"))
            Requests.Add(request);
    }

    public ObservableCollection<HelpRequest> Requests { get; } = new();

    public HelpRequest Publish(HelpRequest request)
    {
        request.PublishedAt = DateTime.Now;
        request.Status = HelpRequestStatus.Published;
        Requests.Add(request);
        Save(request);
        _notifications.Add(request.TutorId, "Nouvelle demande d'aide", $"{request.Subject} · {request.Level}");
        return request;
    }

    public IEnumerable<HelpRequest> GetIncoming(string tutorId) =>
        Requests.Where(request => request.TutorId == tutorId && request.Status == HelpRequestStatus.Published)
            .OrderByDescending(request => request.PublishedAt);

    public TutoringSession Accept(HelpRequest request, ITutorService tutors)
    {
        TutoringSession? session = null;
        _database.InTransaction(() =>
        {
            request.Status = HelpRequestStatus.Accepted;
            Save(request);
            session = _sessions.CreateFromRequest(request);
        });
        _notifications.Add(request.StudentId, "Demande acceptée", "Le tuteur a accepté votre demande et une séance a été proposée.");
        return session ?? throw new InvalidOperationException("La séance acceptée n'a pas été créée.");
    }

    public bool Reject(HelpRequest request)
    {
        if (request.Status != HelpRequestStatus.Published)
            return false;

        _database.InTransaction(() =>
        {
            request.Status = HelpRequestStatus.Rejected;
            Save(request);
        });
        _notifications.Add(request.StudentId, "Demande refusée", "Le tuteur ne peut pas répondre à cette demande.");
        return true;
    }

    private void Save(HelpRequest request) =>
        _database.Save("requests", request.Id, request, ownerId: request.TutorId, relatedId: request.StudentId,
            searchText: $"{request.Subject} {request.Level} {request.Description}", status: request.Status.ToString(),
            occurredAt: request.PublishedAt);
}
