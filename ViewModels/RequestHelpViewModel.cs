using System.Collections.ObjectModel;
using System.Windows.Input;
using TutorLocal.Models;
using TutorLocal.Services;

namespace TutorLocal.ViewModels;

/// <summary>Option d'urgence et de modalité de la demande d'aide.</summary>
public class ChoiceOption
{
    public ChoiceOption(string key, string label, string icon)
    {
        Key = key;
        Label = label;
        Icon = icon;
    }

    public string Key { get; }

    public string Label { get; }

    public string Icon { get; }

    public override string ToString() => Label;
}

/// <summary>ViewModel du formulaire « Demande d'aide express ».</summary>
public class RequestHelpViewModel : ViewModelBase
{
    private const int DescriptionMaxLength = 500;

    private readonly IAuthService _auth;
    private readonly ITutorService _tutorService;
    private readonly IHelpRequestService _helpRequests;
    private readonly IChatService _chatService;

    private string _description = "Je bloque sur le calcul des dérivées partielles et l'application directe sur le tableau de variations de ma feuille d'exercices n°4 distribuée mardi.";

    private ChoiceOption _selectedSubjectLevel;

    private ChoiceOption _selectedUrgency;

    private ChoiceOption _selectedModality;
    private string _meetingPlace = "Médiathèque Parmentier";
    private string _attachmentPath = string.Empty;
    private string _attachmentName = string.Empty;

    public RequestHelpViewModel(INavigationService navigation, IAuthService auth, ITutorService tutorService,
        IHelpRequestService helpRequests, IChatService chatService) : base(navigation)
    {
        _auth = auth;
        _tutorService = tutorService;
        _helpRequests = helpRequests;
        _chatService = chatService;

        SubjectLevels = new ObservableCollection<ChoiceOption>
        {
            new("maths-lycee", "Mathématiques — Lycée (1ère)", "📐"),
            new("maths-college", "Mathématiques — Collège (3e)", "📐"),
            new("physique-lycee", "Physique-Chimie — Lycée (1ère)", "⚡"),
            new("svt-lycee", "SVT — Lycée (Terminale)", "🧬"),
            new("anglais-lycee", "Anglais — Lycée", "🗣️"),
            new("francais-lycee", "Français — Lycée (2nde)", "✍️"),
            new("histoire-lycee", "Histoire-Géographie — Lycée", "🗺️")
        };
        _selectedSubjectLevel = SubjectLevels[0];

        Urgencies = new ObservableCollection<ChoiceOption>
        {
            new("urgent", "Aujourd'hui", "🔥"),
            new("planned", "Cette semaine", "📅")
        };
        _selectedUrgency = Urgencies[0];

        Modalities = new ObservableCollection<ChoiceOption>
        {
            new("inperson", "En présentiel", "🏛️"),
            new("online", "En visio", "💻")
        };
        _selectedModality = Modalities[0];
    }

    public ObservableCollection<ChoiceOption> SubjectLevels { get; }

    public ObservableCollection<ChoiceOption> Urgencies { get; }

    public ObservableCollection<ChoiceOption> Modalities { get; }

    public ChoiceOption SelectedSubjectLevel
    {
        get => _selectedSubjectLevel;
        set
        {
            if (SetProperty(ref _selectedSubjectLevel, value))
                OnPropertyChanged(nameof(SubjectLevelLabel));
        }
    }

    public string SubjectLevelLabel => SelectedSubjectLevel?.Label ?? string.Empty;

    public ChoiceOption SelectedUrgency
    {
        get => _selectedUrgency;
        set
        {
            if (SetProperty(ref _selectedUrgency, value))
            {
                OnPropertyChanged(nameof(IsUrgentSelected));
                OnPropertyChanged(nameof(IsPlannedSelected));
            }
        }
    }

    public bool IsUrgentSelected => SelectedUrgency?.Key == "urgent";

    public bool IsPlannedSelected => SelectedUrgency?.Key == "planned";

    public ChoiceOption SelectedModality
    {
        get => _selectedModality;
        set
        {
            if (SetProperty(ref _selectedModality, value))
            {
                OnPropertyChanged(nameof(IsInPersonSelected));
                OnPropertyChanged(nameof(MeetingPlaceLabel));
                OnPropertyChanged(nameof(MeetingPlaceDistance));
            }
        }
    }

    public bool IsInPersonSelected => SelectedModality?.Key == "inperson";

    public string Description
    {
        get => _description;
        set
        {
            if (SetProperty(ref _description, value))
            {
                OnPropertyChanged(nameof(DescriptionCount));
                OnPropertyChanged(nameof(DescriptionCountLabel));
            }
        }
    }

    public int DescriptionCount => string.IsNullOrEmpty(Description) ? 0 : Description.Length;

    public string DescriptionCountLabel => $"{DescriptionCount} / {DescriptionMaxLength}";

    public string MeetingPlaceLabel => SelectedModality?.Key == "inperson"
        ? MeetingPlace
        : "Visioconférence dans l'application";

    public string MeetingPlace
    {
        get => _meetingPlace;
        private set
        {
            if (SetProperty(ref _meetingPlace, value))
                OnPropertyChanged(nameof(MeetingPlaceLabel));
        }
    }

    public string AttachmentName => _attachmentName;

