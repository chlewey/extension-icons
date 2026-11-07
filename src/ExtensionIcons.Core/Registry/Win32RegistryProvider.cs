using Microsoft.Win32;

namespace ExtensionIcons.Core.Registry;

/// <summary>
/// Real registry access. Exercised manually against a live Windows 11
/// machine (see docs/spikes/001-icon-override-mechanism.md) rather than in
/// the automated test suite — ExtensionIcons.Core.Tests runs only against
/// <see cref="FakeRegistryProvider"/>, per ROADMAP.md's testing approach.
/// </summary>
public sealed class Win32RegistryProvider : IRegistryProvider
{
    public bool KeyExists(RegistryHive hive, string keyPath)
    {
        using var key = Root(hive).OpenSubKey(keyPath);
        return key is not null;
    }

    public string? GetValue(RegistryHive hive, string keyPath, string valueName = "")
    {
        using var key = Root(hive).OpenSubKey(keyPath);
        return key?.GetValue(valueName) as string;
    }

    public void SetValue(RegistryHive hive, string keyPath, string valueName, string value)
    {
        EnsureWritable(hive);
        using var key = Root(hive).CreateSubKey(keyPath, writable: true)
            ?? throw new InvalidOperationException($"Could not create or open key '{keyPath}'.");
        key.SetValue(valueName, value, RegistryValueKind.String);
    }

    public void DeleteValue(RegistryHive hive, string keyPath, string valueName)
    {
        EnsureWritable(hive);
        using var key = Root(hive).OpenSubKey(keyPath, writable: true);
        key?.DeleteValue(valueName, throwOnMissingValue: false);
    }

    public IReadOnlyList<string> GetValueNames(RegistryHive hive, string keyPath)
    {
        using var key = Root(hive).OpenSubKey(keyPath);
        return key?.GetValueNames() ?? [];
    }

    public void DeleteKey(RegistryHive hive, string keyPath, bool recursive = true)
    {
        EnsureWritable(hive);
        var root = Root(hive);
        if (recursive)
        {
            root.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false);
        }
        else
        {
            root.DeleteSubKey(keyPath, throwOnMissingSubKey: false);
        }
    }

    public IReadOnlyList<string> GetSubKeyNames(RegistryHive hive, string keyPath)
    {
        using var key = Root(hive).OpenSubKey(keyPath);
        return key?.GetSubKeyNames() ?? [];
    }

    private static void EnsureWritable(RegistryHive hive)
    {
        if (hive == RegistryHive.ClassesRoot)
        {
            throw new NotSupportedException(
                "RegistryHive.ClassesRoot is a read-only merged view in this app. " +
                "Write to RegistryHive.CurrentUser instead (see ROADMAP.md, per-user only for MVP).");
        }
    }

    private static RegistryKey Root(RegistryHive hive) => hive switch
    {
        RegistryHive.ClassesRoot => Microsoft.Win32.Registry.ClassesRoot,
        RegistryHive.CurrentUser => Microsoft.Win32.Registry.CurrentUser,
        _ => throw new ArgumentOutOfRangeException(nameof(hive), hive, null),
    };
}
