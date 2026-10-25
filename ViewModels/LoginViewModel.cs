using System.Windows.Input;
using TutorLocal.Models;
using TutorLocal.Services;
using TutorLocal.Views;

namespace TutorLocal.ViewModels;

/// <summary>ViewModel de la page de connexion : saisie, validation et authentification.</summary>
public class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly ISessionService _sessions;
    private readonly ITutorService _tutors;

    private string _email = string.Empty;

    private string _password = string.Empty;

    private bool _rememberMe = true;

    private bool _isPasswordHidden = true;

    private UserRole _selectedRole = UserRole.Student;

    public LoginViewModel(INavigationService navigation, IAuthService auth, ISessionService sessions,
        ITutorService tutors) : base(navigation)
    {
        _auth = auth;
        _sessions = sessions;
        _tutors = tutors;
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public bool RememberMe
    {
        get => _rememberMe;
        set => SetProperty(ref _rememberMe, value);
    }

    /// <summary>L'Entry masque la saisie tant que l'utilisateur n'a pas demandé à l'afficher.</summary>
    public bool IsPasswordHidden
    {
        get => _isPasswordHidden;
        private set
        {
            if (SetProperty(ref _isPasswordHidden, value))
                OnPropertyChanged(nameof(TogglePasswordText));
        }
    }

    public string TogglePasswordText => _isPasswordHidden ? "Afficher" : "Masquer";

    private bool IsStudentTab => _selectedRole == UserRole.Student;

    public string StudentTabBackground => IsStudentTab ? "#FFFFFF" : "#EEF0FF";

    public string StudentTabText => IsStudentTab ? "#5B44E9" : "#6B7280";

    public string TutorTabBackground => IsStudentTab ? "#EEF0FF" : "#FFFFFF";

    public string TutorTabText => IsStudentTab ? "#6B7280" : "#5B44E9";

    public ICommand LoginCommand => new AsyncRelayCommand(LoginAsync, isBusySource: () => IsBusy);

    // Onglets Élève / Tuteur =================================================

    public ICommand SelectStudentTabCommand => new RelayCommand(() => SetRole(UserRole.Student));

    public ICommand SelectTutorTabCommand => new RelayCommand(() => SetRole(UserRole.Tutor));

    // Affichage du mot de passe ==============================================

    public ICommand TogglePasswordCommand => new RelayCommand(() => IsPasswordHidden = !_isPasswordHidden);

    public ICommand GoToRegisterCommand => new AsyncRelayCommand(async () =>
    {
        await Navigation.PushAsync<RegisterPage>();
    });

    public new ICommand BackCommand => new RelayCommand(() => _ = Navigation.PopAsync());

    public ICommand ForgotPasswordCommand => new AsyncRelayCommand(async () =>
    {
        await AlertAsync("Mot de passe oublié",
            "Un lien de réinitialisation serait envoyé à ton adresse email (fonctionnalité de démonstration).");
    });

    public ICommand EntLoginCommand => AlertStub("L'authentification EduConnect/ENT sera active une fois l'établissement partenaire connecté.");

    public ICommand GoogleLoginCommand => AlertStub("La connexion Google est prévue pour la prochaine version (démonstration).");

    public ICommand BiometryCommand => AlertStub("La connexion biométrique nécessite une configuration sur l'appareil.");

    private AsyncRelayCommand AlertStub(string message) =>
        new(async () => await AlertAsync("Bientôt disponible", message));

    private void SetRole(UserRole role)
    {
        if (_selectedRole == role)
            return;

        _selectedRole = role;
        OnPropertyChanged(nameof(IsStudentTab));
        OnPropertyChanged(nameof(StudentTabBackground));
        OnPropertyChanged(nameof(StudentTabText));
        OnPropertyChanged(nameof(TutorTabBackground));
        OnPropertyChanged(nameof(TutorTabText));
        OnPropertyChanged(nameof(LoginHint));
    }

    public string LoginHint => IsStudentTab
        ? "Accédez à votre planning de devoirs et tuteurs du quartier."
        : "Gérez les demandes d'aide et votre agenda de tuteur bénévole.";

    private Task LoginAsync()
    {
        ErrorMessage = string.Empty;

        var result = _auth.Login(Email, Password, RememberMe);
        if (!result.Success)
        {
            ErrorMessage = result.Error;
            return Task.CompletedTask;
        }

        // La connexion remplace toute la racine de l'application :
        // l'utilisateur connecté ne doit pas pouvoir "revenir" au formulaire.
        if (_auth.CurrentUser?.Role == Models.UserRole.Tutor)
        {
            _tutors.EnsureTutorProfile(_auth.CurrentUser);
            Navigation.SwitchTab<TutorDashboardPage>();
        }
        else
        {
            Navigation.SwitchTab<DashboardPage>();
        }
        return Task.CompletedTask;
    }

    public override void OnAppearing()
    {
        base.OnAppearing();
        // Pré-remplit l'email pour une expérience fluide pendant les tests.
        if (string.IsNullOrWhiteSpace(Email))
            Email = "alex@exemple.fr";
    }
}
