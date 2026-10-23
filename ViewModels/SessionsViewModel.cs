using System.Collections.ObjectModel;
using System.Windows.Input;
using TutorLocal.Models;
using TutorLocal.Services;

namespace TutorLocal.ViewModels;

/// <summary>
/// Adapter d'affichage d'une séance pour l'UI de la page "Mes séances".
/// Expose des libellés pré-calculés, cohérents avec les statuts.
/// </summary>
public class SessionDisplay : ObservableObject
{
    /// <summary>Contexte de la liste (VM parent) pour les commandes globales.</summary>
    private readonly WeakReference<SessionsViewModel> _owner;

    public SessionDisplay(TutoringSession session, SessionsViewModel owner)
    {
        Session = session;
        _owner = new WeakReference<SessionsViewModel>(owner);
    }

    public TutoringSession Session { get; }

    public string TutorName => Owner()?.ResolveTutor(Session)?.FullName ?? "Tuteur";

    public string TutorAvatar => Owner()?.ResolveTutor(Session)?.Avatar ?? "👤";

    public string SubjectLine => $"{Session.Subject} · {Session.Topic}";

    public SessionStatus Status => Session.Status;

    // --- Libellés de statut ==============================================

    public string StatusLabel => Status switch
    {
        SessionStatus.Pending => "En attente de confirmation",
        SessionStatus.Confirmed => "Confirmée par la tutrice",
        SessionStatus.Completed => $"Terminée le {Session.Date:dd MMM}",
        _ => "Annulée"
    };

    public string StatusBadgeIcon => Status switch
    {
        SessionStatus.Pending => "🕒",
        SessionStatus.Confirmed => "✓",
        SessionStatus.Completed => "✓",
        _ => "✕"
    };

    public bool IsUpcoming => Session.IsUpcoming;

    public bool IsCancelled => Session.Status == SessionStatus.Cancelled;

    public bool FeedbackLeft => Session.FeedbackLeft;

    public string StatusBadgeColor => Status switch
    {
        SessionStatus.Pending => "#FEF3C7",
        SessionStatus.Confirmed => "#EEF2FF",
        SessionStatus.Completed => "#D1FAE5",
        _ => "#F3F4F6"
    };

    public string StatusBadgeTextColor => Status switch
    {
        SessionStatus.Pending => "#B45309",
        SessionStatus.Confirmed => "#4338CA",
        SessionStatus.Completed => "#065F46",
        _ => "#6B7280"
    };

    /// <summary>Liseré coloré à gauche de la carte.</summary>
    public string AccentColor => Status switch
    {
        SessionStatus.Pending => "#F59E0B",
        SessionStatus.Confirmed => "#4338CA",
        SessionStatus.Completed => "#10B981",
        _ => "#E5E7EB"
    };

    public string DateBadge => Session.IsUpcoming
        ? (Session.Date == DateTime.Today ? $"Aujourd'hui à {Session.Date:HH:mm}" : $"Demain à {Session.Date:HH:mm}")
        : $"{Session.Date:dd MMM}";

    public string TimeBadge => Owner()?.FormatTimeBadge(Session) ?? string.Empty;

    public string SubjectDetailLine => Status == SessionStatus.Completed
        ? $"Séance de {Session.Subject} · {Session.Topic}"
        : $"{Session.SubjectIcon} {Session.Subject}";

    public string DateLine => $"{Session.Date:dddd dd MMMM} · {Session.Date:HH'h'mm} - {Session.Date.AddMinutes(Session.DurationMinutes):HH'h'mm}";

    // --- Sections conditionnelles ========================================

    public bool ShowPendingActions => Status == SessionStatus.Pending;

    public bool ShowConfirmedActions => Status == SessionStatus.Confirmed;

    public bool ShowCompletedActions => Status == SessionStatus.Completed;

    public bool ShowFeedbackAsk => Session.NeedsFeedback;

    public string StarsPreview => "⭐⭐⭐⭐☆";

