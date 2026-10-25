namespace TutorLocal.Models;

/// <summary>Tuteur bénévole proposant des séances d'entraide gratuites.</summary>
public class Tutor
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string FullName { get; set; } = string.Empty;

    public string Avatar { get; set; } = string.Empty;

    /// <summary>Initiales discrètes affichées à la place des avatars illustrés.</summary>
    public string Initials
    {
        get
        {
            var parts = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return "T";
            return string.Concat(parts.Take(2).Select(p => char.ToUpperInvariant(p[0])));
        }
    }

    /// <summary>Indique si le tuteur a été vérifié par l'équipe TutorLocal.</summary>
    public bool IsVerified { get; set; } = true;

    /// <summary>Matières enseignées : paires (icône, libellé).</summary>
    public List<TutorSubject> Subjects { get; set; } = new();

    public double Rating { get; set; }

    public int RatingsCount { get; set; }

    /// <summary>Distance en mètres depuis le domicile de l'élève.</summary>
    public int DistanceMeters { get; set; }

    public string LocationLabel { get; set; } = string.Empty;

    /// <summary>Lieu public habituel (médiathèque, bibliothèque…).</summary>
    public string MeetingPlace { get; set; } = string.Empty;

    /// <summary>Présentation du tuteur sous forme "Profil / Formation".</summary>
    public string ProfileLine { get; set; } = string.Empty;

    public string About { get; set; } = string.Empty;

    public int HoursGiven { get; set; }

    public int ResponseTimeMinutes { get; set; }

    public string AvailabilityLabel { get; set; } = string.Empty;

    /// <summary>Derniers avis vérifiés reçus par le tuteur.</summary>
    public List<TutorReview> Reviews { get; set; } = new();

    public string DistanceLabel => DistanceMeters >= 1000
        ? $"À {DistanceMeters / 1000.0:0.#} km · {LocationLabel}"
        : $"À {DistanceMeters} m · {LocationLabel}";

    public string RatingLabel => $"{Rating:0.#} ({RatingsCount})";

    public bool Teaches(string subject) =>
        subject == "Tous" || Subjects.Any(s => s.Name.Equals(subject, StringComparison.OrdinalIgnoreCase));
}

/// <summary>Matière enseignée par un tuteur.</summary>
public class TutorSubject
{
    public TutorSubject(string name, string levels)
    {
        Name = name;
        Levels = levels;
    }

    public string Name { get; set; }

    public string Levels { get; set; }

    public override string ToString() => Name;
}

/// <summary>Avis laissé par un élève sur un tuteur.</summary>
public class TutorReview
{
    public TutorReview(string author, string authorLevel, string comment, int stars, string timeAgo)
    {
        Author = author;
        AuthorLevel = authorLevel;
        Comment = comment;
        Stars = stars;
        TimeAgo = timeAgo;
    }

    public string Author { get; set; }

    public string AuthorLevel { get; set; }

    public string Comment { get; set; }

    public int Stars { get; set; }

    public string TimeAgo { get; set; }

    public string StarsLabel => new('★', Stars);
}
