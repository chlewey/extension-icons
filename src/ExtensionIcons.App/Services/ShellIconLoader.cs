using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ExtensionIcons.Core.Inventory;

namespace ExtensionIcons.App.Services;

/// <summary>
/// Loads the icon a <c>DefaultIcon</c> registry value points at. Returns null when the value
/// has no file path (packaged-app references), the file is missing, or the index doesn't
/// resolve; the UI shows a blank for those and keeps the raw value in a tooltip.
/// </summary>
public sealed class ShellIconLoader
{
    private readonly Dictionary<string, ImageSource?> _cache = new(StringComparer.OrdinalIgnoreCase);

    public ImageSource? Load(string? iconLocation)
    {
        if (iconLocation is null) return null;
        if (_cache.TryGetValue(iconLocation, out var cached)) return cached;

        var icon = LoadUncached(iconLocation);
        _cache[iconLocation] = icon;
        return icon;
    }

    private static ImageSource? LoadUncached(string iconLocation)
    {
        if (!IconLocation.TryParse(iconLocation, out var path, out var index)) return null;

        path = Environment.ExpandEnvironmentVariables(path);
        if (!File.Exists(path)) return null;

        var large = new IntPtr[1];
        try
        {
            if (ExtractIconEx(path, index, large, null, 1) < 1 || large[0] == IntPtr.Zero) return null;

            var source = Imaging.CreateBitmapSourceFromHIcon(large[0], Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        catch (Exception ex) when (ex is ArgumentException or COMException or IOException or InvalidOperationException)
        {
            return null;
        }
        finally
        {
            if (large[0] != IntPtr.Zero) DestroyIcon(large[0]);
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint ExtractIconEx(string file, int iconIndex, IntPtr[]? large, IntPtr[]? small, uint icons);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr icon);
}
