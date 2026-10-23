using System.Windows.Input;
using TutorLocal.Services;

namespace TutorLocal.ViewModels;

/// <summary>ViewModel de la page d'accueil publique (avant connexion).</summary>
public class MainViewModel : ViewModelBase
{
    public MainViewModel(INavigationService navigation) : base(navigation)
    {
    }

    public ICommand GoToRegisterCommand => new AsyncRelayCommand(async () =>
    {
        await Navigation.PushAsync<Views.RegisterPage>();
    }, isBusySource: () => IsBusy);

    public ICommand GoToLoginCommand => new AsyncRelayCommand(async () =>
    {
        await Navigation.PushAsync<Views.LoginPage>();
    }, isBusySource: () => IsBusy);

    public ICommand BecomeTutorCommand => GoToRegisterCommand;
}
