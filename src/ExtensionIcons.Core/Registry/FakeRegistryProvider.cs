namespace ExtensionIcons.Core.Registry;

/// <summary>
/// In-memory stand-in for <see cref="Win32RegistryProvider"/>, used by tests
/// so domain logic can be verified without touching a real machine's
/// registry. Mirrors the real provider's behavior closely enough to catch
/// divergence bugs: nested keys are created implicitly (like
/// RegistryKey.CreateSubKey), ClassesRoot is read-only, and a non-recursive
/// delete on a key that still has children throws, same as
/// RegistryKey.DeleteSubKey.
/// </summary>
public sealed class FakeRegistryProvider : IRegistryProvider
{
    private readonly Dictionary<string, Dictionary<string, string>> _keys =
        new(StringComparer.OrdinalIgnoreCase);

    public bool KeyExists(RegistryHive hive, string keyPath) =>
        _keys.ContainsKey(ComboKey(hive, keyPath));

    public string? GetValue(RegistryHive hive, string keyPath, string valueName = "")
    {
        if (_keys.TryGetValue(ComboKey(hive, keyPath), out var values) &&
            values.TryGetValue(valueName, out var value))
        {
            return value;
        }
        return null;
    }

    public void SetValue(RegistryHive hive, string keyPath, string valueName, string value)
    {
        EnsureWritable(hive);
        EnsureKeyChainExists(hive, keyPath);
        _keys[ComboKey(hive, keyPath)][valueName] = value;
    }

    /// <summary>
    /// Test setup only: populates the read-only <see cref="RegistryHive.ClassesRoot"/>
    /// view, which <see cref="SetValue"/> deliberately refuses to write. Unlike the
    /// real merged view, this does not overlay <c>HKCU\Software\Classes</c> on top.
    /// </summary>
    public void SeedClassesRoot(string keyPath, string valueName, string value)
    {
        EnsureKeyChainExists(RegistryHive.ClassesRoot, keyPath);
        _keys[ComboKey(RegistryHive.ClassesRoot, keyPath)][valueName] = value;
    }

    public void DeleteKey(RegistryHive hive, string keyPath, bool recursive = true)
    {
        EnsureWritable(hive);
        var combo = ComboKey(hive, keyPath);
        var childPrefix = combo + "\\";

        if (!recursive && _keys.Keys.Any(k => k.StartsWith(childPrefix, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"Registry key '{keyPath}' has subkeys; a non-recursive delete is not supported.");
        }

        _keys.Remove(combo);
        foreach (var key in _keys.Keys.Where(k => k.StartsWith(childPrefix, StringComparison.OrdinalIgnoreCase)).ToList())
        {
            _keys.Remove(key);
        }
    }

    public IReadOnlyList<string> GetSubKeyNames(RegistryHive hive, string keyPath)
    {
        var normalized = Normalize(keyPath);
        var childPrefix = normalized.Length == 0 ? $"{hive}:" : $"{hive}:{normalized}\\";

        var names = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in _keys.Keys)
        {
            if (!key.StartsWith(childPrefix, StringComparison.OrdinalIgnoreCase)) continue;

            var remainder = key[childPrefix.Length..];
            var separatorIndex = remainder.IndexOf('\\');
            var childName = separatorIndex < 0 ? remainder : remainder[..separatorIndex];

            if (childName.Length > 0 && seen.Add(childName))
            {
                names.Add(childName);
            }
        }
        return names;
    }

    private void EnsureKeyChainExists(RegistryHive hive, string keyPath)
    {
        var segments = Normalize(keyPath).Split('\\', StringSplitOptions.RemoveEmptyEntries);
        var accumulated = "";
        foreach (var segment in segments)
        {
            accumulated = accumulated.Length == 0 ? segment : $"{accumulated}\\{segment}";
            var combo = $"{hive}:{accumulated}";
            if (!_keys.ContainsKey(combo))
            {
                _keys[combo] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        // keyPath itself may be empty (hive root) or already covered by the loop above.
        var rootCombo = ComboKey(hive, keyPath);
        if (!_keys.ContainsKey(rootCombo))
        {
            _keys[rootCombo] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
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

    private static string ComboKey(RegistryHive hive, string keyPath) => $"{hive}:{Normalize(keyPath)}";

    private static string Normalize(string keyPath) => keyPath.Trim('\\');
}
