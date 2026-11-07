using System.Windows.Media;
using ExtensionIcons.App.Resources;
using ExtensionIcons.App.Services;
using ExtensionIcons.Core.Catalog;
using ExtensionIcons.Core.Inventory;

namespace ExtensionIcons.App.ViewModels;

/// <summary>One grid row: an <see cref="ExtensionRegistration"/> plus its catalog default, if any.</summary>
public sealed class InventoryRowViewModel
{
    private readonly ShellIconLoader _iconLoader;
    private readonly CatalogIconProvider _catalogIcons;
    private readonly CatalogEntry? _catalogEntry;

    public InventoryRowViewModel(
        ExtensionRegistration registration, ShellIconLoader iconLoader, CatalogIconProvider catalogIcons)
    {
        Registration = registration;
        _iconLoader = iconLoader;
        _catalogIcons = catalogIcons;
        _catalogEntry = BuiltInCatalog.TryGet(registration.Extension, out var entry) ? entry : null;
    }

    public ExtensionRegistration Registration { get; }

    public string Extension => Registration.Extension;
    public string ProgId => Registration.EffectiveProgId ?? "";
    public string Description => Registration.ProgIdDescription ?? "";
    public string IconLocation => Registration.IconLocation ?? "";

    /// <summary>The program that opens the file; falls back to the type name, then the ProgID (packaged apps have no command).</summary>
    public string Owner =>
        OpenCommand.GetExecutableName(Registration.OpenCommand)
        ?? Registration.ProgIdDescription
        ?? Registration.EffectiveProgId
        ?? "";

    public string Source =>
        Registration.UserChoiceProgId is not null ? Strings.SourceUserChoice
        : Registration.ClassProgId is not null ? Strings.SourceClassDefault
        : Strings.SourceNone;

    public string CatalogFormat => _catalogEntry?.Description ?? "";

    // Loaded on first access: the grid virtualizes, so only visible rows ever extract an icon.
    private ImageSource? _currentIcon;
    private bool _currentIconLoaded;
    public ImageSource? CurrentIcon
    {
        get
        {
            if (!_currentIconLoaded)
            {
                _currentIcon = _iconLoader.Load(Registration.IconLocation);
                _currentIconLoaded = true;
            }
            return _currentIcon;
        }
    }

    public ImageSource? CatalogIcon => _catalogEntry is null ? null : _catalogIcons.Get(_catalogEntry.IconKey);
}
