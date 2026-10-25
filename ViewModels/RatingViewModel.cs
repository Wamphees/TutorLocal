using System.Collections.ObjectModel;
using System.Windows.Input;
using TutorLocal.Models;
using TutorLocal.Services;

namespace TutorLocal.ViewModels;

/// <summary>Étoile de la barre de notation.</summary>
public class RatingStar : ObservableObject
{
    private readonly RatingViewModel _owner;

    public RatingStar(RatingViewModel owner, int index)
    {
        _owner = owner;
        Index = index;
    }

    public int Index { get; }

    /// <summary>Étoile remplie (sélectionnée) ou vide.</summary>
    public string Glyph => _owner.IsFilled(Index) ? "★" : "☆";

    public string Color => _owner.IsFilled(Index) ? "#F59E0B" : "#D1D5DB";

    public ICommand SelectCommand => new RelayCommand(() => _owner.Select(Index));

    /// <summary>Rafraîchit l'affichage de l'étoile (appelé par le VM après un changement de note).</summary>
    public void Refresh()
    {
        OnPropertyChanged(nameof(Glyph));
        OnPropertyChanged(nameof(Color));
    }
}

/// <summary>Tag / point fort sélectionnable dans l'avis.</summary>
public class RatingTag : ObservableObject
{
    private readonly RatingViewModel _owner;

    public RatingTag(RatingViewModel owner, string label)
    {
        _owner = owner;
        Label = label;
        IsSelected = false;
    }

    public string Label { get; }

    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                OnPropertyChanged(nameof(BackgroundColor));
                OnPropertyChanged(nameof(TextColor));
                OnPropertyChanged(nameof(IsInactive));
            }
        }
    }

    public bool IsInactive => !IsSelected;

    public string BackgroundColor => IsSelected ? "#EEF0FF" : "#F3F4F6";

    public string TextColor => IsSelected ? "#5B44E9" : "#111827";

    public ICommand ToggleCommand => new RelayCommand(() => _owner.ToggleTag(this));
}

/// <summary>ViewModel de la page d'évaluation de fin de séance.</summary>
public class RatingViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly ISessionService _sessionService;
    private readonly ITutorService _tutorService;

    private int _score;

    private string _comment = string.Empty;

    public RatingViewModel(INavigationService navigation, IAuthService auth, ISessionService sessionService, ITutorService tutorService) : base(navigation)
    {
        _auth = auth;
        _sessionService = sessionService;
        _tutorService = tutorService;

        Stars = new ObservableCollection<RatingStar>(Enumerable.Range(1, 5).Select(i => new RatingStar(this, i)));
        Tags = new ObservableCollection<RatingTag>
        {
            new(this, "Pédagogue & Patient"),
            new(this, "Ponctuel"),
            new(this, "Explications claires"),
            new(this, "Bienveillant"),
            new(this, "Méthodique")
        };
    }

    public ObservableCollection<RatingStar> Stars { get; }

    public ObservableCollection<RatingTag> Tags { get; }

    public TutoringSession? Session => _sessionService.SessionForRating;

    public bool HasSession => Session != null;

    public string TutorName =>
        _tutorService.GetById(Session?.TutorId ?? string.Empty)?.FullName ?? "Votre tuteur";

    public string ScoreLabel => _score switch
    {
        0 => "Touchez les étoiles pour noter la séance",
        1 => "Décevant",
        2 => "Peut mieux faire",
        3 => "Correct",
        4 => "Très bien",
        _ => "Excellent ! Très pédagogue et encourageant"
    };

    public string Comment
    {
        get => _comment;
        set => SetProperty(ref _comment, value);
    }

    public string TutorFirstName
    {
        get
        {
            var name = TutorName;
            var space = name.IndexOf(' ');
            return space > 0 ? name[..space] : name;
        }
    }

    // Sélection ===========================================================

    internal bool IsFilled(int starIndex) => starIndex <= _score;

    internal void Select(int index)
    {
        if (_score == index)
            return;

        _score = index;
        RefreshStars();
        OnPropertyChanged(nameof(ScoreLabel));
        OnPropertyChanged(nameof(HasSelectedScore));
        OnPropertyChanged(nameof(ScoreCountLabel));
    }

    internal void ToggleTag(RatingTag tag) => tag.IsSelected = !tag.IsSelected;

    public bool HasSelectedScore => _score > 0;

    public string ScoreCountLabel => _score > 0 ? $"{_score}/5 ★" : "";

    // Actions =============================================================

    public ICommand SubmitCommand => new AsyncRelayCommand(SubmitAsync, isBusySource: () => IsBusy);

    public ICommand CloseCommand => new AsyncRelayCommand(async () => await Navigation.PopAsync());

    public override void OnAppearing()
    {
        base.OnAppearing();

        _score = 0;
        RefreshStars();
        Comment = string.Empty;
        foreach (var tag in Tags)
            tag.IsSelected = false;

        OnPropertyChanged(nameof(TutorName));
        OnPropertyChanged(nameof(TutorFirstName));
        OnPropertyChanged(nameof(ScoreLabel));
    }

    private void RefreshStars()
    {
        foreach (var star in Stars)
            star.Refresh();
    }

    private async Task SubmitAsync()
    {
        ErrorMessage = string.Empty;

        var session = Session;
        if (session == null)
        {
            ErrorMessage = "Impossible de trouver la séance à évaluer.";
            return;
        }

        if (_score == 0)
        {
            ErrorMessage = "Veuillez sélectionner une note entre 1 et 5 étoiles.";
            return;
        }

        var tags = Tags.Where(t => t.IsSelected).Select(t => t.Label).ToList();
        var feedback = _sessionService.SaveFeedback(session, _score, tags, Comment?.Trim() ?? string.Empty);

        await AlertAsync("Merci pour votre avis 🤍",
            $"Votre note de {_score}/5 a été transmise à {TutorName}. " +
            "Votre avis permet de valoriser l'engagement bénévole de ce tuteur !");

        await Navigation.PopAsync();
    }
}
