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

    private string _email = string.Empty;

    private string _password = string.Empty;

    private bool _rememberMe = true;

    public LoginViewModel(INavigationService navigation, IAuthService auth, ISessionService sessions) : base(navigation)
    {
        _auth = auth;
        _sessions = sessions;
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

    public ICommand LoginCommand => new AsyncRelayCommand(LoginAsync, isBusySource: () => IsBusy);

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
        Navigation.SwitchTab<DashboardPage>();
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
