using System.Text.Json;
using SQLite;

namespace TutorLocal.Services;

/// <summary>SQLite storage local to this installation; no network or remote server is used.</summary>
public sealed class LocalDatabase
{
    private const int CurrentSchemaVersion = 1;
    private readonly object _sync = new();
    private readonly SQLiteConnection _connection;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public LocalDatabase()
    {
        SQLitePCL.Batteries_V2.Init();
        var path = Path.Combine(FileSystem.AppDataDirectory, "tutorlocal.db3");
        _connection = new SQLiteConnection(path, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);
        _connection.CreateTable<StoredRecord>();

        var version = _connection.ExecuteScalar<int>("PRAGMA user_version");
        if (version > CurrentSchemaVersion)
            throw new InvalidOperationException($"SQLite schema version {version} is newer than supported version {CurrentSchemaVersion}.");
        if (version < CurrentSchemaVersion)
            _connection.Execute($"PRAGMA user_version = {CurrentSchemaVersion}");
    }

    public IReadOnlyList<T> Load<T>(string kind, string? ownerId = null)
    {
        lock (_sync)
        {
            var records = _connection.Table<StoredRecord>().Where(record => record.Kind == kind);
            if (ownerId != null)
                records = records.Where(record => record.OwnerId == ownerId);

            return records.ToList()
                .Select(record => JsonSerializer.Deserialize<T>(record.Payload, JsonOptions)
                    ?? throw new InvalidDataException($"SQLite record '{record.Key}' contains an empty payload."))
                .ToList();
        }
    }

    public T? Get<T>(string kind, string id)
    {
        lock (_sync)
        {
            var record = _connection.Find<StoredRecord>(RecordKey(kind, id));
            return record == null
                ? default
                : JsonSerializer.Deserialize<T>(record.Payload, JsonOptions)
                    ?? throw new InvalidDataException($"SQLite record '{record.Key}' contains an empty payload.");
        }
    }

    public void Save<T>(string kind, string id, T value, string ownerId = "", string relatedId = "",
        string searchText = "", string status = "", DateTime? occurredAt = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        lock (_sync)
        {
            _connection.InsertOrReplace(new StoredRecord
            {
                Key = RecordKey(kind, id),
                Kind = kind,
                OwnerId = ownerId,
                RelatedId = relatedId,
                SearchText = searchText,
                Status = status,
                OccurredAt = occurredAt?.Ticks ?? 0,
                Payload = JsonSerializer.Serialize(value, JsonOptions)
            });
        }
    }

    public void Delete(string kind, string id)
    {
        lock (_sync)
            _connection.Delete<StoredRecord>(RecordKey(kind, id));
    }

    public void InTransaction(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        lock (_sync)
            _connection.RunInTransaction(action);
    }

    private static string RecordKey(string kind, string id) => $"{kind}:{id}";

    [Table("LocalRecords")]
    private sealed class StoredRecord
    {
        public StoredRecord()
        {
        }

        [PrimaryKey]
        public string Key { get; set; } = string.Empty;

        [Indexed]
        public string Kind { get; set; } = string.Empty;

        [Indexed]
        public string OwnerId { get; set; } = string.Empty;

        [Indexed]
        public string RelatedId { get; set; } = string.Empty;

        public string SearchText { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public long OccurredAt { get; set; }

        public string Payload { get; set; } = string.Empty;
    }
}
