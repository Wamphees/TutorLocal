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

    HelpRequest Publish(HelpRequest request);

    /// <summary>Simule l'acceptation de la demande par un tuteur bénévole du quartier.</summary>
    TutoringSession Accept(HelpRequest request, ITutorService tutors);
}

public class HelpRequestService : IHelpRequestService
{
    private readonly ISessionService _sessions;

    public HelpRequestService(ISessionService sessions)
    {
        _sessions = sessions;
    }

    public ObservableCollection<HelpRequest> Requests { get; } = new();

    public HelpRequest Publish(HelpRequest request)
    {
        request.PublishedAt = DateTime.Now;
        request.Status = HelpRequestStatus.Published;
        Requests.Add(request);
        return request;
    }

    public TutoringSession Accept(HelpRequest request, ITutorService tutors)
    {
        request.Status = HelpRequestStatus.Accepted;
        return _sessions.CreateFromRequest(request);
    }
}
