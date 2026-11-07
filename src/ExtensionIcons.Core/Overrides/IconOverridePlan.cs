using ExtensionIcons.Core.Snapshots;

namespace ExtensionIcons.Core.Overrides;

/// <summary>How an icon override is carried out; see docs/spikes/002-icon-resolution.md.</summary>
public enum IconOverrideStrategy
{
    /// <summary>
    /// The extension gets its own ProgID as its class default, an exact copy of the old one apart
    /// from the icon. Affects only that extension. Used only when it has no <c>UserChoice</c> and the
    /// old ProgID is simple enough to copy without losing anything (no DDE, DelegateExecute, shell
    /// extensions or non-text values).
    /// </summary>
    DedicatedProgId,

    /// <summary>
    /// Sets the <c>DefaultIcon</c> of the ProgID the extension already resolves to. Never affects how
    /// files open, but changes the icon of every extension that resolves to that ProgID.
    /// </summary>
    ProgIdDefaultIcon,
}

/// <summary>What an icon override would do, computed without changing anything.</summary>
/// <param name="TargetProgId">The ProgID that is created (dedicated) or whose icon is replaced (shared).</param>
/// <param name="AffectedExtensions">Every extension whose icon changes, including the requested one.</param>
public sealed record IconOverridePlan(
    string Extension,
    string IconLocation,
    IconOverrideStrategy Strategy,
    string TargetProgId,
    IReadOnlyList<string> AffectedExtensions,
    IReadOnlyList<RegistryWrite> Writes)
{
    /// <summary>True when applying it also changes the icon of extensions other than the requested one.</summary>
    public bool AffectsOtherExtensions => AffectedExtensions.Count > 1;
}
