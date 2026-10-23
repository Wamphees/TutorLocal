using System.Collections.ObjectModel;
using System.Windows.Input;
using TutorLocal.Models;
using TutorLocal.Services;

namespace TutorLocal.ViewModels;

public sealed class TutorRequestCardViewModel
{
    private readonly TutorDashboardViewModel _owner;

    public TutorRequestCardViewModel(HelpRequest request, TutorDashboardViewModel owner)
    {
        Request = request;
        _owner = owner;
    }

    public HelpRequest Request { get; }

    public string SubjectLine => $"{Request.Subject} · {Request.Level}";

    public string ScheduleLabel => Request.IsUrgent ? "Aujourd'hui" : "Cette semaine";

    public string ModalityLabel => Request.Modality == SessionModality.Online ? "Visioconférence" : Request.MeetingPlace;

    public ICommand AcceptCommand => new AsyncRelayCommand(async () => await _owner.AcceptAsync(Request));

    public ICommand RejectCommand => new AsyncRelayCommand(async () => await _owner.RejectAsync(Request));

    public ICommand MessageCommand => new AsyncRelayCommand(async () => await _owner.OpenChatAsync(Request));
}

public sealed class TutorDashboardViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly ITutorService _tutors;
    private readonly IHelpRequestService _requests;
    private readonly IChatService _chat;

    public TutorDashboardViewModel(INavigationService navigation, IAuthService auth, ITutorService tutors,
        IHelpRequestService requests, IChatService chat) : base(navigation)
    {
        _auth = auth;
        _tutors = tutors;
        _requests = requests;
        _chat = chat;
    }

    public ObservableCollection<TutorRequestCardViewModel> IncomingRequests { get; } = new();

    public string TutorName => _auth.CurrentUser?.FullName ?? "Tuteur";

    public bool HasRequests => IncomingRequests.Count > 0;

    public ICommand LogoutCommand => new RelayCommand(() =>
    {
        _auth.Logout();
        Navigation.SwitchTab<Views.MainPage>();
    });

    public override void OnAppearing()
    {
        base.OnAppearing();
        CurrentTutor();
        RefreshRequests();
        OnPropertyChanged(nameof(TutorName));
    }

    internal async Task AcceptAsync(HelpRequest request)
    {
        var tutor = CurrentTutor();
        if (tutor == null)
            return;

        _requests.Accept(request, _tutors);
        RefreshRequests();
        await AlertAsync("Demande acceptée", "Une séance a été ajoutée à votre agenda. Vous pouvez contacter l'élève.");
    }

    internal async Task RejectAsync(HelpRequest request)
    {
        if (await ConfirmAsync("Refuser la demande", "Cette demande sera retirée de votre liste.", "Refuser"))
        {
            _requests.Reject(request);
            RefreshRequests();
        }
    }

    internal async Task OpenChatAsync(HelpRequest request)
    {
        var tutor = CurrentTutor();
        if (tutor == null)
            return;

        _tutors.Select(tutor);
        _chat.GetOrCreate(request.StudentId, tutor.Id);
        await Navigation.PushAsync<Views.ChatPage>();
    }

    private Tutor? CurrentTutor()
    {
        var user = _auth.CurrentUser;
        return user?.Role == UserRole.Tutor ? _tutors.EnsureTutorProfile(user) : null;
    }

    private void RefreshRequests()
    {
        IncomingRequests.Clear();
        var tutorId = _auth.CurrentUser?.Id;
        if (tutorId != null)
            foreach (var request in _requests.GetIncoming(tutorId))
                IncomingRequests.Add(new TutorRequestCardViewModel(request, this));
        OnPropertyChanged(nameof(HasRequests));
    }

    private static async Task<bool> ConfirmAsync(string title, string message, string accept)
    {
        var page = Application.Current?.MainPage;
        return page != null && await page.DisplayAlert(title, message, accept, "Annuler");
    }
}
