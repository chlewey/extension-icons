using ExtensionIcons.Core.Registry;

namespace ExtensionIcons.Core.Snapshots;

/// <summary>
/// What one registry value looked like before a write, enough to put it back.
/// </summary>
/// <param name="PreviousValue">Null if the value did not exist.</param>
/// <param name="CreatedKeyPath">
/// Shallowest key this write brought into existence (itself or an ancestor), or null if the
/// key already existed. Restoring removes it again, once it is empty.
/// </param>
public sealed record RegistryValueChange(
    RegistryHive Hive,
    string KeyPath,
    string ValueName,
    string? PreviousValue,
    string? CreatedKeyPath);

/// <summary>
/// Taken before a set of registry writes and kept until the user discards it. Restoring a
/// snapshot returns exactly the values it recorded to their earlier state.
/// </summary>
/// <param name="Sequence">Strictly increasing; orders snapshots even when timestamps tie.</param>
/// <param name="Extensions">Extensions whose icon or association this change affects.</param>
/// <param name="RevertedUtc">Set once the snapshot has been restored.</param>
public sealed record Snapshot(
    Guid Id,
    long Sequence,
    DateTime CreatedUtc,
    string Description,
    IReadOnlyList<string> Extensions,
    IReadOnlyList<RegistryValueChange> Changes,
    DateTime? RevertedUtc = null)
{
    public bool IsReverted => RevertedUtc is not null;
}
