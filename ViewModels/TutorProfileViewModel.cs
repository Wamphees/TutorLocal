using System.Collections.ObjectModel;
using System.Windows.Input;
using TutorLocal.Models;
using TutorLocal.Services;

namespace TutorLocal.ViewModels;

/// <summary>ViewModel de la fiche détaillée d'un tuteur bénévole.</summary>
public class TutorProfileViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly ITutorService _tutorService;
    private readonly ISessionService _sessionService;

    private bool _isFavorite;

    public TutorProfileViewModel(INavigationService navigation, IAuthService auth, ITutorService tutorService,
        ISessionService sessionService) : base(navigation)
    {
        _auth = auth;
        _tutorService = tutorService;
        _sessionService = sessionService;

        Levels = new ObservableCollection<string>();
    }

    public Tutor? Tutor { get; private set; }

    public ObservableCollection<TutorSubject> Subjects { get; } = new();

    public ObservableCollection<string> Levels { get; }

    public bool HasTutor => Tutor != null;

    public string FullName => Tutor?.FullName ?? "Tuteur";

    public string Avatar => Tutor?.Avatar ?? "👤";

    public string RatingLabel => Tutor is { } t ? $"{t.Rating:0.#}" : "-";

    public string RatingCountLabel => Tutor is { } t ? $"{t.RatingsCount} avis vérifiés" : string.Empty;

    public string HoursLabel => Tutor is { } t ? $"{t.HoursGiven} h" : "-";

    public string ResponseLabel => Tutor is { } t
        ? (t.ResponseTimeMinutes < 60 ? $"< {Math.Ceiling(t.ResponseTimeMinutes / 10d) * 10} min" : "quelques heures")
        : "-";

    public string LocationPillLabel => $"Paris 11ᵉ · {Tutor?.DistanceLabel ?? ""}";

    public string About => Tutor?.About ?? string.Empty;

    public string IsFavoriteIcon => _isFavorite ? "❤️" : "🤍";

    public string IsFavoriteLabel => _isFavorite ? "Tuteur favori" : "Ajouter aux favoris";

    // Actions =============================================================

    public ICommand ToggleFavoriteCommand => new RelayCommand(() =>
    {
        var userId = _auth.CurrentUser?.Id;
        if (userId == null || Tutor == null)
            return;

        _tutorService.ToggleFavorite(userId, Tutor.Id);
        _isFavorite = _tutorService.IsFavorite(userId, Tutor.Id);
        OnPropertyChanged(nameof(IsFavoriteIcon));
        OnPropertyChanged(nameof(IsFavoriteLabel));
    });

    public ICommand ShareCommand => new AsyncRelayCommand(async () =>
    {
        if (Tutor == null)
            return;

        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = $"Profil de {Tutor.FullName}",
            Text = $"Découvrez le profil de {Tutor.FullName} sur TutorLocal : {Tutor.ProfileLine}"
        });
    });

    /// <summary>« Planifier une séance gratuite » : crée une séance en attente et ouvre le chat.</summary>
    public ICommand PlanSessionCommand => new AsyncRelayCommand(PlanSessionAsync, isBusySource: () => IsBusy);

    public ICommand OpenChatCommand => new AsyncRelayCommand(async () =>
    {
        await Navigation.PushAsync<Views.ChatPage>();
    });

    public override void OnAppearing()
    {
        base.OnAppearing();

        Tutor = _tutorService.SelectedTutor;
        _isFavorite = Tutor != null && _auth.CurrentUser != null &&
                      _tutorService.IsFavorite(_auth.CurrentUser.Id, Tutor.Id);
        OnPropertyChanged(nameof(Tutor));
        OnPropertyChanged(nameof(FullName));
        OnPropertyChanged(nameof(Avatar));
        OnPropertyChanged(nameof(RatingLabel));
        OnPropertyChanged(nameof(RatingCountLabel));
        OnPropertyChanged(nameof(HoursLabel));
        OnPropertyChanged(nameof(ResponseLabel));
        OnPropertyChanged(nameof(LocationPillLabel));
        OnPropertyChanged(nameof(About));
        OnPropertyChanged(nameof(HasTutor));
        OnPropertyChanged(nameof(IsFavoriteIcon));
        OnPropertyChanged(nameof(IsFavoriteLabel));

        Subjects.Clear();
        if (Tutor != null)
            foreach (var subject in Tutor.Subjects)
                Subjects.Add(subject);
    }

    private async Task PlanSessionAsync()
    {
        ErrorMessage = string.Empty;

        var student = _auth.CurrentUser;
        if (student == null || Tutor == null)
        {
            ErrorMessage = "Session expirée, reconnectez-vous.";
            return;
        }

        var subject = Tutor.Subjects.First().Name;
        var date = DateTime.Today.AddDays(1).AddHours(15);
        var session = _sessionService.Schedule(student.Id, Tutor, subject, date, SessionModality.InPerson);

        await AlertAsync("Séance planifiée 📅",
            $"Votre demande de séance en {subject} avec {Tutor.FullName} a été envoyée pour le " +
            $"{date:dddd dd MMM à HH:mm}. En attente de confirmation du tuteur.");

        await Navigation.PopAsync();
    }
}
