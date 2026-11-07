namespace ExtensionIcons.Core.Icons;

/// <summary>
/// Keeps a private copy of every icon file used in an override, so the registry's
/// <c>DefaultIcon</c> keeps resolving after the original file is moved or deleted.
/// </summary>
public interface IIconStore
{
    /// <summary>The folder icons are copied into.</summary>
    string Directory { get; }

    /// <summary>
    /// Copies an .ico file into the store (reusing the copy if identical content is already
    /// there) and returns the path to put in the registry.
    /// </summary>
    /// <exception cref="ArgumentException">The file is not an .ico file.</exception>
    /// <exception cref="FileNotFoundException">The source file does not exist.</exception>
    string Install(string sourcePath);
}
