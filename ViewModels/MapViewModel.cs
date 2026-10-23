using System.Windows.Input;
using TutorLocal.Services;

namespace TutorLocal.ViewModels;

/// <summary>ViewModel de la carte des tuteurs à proximité (vue plan).</summary>
public class MapViewModel : ViewModelBase
{
    private readonly ITutorService _tutorService;

    public MapViewModel(INavigationService navigation, ITutorService tutorService) : base(navigation)
    {
        _tutorService = tutorService;
    }

    public TutorLocal.Models.Tutor? Tutor => _tutorService.SelectedTutor ?? _tutorService.Tutors.First();

    public string FullName => Tutor?.FullName ?? "Tuteur";

    public string Avatar => Tutor?.Avatar ?? "👤";

    public string RatingLabel => Tutor is { } t ? $"⭐ {t.Rating:0.#} ({t.RatingsCount} avis)" : string.Empty;

    public string AvailabilityLabel => Tutor?.AvailabilityLabel ?? string.Empty;

    public string SubjectsLabel => Tutor == null
        ? string.Empty
        : string.Join("  •  ", Tutor.Subjects.Select(s => $"{s.Name} - {s.Levels}"));

    public string DistanceLabel => $"À {Tutor?.DistanceLabel ?? ""} ({Tutor?.LocationLabel ?? ""})";

    public ICommand ShowProfileCommand => new AsyncRelayCommand(async () =>
    {
        if (Tutor != null)
            _tutorService.Select(Tutor);
        await Navigation.PushAsync<Views.TutorProfilePage>();
    });

    public ICommand OpenChatCommand => new AsyncRelayCommand(async () =>
    {
        if (Tutor != null)
            _tutorService.Select(Tutor);
        await Navigation.PushAsync<Views.ChatPage>();
    });

    public ICommand GoToDashboardCommand => new Command(() => Navigation.SwitchTab<Views.DashboardPage>());

    public ICommand GoToSessionsCommand => new Command(() => Navigation.SwitchTab<Views.SessionsPage>());

    public ICommand GoToProfileCommand => new Command(() => Navigation.SwitchTab<Views.ProfilePage>());

    public override void OnAppearing()
    {
        base.OnAppearing();
        OnPropertyChanged(nameof(Tutor));
        OnPropertyChanged(nameof(FullName));
        OnPropertyChanged(nameof(Avatar));
        OnPropertyChanged(nameof(RatingLabel));
        OnPropertyChanged(nameof(AvailabilityLabel));
        OnPropertyChanged(nameof(SubjectsLabel));
        OnPropertyChanged(nameof(DistanceLabel));
    }
}
