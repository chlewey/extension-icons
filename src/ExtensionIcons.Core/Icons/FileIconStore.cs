using System.Security.Cryptography;

namespace ExtensionIcons.Core.Icons;

/// <summary>
/// Icon copies under one folder (normally <c>%LOCALAPPDATA%\ExtensionIcons\icons</c>). Files are
/// named after the source file plus a short content hash, so different icons never collide and
/// the same icon is stored once.
/// </summary>
public sealed class FileIconStore : IIconStore
{
    private readonly string _directory;

    public FileIconStore(string directory)
    {
        _directory = Path.GetFullPath(directory);
    }

    public string Directory => _directory;

    public string Install(string sourcePath)
    {
        if (!string.Equals(Path.GetExtension(sourcePath), ".ico", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Only .ico files can be used as an icon.", nameof(sourcePath));
        }

        var bytes = File.ReadAllBytes(sourcePath); // throws FileNotFoundException when missing
        var hash = Convert.ToHexString(SHA256.HashData(bytes))[..8].ToLowerInvariant();

        // The same content under a different source name reuses the copy that is already there.
        if (System.IO.Directory.Exists(_directory) &&
            System.IO.Directory.EnumerateFiles(_directory, $"*-{hash}.ico").FirstOrDefault() is { } existing)
        {
            return existing;
        }

        var target = Path.Combine(_directory, $"{SafeName(Path.GetFileNameWithoutExtension(sourcePath))}-{hash}.ico");
        System.IO.Directory.CreateDirectory(_directory);
        File.WriteAllBytes(target, bytes);
        return target;
    }

    private static string SafeName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return cleaned.Length == 0 ? "icon" : cleaned;
    }
}
