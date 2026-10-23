using System.Collections.ObjectModel;
using System.Windows.Input;
using TutorLocal.Services;

namespace TutorLocal.ViewModels;

/// <summary>ViewModel de la messagerie élève ↔ tuteur.</summary>
public class ChatViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly ITutorService _tutorService;
    private readonly IChatService _chatService;
    private readonly ISessionService _sessionService;

    private string _newMessage = string.Empty;

    private Models.Tutor? _tutor;

    public ChatViewModel(INavigationService navigation, IAuthService auth, ITutorService tutorService,
        IChatService chatService, ISessionService sessionService) : base(navigation)
    {
        _auth = auth;
        _tutorService = tutorService;
        _chatService = chatService;
        _sessionService = sessionService;

        Messages = new ObservableCollection<Models.ChatMessage>();
    }

    public ObservableCollection<Models.ChatMessage> Messages { get; }

    public Models.Tutor Tutor =>
        _tutor ?? _tutorService.Tutors.First();

    public bool HasTutor => _tutor != null;

    public string TutorHeaderLine => $"{Tutor.Avatar} {Tutor.FullName}";

    public string NewMessage
    {
        get => _newMessage;
        set => SetProperty(ref _newMessage, value);
    }

    public bool CanSend => !string.IsNullOrWhiteSpace(_newMessage);

    /// <summary>Séance planifiée avec ce tuteur (carte bleue en haut du fil).</summary>
    public Models.TutoringSession? PlannedSession { get; private set; }

    public bool HasPlannedSession => PlannedSession != null;

    public string PlannedSessionTitle => $"Séance de {PlannedSession?.Subject} confirmée";

    public string PlannedSessionLine => PlannedSession == null
        ? string.Empty
        : $"{PlannedSession.Date:dddd dd MMM.} à {PlannedSession.Date:HH'h'mm} à {(PlannedSession.Modality == Models.SessionModality.Online ? "la visioconférence sécurisée" : PlannedSession.LocationTitle)} (lieu public garanti).";

    // Actions =============================================================

    public ICommand SendCommand => new AsyncRelayCommand(SendAsync, isBusySource: () => IsBusy);

    public ICommand OpenTutorProfileCommand => new AsyncRelayCommand(async () =>
    {
        await Navigation.PushAsync<Views.TutorProfilePage>();
    });

    public override void OnAppearing()
    {
        base.OnAppearing();

        _tutor = _tutorService.SelectedTutor;
        OnPropertyChanged(nameof(Tutor));
        OnPropertyChanged(nameof(TutorHeaderLine));

        var studentId = _auth.CurrentUser?.Id ?? "alex";
        var conversation = _chatService.GetOrCreate(studentId, Tutor.Id);

        Messages.Clear();
        foreach (var message in conversation.Messages)
            Messages.Add(message);

        PlannedSession = _sessionService.GetUpcoming(studentId)
            .FirstOrDefault(s => s.TutorId == Tutor.Id);
        OnPropertyChanged(nameof(PlannedSession));
        OnPropertyChanged(nameof(HasPlannedSession));
        OnPropertyChanged(nameof(PlannedSessionTitle));
        OnPropertyChanged(nameof(PlannedSessionLine));
        OnPropertyChanged(nameof(CanSend));
    }

    private async Task SendAsync()
    {
        var text = NewMessage.Trim();
        if (string.IsNullOrEmpty(text))
            return;

        NewMessage = string.Empty;

        var studentId = _auth.CurrentUser?.Id ?? "alex";
        var message = _chatService.SendMessage(studentId, Tutor.Id, text);
        Messages.Add(message);

        IsBusy = true;

        // Le tuteur bénévole "respond" après un temps de réaction réaliste.
        var reply = await _chatService.WaitForTutorReplyAsync(studentId, Tutor.Id);
        if (reply != null)
            Messages.Add(reply);

        IsBusy = false;
    }
}
