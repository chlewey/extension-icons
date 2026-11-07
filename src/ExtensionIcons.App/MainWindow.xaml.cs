using System.Windows;
using ExtensionIcons.App.ViewModels;
using ExtensionIcons.Core.Inventory;
using ExtensionIcons.Core.Registry;

namespace ExtensionIcons.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new(new ExtensionInventory(new Win32RegistryProvider()));

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        Loaded += async (_, _) => await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