    // --- Commandes =======================================================

    public ICommand MessageCommand => new AsyncRelayCommand(async () =>
    {
        var owner = Owner();
        if (owner != null)
            await owner.OpenChatAsync(Session);
    });

    public ICommand CancelCommand => new AsyncRelayCommand(async () =>
    {
        var owner = Owner();
        if (owner != null)
            await owner.CancelAsync(Session);
    });

    public ICommand JoinVisioCommand => new AsyncRelayCommand(async () =>
    {
        var owner = Owner();
        if (owner != null)
            await owner.JoinVisioAsync(Session);
    });

    public ICommand RescheduleCommand => new AsyncRelayCommand(async () =>
    {
        var owner = Owner();
        if (owner != null)
            await owner.RescheduleAsync(Session);
    });

    public ICommand RateCommand => new AsyncRelayCommand(async () =>
    {
        var owner = Owner();
        if (owner != null)
            await owner.OpenRatingAsync(Session);
    });

    private SessionsViewModel? Owner()
    {
        return _owner.TryGetTarget(out var owner) ? owner : null;
    }
}

/// <summary>ViewModel de la page « Mes séances » : onglets, filtres, actions.</summary>
public class SessionsViewModel : ViewModelBase
{
    private const string StudentIdSeed = "alex";

    /// <summary>Identifiant de l'élève connecté (compte de démonstration sinon).</summary>
    private string StudentId => _auth.CurrentUser?.Id ?? StudentIdSeed;

    private readonly IAuthService _auth;
    private readonly ITutorService _tutorService;
    private readonly ISessionService _sessionService;

    private SessionTab _selectedTab = SessionTab.Upcoming;

    public SessionsViewModel(INavigationService navigation, IAuthService auth, ITutorService tutorService, ISessionService sessionService)
        : base(navigation)
    {
        _auth = auth;
        _tutorService = tutorService;
        _sessionService = sessionService;
    }

    private enum SessionTab
    {
        Upcoming,
        Completed,
        Cancelled
    }

    public ObservableCollection<SessionDisplay> Sessions { get; } = new();

    public int UpcomingCount => _sessionService.GetUpcoming(StudentId).Count();

    public int CompletedCount => _sessionService.GetCompleted(StudentId).Count();

    public int CancelledCount => _sessionService.GetCancelled(StudentId).Count();

    public int TotalCount => _sessionService.Sessions.Count();

    public bool IsUpcomingTab => _selectedTab == SessionTab.Upcoming;

    public bool IsCompletedTab => _selectedTab == SessionTab.Completed;

    public bool IsCancelledTab => _selectedTab == SessionTab.Cancelled;

    // Onglets =============================================================

    public ICommand SelectUpcomingTabCommand => new RelayCommand(() => SelectTab(SessionTab.Upcoming));

    public ICommand SelectCompletedTabCommand => new RelayCommand(() => SelectTab(SessionTab.Completed));

    public ICommand SelectCancelledTabCommand => new RelayCommand(() => SelectTab(SessionTab.Cancelled));

    private void SelectTab(SessionTab tab)
    {
        if (_selectedTab == tab)
            return;

        _selectedTab = tab;
        OnPropertyChanged(nameof(IsUpcomingTab));
        OnPropertyChanged(nameof(IsCompletedTab));
        OnPropertyChanged(nameof(IsCancelledTab));
        LoadSessions();
    }

    // Actions d'onglet supérieur ==========================================

    public ICommand OpenSearchCommand => AlertStub("La recherche avancée arrive bientôt !");

    public ICommand OpenFilterCommand => AlertStub("Les préférences de filtrage arrivent bientôt !");

    public ICommand OpenRequestHelpCommand => new AsyncRelayCommand(async () =>
    {
        await Navigation.PushAsync<Views.RequestHelpPage>();
    });

    // Navigation bas de page ==============================================

    public ICommand GoToDashboardCommand => new Command(() => Navigation.SwitchTab<Views.DashboardPage>());

