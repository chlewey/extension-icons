namespace ExtensionIcons.Core.Catalog;

/// <summary>
/// A "contested" format the app ships a default icon for, independent of any
/// installed application.
/// </summary>
/// <param name="Extension">Including the leading dot, lower case.</param>
/// <param name="Description">English format name; will move to .resx for localization.</param>
/// <param name="IconKey">
/// Name of the bundled icon asset. Several extensions can share one key (.htm/.html).
/// The assets themselves are not bundled yet.
/// </param>
public sealed record CatalogEntry(string Extension, string Description, string IconKey);
