namespace ExtensionIcons.Core.Registry;

/// <summary>
/// Thin abstraction over the pieces of the registry this app needs, so
/// domain logic can run against <see cref="FakeRegistryProvider"/> in tests
/// instead of a real machine's registry. Paths are relative to the given
/// hive's root (e.g. "Software\Classes\.htm" under <see cref="RegistryHive.CurrentUser"/>,
/// or ".htm" under <see cref="RegistryHive.ClassesRoot"/>) and use registry
/// convention: case-insensitive, backslash-separated.
/// </summary>
public interface IRegistryProvider
{
    bool KeyExists(RegistryHive hive, string keyPath);

    /// <summary>Returns null if the key or the named value doesn't exist. Empty string names the key's default value.</summary>
    string? GetValue(RegistryHive hive, string keyPath, string valueName = "");

    /// <summary>Creates the key (and any missing parent keys) if it doesn't already exist.</summary>
    void SetValue(RegistryHive hive, string keyPath, string valueName, string value);

    /// <summary>No-op if the key or the value doesn't exist.</summary>
    void DeleteValue(RegistryHive hive, string keyPath, string valueName);

    /// <summary>Names of the key's values (empty string is the default value), or empty if the key doesn't exist.</summary>
    IReadOnlyList<string> GetValueNames(RegistryHive hive, string keyPath);

    void DeleteKey(RegistryHive hive, string keyPath, bool recursive = true);

    /// <summary>Immediate child key names, or empty if the key doesn't exist or has none.</summary>
    IReadOnlyList<string> GetSubKeyNames(RegistryHive hive, string keyPath);
}
