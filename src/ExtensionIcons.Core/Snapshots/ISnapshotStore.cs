namespace ExtensionIcons.Core.Snapshots;

/// <summary>Durable home for snapshots. Must survive the app being closed: "fix mode" depends on it.</summary>
public interface ISnapshotStore
{
    /// <summary>Inserts the snapshot, or replaces the one with the same <see cref="Snapshot.Id"/>.</summary>
    void Save(Snapshot snapshot);

    /// <summary>All snapshots, oldest first (by <see cref="Snapshot.Sequence"/>).</summary>
    IReadOnlyList<Snapshot> List();
}
