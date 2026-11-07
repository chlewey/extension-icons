namespace ExtensionIcons.Core.Inventory;

/// <summary>
/// What the registry currently says about one file extension: which ProgID it
/// resolves to, where that ProgID's icon comes from, and what opens it. Read-only
/// snapshot; nothing here writes back.
/// </summary>
/// <param name="Extension">Including the leading dot, e.g. ".htm".</param>
/// <param name="ClassProgId">Default value of the extension's own class key, if any.</param>
/// <param name="UserChoiceProgId">
/// ProgID in the per-user <c>UserChoice</c> key, if the user has ever picked a default app.
/// When present it takes precedence over <paramref name="ClassProgId"/> for the open action;
/// whether it also governs the icon is still an open question (spike 001).
/// </param>
/// <param name="IconLocation">Default value of <c>&lt;ProgID&gt;\DefaultIcon</c>, e.g. "shell32.dll,41" or "C:\App\app.exe,0".</param>
/// <param name="ProgIdDescription">Default value of the ProgID key, i.e. the friendly type name Explorer shows.</param>
/// <param name="OpenCommand">Raw <c>shell\open\command</c> string of the effective ProgID.</param>
public sealed record ExtensionRegistration(
    string Extension,
    string? ClassProgId,
    string? UserChoiceProgId,
    string? IconLocation,
    string? ProgIdDescription,
    string? OpenCommand)
{
    public string? EffectiveProgId => UserChoiceProgId ?? ClassProgId;
}
