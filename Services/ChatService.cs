using TutorLocal.Models;

namespace TutorLocal.Services;

/// <summary>
/// Messagerie sécurisée élève ↔ tuteur. Les réponses du tuteur sont simulées
/// (bénévole virtuel de démonstration) avec un temps de réponse réaliste.
/// </summary>
public interface IChatService
{
    /// <summary>Conversation courante (entre l'élève connecté et le tuteur sélectionné).</summary>
    Conversation Current { get; }

    Conversation GetOrCreate(string studentId, string tutorId);

    /// <summary>Ajoute le message de l'élève au fil.</summary>
    ChatMessage SendMessage(string studentId, string tutorId, string content);

    ChatMessage SendTutorMessage(string studentId, string tutorId, string content);

    ChatMessage SendAttachment(string studentId, string tutorId, string senderId, string path, string fileName);

    /// <summary>Génère la réponse du tuteur après un temps de réaction réaliste.</summary>
    Task<ChatMessage?> WaitForTutorReplyAsync(string studentId, string tutorId);
}

public class ChatService : IChatService
{
    private static readonly string[] TutorReplies =
    {
        "Bien reçu, merci ! Je regarde ton exercice et je te prépare une méthode pas à pas pour la séance. 👍",
        "Avec plaisir ! Propose-moi 2 questions précises qu'on pourra traiter ensemble au début de la séance.",
        "Parfait, c'est très clair ! On commencera par revoir la formule du cours avant de passer aux calculs. À demain ! 👋",
        "Très bonne question ! Je te conseille de refaire l'exercice 3 du chapitre, c'est exactement le même raisonnement.",
        "C'est noté ! Si tu as une fiche ou une photo de ta copie, envoie-la moi, ça m'aidera à préparer des exemples adaptés."
    };

    private int _replyIndex;

    private readonly ITutorService _tutorService;
    private readonly LocalDatabase _database;

    private readonly List<Conversation> _conversations = new();

    public Conversation Current { get; private set; } = new();

    public ChatService(ITutorService tutorService, LocalDatabase database)
    {
        _tutorService = tutorService;
        _database = database;
        var savedConversations = _database.Load<Conversation>("conversations");
        if (savedConversations.Count == 0)
            SeedConversations();
        else
            _conversations.AddRange(savedConversations);
    }

    public Conversation GetOrCreate(string studentId, string tutorId)
    {
        var conversation = _conversations.FirstOrDefault(c => c.StudentId == studentId && c.TutorId == tutorId);
        if (conversation != null)
        {
            Current = conversation;
            return conversation;
        }

        conversation = new Conversation { StudentId = studentId, TutorId = tutorId };
        _conversations.Add(conversation);
        Current = conversation;
        Save(conversation);
        return conversation;
    }

    public ChatMessage SendMessage(string studentId, string tutorId, string content)
    {
        var conversation = GetOrCreate(studentId, tutorId);
        var message = new ChatMessage
        {
            SenderId = studentId,
            IsFromUser = true,
            Content = content,
            SentAt = DateTime.Now
        };
        conversation.Messages.Add(message);

        // Simule la lecture du message par le tuteur.
        foreach (var m in conversation.Messages.Where(m => m.IsFromUser && !m.IsRead))
            m.IsRead = true;

        Save(conversation);
        return message;
    }

    public ChatMessage SendTutorMessage(string studentId, string tutorId, string content)
    {
        var conversation = GetOrCreate(studentId, tutorId);
        var message = new ChatMessage
        {
            SenderId = tutorId,
            IsFromUser = false,
            Content = content,
            SentAt = DateTime.Now
        };
        conversation.Messages.Add(message);
        foreach (var previous in conversation.Messages.Where(item => item.SenderId == studentId && !item.IsRead))
            previous.IsRead = true;
        Save(conversation);
        return message;
    }

