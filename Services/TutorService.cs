using System.Collections.ObjectModel;
using TutorLocal.Models;

namespace TutorLocal.Services;

/// <summary>
/// Annuaire des tuteurs bénévoles disponibles autour de l'élève.
/// Gère aussi le tuteur « sélectionné » (fiche détaillée, chat, demande de cours).
/// </summary>
public interface ITutorService
{
    ObservableCollection<Tutor> Tutors { get; }

    /// <summary>Tuteur sélectionné avant l'ouverture d'une fiche ou d'une conversation.</summary>
    Tutor? SelectedTutor { get; set; }

    Tutor? GetById(string tutorId);

    /// <summary>Recherche par matière (« Tous » retourne l'annuaire complet).</summary>
    IEnumerable<Tutor> GetTutors(string subject = "Tous");

    void Select(Tutor tutor);

    bool IsFavorite(string userId, string tutorId);

    void ToggleFavorite(string userId, string tutorId);

    int GetFavoriteCount(string userId);

    Tutor EnsureTutorProfile(User user);

    /// <summary>Enregistre l'avis de l'élève et recalcule la moyenne du tuteur.</summary>
    void AddRating(string tutorId, int score);

    int AvailableMentorsCount { get; }
}

public class TutorService : ITutorService
{
    private const string TutorRecord = "tutors";
    private const string FavoriteRecord = "favorites";
    private const string SelectedTutorRecord = "state";
    private readonly LocalDatabase _database;
    private Tutor? _selectedTutor;

    public ObservableCollection<Tutor> Tutors { get; } = new();

    public Tutor? SelectedTutor
    {
        get
        {
            if (_selectedTutor != null)
                return _selectedTutor;

            var selectedId = _database.Get<string>(SelectedTutorRecord, "selectedTutor");
            _selectedTutor = selectedId == null ? null : GetById(selectedId);
            return _selectedTutor;
        }
        set
        {
            _selectedTutor = value;
            if (value == null)
                _database.Delete(SelectedTutorRecord, "selectedTutor");
            else
                _database.Save(SelectedTutorRecord, "selectedTutor", value.Id);
        }
    }

    public int AvailableMentorsCount => 14;

    public TutorService(LocalDatabase database)
    {
        _database = database;
        var savedTutors = _database.Load<Tutor>(TutorRecord);
        if (savedTutors.Count == 0)
        {
            SeedTutors();
            foreach (var tutor in Tutors)
                SaveTutor(tutor);
        }
        else
        {
            foreach (var tutor in savedTutors)
                Tutors.Add(tutor);
        }
    }

    public Tutor? GetById(string tutorId) => Tutors.FirstOrDefault(t => t.Id == tutorId);

    public IEnumerable<Tutor> GetTutors(string subject = "Tous") =>
        Tutors.Where(t => t.Teaches(subject)).OrderBy(t => t.DistanceMeters);

    public void Select(Tutor tutor) => SelectedTutor = tutor;

    public void AddRating(string tutorId, int score)
    {
        var tutor = GetById(tutorId);
        if (tutor == null)
            return;

        // Moyenne pondérée : le nouvel avis compte comme un avis supplémentaire.
        var total = tutor.Rating * tutor.RatingsCount + score;
        tutor.RatingsCount++;
        tutor.Rating = Math.Round(total / tutor.RatingsCount, 1);
        SaveTutor(tutor);
    }

    public bool IsFavorite(string userId, string tutorId) =>
        _database.Get<string>(FavoriteRecord, $"{userId}:{tutorId}") != null;

    public void ToggleFavorite(string userId, string tutorId)
    {
        var key = $"{userId}:{tutorId}";
        if (IsFavorite(userId, tutorId))
            _database.Delete(FavoriteRecord, key);
        else
            _database.Save(FavoriteRecord, key, tutorId, ownerId: userId, relatedId: tutorId);
    }

    public int GetFavoriteCount(string userId) => _database.Load<string>(FavoriteRecord, userId).Count;

    public Tutor EnsureTutorProfile(User user)
    {
        var existing = GetById(user.Id);
        if (existing != null)
            return existing;

        var tutor = new Tutor
        {
            Id = user.Id,
            FullName = user.FullName,
            Avatar = user.Avatar,
            ProfileLine = "Tuteur bénévole",
            About = "Profil de tuteur bénévole.",
            LocationLabel = "Quartier local",
            MeetingPlace = "Médiathèque de proximité",
            Subjects = { new TutorSubject("📘", "Aide scolaire", "Tous niveaux") }
        };
        Tutors.Add(tutor);
        SaveTutor(tutor);
        return tutor;
    }

    private void SaveTutor(Tutor tutor) =>
        _database.Save(TutorRecord, tutor.Id, tutor, searchText: $"{tutor.FullName} {string.Join(' ', tutor.Subjects.Select(s => s.Name))}");

