using ExtensionIcons.Core.Inventory;
using ExtensionIcons.Core.Registry;
using ExtensionIcons.Core.Snapshots;

namespace ExtensionIcons.Core.Overrides;

/// <summary>
/// Changes the icon of a file extension in the per-user registry (<c>HKCU\Software\Classes</c>)
/// without changing which program opens it, or how. Plan first, then apply: the plan shows which
/// other extensions share the icon, and applying one that affects others needs explicit
/// acknowledgment. All writes go through <see cref="SnapshotManager"/>, so they can be undone.
/// </summary>
public sealed class IconOverrideService
{
    public const string DedicatedProgIdPrefix = "ExtensionIcons.";

    private const string UserClasses = @"Software\Classes";

    private readonly ExtensionInventory _inventory;
    private readonly IRegistryProvider _registry;
    private readonly SnapshotManager _snapshots;

    public IconOverrideService(ExtensionInventory inventory, IRegistryProvider registry, SnapshotManager snapshots)
    {
        _inventory = inventory;
        _registry = registry;
        _snapshots = snapshots;
    }

    /// <exception cref="IconOverrideNotSupportedException">The icon of this extension cannot be overridden.</exception>
    public IconOverridePlan Plan(string extension, string iconLocation)
    {
        if (!IconLocation.TryParse(iconLocation, out _, out _))
        {
            throw new IconOverrideNotSupportedException(
                IconOverrideProblem.InvalidIconLocation, $"'{iconLocation}' is not a usable icon location.");
        }

        var registration = _inventory.Get(extension)
            ?? throw new IconOverrideNotSupportedException(
                IconOverrideProblem.NotRegistered, $"Extension '{extension}' is not registered.");

        // UserChoice decides the ProgID, so a class-default ProgID of our own would never be consulted.
        if (registration.UserChoiceProgId is { } userChoiceProgId)
        {
            return PlanSharedProgId(registration, userChoiceProgId, iconLocation);
        }

        var classProgId = registration.ClassProgId
            ?? throw new IconOverrideNotSupportedException(
                IconOverrideProblem.NoProgId, $"'{registration.Extension}' has no ProgID to override.");

        var dedicated = DedicatedProgIdPrefix + registration.Extension.TrimStart('.');
        if (string.Equals(classProgId, dedicated, StringComparison.OrdinalIgnoreCase))
        {
            return PlanDedicatedProgId(registration, dedicated, [], iconLocation);
        }

        return TryCopyProgId(classProgId, dedicated, registration.Extension) is { } copy
            ? PlanDedicatedProgId(registration, dedicated, copy, iconLocation)
            : PlanSharedProgId(registration, classProgId, iconLocation);
    }

    /// <param name="acknowledgeSharedExtensions">
    /// Required when the plan changes the icon of other extensions too; the caller should only pass
    /// true after showing the user <see cref="IconOverridePlan.AffectedExtensions"/>.
    /// </param>
    public Snapshot Apply(IconOverridePlan plan, bool acknowledgeSharedExtensions = false)
    {
        if (plan.AffectsOtherExtensions && !acknowledgeSharedExtensions)
        {
            throw new InvalidOperationException(
                $"This change also affects {string.Join(", ", plan.AffectedExtensions.Where(e => !SameExtension(e, plan.Extension)))}; " +
                "pass acknowledgeSharedExtensions after confirming with the user.");
        }

        var description = plan.Strategy == IconOverrideStrategy.DedicatedProgId
            ? $"Icon for {plan.Extension} (own ProgID {plan.TargetProgId})"
            : $"Icon of ProgID {plan.TargetProgId} (for {string.Join(", ", plan.AffectedExtensions)})";

        return _snapshots.ApplyWithSnapshot(description, plan.AffectedExtensions, plan.Writes);
    }

    private IconOverridePlan PlanSharedProgId(ExtensionRegistration registration, string progId, string iconLocation)
    {
        var affected = _inventory.Enumerate()
            .Where(r => string.Equals(r.EffectiveProgId, progId, StringComparison.OrdinalIgnoreCase))
            .Select(r => r.Extension)
            .Append(registration.Extension)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new IconOverridePlan(
            registration.Extension,
            iconLocation,
            IconOverrideStrategy.ProgIdDefaultIcon,
            progId,
            affected,
            [UserWrite($@"{progId}\DefaultIcon", "", iconLocation)]);
    }