    public ChatMessage SendAttachment(string studentId, string tutorId, string senderId, string path, string fileName)
    {
        var conversation = GetOrCreate(studentId, tutorId);
        var message = new ChatMessage
        {
            SenderId = senderId,
            IsFromUser = senderId == studentId,
            Content = $"📎 {fileName}",
            SentAt = DateTime.Now,
            HasImage = true,
            AttachmentPath = path,
            AttachmentName = fileName
        };
        conversation.Messages.Add(message);
        Save(conversation);
        return message;
    }

    public async Task<ChatMessage?> WaitForTutorReplyAsync(string studentId, string tutorId)
    {
        var tutor = _tutorService.GetById(tutorId);
        var delay = TimeSpan.FromSeconds(Math.Max(2, (tutor?.ResponseTimeMinutes ?? 10) / 6d));
        await Task.Delay(delay);

        var conversation = GetOrCreate(studentId, tutorId);
        var reply = new ChatMessage
        {
            SenderId = tutorId,
            IsFromUser = false,
            Content = TutorReplies[_replyIndex++ % TutorReplies.Length],
            SentAt = DateTime.Now
        };
        conversation.Messages.Add(reply);
        Save(conversation);
        return reply;
    }

    private void SeedConversations()
    {
        // Conversation de démonstration avec Thomas, cohérente avec la maquette.
        var thomas = new Conversation
        {
            StudentId = "alex",
            TutorId = "tutor-thomas"
        };

        foreach (var seeded in new[]
        {
            new ChatMessage
            {
                SenderId = "tutor-thomas",
                IsFromUser = false,
                Content = "Bonjour Alex ! J'ai bien vu ta demande sur le chapitre des dérivées. Tu as ton manuel scolaire ou une fiche d'exercices particulière sur laquelle tu souhaites travailler ?",
                SentAt = DateTime.Now.AddHours(-2).AddMinutes(-3),
                IsRead = true
            },
            new ChatMessage
            {
                SenderId = "alex",
                IsFromUser = true,
                Content = "Bonjour Thomas, merci beaucoup ! Oui j'ai pris en photo l'exercice 42 de la page 118 sur les tangentes, je te l'envoie tout de suite.",
                SentAt = DateTime.Now.AddHours(-2)
            },
            new ChatMessage
            {
                SenderId = "alex",
                IsFromUser = true,
                Content = "📸 Exercice_42_maths.jpg",
                SentAt = DateTime.Now.AddHours(-2).AddMinutes(1)
            },
            new ChatMessage
            {
                SenderId = "tutor-thomas",
                IsFromUser = false,
                Content = "Parfait, c'est très clair ! On commencera par revoir la formule du nombre dérivé avant de passer aux calculs. À demain ! 👋",
                SentAt = DateTime.Now.AddHours(-2).AddMinutes(3),
                IsRead = true
            }
        })
        {
            thomas.Messages.Add(seeded);
        }

        _conversations.Add(thomas);
        Current = thomas;
        Save(thomas);
    }

    private void Save(Conversation conversation) =>
        _database.Save("conversations", conversation.Id, CreateStorageCopy(conversation), ownerId: conversation.StudentId,
            relatedId: conversation.TutorId, searchText: string.Join(' ', conversation.Messages.Select(message => message.Content)),
            occurredAt: conversation.Messages.LastOrDefault()?.SentAt);

    private static Conversation CreateStorageCopy(Conversation conversation)
    {
        var copy = new Conversation
        {
            Id = conversation.Id,
            StudentId = conversation.StudentId,
            TutorId = conversation.TutorId,
            PlannedSessionDate = conversation.PlannedSessionDate,
            PlannedSessionPlace = conversation.PlannedSessionPlace
        };
        foreach (var message in conversation.Messages)
        {
            copy.Messages.Add(new ChatMessage
            {
                Id = message.Id,
                SenderId = message.SenderId,
                IsFromUser = message.SenderId == conversation.StudentId,
                Content = message.Content,
                SentAt = message.SentAt,
                HasImage = message.HasImage,
                AttachmentPath = message.AttachmentPath,
                AttachmentName = message.AttachmentName,
                IsRead = message.IsRead
            });
        }
        return copy;
    }
}