    public string AttachmentButtonLabel => string.IsNullOrWhiteSpace(AttachmentName)
        ? "Joindre une photo du devoir"
        : AttachmentName;

    public string MeetingPlaceDistance => SelectedModality?.Key == "inperson"
        ? "Paris 11e • À 450 m de chez vous"
        : "Lien interactif sécurisé généré avant la séance";

    // Sélecteurs ============================

    public ICommand PickSubjectLevelCommand => new Command<ChoiceOption>(option =>
    {
        if (option != null && SubjectLevels.Contains(option))
            SelectedSubjectLevel = option;
    });

    /// <summary>Ouvre un simple sélecteur de matière (Parser = index d'option).</summary>
    public ICommand ChooseSubjectLevelCommand => new AsyncRelayCommand(async () =>
    {
        var labels = SubjectLevels.Select(s => s.Label).ToArray();
        var page = Application.Current?.MainPage;
        if (page == null)
            return;

        var chosen = await page.DisplayActionSheet("Choisir la matière", "Annuler", null, labels);
        var index = Array.IndexOf(labels, chosen);
        if (index >= 0)
            SelectedSubjectLevel = SubjectLevels[index];
    });

    public ICommand SelectUrgentCommand => new RelayCommand(() => SelectedUrgency = Urgencies[0]);

    public ICommand SelectPlannedCommand => new RelayCommand(() => SelectedUrgency = Urgencies[1]);

    public ICommand SelectInPersonCommand => new RelayCommand(() => SelectedModality = Modalities[0]);

    public ICommand SelectOnlineCommand => new RelayCommand(() => SelectedModality = Modalities[1]);

    public ICommand EditMeetingPlaceCommand => new AsyncRelayCommand(EditMeetingPlaceAsync);

    public ICommand AttachFileCommand => new AsyncRelayCommand(AttachFileAsync);

    public ICommand PublishCommand => new AsyncRelayCommand(PublishAsync, isBusySource: () => IsBusy);

    // Action principal =======

    private async Task PublishAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Description))
        {
            ErrorMessage = "Décrivez votre difficulté pour aider les tuteurs à se préparer.";
            return;
        }

        if (Description.Length > DescriptionMaxLength)
        {
            ErrorMessage = $"Votre description dépasse {DescriptionMaxLength} caractères.";
            return;
        }

        var student = _auth.CurrentUser;
        if (student == null)
        {
            ErrorMessage = "Session expirée, reconnectez-vous.";
            return;
        }

        var tutor = _tutorService.SelectedTutor ?? _tutorService.Tutors.First();

        var parts = SelectedSubjectLevel.Label.Split('—');
        var subject = parts[0].Trim();
        var icon = SelectedSubjectLevel.Icon;

        var request = new HelpRequest
        {
            StudentId = student.Id,
            TutorId = tutor.Id,
            Subject = subject,
            SubjectIcon = icon,
            Level = parts.Length > 1 ? parts[1].Trim() : student.LevelLabel,
            Description = Description.Trim(),
            IsUrgent = IsUrgentSelected,
            Modality = IsInPersonSelected ? SessionModality.InPerson : SessionModality.Online,
            MeetingPlace = MeetingPlaceLabel,
            AttachmentPath = _attachmentPath,
            AttachmentName = _attachmentName
        };

        _helpRequests.Publish(request);

        // La demande apparaît immédiatement dans la messagerie du tuteur.
        _chatService.SendMessage(student.Id, tutor.Id,
            $"⚡ Nouvelle demande d'aide en {subject} ({request.Level}) : « {request.Description} »");
        if (!string.IsNullOrWhiteSpace(request.AttachmentPath))
            _chatService.SendAttachment(student.Id, tutor.Id, student.Id, request.AttachmentPath, request.AttachmentName);

        await AlertAsync("Demande publiée ⚡",
            $"Votre demande a été transmise à {tutor.FullName}. Vous recevrez une notification lorsque le tuteur aura répondu.");

        await Navigation.PopAsync();
    }

    private async Task EditMeetingPlaceAsync()
    {
        var page = Application.Current?.MainPage;
        if (page == null)
            return;

        var place = await page.DisplayPromptAsync("Lieu de rencontre",
            "Choisissez un lieu public de rencontre.", "Enregistrer", "Annuler",
            "Ex. Médiathèque de proximité", 120, Keyboard.Text, MeetingPlace);
        if (!string.IsNullOrWhiteSpace(place))
            MeetingPlace = place.Trim();
    }

    private async Task AttachFileAsync()
    {
        var file = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Joindre une photo du devoir",
            FileTypes = FilePickerFileType.Images
        });
        if (file == null)
            return;

        var directory = Path.Combine(FileSystem.AppDataDirectory, "Attachments");
        Directory.CreateDirectory(directory);
        _attachmentName = file.FileName;
        _attachmentPath = Path.Combine(directory, $"{Guid.NewGuid():N}_{Path.GetFileName(file.FileName)}");
        await using var source = await file.OpenReadAsync();
        await using var destination = File.Create(_attachmentPath);
        await source.CopyToAsync(destination);
        OnPropertyChanged(nameof(AttachmentName));
        OnPropertyChanged(nameof(AttachmentButtonLabel));
    }
}
