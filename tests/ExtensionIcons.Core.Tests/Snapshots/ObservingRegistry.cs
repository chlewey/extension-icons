using ExtensionIcons.Core.Registry;

namespace ExtensionIcons.Core.Tests.Snapshots;

/// <summary>
/// Wraps a registry provider so a test can watch writes (and make one fail) without
/// changing what the wrapped provider does.
/// </summary>
internal sealed class ObservingRegistry : IRegistryProvider
{
    private readonly IRegistryProvider _inner;

    public ObservingRegistry(IRegistryProvider inner)
    {
        _inner = inner;
    }

    /// <summary>Called with the 1-based number of the SetValue call, before it is forwarded. May throw.</summary>
    public Action<int>? BeforeSetValue { get; set; }

    public int SetValueCalls { get; private set; }

    public void SetValue(RegistryHive hive, string keyPath, string valueName, string value)
    {
        SetValueCalls++;
        BeforeSetValue?.Invoke(SetValueCalls);
        _inner.SetValue(hive, keyPath, valueName, value);
    }

    public bool KeyExists(RegistryHive hive, string keyPath) => _inner.KeyExists(hive, keyPath);

    public string? GetValue(RegistryHive hive, string keyPath, string valueName = "") =>
        _inner.GetValue(hive, keyPath, valueName);

    public void DeleteValue(RegistryHive hive, string keyPath, string valueName) =>
        _inner.DeleteValue(hive, keyPath, valueName);

    public IReadOnlyList<string> GetValueNames(RegistryHive hive, string keyPath) =>
        _inner.GetValueNames(hive, keyPath);

    public void DeleteKey(RegistryHive hive, string keyPath, bool recursive = true) =>
        _inner.DeleteKey(hive, keyPath, recursive);

    public IReadOnlyList<string> GetSubKeyNames(RegistryHive hive, string keyPath) =>
        _inner.GetSubKeyNames(hive, keyPath);
}
