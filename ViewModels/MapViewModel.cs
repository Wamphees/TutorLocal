using System.Windows.Input;
using TutorLocal.Models;
using TutorLocal.Services;

namespace TutorLocal.ViewModels;

/// <summary>ViewModel de la carte des tuteurs à proximité (vue plan).</summary>
public class MapViewModel : ViewModelBase
{
    private readonly ITutorService _tutorService;
    private readonly INotificationService _notifications;
    private string _searchText = string.Empty;
    private bool _withinTwoKm = true;
    private bool _availableToday;
    private bool _mathsOnly;

    public MapViewModel(INavigationService navigation, ITutorService tutorService,
        IAuthService auth, INotificationService notifications) : base(navigation)
    {
        _tutorService = tutorService;
        _auth = auth;
        _notifications = notifications;
    }

    private readonly IAuthService _auth;

    public Tutor? Tutor => FilteredTutors().FirstOrDefault();

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                RefreshTutor();
        }
    }

    public int NotificationCount => _notifications.UnreadCount(_auth.CurrentUser?.Id ?? "alex");

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
        var tutor = Tutor;
        if (tutor == null)
        {
            await AlertAsync("Aucun tuteur trouvé", "Modifiez la recherche ou les filtres.");
            return;
        }

        _tutorService.Select(tutor);
        await Navigation.PushAsync<Views.TutorProfilePage>();
    });

    public ICommand OpenChatCommand => new AsyncRelayCommand(async () =>
    {
        var tutor = Tutor;
        if (tutor == null)
        {
            await AlertAsync("Aucun tuteur trouvé", "Modifiez la recherche ou les filtres.");
            return;
        }

        _tutorService.Select(tutor);
        await Navigation.PushAsync<Views.ChatPage>();
    });

    public ICommand GoToDashboardCommand => new Command(() => Navigation.SwitchTab<Views.DashboardPage>());

    public ICommand GoToSessionsCommand => new Command(() => Navigation.SwitchTab<Views.SessionsPage>());

    public ICommand GoToProfileCommand => new Command(() => Navigation.SwitchTab<Views.ProfilePage>());

    public ICommand OpenNotificationsCommand => new AsyncRelayCommand(async () =>
    {
        await _notifications.ShowUnreadAsync(_auth.CurrentUser?.Id ?? "alex");
        OnPropertyChanged(nameof(NotificationCount));
    });

    public ICommand ToggleDistanceFilterCommand => new Command(() =>
    {
        _withinTwoKm = !_withinTwoKm;
        RefreshTutor();
    });

    public ICommand ToggleAvailabilityFilterCommand => new Command(() =>
    {
        _availableToday = !_availableToday;
        RefreshTutor();
    });

    public ICommand ToggleMathFilterCommand => new Command(() =>
    {
        _mathsOnly = !_mathsOnly;
        RefreshTutor();
    });

    public ICommand FindMyLocationCommand => new AsyncRelayCommand(async () =>
    {
        try
        {
            var permission = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (permission != PermissionStatus.Granted)
                permission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (permission != PermissionStatus.Granted)
            {
                await AlertAsync("Permission refusée", "Autorisez la localisation pour afficher votre position.");
                return;
            }

            var request = new GeolocationRequest(GeolocationAccuracy.Default, TimeSpan.FromSeconds(10));
            var location = await Geolocation.Default.GetLocationAsync(request);
            if (location == null)
            {
                await AlertAsync("Localisation indisponible", "Aucune position n'a été retournée par l'appareil.");
                return;
            }

            await AlertAsync("Votre position", $"{location.Latitude:F5}, {location.Longitude:F5}");
        }
        catch (Exception exception) when (exception is FeatureNotSupportedException or PermissionException or FeatureNotEnabledException)
        {
            await AlertAsync("Localisation indisponible", exception.Message);
        }
    });

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
        OnPropertyChanged(nameof(NotificationCount));
        RefreshTutor();
    }

    private IEnumerable<Tutor> FilteredTutors()
    {
        var matches = _tutorService.Tutors.AsEnumerable();
        if (_withinTwoKm)
            matches = matches.Where(tutor => tutor.DistanceMeters <= 2000);
        if (_availableToday)
            matches = matches.Where(tutor => tutor.AvailabilityLabel.Contains("aujourd'hui", StringComparison.OrdinalIgnoreCase));
        if (_mathsOnly)
            matches = matches.Where(tutor => tutor.Subjects.Any(subject =>
                subject.Name.Contains("math", StringComparison.OrdinalIgnoreCase)));
        if (!string.IsNullOrWhiteSpace(SearchText))
            matches = matches.Where(tutor =>
                tutor.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                tutor.LocationLabel.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                tutor.Subjects.Any(subject => subject.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));
        return matches.OrderBy(tutor => tutor.DistanceMeters);
    }

    private void RefreshTutor()
    {
        OnPropertyChanged(nameof(Tutor));
        OnPropertyChanged(nameof(FullName));
        OnPropertyChanged(nameof(Avatar));
        OnPropertyChanged(nameof(RatingLabel));
        OnPropertyChanged(nameof(AvailabilityLabel));
        OnPropertyChanged(nameof(SubjectsLabel));
        OnPropertyChanged(nameof(DistanceLabel));
    }
}
