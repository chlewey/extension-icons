using ExtensionIcons.Core.Registry;

namespace ExtensionIcons.Core.Snapshots;

/// <summary>One registry string value this app intends to set. Empty <paramref name="ValueName"/> is the key's default value.</summary>
public sealed record RegistryWrite(RegistryHive Hive, string KeyPath, string ValueName, string Value);
