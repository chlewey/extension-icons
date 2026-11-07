using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExtensionIcons.App.Resources;
using ExtensionIcons.App.Services;
using ExtensionIcons.Core.Inventory;

namespace ExtensionIcons.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly ExtensionInventory _inventory;
    private readonly ShellIconLoader _iconLoader = new();
    private readonly CatalogIconProvider _catalogIcons = new();

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _catalogOnly;
    [ObservableProperty] private bool _userChoiceOnly;
    [ObservableProperty] private string _statusText = Strings.StatusLoading;

    public MainViewModel(ExtensionInventory inventory)
    {
        _inventory = inventory;
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
            UpdateStatus();
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            StatusText = string.Format(Strings.StatusLoadFailed, ex.Message);
        }
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
