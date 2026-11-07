namespace ExtensionIcons.Core.Catalog;

/// <summary>
/// Small built-in set of general-purpose formats that apps routinely take over
/// (see README, "The problem"). Deliberately short for the MVP; ROADMAP.md plans
/// an expanded, curated catalog for v1.0.
/// </summary>
public static class BuiltInCatalog
{
    public static IReadOnlyList<CatalogEntry> Entries { get; } =
    [
        new(".htm", "HTML document", "html"),
        new(".html", "HTML document", "html"),
        new(".xml", "XML document", "xml"),
        new(".csv", "Comma-separated values", "csv"),
        new(".json", "JSON document", "json"),
        new(".txt", "Text document", "text"),
        new(".md", "Markdown document", "markdown"),
        new(".log", "Log file", "log"),
        new(".ini", "Configuration settings", "settings"),
        new(".yaml", "YAML document", "yaml"),
        new(".yml", "YAML document", "yaml"),
    ];

    private static readonly Dictionary<string, CatalogEntry> ByExtension =
        Entries.ToDictionary(e => e.Extension, StringComparer.OrdinalIgnoreCase);

    /// <summary>Accepts ".htm" or "htm", in any case.</summary>
    public static bool TryGet(string extension, out CatalogEntry entry) =>
        ByExtension.TryGetValue(extension.StartsWith('.') ? extension : "." + extension, out entry!);
}
