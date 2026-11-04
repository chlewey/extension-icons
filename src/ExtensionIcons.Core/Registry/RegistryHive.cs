namespace ExtensionIcons.Core.Registry;

/// <summary>
/// Hives this app is allowed to address. HKEY_LOCAL_MACHINE is deliberately
/// absent: the MVP only ever writes to HKEY_CURRENT_USER (see ROADMAP.md,
/// "per-user only for MVP"); machine-wide writes are a v1.0 elevated-helper
/// concern and should get their own explicit hive when that lands.
/// </summary>
public enum RegistryHive
{
    /// <summary>Read-only merged view (HKLM\Software\Classes overlaid by HKCU\Software\Classes).</summary>
    ClassesRoot,

    /// <summary>HKEY_CURRENT_USER. The only hive this app writes to in the MVP.</summary>
    CurrentUser,
}
