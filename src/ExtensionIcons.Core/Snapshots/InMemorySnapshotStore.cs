namespace ExtensionIcons.Core.Snapshots;

/// <summary>Non-durable store for tests.</summary>
public sealed class InMemorySnapshotStore : ISnapshotStore
{
    private readonly Dictionary<Guid, Snapshot> _snapshots = [];

    public void Save(Snapshot snapshot) => _snapshots[snapshot.Id] = snapshot;

    public IReadOnlyList<Snapshot> List() => _snapshots.Values.OrderBy(s => s.Sequence).ToList();
}