    private void SeedTutors()
    {
        Tutors.Add(new Tutor
        {
            Id = "tutor-thomas",
            FullName = "Thomas D.",
            Avatar = "👨",
            Subjects =
            {
                new TutorSubject("📐", "Mathématiques", "Tous niveaux"),
                new TutorSubject("⚡", "Physique-Chimie", "Collège & Lycée"),
                new TutorSubject("💻", "Algorithmique / NSI", "Débutant")
            },
            Rating = 4.9,
            RatingsCount = 14,
            DistanceMeters = 800,
            LocationLabel = "Bibliothèque Parmentier",
            MeetingPlace = "Médiathèque Marguerite Duras · 115 Rue de Bagnolet, 75020 Paris",
            ProfileLine = "Étudiant en 2ᵉ année d'école d'ingénieurs (CentraleSupélec)",
            About = "Passionné de sciences, j'aide bénévolement les élèves de collège et lycée à débloquer leurs difficultés en maths et physique. Mon approche est basée sur la patience, l'écoute et des exemples concrets du quotidien pour redonner confiance sans stress !",
            HoursGiven = 28,
            ResponseTimeMinutes = 10,
            AvailabilityLabel = "Disponible aujourd'hui dès 15h",
            Reviews =
            {
                new TutorReview("Inès M.", "Terminale Générale",
                    "« Super pédagogue ! Thomas m'a aidée à comprendre les suites géométriques en une heure seulement. Très patient et clair ! »",
                    5, "Il y a 3 jours"),
                new TutorReview("Karim B.", "1ère Générale",
                    "« Explications très claires sur les dérivées, on avance à mon rythme. »",
                    5, "Il y a 1 semaine")
            }
        });

        Tutors.Add(new Tutor
        {
            Id = "tutor-sarah",
            FullName = "Sarah M.",
            Avatar = "👩",
            Subjects =
            {
                new TutorSubject("🧬", "SVT", "Lycée"),
                new TutorSubject("🧪", "Chimie", "Collège & Lycée")
            },
            Rating = 4.8,
            RatingsCount = 11,
            DistanceMeters = 1400,
            LocationLabel = "CDI Lycée Voltaire",
            MeetingPlace = "CDI du lycée Voltaire · Rue de la Roquette, 75011 Paris",
            ProfileLine = "Étudiante en 3ᵉ année de licence de biologie (Sorbonne Université)",
            About = "Amoureuse des sciences du vivant, je propose des séances de méthodologie et de révisions ciblées en SVT et chimie. Ensemble, on transforme les chapitres difficiles en points forts !",
            HoursGiven = 21,
            ResponseTimeMinutes = 15,
            AvailabilityLabel = "Disponible samedi toute la journée",
            Reviews =
            {
                new TutorReview("Léa T.", "Terminale",
                    "« Sarah m'a préparée à l'épreuve de SVT, ses fiches sont en or. »", 5, "Il y a 2 jours")
            }
        });

        Tutors.Add(new Tutor
        {
            Id = "tutor-lucas",
            FullName = "Lucas B.",
            Avatar = "🧔",
            Subjects =
            {
                new TutorSubject("🇬🇧", "Anglais", "Lycée"),
                new TutorSubject("🗣️", "Expression orale", "Tous niveaux")
            },
            Rating = 4.7,
            RatingsCount = 9,
            DistanceMeters = 2100,
            LocationLabel = "Centre social Parmentier",
            MeetingPlace = "Centre social Parmentier · 75011 Paris",
            ProfileLine = "Étudiant en LLCE Anglais (Université Paris 8)",
            About = "Bilingue franco-britannique, je fais travailler l'oral et la confiance en anglais sans avoir peur de se tromper. Objectif : parler spontanément !",
            HoursGiven = 16,
            ResponseTimeMinutes = 20,
            AvailabilityLabel = "Disponible les soirées en semaine",
            Reviews =
            {
                new TutorReview("Manon R.", "1ère",
                    "« Mes partiels d'anglais oraux se sont très bien passées grâce à Lucas. »", 5, "Il y a 5 jours")
            }
        });

        Tutors.Add(new Tutor
        {
            Id = "tutor-mehdi",
            FullName = "Mehdi K.",
            Avatar = "🧑",
            Subjects =
            {
                new TutorSubject("✍️", "Français", "Collège & Lycée"),
                new TutorSubject("📚", "Philosophie", "Lycée")
            },
            Rating = 4.6,
            RatingsCount = 7,
            DistanceMeters = 2600,
            LocationLabel = "Médiathèque Marguerite Duras",
            MeetingPlace = "Médiathèque Marguerite Duras · 75020 Paris",
            ProfileLine = "Étudiant en master de lettres modernes",
            About = "La dissertation et le commentaire n'auront plus de secret pour vous : méthode, plan type et relectures bienveillantes.",
            HoursGiven = 12,
            ResponseTimeMinutes = 25,
            AvailabilityLabel = "Disponible mercredi après-midi",
            Reviews =
            {
                new TutorReview("Yanis F.", "Terminale",
                    "« Ansi méthodique, j'ai gagné 4 points au bac blanc de philo. »", 5, "Il y a 6 jours")
            }
        });

        Tutors.Add(new Tutor
        {
            Id = "tutor-claire",
            FullName = "Claire R.",
            Avatar = "👩‍🦰",
            Subjects =
            {
                new TutorSubject("🗺️", "Histoire-Géographie", "Lycée"),
                new TutorSubject("⚖️", "EMC", "Collège")
            },
            Rating = 4.75,
            RatingsCount = 6,
            DistanceMeters = 3200,
            LocationLabel = "Médiathèque Violette Leduc",
            MeetingPlace = "Médiathèque Violette Leduc · 75020 Paris",
            ProfileLine = "Étudiante en histoire (Panthéon-Sorbonne)",
            About = "Croquis, repères, dissertation : je rends l'histoire-géo visuelle et facile à mémoriser.",
            HoursGiven = 9,
            ResponseTimeMinutes = 30,
            AvailabilityLabel = "Disponible jeudi et samedi",
            Reviews =
            {
                new TutorReview("Théo G.", "2nde",
                    "« Claire rend les croquis faciles, on apprend en s'amusant. »", 5, "Il y a 2 semaines")
            }
        });
    }
}
