using ExtensionIcons.Core.Registry;

namespace ExtensionIcons.Core.Inventory;

/// <summary>
/// Read-only view of how the registry associates file extensions with ProgIDs,
/// icons and open commands. Goes through <see cref="IRegistryProvider"/> so it
/// can be tested against <see cref="FakeRegistryProvider"/>.
/// </summary>
public sealed class ExtensionInventory
{
    private const string FileExtsPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts";

    private readonly IRegistryProvider _registry;

    public ExtensionInventory(IRegistryProvider registry)
    {
        _registry = registry;
    }

    /// <summary>Every registered extension (class keys starting with a dot), ordered by name.</summary>
    public IReadOnlyList<ExtensionRegistration> Enumerate() =>
        _registry.GetSubKeyNames(RegistryHive.ClassesRoot, "")
            .Where(name => name.Length > 1 && name[0] == '.')
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(Resolve)
            .ToList();

    /// <summary>Null if the extension has no class key. Accepts ".htm" or "htm".</summary>
    public ExtensionRegistration? Get(string extension)
    {
        var normalized = Normalize(extension);
        return _registry.KeyExists(RegistryHive.ClassesRoot, normalized) ? Resolve(normalized) : null;
    }

    private ExtensionRegistration Resolve(string extension)
    {
        // Registry lookups are case-insensitive; report one canonical spelling either way.
        extension = extension.ToLowerInvariant();
        var classProgId = NullIfEmpty(_registry.GetValue(RegistryHive.ClassesRoot, extension));
        var userChoice = NullIfEmpty(_registry.GetValue(
            RegistryHive.CurrentUser, $@"{FileExtsPath}\{extension}\UserChoice", "ProgId"));
        var progId = userChoice ?? classProgId;

        if (progId is null)
        {
            return new ExtensionRegistration(extension, classProgId, userChoice, null, null, null);
        }

        return new ExtensionRegistration(
            extension,
            classProgId,
            userChoice,
            IconLocation: NullIfEmpty(_registry.GetValue(RegistryHive.ClassesRoot, $@"{progId}\DefaultIcon")),
            ProgIdDescription: NullIfEmpty(_registry.GetValue(RegistryHive.ClassesRoot, progId)),
            OpenCommand: NullIfEmpty(_registry.GetValue(RegistryHive.ClassesRoot, $@"{progId}\shell\open\command")));
    }

    private static string Normalize(string extension) =>
        extension.StartsWith('.') ? extension : "." + extension;

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;
}
