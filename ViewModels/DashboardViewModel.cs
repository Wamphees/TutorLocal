using System.Collections.ObjectModel;
using System.Windows.Input;
using TutorLocal.Models;
using TutorLocal.Services;

namespace TutorLocal.ViewModels;

/// <summary>Tuteur affiché dans le carrousel « à proximité » du tableau de bord.</summary>
public class TutorCardViewModel : ObservableObject
{
    private readonly ITutorService _tutors;

    private readonly INavigationService _navigation;

    public TutorCardViewModel(Tutor tutor, ITutorService tutors, INavigationService navigation)
    {
        Tutor = tutor;
        _tutors = tutors;
        _navigation = navigation;
    }

    public Tutor Tutor { get; }

    public string FullName => Tutor.FullName;

    public string Avatar => Tutor.Avatar;

    public string RatingLabel => Tutor.RatingLabel;

    public string DistanceLabel => Tutor.DistanceLabel;

    public ObservableCollection<TutorSubject> Subjects { get; } = new();

    public void Load()
    {
        Subjects.Clear();
        foreach (var s in Tutor.Subjects.Take(3))
            Subjects.Add(s);
    }

    /// <summary>Commande « Demander un cours » de la carte : ouvre la messagerie du tuteur.</summary>
    public ICommand RequestCourseCommand => new AsyncRelayCommand(async () =>
    {
        _tutors.Select(Tutor);
        await _navigation.PushAsync<Views.ChatPage>();
    });
}

/// <summary>Pill de filtre par matière du tableau de bord.</summary>
public class SubjectFilterItem : ObservableObject
{
    private readonly WeakReference<DashboardViewModel> _owner;

    public SubjectFilterItem(DashboardViewModel owner, string label, bool isSelected)
    {
        _owner = new WeakReference<DashboardViewModel>(owner);
        Label = label;
        _isSelected = isSelected;
    }

    public string Label { get; }

    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                OnPropertyChanged(nameof(BackgroundColor));
                OnPropertyChanged(nameof(TextColor));
            }
        }
    }

    public string BackgroundColor => IsSelected ? "#4338CA" : "#FFFFFF";

    public string TextColor => IsSelected ? "#FFFFFF" : "#111827";

    public ICommand SelectCommand => new RelayCommand(() => Owner()?.SelectFilter(this));

    private DashboardViewModel? Owner() => _owner.TryGetTarget(out var owner) ? owner : null;
}

/// <summary>ViewModel du tableau de bord : accueil de l'élève connecté.</summary>
public class DashboardViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly ITutorService _tutorService;
    private readonly ISessionService _sessionService;
    private readonly INotificationService _notifications;

    private string _searchText = string.Empty;

    private string _selectedSubject = "Tous";

    private TutoringSession? _nextSession;

    public DashboardViewModel(INavigationService navigation, IAuthService auth, ITutorService tutorService,
        ISessionService sessionService, INotificationService notifications)
        : base(navigation)
    {
        _auth = auth;
        _tutorService = tutorService;
        _sessionService = sessionService;
        _notifications = notifications;

        SubjectFilters = new ObservableCollection<SubjectFilterItem>(
            new[] { "Tous", "Maths", "Physique", "Chimie", "SVT", "Anglais", "Français" }
                .Select(label => new SubjectFilterItem(this, label, label == "Tous")));
    }

    public ObservableCollection<TutorCardViewModel> Tutors { get; } = new();

    public ObservableCollection<SubjectFilterItem> SubjectFilters { get; }

    private string _nextSessionStatusLabel = "Confirmé";

    /// <summary>Badge d'état de la carte « Rappel de révision ».</summary>
    public string NextSessionStatusLabel
    {
        get => _nextSessionStatusLabel;
        private set => SetProperty(ref _nextSessionStatusLabel, value);
    }

    /// <summary>Compte à rebours dynamique (« Dans 22h 30min »).</summary>
    public string NextSessionCountdown { get; private set; } = string.Empty;

    public string UserFirstName => _auth.CurrentUser?.DisplayName ?? "Élève";

    public int NotificationCount => _notifications.UnreadCount(_auth.CurrentUser?.Id ?? "alex");

    public ICommand OpenNotificationsCommand => new AsyncRelayCommand(async () =>
    {
        await _notifications.ShowUnreadAsync(_auth.CurrentUser?.Id ?? "alex");
        OnPropertyChanged(nameof(NotificationCount));
    });

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                RefreshTutors();
        }
    }

    public string SelectedSubject
    {
        get => _selectedSubject;
        private set
        {
            if (SetProperty(ref _selectedSubject, value))
                RefreshTutors();
        }
    }

    /// <summary>Première séance à venir (carte « Rappel de révision »).</summary>
    public TutoringSession? NextSession
    {
        get => _nextSession;
        private set
        {
            if (SetProperty(ref _nextSession, value))
                OnPropertyChanged(nameof(HasNextSession));
        }
    }

    public bool HasNextSession => NextSession != null;

    /// <summary>Sujet de la prochaine séance pour la carte "Rappel de révision".</summary>
    public string NextSessionTitle => NextSession == null ? string.Empty : $"{NextSession.Subject} · {SessionTimeLabel(NextSession)}";

    public string NextSessionTutorName => SessionTutor(NextSession)?.FullName ?? string.Empty;

    public string NextSessionTutorSubject =>
        NextSession == null ? string.Empty : $"{NextSession.Subject} · {SessionTutor(NextSession)?.ProfileLine ?? "Bénévole"}";

    public string NextSessionTimeBadge => NextSession == null ? string.Empty : SessionCountdownLabel(NextSession);

    public string NextSessionPlace => NextSession?.LocationTitle ?? string.Empty;

    public ICommand SelectSubjectCommand => new Command<SubjectFilterItem>(item =>
    {
        if (item != null)
            SelectFilter(item);
    });

    public ICommand CreateRequestCommand => new AsyncRelayCommand(async () =>
    {
        await Navigation.PushAsync<Views.RequestHelpPage>();
    });

    public ICommand OpenChatCommand => new AsyncRelayCommand(async () =>
    {
        var session = NextSession;
        if (session != null)
        {
            var tutor = SessionTutor(session);
            if (tutor != null)
                _tutorService.Select(tutor);
        }
        await Navigation.PushAsync<Views.ChatPage>();
    });

    public ICommand OpenMapCommand => new Command(() => Navigation.SwitchTab<Views.MapPage>());

    public ICommand OpenSessionsCommand => new Command(() => Navigation.SwitchTab<Views.SessionsPage>());

    public ICommand OpenProfileCommand => new Command(() => Navigation.SwitchTab<Views.ProfilePage>());

    /// <summary>Tap sur une carte tuteur du carrousel : ouvre la fiche détaillée.</summary>
    public ICommand OpenTutorCardCommand => new AsyncRelayCommand(async parameter =>
    {
        if (parameter is not TutorCardViewModel card)
            return;

        _tutorService.Select(card.Tutor);
        await Navigation.PushAsync<Views.TutorProfilePage>();
    });

    public override void OnAppearing()
    {
        base.OnAppearing();
        OnPropertyChanged(nameof(UserFirstName));
        OnPropertyChanged(nameof(NotificationCount));
        RefreshTutors();
        RefreshNextSession();
    }

