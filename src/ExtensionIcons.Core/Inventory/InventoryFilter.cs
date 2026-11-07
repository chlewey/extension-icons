using ExtensionIcons.Core.Catalog;

namespace ExtensionIcons.Core.Inventory;

/// <summary>Which inventory rows to show. All active conditions must hold.</summary>
/// <param name="Search">Case-insensitive text matched against extension, ProgID, description and open command.</param>
/// <param name="CatalogOnly">Only extensions in <see cref="BuiltInCatalog"/>.</param>
/// <param name="UserChoiceOnly">Only extensions where the user has a <c>UserChoice</c> override.</param>
public sealed record InventoryFilter(string? Search = null, bool CatalogOnly = false, bool UserChoiceOnly = false)
{
    public bool Matches(ExtensionRegistration registration)
    {
        if (CatalogOnly && !BuiltInCatalog.TryGet(registration.Extension, out _)) return false;
        if (UserChoiceOnly && registration.UserChoiceProgId is null) return false;

        var search = Search?.Trim();
        if (string.IsNullOrEmpty(search)) return true;

        return Contains(registration.Extension, search)
            || Contains(registration.EffectiveProgId, search)
            || Contains(registration.ProgIdDescription, search)
            || Contains(registration.OpenCommand, search);
    }

    private static bool Contains(string? haystack, string needle) =>
        haystack?.Contains(needle, StringComparison.OrdinalIgnoreCase) == true;
}
