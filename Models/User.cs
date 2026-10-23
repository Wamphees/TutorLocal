namespace TutorLocal.Models;

/// <summary>Rôle d'un compte utilisateur.</summary>
public enum UserRole
{
    Student,
    Tutor
}

/// <summary>Niveau scolaire de l'utilisateur (élève).</summary>
public enum SchoolLevel
{
    College,
    Lycee,
    Superieur
}

/// <summary>Utilisateur de l'application (élève, parent ou tuteur bénévole).</summary>
public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Student;

    public SchoolLevel Level { get; set; } = SchoolLevel.Lycee;

    /// <summary>Avatar emoji affiché dans l'application.</summary>
    public string Avatar { get; set; } = "👦";

    public string DisplayName => Role == UserRole.Tutor ? FullName : FullName.Split(' ')[0];

    public string LevelLabel => Level switch
    {
        SchoolLevel.College => "Élève - Collège",
        SchoolLevel.Lycee => "Élève - Lycée",
        SchoolLevel.Superieur => "Élève - Enseignement supérieur",
        _ => "Élève"
    };
}
