using System.Windows.Input;
using TutorLocal.Models;
using TutorLocal.Services;

namespace TutorLocal.ViewModels;

/// <summary>ViewModel de la page « Mon Profil ».</summary>
public class ProfileViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly ISessionService _sessionService;
    private readonly ITutorService _tutorService;
    private readonly INotificationService _notifications;

    public ProfileViewModel(INavigationService navigation, IAuthService auth, ISessionService sessionService,
        ITutorService tutorService, INotificationService notifications) : base(navigation)
    {
        _auth = auth;
        _sessionService = sessionService;
        _tutorService = tutorService;
        _notifications = notifications;
    }

    public string FullName => _auth.CurrentUser?.FullName ?? "Invité";

    public string LevelLabel => _auth.CurrentUser?.LevelLabel ?? "Élève";

    public string Initials => _auth.CurrentUser?.Initials ?? "U";

    public int UpcomingCount => _sessionService.GetUpcoming(StudentId).Count();

    /// <summary>Heures de cours suivies = séances terminées.</summary>
    public string HoursLabel => _sessionService
        .GetCompleted(StudentId)
        .Sum(s => s.DurationMinutes / 60d)
        .ToString("0") + " h";

    public int CompletedCount => _sessionService.GetCompleted(StudentId).Count();

    /// <summary>Tuteurs rencontrés au moins une fois (favoris).</summary>
    public int FavoriteTutors => _tutorService.GetFavoriteCount(StudentId);

    public int NotificationCount => _notifications.UnreadCount(StudentId);

    private string StudentId => _auth.CurrentUser?.Id ?? "alex";

    // Actions =============================================================

    public ICommand OpenNotificationsCommand => new AsyncRelayCommand(async () =>
    {
        await _notifications.ShowUnreadAsync(StudentId);
        OnPropertyChanged(nameof(NotificationCount));
    });

    public ICommand OpenPersonalInfoCommand => AlertStub("La modification du profil arrive bientôt !");

    public ICommand OpenPaymentCommand => AlertStub("TutorLocal est 100% gratuit : aucun abonnement nécessaire !");

    public ICommand OpenSettingsCommand => AlertStub("Les préférences de l'application arrivent bientôt !");

    public ICommand EditProfileCommand => OpenPersonalInfoCommand;

    public ICommand GoToDashboardCommand => new Command(() => Navigation.SwitchTab<Views.DashboardPage>());

    public ICommand GoToMapCommand => new Command(() => Navigation.SwitchTab<Views.MapPage>());

    public ICommand GoToSessionsCommand => new Command(() => Navigation.SwitchTab<Views.SessionsPage>());

    public ICommand LogoutCommand => new AsyncRelayCommand(LogoutAsync);

    private async Task LogoutAsync()
    {
        var confirmed = await ConfirmAsync(
            "Se déconnecter", "Voulez-vous vraiment quitter votre session ?", "Se déconnecter");

        if (confirmed != true)
            return;

        _auth.Logout();

        // On remplace la racine : impossible de "revenir" vers le profil après déconnexion.
        Navigation.SwitchTab<Views.MainPage>();
    }

    public override void OnAppearing()
    {
        base.OnAppearing();

        OnPropertyChanged(nameof(FullName));
        OnPropertyChanged(nameof(LevelLabel));
        OnPropertyChanged(nameof(Initials));
        OnPropertyChanged(nameof(UpcomingCount));
        OnPropertyChanged(nameof(HoursLabel));
        OnPropertyChanged(nameof(CompletedCount));
        OnPropertyChanged(nameof(FavoriteTutors));
        OnPropertyChanged(nameof(NotificationCount));
    }
    private AsyncRelayCommand AlertStub(string message) =>
        new(async () => await AlertAsync("Bientôt disponible", message));

    /// <summary>Demande une confirmation null-safe à l'utilisateur.</summary>
    private static async Task<bool> ConfirmAsync(string title, string message, string accept)
    {
        var page = Application.Current?.MainPage;
        if (page == null)
            return false;
        return await page.DisplayAlert(title, message, accept, "Annuler");
    }
}
