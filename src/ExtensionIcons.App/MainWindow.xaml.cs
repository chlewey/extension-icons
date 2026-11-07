using System.IO;
using System.Windows;
using ExtensionIcons.App.Services;
using ExtensionIcons.App.ViewModels;
using ExtensionIcons.Core.Icons;
using ExtensionIcons.Core.Inventory;
using ExtensionIcons.Core.Overrides;
using ExtensionIcons.Core.Registry;
using ExtensionIcons.Core.Shell;
using ExtensionIcons.Core.Snapshots;

namespace ExtensionIcons.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ExtensionIcons");
        var registry = new Win32RegistryProvider();
        var inventory = new ExtensionInventory(registry);
        var snapshots = new SnapshotManager(
            registry, new FileSnapshotStore(Path.Combine(appData, "snapshots")), new Win32ShellNotifier());

        _viewModel = new MainViewModel(
            inventory,
            new IconOverrideService(inventory, registry, snapshots),
            snapshots,
            new FileIconStore(Path.Combine(appData, "icons")),
            new WpfDialogService(),
            Path.Combine(AppContext.BaseDirectory, "Assets", "Icons"));

        DataContext = _viewModel;
        Loaded += async (_, _) => await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
