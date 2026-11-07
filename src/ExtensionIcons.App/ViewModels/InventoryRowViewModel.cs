using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using ExtensionIcons.App.Resources;
using ExtensionIcons.App.Services;
using ExtensionIcons.Core.Catalog;
using ExtensionIcons.Core.Inventory;

namespace ExtensionIcons.App.ViewModels;

/// <summary>One grid row: an <see cref="ExtensionRegistration"/> plus its catalog default, if any.</summary>
public sealed class InventoryRowViewModel : ObservableObject
{
    private readonly ShellIconLoader _iconLoader;
    private readonly CatalogIconProvider _catalogIcons;
    private readonly CatalogEntry? _catalogEntry;

    private ExtensionRegistration _registration;
    private bool _isOverridden;

    // Loaded on first access: the grid virtualizes, so only visible rows ever extract an icon.
    private ImageSource? _currentIcon;
    private bool _currentIconLoaded;

    public InventoryRowViewModel(
        ExtensionRegistration registration, ShellIconLoader iconLoader, CatalogIconProvider catalogIcons)
    {
        _registration = registration;
        _iconLoader = iconLoader;
        _catalogIcons = catalogIcons;
        _catalogEntry = BuiltInCatalog.TryGet(registration.Extension, out var entry) ? entry : null;
    }

    public ExtensionRegistration Registration => _registration;

    public string Extension => _registration.Extension;
    public string ProgId => _registration.EffectiveProgId ?? "";
    public string Description => _registration.ProgIdDescription ?? "";
    public string IconLocation => _registration.IconLocation ?? "";

    /// <summary>The program that opens the file; falls back to the type name, then the ProgID (packaged apps have no command).</summary>
    public string Owner =>
        OpenCommand.GetExecutableName(_registration.OpenCommand)
        ?? _registration.ProgIdDescription
        ?? _registration.EffectiveProgId
        ?? "";

    public string Source =>
        IsOverridden ? Strings.SourceOverridden
        : _registration.UserChoiceProgId is not null ? Strings.SourceUserChoice
        : _registration.ClassProgId is not null ? Strings.SourceClassDefault
        : Strings.SourceNone;

    public string CatalogFormat => _catalogEntry?.Description ?? "";

    public bool HasCatalogIcon => _catalogEntry is not null;
    public string? CatalogIconKey => _catalogEntry?.IconKey;
    public ImageSource? CatalogIcon => _catalogEntry is null ? null : _catalogIcons.Get(_catalogEntry.IconKey);

    /// <summary>True while an active snapshot records a change Extension Icons made to this extension.</summary>
    public bool IsOverridden
    {
        get => _isOverridden;
        set
        {
            if (SetProperty(ref _isOverridden, value))
            {
                OnPropertyChanged(nameof(Source));
            }
        }
    }

    public ImageSource? CurrentIcon
    {
        get
        {
            if (!_currentIconLoaded)
            {
                _currentIcon = _iconLoader.Load(_registration.IconLocation);
                _currentIconLoaded = true;
            }
            return _currentIcon;
        }
    }

    /// <summary>Re-reads what the registry says, after an override or a restore.</summary>
    public void Update(ExtensionRegistration registration)
    {
        _registration = registration;
        _currentIconLoaded = false;
        OnPropertyChanged(string.Empty); // everything derived from the registration
    }
}
