using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Security;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExtensionIcons.App.Resources;
using ExtensionIcons.App.Services;
using ExtensionIcons.Core.Icons;
using ExtensionIcons.Core.Inventory;
using ExtensionIcons.Core.Overrides;
using ExtensionIcons.Core.Snapshots;
using System.Windows.Data;

namespace ExtensionIcons.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly ExtensionInventory _inventory;
    private readonly IconOverrideService _overrides;
    private readonly SnapshotManager _snapshots;
    private readonly IIconStore _iconStore;
    private readonly IDialogService _dialogs;
    private readonly string _catalogIconDirectory;
    private readonly ShellIconLoader _iconLoader = new();
    private readonly CatalogIconProvider _catalogIcons = new();

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _catalogOnly;
    [ObservableProperty] private bool _userChoiceOnly;
    [ObservableProperty] private string _statusText = Strings.StatusLoading;

    [NotifyCanExecuteChangedFor(nameof(UseCatalogIconCommand))]
    [NotifyCanExecuteChangedFor(nameof(ChooseIconFileCommand))]
    [NotifyCanExecuteChangedFor(nameof(RestoreSelectedCommand))]
    [ObservableProperty] private InventoryRowViewModel? _selectedRow;

    public MainViewModel(
        ExtensionInventory inventory,
        IconOverrideService overrides,
        SnapshotManager snapshots,
        IIconStore iconStore,
        IDialogService dialogs,
        string catalogIconDirectory)
    {
        _inventory = inventory;
        _overrides = overrides;
        _snapshots = snapshots;
        _iconStore = iconStore;
        _dialogs = dialogs;
        _catalogIconDirectory = catalogIconDirectory;

        RowsView = CollectionViewSource.GetDefaultView(Rows);
        RowsView.Filter = row => CurrentFilter.Matches(((InventoryRowViewModel)row).Registration);
    }

    public ObservableCollection<InventoryRowViewModel> Rows { get; } = [];

    /// <summary>Filtered, sortable view the grid binds to.</summary>
    public ICollectionView RowsView { get; }

    private InventoryFilter CurrentFilter => new(SearchText, CatalogOnly, UserChoiceOnly);

    [RelayCommand]
    private async Task LoadAsync()
    {
        StatusText = Strings.StatusLoading;
        try
        {
            var registrations = await Task.Run(_inventory.Enumerate);
            foreach (var registration in registrations)
            {
                Rows.Add(new InventoryRowViewModel(registration, _iconLoader, _catalogIcons));
            }
            RefreshOverrideState();
            UpdateStatus();
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException or InvalidDataException)
        {
            StatusText = string.Format(Strings.StatusLoadFailed, ex.Message);
        }
    }

    private bool HasSelection => SelectedRow is not null;
    private bool HasCatalogSelection => SelectedRow?.HasCatalogIcon == true;
    private bool CanRestoreSelected => SelectedRow?.IsOverridden == true;
    private bool CanRestoreAll => Rows.Any(r => r.IsOverridden);

    [RelayCommand(CanExecute = nameof(HasCatalogSelection))]
    private void UseCatalogIcon()
    {
        if (SelectedRow is { CatalogIconKey: { } key } row)
        {
            ChangeIcon(row, Path.Combine(_catalogIconDirectory, key + ".ico"));
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void ChooseIconFile()
    {
        if (SelectedRow is not { } row) return;

        var file = _dialogs.PickIconFile(string.Format(Strings.PickIconTitle, row.Extension));
        if (file is not null)
        {
            ChangeIcon(row, file);
        }
    }

    [RelayCommand(CanExecute = nameof(CanRestoreSelected))]
    private void RestoreSelected()
    {
        if (SelectedRow is not { } row) return;

        Restore(() => _snapshots.RevertExtension(row.Extension));
    }

    [RelayCommand(CanExecute = nameof(CanRestoreAll))]
    private void RestoreAll()
    {
        if (_dialogs.Confirm(Strings.ConfirmRestoreAllMessage))
        {
            Restore(_snapshots.RevertAll);
        }
    }

    private void ChangeIcon(InventoryRowViewModel row, string iconFile)
    {
        try
        {
            var plan = _overrides.Plan(row.Extension, _iconStore.Install(iconFile));

            var acknowledged = false;
            if (plan.AffectsOtherExtensions)
            {
                var others = plan.AffectedExtensions.Where(e => !string.Equals(e, row.Extension, StringComparison.OrdinalIgnoreCase));
                if (!_dialogs.Confirm(string.Format(Strings.ConfirmSharedMessage, row.Extension, plan.TargetProgId, string.Join(", ", others))))
                {
                    return;
                }
                acknowledged = true;
            }

            _overrides.Apply(plan, acknowledged);
            RefreshRows(plan.AffectedExtensions);
            StatusText = string.Format(Strings.StatusIconChanged, string.Join(", ", plan.AffectedExtensions));
        }
        catch (IconOverrideNotSupportedException ex)
        {
            _dialogs.ShowError(ex.Problem switch
            {
                IconOverrideProblem.NotRegistered => string.Format(Strings.ErrorNotRegistered, row.Extension),
                IconOverrideProblem.InvalidIconLocation => Strings.ErrorInvalidIcon,
                IconOverrideProblem.NoProgId => string.Format(Strings.ErrorNoProgId, row.Extension),
                _ => string.Format(Strings.ErrorGeneric, ex.Message),
            });
        }
        catch (ArgumentException)
        {
            _dialogs.ShowError(Strings.ErrorNotIco);
        }
        catch (FileNotFoundException)
        {
            _dialogs.ShowError(Strings.ErrorFileMissing);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or SecurityException)
        {
            _dialogs.ShowError(string.Format(Strings.ErrorGeneric, ex.Message));
        }
    }

    private void Restore(Func<IReadOnlyList<Snapshot>> revert)
    {
        try
        {
            var reverted = revert();
            if (reverted.Count == 0)
            {
                StatusText = Strings.StatusNothingToRestore;
                return;
            }

            var extensions = reverted.SelectMany(s => s.Extensions).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToList();
            RefreshRows(extensions);
            StatusText = string.Format(Strings.StatusRestored, string.Join(", ", extensions));
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or SecurityException or InvalidDataException)
        {
            _dialogs.ShowError(string.Format(Strings.ErrorGeneric, ex.Message));
        }
    }

    /// <summary>Re-reads the given extensions from the registry and updates their rows.</summary>
    private void RefreshRows(IEnumerable<string> extensions)
    {
        var wanted = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);
        foreach (var row in Rows.Where(r => wanted.Contains(r.Extension)))
        {
            if (_inventory.Get(row.Extension) is { } fresh)
            {
                row.Update(fresh);
            }
        }
        RefreshOverrideState();
    }

    private void RefreshOverrideState()
    {
        var touched = _snapshots.Active()
            .SelectMany(s => s.Extensions)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var row in Rows)
        {
            row.IsOverridden = touched.Contains(row.Extension);
        }
        RestoreSelectedCommand.NotifyCanExecuteChanged();
        RestoreAllCommand.NotifyCanExecuteChanged();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnCatalogOnlyChanged(bool value) => ApplyFilter();
    partial void OnUserChoiceOnlyChanged(bool value) => ApplyFilter();

    private void ApplyFilter()
    {
        RowsView.Refresh();
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        var shown = RowsView.Cast<object>().Count();
        StatusText = string.Format(Strings.StatusShowing, shown, Rows.Count);
    }
}