    public ICommand GoToMapCommand => new Command(() => Navigation.SwitchTab<Views.MapPage>());

    public ICommand GoToProfileCommand => new Command(() => Navigation.SwitchTab<Views.ProfilePage>());

    // Actions de séance ===================================================

    public async Task OpenChatAsync(TutoringSession session)
    {
        var tutor = ResolveTutor(session);
        if (tutor != null)
            _tutorService.Select(tutor);
        await Navigation.PushAsync<Views.ChatPage>();
    }

    public async Task CancelAsync(TutoringSession session)
    {
        var confirmed = await ConfirmAsync("Annuler la séance",
            $"Voulez-vous vraiment annuler la séance de {ResolveTutor(session)?.FullName ?? "tuteur"} prévue le {session.Date:dddd dd MMMM} ?" +
            "\nLe tuteur sera notifié de votre annulation.", "Annuler la séance");

        if (confirmed != true)
            return;

        _sessionService.Cancel(session.Id);
        LoadSessions();
        RefreshCounts();
    }

    public async Task JoinVisioAsync(TutoringSession session)
    {
        var seconds = (session.Date - DateTime.Now).TotalSeconds;
        if (seconds > 15 * 60)
        {
            await AlertAsync("Visio pas encore disponible",
                "Le lien sécurisé de la visioconférence sera généré 15 minutes avant le début de la séance.");
            return;
        }

        await AlertAsync("Connexion à la visio…", "Ouverture de la salle sécurisée…");
    }

    public async Task RescheduleAsync(TutoringSession session)
    {
        var tutor = ResolveTutor(session);
        if (tutor == null)
            return;

        _tutorService.Select(tutor);
        await Navigation.PushAsync<Views.RequestHelpPage>();
    }

    public async Task OpenRatingAsync(TutoringSession session)
    {
        _sessionService.SessionForRating = session;
        await Navigation.PushAsync<Views.RatingPage>();
    }

    public string FormatTimeBadge(TutoringSession session)
    {
        if (!session.IsUpcoming)
            return session.Status == SessionStatus.Completed ? $"{session.DurationMinutes / 60d:0.#} h effectuée" : "Annulée";

        var remaining = session.Date - DateTime.Now;
        if (remaining <= TimeSpan.Zero) return "Imminent";
        if (remaining.TotalDays < 1) return $"Dans {(int)remaining.TotalHours}h {remaining.Minutes}min";
        return $"Dans {remaining.Days} jours";
    }

    public Tutor? ResolveTutor(TutoringSession session) =>
        _tutorService.GetById(session.TutorId);

    public override void OnAppearing()
    {
        base.OnAppearing();
        LoadSessions();
        RefreshCounts();
    }

    private void LoadSessions()
    {
        var student = _auth.CurrentUser?.Id ?? StudentIdSeed;

        Sessions.Clear();
        var items = _selectedTab switch
        {
            SessionTab.Completed => _sessionService.GetCompleted(student),
            SessionTab.Cancelled => _sessionService.GetCancelled(student),
            _ => _sessionService.GetUpcoming(student)
        };

        foreach (var session in items)
            Sessions.Add(new SessionDisplay(session, this));
    }

    private void RefreshCounts()
    {
        OnPropertyChanged(nameof(UpcomingCount));
        OnPropertyChanged(nameof(CompletedCount));
        OnPropertyChanged(nameof(CancelledCount));
        OnPropertyChanged(nameof(TotalCount));
    }

    private AsyncRelayCommand AlertStub(string message) =>
        new(async () => await AlertAsync("Bientôt disponible", message), isBusySource: () => IsBusy);

    /// <summary>Demande une confirmation null-safe à l'utilisateur.</summary>
    private static async Task<bool> ConfirmAsync(string title, string message, string accept)
    {
        var page = Application.Current?.MainPage;
        if (page == null)
            return false;
        return await page.DisplayAlert(title, message, accept, "Conserver");
    }
}
