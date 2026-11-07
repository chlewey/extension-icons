namespace ExtensionIcons.Core.Inventory;

/// <summary>
/// Parser for the <c>DefaultIcon</c> registry convention: <c>path</c> or <c>path,index</c>,
/// optionally quoted. A negative index names a resource id instead of a position.
/// Environment variables in the path are left unexpanded; expanding and loading are the
/// caller's job.
/// </summary>
public static class IconLocation
{
    /// <summary>
    /// False for empty values and for packaged-app resource references
    /// (<c>@{Package?ms-resource://...}</c>), which have no file path to load from.
    /// </summary>
    public static bool TryParse(string? value, out string path, out int index)
    {
        path = "";
        index = 0;

        if (string.IsNullOrWhiteSpace(value)) return false;

        var text = value.Trim();
        if (text[0] == '@') return false;

        string indexPart;
        if (text[0] == '"')
        {
            var close = text.IndexOf('"', 1);
            if (close < 0) return false;
            path = text[1..close];
            indexPart = text[(close + 1)..].Trim().TrimStart(',');
        }
        else
        {
            var comma = text.LastIndexOf(',');
            if (comma >= 0 && int.TryParse(text[(comma + 1)..].Trim(), out var parsed))
            {
                path = text[..comma].Trim();
                index = parsed;
                return path.Length > 0;
            }
            path = text;
            indexPart = "";
        }

        if (indexPart.Length > 0 && !int.TryParse(indexPart, out index)) return false;
        return path.Length > 0;
    }
}
