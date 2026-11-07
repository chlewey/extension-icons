using System.Windows;
using ExtensionIcons.App.Resources;
using Microsoft.Win32;

namespace ExtensionIcons.App.Services;

public sealed class WpfDialogService : IDialogService
{
    public string? PickIconFile(string title)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = Strings.PickIconFilter,
            CheckFileExists = true,
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public bool Confirm(string message) =>
        MessageBox.Show(message, Strings.DialogTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No)
            == MessageBoxResult.Yes;

    public void ShowError(string message) =>
        MessageBox.Show(message, Strings.DialogTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
}
