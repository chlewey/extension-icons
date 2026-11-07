using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExtensionIcons.Core.Snapshots;

/// <summary>
/// One JSON file per snapshot in a folder (normally <c>%LOCALAPPDATA%\ExtensionIcons\snapshots</c>).
/// Files are written to a temp name and moved into place, so a crash mid-write cannot leave a
/// half-written snapshot. An unreadable file raises an error instead of being skipped: silently
/// losing a snapshot would make "fix mode" untrustworthy.
/// </summary>
public sealed class FileSnapshotStore : ISnapshotStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _directory;

    public FileSnapshotStore(string directory)
    {
        _directory = directory;
    }

    public void Save(Snapshot snapshot)
    {
        Directory.CreateDirectory(_directory);
        var path = PathFor(snapshot.Id);
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(snapshot, Options));
        File.Move(temp, path, overwrite: true);
    }

    public IReadOnlyList<Snapshot> List()
    {
        if (!Directory.Exists(_directory)) return [];

        var snapshots = new List<Snapshot>();
        foreach (var file in Directory.EnumerateFiles(_directory, "*.json"))
        {
            try
            {
                var snapshot = JsonSerializer.Deserialize<Snapshot>(File.ReadAllText(file), Options)
                    ?? throw new InvalidDataException("empty snapshot");
                snapshots.Add(snapshot);
            }
            catch (Exception ex) when (ex is JsonException or InvalidDataException)
            {
                throw new InvalidDataException($"Snapshot file '{file}' is unreadable: {ex.Message}", ex);
            }
        }
        return snapshots.OrderBy(s => s.Sequence).ToList();
    }

    private string PathFor(Guid id) => Path.Combine(_directory, $"{id:N}.json");
}
