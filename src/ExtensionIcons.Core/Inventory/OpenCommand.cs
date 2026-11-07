using System.Text.RegularExpressions;

namespace ExtensionIcons.Core.Inventory;

/// <summary>Picks the program out of a <c>shell\open\command</c> string.</summary>
public static partial class OpenCommand
{
    /// <summary>
    /// File name of the executable, e.g. "msedge.exe" from
    /// <c>"C:\...\msedge.exe" --single-argument %1</c>. Handles quoted paths and unquoted
    /// paths containing spaces. Null for empty input.
    /// </summary>
    public static string? GetExecutableName(string? command)
    {
        if (string.IsNullOrWhiteSpace(command)) return null;

        var text = command.Trim();
        string executable;

        if (text[0] == '"')
        {
            var close = text.IndexOf('"', 1);
            executable = close < 0 ? text[1..] : text[1..close];
        }
        else
        {
            var match = ExecutablePath().Match(text);
            executable = match.Success ? match.Groups[1].Value : text.Split(' ', 2)[0];
        }

        var name = executable[(executable.LastIndexOfAny(['\\', '/']) + 1)..];
        return name.Length == 0 ? null : name;
    }

    [GeneratedRegex(@"^(.+?\.(?:exe|com|bat|cmd))(?=\s|$)", RegexOptions.IgnoreCase)]
    private static partial Regex ExecutablePath();
}
