namespace ExtensionIcons.Core.Overrides;

public enum IconOverrideProblem
{
    /// <summary>The extension has no registration to override.</summary>
    NotRegistered,

    /// <summary>The icon value is empty or not a "path" / "path,index" reference.</summary>
    InvalidIconLocation,

    /// <summary>The extension has no ProgID at all, so there is no icon entry to override safely.</summary>
    NoProgId,
}

/// <summary>The icon of this extension cannot be overridden. <see cref="Problem"/> says why, for localized UI text.</summary>
public sealed class IconOverrideNotSupportedException : Exception
{
    public IconOverrideNotSupportedException(IconOverrideProblem problem, string message) : base(message)
    {
        Problem = problem;
    }

    public IconOverrideProblem Problem { get; }
}