    private static IconOverridePlan PlanDedicatedProgId(
        ExtensionRegistration registration, string dedicated, List<RegistryWrite> copy, string iconLocation)
    {
        var writes = new List<RegistryWrite>(copy)
        {
            UserWrite($@"{dedicated}\DefaultIcon", "", iconLocation),
            UserWrite(registration.Extension, "", dedicated), // last: the extension only switches once the ProgID is complete
        };

        return new IconOverridePlan(
            registration.Extension, iconLocation, IconOverrideStrategy.DedicatedProgId, dedicated,
            [registration.Extension], writes);
    }

    /// <summary>
    /// Copies a ProgID so that nothing about it changes except the icon, or returns null if that
    /// cannot be guaranteed. Only a deliberately small shape is accepted: the ProgID key, its
    /// <c>DefaultIcon</c> (replaced) and <c>shell</c>; each verb key; and each verb's <c>command</c>
    /// key; all holding text values, and no <c>DelegateExecute</c>. Anything else (DDE, shell
    /// extensions, <c>CurVer</c>, drop targets, numeric flags) means a copy could silently lose
    /// behavior, so the caller falls back to changing the existing ProgID's icon instead.
    /// </summary>
    private List<RegistryWrite>? TryCopyProgId(string source, string target, string extension)
    {
        var writes = new List<RegistryWrite>();

        if (!TryCopyValues(source, target, writes, defaultIfMissing: $"{extension} file")) return null;

        foreach (var child in _registry.GetSubKeyNames(RegistryHive.ClassesRoot, source))
        {
            if (SameName(child, "DefaultIcon")) continue;
            if (!SameName(child, "shell")) return null;

            if (!TryCopyValues($@"{source}\shell", $@"{target}\shell", writes)) return null;

            foreach (var verb in _registry.GetSubKeyNames(RegistryHive.ClassesRoot, $@"{source}\shell"))
            {
                var verbSource = $@"{source}\shell\{verb}";
                var verbTarget = $@"{target}\shell\{verb}";
                if (!TryCopyValues(verbSource, verbTarget, writes)) return null;

                foreach (var part in _registry.GetSubKeyNames(RegistryHive.ClassesRoot, verbSource))
                {
                    if (!SameName(part, "command")) return null;
                    if (!TryCopyValues($@"{verbSource}\command", $@"{verbTarget}\command", writes)) return null;
                }
            }
        }

        var openCommandPath = $@"{UserClasses}\{target}\shell\open\command";
        var hasOpenCommand = writes.Any(w =>
            string.Equals(w.KeyPath, openCommandPath, StringComparison.OrdinalIgnoreCase) &&
            w.ValueName.Length == 0 && w.Value.Length > 0);
        return hasOpenCommand ? writes : null;
    }

    /// <summary>
    /// Queues a copy of every value of <paramref name="source"/> under <paramref name="target"/>.
    /// False if a value cannot be copied faithfully (non-text, or DelegateExecute).
    /// </summary>
    private bool TryCopyValues(string source, string target, List<RegistryWrite> writes, string? defaultIfMissing = null)
    {
        var names = _registry.GetValueNames(RegistryHive.ClassesRoot, source);
        foreach (var name in names)
        {
            if (SameName(name, "DelegateExecute")) return false;

            var value = _registry.GetValue(RegistryHive.ClassesRoot, source, name);
            if (value is null) return false; // listed but not text: cannot be copied as a string

            writes.Add(UserWrite(target, name, value));
        }

        if (defaultIfMissing is not null && !names.Contains("", StringComparer.Ordinal))
        {
            writes.Add(UserWrite(target, "", defaultIfMissing));
        }
        return true;
    }

    private static RegistryWrite UserWrite(string classesKeyPath, string valueName, string value) =>
        new(RegistryHive.CurrentUser, $@"{UserClasses}\{classesKeyPath}", valueName, value);

    private static bool SameName(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static bool SameExtension(string a, string b) => SameName(a, b);
}
