using ExtensionIcons.Core.Registry;
using ExtensionIcons.Core.Shell;

namespace ExtensionIcons.Core.Snapshots;

/// <summary>
/// The only way this app writes to the registry: every set of writes is recorded in a
/// snapshot, and that snapshot is saved durably before the first value is touched. If a write
/// fails part-way, the already-written values are put back. Snapshots can later be restored
/// one at a time, per extension, or all at once ("fix mode").
/// </summary>
public sealed class SnapshotManager
{
    private readonly IRegistryProvider _registry;
    private readonly ISnapshotStore _store;
    private readonly IShellNotifier _shell;
    private readonly TimeProvider _time;

    public SnapshotManager(
        IRegistryProvider registry, ISnapshotStore store, IShellNotifier shell, TimeProvider? time = null)
    {
        _registry = registry;
        _store = store;
        _shell = shell;
        _time = time ?? TimeProvider.System;
    }

    /// <summary>All snapshots, oldest first, including ones already restored.</summary>
    public IReadOnlyList<Snapshot> List() => _store.List();

    /// <summary>Snapshots not yet restored, newest first.</summary>
    public IReadOnlyList<Snapshot> Active() =>
        _store.List().Where(s => !s.IsReverted).OrderByDescending(s => s.Sequence).ToList();

    public Snapshot ApplyWithSnapshot(
        string description, IReadOnlyList<string> extensions, IReadOnlyList<RegistryWrite> writes)
    {
        if (writes.Any(w => w.Hive != RegistryHive.CurrentUser))
        {
            throw new ArgumentException("Only HKEY_CURRENT_USER writes are allowed.", nameof(writes));
        }

        var changes = Capture(writes);
        var snapshot = new Snapshot(
            Guid.NewGuid(), NextSequence(), Now(), description, extensions.ToList(), changes);

        _store.Save(snapshot); // durable before any write: if we crash mid-way, the way back exists

        try
        {
            foreach (var write in writes)
            {
                _registry.SetValue(write.Hive, write.KeyPath, write.ValueName, write.Value);
            }
        }
        catch
        {
            TryRollBack(snapshot);
            throw;
        }

        _shell.NotifyAssociationsChanged();
        return snapshot;
    }

    /// <summary>Restores one snapshot. A snapshot that was already restored is left alone.</summary>
    public void Restore(Guid id)
    {
        var snapshot = _store.List().FirstOrDefault(s => s.Id == id)
            ?? throw new KeyNotFoundException($"No snapshot with id {id}.");
        if (snapshot.IsReverted) return;

        RestoreAndMark(snapshot);
        _shell.NotifyAssociationsChanged();
    }

    /// <summary>
    /// Restores every active snapshot that touched the extension, newest first. A snapshot that
    /// changed a ProgID shared with other extensions is restored as a whole, so those
    /// extensions come back too; they are listed in the returned snapshots.
    /// </summary>
    public IReadOnlyList<Snapshot> RevertExtension(string extension)
    {
        var affected = Active()
            .Where(s => s.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            .ToList();
        return RevertAll(affected);
    }

    /// <summary>Restores everything this app has changed, newest first.</summary>
    public IReadOnlyList<Snapshot> RevertAll() => RevertAll(Active());

    private IReadOnlyList<Snapshot> RevertAll(IReadOnlyList<Snapshot> snapshots)
    {
        foreach (var snapshot in snapshots)
        {
            RestoreAndMark(snapshot);
        }
        if (snapshots.Count > 0)
        {
            _shell.NotifyAssociationsChanged();
        }
        return snapshots;
    }

    private List<RegistryValueChange> Capture(IReadOnlyList<RegistryWrite> writes)
    {
        var plannedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var changes = new List<RegistryValueChange>();
        foreach (var write in writes)
        {
            changes.Add(new RegistryValueChange(
                write.Hive,
                write.KeyPath,
                write.ValueName,
                _registry.GetValue(write.Hive, write.KeyPath, write.ValueName),
                ShallowestNewKey(write, plannedKeys)));
        }
        return changes;
    }

    /// <summary>
    /// The shallowest key on the write's path that neither exists now nor will be created by an
    /// earlier write in the same batch, or null if there is none.
    /// </summary>
    private string? ShallowestNewKey(RegistryWrite write, HashSet<string> plannedKeys)
    {
        string? shallowest = null;
        var prefix = "";
        foreach (var segment in write.KeyPath.Trim('\\').Split('\\', StringSplitOptions.RemoveEmptyEntries))
        {
            prefix = prefix.Length == 0 ? segment : $@"{prefix}\{segment}";
            if (_registry.KeyExists(write.Hive, prefix) || plannedKeys.Contains($"{write.Hive}:{prefix}")) continue;

            shallowest ??= prefix;
            plannedKeys.Add($"{write.Hive}:{prefix}");
        }
        return shallowest;
    }

    private void TryRollBack(Snapshot snapshot)
    {
        try
        {
            RestoreAndMark(snapshot);
            _shell.NotifyAssociationsChanged();
        }
        catch (Exception)
        {
            // Leave the snapshot active so the user can retry the restore; the caller still
            // gets the original write failure.
        }
    }

    private void RestoreAndMark(Snapshot snapshot)
    {
        for (var i = snapshot.Changes.Count - 1; i >= 0; i--)
        {
            Undo(snapshot.Changes[i]);
        }
        _store.Save(snapshot with { RevertedUtc = Now() });
    }

    private void Undo(RegistryValueChange change)
    {
        if (change.PreviousValue is not null)
        {
            _registry.SetValue(change.Hive, change.KeyPath, change.ValueName, change.PreviousValue);
            return;
        }

        _registry.DeleteValue(change.Hive, change.KeyPath, change.ValueName);
        if (change.CreatedKeyPath is not null)
        {
            PruneEmptyKeys(change.Hive, change.KeyPath, change.CreatedKeyPath);
        }
    }

    /// <summary>
    /// Removes the key and then its parents, up to and including <paramref name="topKey"/>, but
    /// stops at the first one that still holds values or subkeys, so anything that appeared
    /// there since the snapshot is never deleted.
    /// </summary>
    private void PruneEmptyKeys(RegistryHive hive, string keyPath, string topKey)
    {
        var current = keyPath.Trim('\\');
        var top = topKey.Trim('\\');
        while (true)
        {
            if (_registry.KeyExists(hive, current))
            {
                if (_registry.GetSubKeyNames(hive, current).Count > 0 ||
                    _registry.GetValueNames(hive, current).Count > 0)
                {
                    return;
                }
                _registry.DeleteKey(hive, current, recursive: false);
            }

            if (string.Equals(current, top, StringComparison.OrdinalIgnoreCase)) return;

            var separator = current.LastIndexOf('\\');
            if (separator < 0) return;
            current = current[..separator];
        }
    }

    private long NextSequence() => _store.List().Select(s => s.Sequence).DefaultIfEmpty(0).Max() + 1;

    private DateTime Now() => _time.GetUtcNow().UtcDateTime;
}
