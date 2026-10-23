using System.Text.Json;
using System.Text.RegularExpressions;
using TutorLocal.Models;

namespace TutorLocal.Services;

/// <summary>
/// Service d'authentification et de gestion du « compte » de l'utilisateur.
/// Persiste localement les comptes (Preferences) : rôle de "backend" de démonstration.
/// </summary>
public interface IAuthService
{
    /// <summary>Utilisateur connecté, ou null si visiteur anonyme.</summary>
    User? CurrentUser { get; }

    /// <summary>Indique si un compte est enregistré avec cet email.</summary>
    bool EmailExists(string email);

    /// <summary>Connexion : valide les identifiants puis ouvre la session.</summary>
    AuthResult Login(string email, string password, bool rememberMe);

    /// <summary>Inscription d'un nouveau compte, puis ouverture automatique de la session.</summary>
    AuthResult Register(string fullName, string email, string password, SchoolLevel level, UserRole role, bool termsAccepted);

    /// <summary>Tente de restaurer une session persistée (option « se souvenir de moi »).</summary>
    bool TryRestoreSession();

    /// <summary>Ferme la session courante.</summary>
    void Logout();
}

public class AuthResult
{
    public bool Success { get; private init; }

    public string Error { get; private init; } = string.Empty;

    public static AuthResult Ok() => new() { Success = true };

    public static AuthResult Fail(string error) => new() { Success = false, Error = error };
}

public class AuthService : IAuthService
{
    private const string UsersKey = "tutorlocal_users";
    private const string CurrentUserKey = "tutorlocal_current_user";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly List<User> _users = new();

    private readonly List<SeedUser> _seedAccounts = new()
    {
        // Compte de démonstration : page de connexion de la maquette.
        // L'Id "alex" est celui utilisé par les données de démonstration (séances, chats...).
        new SeedUser("alex", "Alex Dupont", "alex@exemple.fr", "tutor123", UserRole.Student, SchoolLevel.Lycee, "👦")
    };

    public AuthService()
    {
        LoadUsers();
    }

    public User? CurrentUser { get; private set; }

    private sealed record SeedUser(string Id, string Name, string Email, string Password, UserRole Role, SchoolLevel Level, string Avatar);

    public bool EmailExists(string email) =>
        _users.Any(u => u.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase)) ||
        _seedAccounts.Any(s => s.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase));

    public AuthResult Login(string email, string password, bool rememberMe)
    {
        if (string.IsNullOrWhiteSpace(email))
            return AuthResult.Fail("Veuillez saisir votre adresse email.");
        if (string.IsNullOrWhiteSpace(password))
            return AuthResult.Fail("Veuillez saisir votre mot de passe.");

        var normalized = email.Trim();

        var user = _users.FirstOrDefault(u => u.Email.Equals(normalized, StringComparison.OrdinalIgnoreCase))
                   ?? _seedAccounts.Select(s => new User
                       {
                           Id = s.Id,
                           FullName = s.Name,
                           Email = s.Email,
                           Password = s.Password,
                           Role = s.Role,
                           Level = s.Level,
                           Avatar = s.Avatar
                       })
                       .FirstOrDefault(u => u.Email.Equals(normalized, StringComparison.OrdinalIgnoreCase));

        if (user == null)
            return AuthResult.Fail("Aucun compte n'existe avec cet email.");

        if (!user.Password.Equals(password))
            return AuthResult.Fail("Mot de passe incorrect.");

        OpenSession(user, rememberMe);
        return AuthResult.Ok();
    }

    public AuthResult Register(string fullName, string email, string password, SchoolLevel level, UserRole role, bool termsAccepted)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length < 3)
            return AuthResult.Fail("Veuillez saisir votre nom complet (3 caractères minimum).");

        if (string.IsNullOrWhiteSpace(email) || !Regex.IsMatch(email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            return AuthResult.Fail("L'adresse email n'est pas valide.");

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return AuthResult.Fail("Le mot de passe doit contenir au moins 6 caractères.");

        if (!termsAccepted)
            return AuthResult.Fail("Veuillez accepter la charte d'entraide pour créer votre compte.");

        if (EmailExists(email))
            return AuthResult.Fail("Un compte existe déjà avec cet email.");

        var user = new User
        {
            FullName = fullName.Trim(),
            Email = email.Trim(),
            Password = password,
            Role = role,
            Level = level,
            Avatar = role == UserRole.Tutor ? "🤝" : "👦"
        };

        _users.Add(user);
        SaveUsers();

        OpenSession(user, rememberMe: false);
        return AuthResult.Ok();
    }

    public bool TryRestoreSession()
    {
        var storedId = Preferences.Get(CurrentUserKey, string.Empty);
        if (string.IsNullOrWhiteSpace(storedId))
            return false;

        var stored = Preferences.Get(UsersKey, "[]");
        try
        {
            var users = JsonSerializer.Deserialize<List<StoredUser>>(stored, JsonOptions) ?? new();
            var match = users.FirstOrDefault(u => u.Id == storedId);
            if (match == null)
                return false;

            CurrentUser = new User
            {
                Id = match.Id,
                FullName = match.FullName,
                Email = match.Email,
                Password = match.Password,
                Role = match.Role,
                Level = match.Level,
                Avatar = string.IsNullOrEmpty(match.Avatar) ? "👦" : match.Avatar
            };
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Logout()
    {
        CurrentUser = null;
        Preferences.Remove(CurrentUserKey);
    }

    private void OpenSession(User user, bool rememberMe)
    {
        CurrentUser = user;

        if (rememberMe)
        {
            Preferences.Set(CurrentUserKey, user.Id);
            SaveUsers();
        }
        else
        {
            Preferences.Remove(CurrentUserKey);
        }
    }

    private void LoadUsers()
    {
        var stored = Preferences.Get(UsersKey, string.Empty);
        if (string.IsNullOrWhiteSpace(stored))
            return;

        try
        {
            var users = JsonSerializer.Deserialize<List<StoredUser>>(stored, JsonOptions) ?? new();
            _users.AddRange(users.Select(u => new User
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Password = u.Password,
                Role = u.Role,
                Level = u.Level,
                Avatar = string.IsNullOrEmpty(u.Avatar) ? "👦" : u.Avatar
            }));
        }
        catch
        {
            // Stockage corrompu : on repart d'une base vide.
        }
    }

    private void SaveUsers()
    {
        // Les comptes de démonstration sont persistés en premier.
        var toStore = _seedAccounts
            .Select(s => new StoredUser(s.Id, s.Name, s.Email, s.Password, s.Role, s.Level, s.Avatar))
            .Concat(_users.Select(u => new StoredUser(u.Id, u.FullName, u.Email, u.Password, u.Role, u.Level, u.Avatar)))
            .ToList();

        Preferences.Set(UsersKey, JsonSerializer.Serialize(toStore, JsonOptions));
    }

    private sealed record StoredUser(string Id, string FullName, string Email, string Password, UserRole Role, SchoolLevel Level, string Avatar);
}
