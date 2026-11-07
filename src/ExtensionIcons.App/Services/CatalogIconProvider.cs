using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ExtensionIcons.App.Services;

/// <summary>Loads the bundled catalog icon for an icon key (Assets/Icons/&lt;key&gt;.png).</summary>
public sealed class CatalogIconProvider
{
    private readonly Dictionary<string, ImageSource?> _cache = new(StringComparer.OrdinalIgnoreCase);

    public ImageSource? Get(string iconKey)
    {
        if (_cache.TryGetValue(iconKey, out var cached)) return cached;

        ImageSource? image = null;
        try
        {
            var bitmap = new BitmapImage(new Uri($"pack://application:,,,/Assets/Icons/{iconKey}.png", UriKind.Absolute));
            bitmap.Freeze();
            image = bitmap;
        }
        catch (IOException)
        {
            // Missing asset: show no icon rather than failing the whole grid.
        }

        _cache[iconKey] = image;
        return image;
    }
}
