using System.Text.Json;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
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
    private const string LegacyCurrentUserKey = "tutorlocal_current_user";
    private const string CurrentUserRecord = "auth.currentUserId";
    private const string UserRecord = "users";
    private const int PasswordIterations = 120_000;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly List<User> _users = new();
    private readonly Dictionary<string, StoredAccount> _credentials = new();
    private readonly LocalDatabase _database;

    private readonly List<SeedUser> _seedAccounts = new()
    {
        // Compte de démonstration : page de connexion de la maquette.
        // L'Id "alex" est celui utilisé par les données de démonstration (séances, chats...).
        new SeedUser("alex", "Alex Dupont", "alex@exemple.fr", "tutor123", UserRole.Student, SchoolLevel.Lycee)
    };

    public AuthService(LocalDatabase database)
    {
        _database = database;
        LoadUsers();
    }

    public User? CurrentUser { get; private set; }

    private sealed record SeedUser(string Id, string Name, string Email, string Password, UserRole Role, SchoolLevel Level);

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

        var user = _users.FirstOrDefault(u => u.Email.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        if (user != null)
        {
            if (!_credentials.TryGetValue(user.Id, out var credential) || !VerifyPassword(password, credential))
                return AuthResult.Fail("Mot de passe incorrect.");
        }
        else
        {
            var seed = _seedAccounts.FirstOrDefault(account =>
                account.Email.Equals(normalized, StringComparison.OrdinalIgnoreCase));
            if (seed == null)
                return AuthResult.Fail("Aucun compte n'existe avec cet email.");
            if (!seed.Password.Equals(password, StringComparison.Ordinal))
                return AuthResult.Fail("Mot de passe incorrect.");
            user = new User
            {
                Id = seed.Id,
                FullName = seed.Name,
                Email = seed.Email,
                Role = seed.Role,
                Level = seed.Level
            };
        }

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
            Role = role,
            Level = level
        };

        _users.Add(user);
        SaveAccount(user, password);

        OpenSession(user, rememberMe: false);
        return AuthResult.Ok();
    }

    public bool TryRestoreSession()
    {
        var storedId = _database.Get<string>("state", CurrentUserRecord);
        if (string.IsNullOrWhiteSpace(storedId))
            return false;

        var user = _users.FirstOrDefault(candidate => candidate.Id == storedId);
        if (user == null)
        {
            var seed = _seedAccounts.FirstOrDefault(candidate => candidate.Id == storedId);
            if (seed == null)
                return false;
            user = new User
            {
                Id = seed.Id,
                FullName = seed.Name,
                Email = seed.Email,
                Role = seed.Role,
                Level = seed.Level
            };
        }

        CurrentUser = user;
        return true;
    }

    public void Logout()
    {
        CurrentUser = null;
        _database.Delete("state", CurrentUserRecord);
    }

    private void OpenSession(User user, bool rememberMe)
    {
        CurrentUser = user;

        if (rememberMe)
            _database.Save("state", CurrentUserRecord, user.Id);
        else
            _database.Delete("state", CurrentUserRecord);
    }

    private void LoadUsers()
    {
        foreach (var account in _database.Load<StoredAccount>(UserRecord))
            AddAccount(account);

        var stored = Preferences.Get(UsersKey, string.Empty);
        if (string.IsNullOrWhiteSpace(stored))
            return;

        try
        {
            var legacyUsers = JsonSerializer.Deserialize<List<LegacyStoredUser>>(stored, JsonOptions) ?? new();
            foreach (var legacy in legacyUsers)
            {
                if (_users.Any(user => user.Id == legacy.Id ||
                                       user.Email.Equals(legacy.Email, StringComparison.OrdinalIgnoreCase)) ||
                    _seedAccounts.Any(seed => seed.Id == legacy.Id ||
                                              seed.Email.Equals(legacy.Email, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var user = new User
                {
                    Id = legacy.Id,
                    FullName = legacy.FullName,
                    Email = legacy.Email,
                    Role = legacy.Role,
                    Level = legacy.Level
                };
                _users.Add(user);
                SaveAccount(user, legacy.Password);
            }

            Preferences.Remove(UsersKey);
        }
        catch (JsonException exception)
        {
            System.Diagnostics.Debug.WriteLine($"Could not migrate legacy account preferences: {exception}");
        }

        var legacyCurrentId = Preferences.Get(LegacyCurrentUserKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(legacyCurrentId))
        {
            var exists = _users.Any(user => user.Id == legacyCurrentId) ||
                         _seedAccounts.Any(seed => seed.Id == legacyCurrentId);
            if (exists && _database.Get<string>("state", CurrentUserRecord) == null)
                _database.Save("state", CurrentUserRecord, legacyCurrentId);
            Preferences.Remove(LegacyCurrentUserKey);
        }
    }

    private void AddAccount(StoredAccount account)
    {
        var user = new User
        {
            Id = account.Id,
            FullName = account.FullName,
            Email = account.Email,
            Role = account.Role,
            Level = account.Level
        };
        _users.Add(user);
        _credentials[user.Id] = account;
    }

    private void SaveAccount(User user, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var account = new StoredAccount
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PasswordSalt = Convert.ToBase64String(salt),
            PasswordHash = Convert.ToBase64String(HashPassword(password, salt)),
            Role = user.Role,
            Level = user.Level
        };
        _credentials[user.Id] = account;
        _database.Save(UserRecord, user.Id, account, searchText: $"{user.FullName} {user.Email}");
    }

    private static byte[] HashPassword(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, PasswordIterations, HashAlgorithmName.SHA256, 32);

    private static bool VerifyPassword(string password, StoredAccount account)
    {
        var salt = Convert.FromBase64String(account.PasswordSalt);
        var expected = Convert.FromBase64String(account.PasswordHash);
        var actual = HashPassword(password, salt);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private sealed class StoredAccount
    {
        public StoredAccount()
        {
        }

        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordSalt { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public SchoolLevel Level { get; set; }
        public string Avatar { get; set; } = string.Empty;
    }

    private sealed class LegacyStoredUser
    {
        public LegacyStoredUser()
        {
        }

        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public SchoolLevel Level { get; set; }
        public string Avatar { get; set; } = string.Empty;
    }
}
