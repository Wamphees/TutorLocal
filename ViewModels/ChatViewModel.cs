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
    private string _studentId = "alex";
    private string _viewerId = string.Empty;

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

    public bool IsStudent => _auth.CurrentUser?.Role != Models.UserRole.Tutor;

    public string TutorHeaderLine => Tutor.FullName;

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

        var user = _auth.CurrentUser;
        _viewerId = user?.Id ?? "alex";
        if (user?.Role == Models.UserRole.Tutor &&
            _chatService.Current.TutorId == user.Id &&
            !string.IsNullOrWhiteSpace(_chatService.Current.StudentId))
        {
            _studentId = _chatService.Current.StudentId;
            _tutor = _tutorService.GetById(user.Id) ?? _tutorService.EnsureTutorProfile(user);
        }
        else
        {
            _studentId = user?.Id ?? "alex";
            _tutor = _tutorService.SelectedTutor;
        }

        OnPropertyChanged(nameof(Tutor));
        OnPropertyChanged(nameof(TutorHeaderLine));
        OnPropertyChanged(nameof(IsStudent));

        var conversation = _chatService.GetOrCreate(_studentId, Tutor.Id);

        Messages.Clear();
        foreach (var message in conversation.Messages)
        {
            message.IsFromUser = message.SenderId == _viewerId;
            Messages.Add(message);
        }

        PlannedSession = _sessionService.GetUpcoming(_studentId)
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

        var isTutor = _auth.CurrentUser?.Role == Models.UserRole.Tutor;
        var message = isTutor
            ? _chatService.SendTutorMessage(_studentId, Tutor.Id, text)
            : _chatService.SendMessage(_studentId, Tutor.Id, text);
        message.IsFromUser = true;
        Messages.Add(message);

        if (isTutor)
            return;

        IsBusy = true;
        try
        {
            var reply = await _chatService.WaitForTutorReplyAsync(_studentId, Tutor.Id);
            if (reply != null)
            {
                reply.IsFromUser = reply.SenderId == _viewerId;
                Messages.Add(reply);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public ICommand AttachFileCommand => new AsyncRelayCommand(AttachFileAsync, isBusySource: () => IsBusy);

    public ICommand OpenAttachmentCommand => new AsyncRelayCommand(async parameter =>
    {
        if (parameter is not string path || string.IsNullOrWhiteSpace(path))
            return;

        if (!File.Exists(path))
        {
            await AlertAsync("Fichier introuvable", "Cette pièce jointe n'est plus disponible sur cet appareil.");
            return;
        }

        try
        {
            await Launcher.Default.OpenAsync(new OpenFileRequest(Path.GetFileName(path), new ReadOnlyFile(path)));
        }
        catch (Exception exception)
        {
            await AlertAsync("Ouverture impossible", exception.Message);
        }
    });

    private async Task AttachFileAsync()
    {
        var file = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Choisir une photo",
            FileTypes = FilePickerFileType.Images
        });
        if (file == null)
            return;

        var directory = Path.Combine(FileSystem.AppDataDirectory, "Attachments");
        Directory.CreateDirectory(directory);
        var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(file.FileName)}";
        var destination = Path.Combine(directory, safeName);
        await using (var source = await file.OpenReadAsync())
        await using (var target = File.Create(destination))
            await source.CopyToAsync(target);

        var message = _chatService.SendAttachment(_studentId, Tutor.Id, _viewerId, destination, file.FileName);
        message.IsFromUser = true;
        Messages.Add(message);
    }
}