private void RefreshTutors()
    {
        var subject = _selectedSubject switch
        {
            "Maths" => "Mathématiques",
            "Physique" => "Physique-Chimie",
            "Chimie" => "Chimie",
            "SVT" => "SVT",
            "Anglais" => "Anglais",
            "Français" => "Français",
            _ => "Tous"
        };

        Tutors.Clear();
        foreach (var tutor in _tutorService.GetTutors(subject))
        {
            var matchesSearch = string.IsNullOrWhiteSpace(SearchText)
                || tutor.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || tutor.Subjects.Any(s => s.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            if (matchesSearch)
            {
                var card = new TutorCardViewModel(tutor, _tutorService, Navigation);
                card.Load();
                Tutors.Add(card);
            }
        }
    }

    private void RefreshNextSession()
    {
        NextSession = _sessionService.GetNextUpcoming(_auth.CurrentUser?.Id ?? "alex");

        if (NextSession != null)
        {
            NextSessionStatusLabel = NextSession.Status == SessionStatus.Confirmed ? "Confirmé" : "En attente";
            NextSessionCountdown = SessionCountdownLabel(NextSession);
        }

        OnPropertyChanged(nameof(NextSessionTitle));
        OnPropertyChanged(nameof(NextSessionTutorName));
        OnPropertyChanged(nameof(NextSessionTutorSubject));
        OnPropertyChanged(nameof(NextSessionTimeBadge));
        OnPropertyChanged(nameof(NextSessionPlace));
        OnPropertyChanged(nameof(NextSessionStatusLabel));
        OnPropertyChanged(nameof(NextSessionCountdown));
    }

    internal void SelectFilter(SubjectFilterItem item)
    {
        foreach (var filter in SubjectFilters)
            filter.IsSelected = filter == item;
        SelectedSubject = item.Label;
        RefreshTutors();
    }

    private Tutor? SessionTutor(TutoringSession? session) =>
        session == null ? null : _tutorService.GetById(session.TutorId);

    internal static string SessionTimeLabel(TutoringSession session) =>
        SessionDisplayHelper.DateBadge(session);

    internal static string SessionCountdownLabel(TutoringSession session) =>
        SessionDisplayHelper.TimeRemaining(session);

    internal static class SessionDisplayHelper
    {
        public static string DateBadge(TutoringSession s)
        {
            var diff = s.Date.Date - DateTime.Today;
            if (diff.Days == 0) return $"Aujourd'hui à {s.Date:HH:mm}";
            if (diff.Days == 1) return $"Demain à {s.Date:HH:mm}";
            if (diff.Days is > 1 and < 7) return $"Dans {diff.Days} jours";
            return s.Date.ToString("ddd d MMM à HH:mm");
        }

        public static string TimeRemaining(TutoringSession s)
        {
            var remaining = s.Date - DateTime.Now;
            if (remaining <= TimeSpan.Zero) return "Maintenant";
            if (remaining.TotalHours < 1) return $"Dans {remaining.Minutes} min";
            if (remaining.TotalHours < 24) return $"Dans {(int)remaining.TotalHours}h {remaining.Minutes}min";
            return $"Dans {remaining.Days}j {remaining.Hours}h";
        }
    }
}
