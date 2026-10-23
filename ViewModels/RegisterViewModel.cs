using System.Collections.ObjectModel;
using System.Windows.Input;
using TutorLocal.Models;
using TutorLocal.Services;
using TutorLocal.Views;

namespace TutorLocal.ViewModels;

/// <summary>Option du sélecteur de niveau scolaire (pill sélectionnable).</summary>
public class SchoolLevelOption : ObservableObject
{
    private bool _isSelected;

    public SchoolLevelOption(SchoolLevel level, string label, bool isSelected = false)
    {
        Level = level;
        Label = label;
        _isSelected = isSelected;
    }

    public SchoolLevel Level { get; }

    public string Label { get; }

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

    public string BackgroundColor => IsSelected ? "#5B44E9" : "#EEF0FF";

    public string TextColor => IsSelected ? "#FFFFFF" : "#111827";

    public override string ToString() => Label;
}

/// <summary>ViewModel de la page d'inscription : création de compte élève ou tuteur.</summary>
public class RegisterViewModel : ViewModelBase
{
    private readonly IAuthService _auth;

    private string _fullName = string.Empty;

    private string _email = string.Empty;

    private string _password = string.Empty;

    private SchoolLevelOption _selectedLevel;

    private UserRole _role = UserRole.Student;

    private bool _termsAccepted;

    public RegisterViewModel(INavigationService navigation, IAuthService auth) : base(navigation)
    {
        _auth = auth;
        Levels = new ObservableCollection<SchoolLevelOption>
        {
            new(SchoolLevel.College, "Collège"),
            new(SchoolLevel.Lycee, "Lycée", isSelected: true),
            new(SchoolLevel.Superieur, "Supérieur")
        };
        _selectedLevel = Levels.First(l => l.IsSelected);
    }

    public ObservableCollection<SchoolLevelOption> Levels { get; }

    public string FullName
    {
        get => _fullName;
        set => SetProperty(ref _fullName, value);
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

    public SchoolLevelOption SelectedLevel
    {
        get => _selectedLevel;
        private set => SetProperty(ref _selectedLevel, value);
    }

    public bool TermsAccepted
    {
        get => _termsAccepted;
        set => SetProperty(ref _termsAccepted, value);
    }

    public bool IsStudent => _role == UserRole.Student;

    public bool IsTutor => _role == UserRole.Tutor;

    // Sélections ==========================================================

    public ICommand SelectStudentCommand => new RelayCommand(() => SetRole(UserRole.Student));

    public ICommand SelectTutorCommand => new RelayCommand(() => SetRole(UserRole.Tutor));

    public ICommand SelectLevelCommand => new Command<SchoolLevelOption>(option =>
    {
        if (option == null || option.IsSelected)
            return;

        foreach (var level in Levels)
            level.IsSelected = level == option;
        SelectedLevel = option;
    });

    // Actions =============================================================

    public ICommand RegisterCommand => new AsyncRelayCommand(RegisterAsync, isBusySource: () => IsBusy);

    public ICommand GoToLoginCommand => new AsyncRelayCommand(async () =>
    {
        await Navigation.PushAsync<LoginPage>();
    });

    private void SetRole(UserRole role)
    {
        if (_role == role)
            return;

        _role = role;
        OnPropertyChanged(nameof(IsStudent));
        OnPropertyChanged(nameof(IsTutor));
    }

    private async Task RegisterAsync()
    {
        ErrorMessage = string.Empty;

        var result = _auth.Register(FullName, Email, Password, SelectedLevel.Level, _role, TermsAccepted);
        if (!result.Success)
        {
            ErrorMessage = result.Error;
            return;
        }

        await AlertAsync("Bienvenue dans la communauté !",
            $"Votre compte a bien été créé. Bonne route {(_auth.CurrentUser?.DisplayName ?? "l'entraide")} 👋");

        Navigation.SwitchTab<DashboardPage>();
    }
}
