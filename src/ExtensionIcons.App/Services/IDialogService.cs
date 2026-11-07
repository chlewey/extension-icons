namespace ExtensionIcons.App.Services;

/// <summary>The few modal interactions the view models need, kept out of the view models themselves.</summary>
public interface IDialogService
{
    /// <summary>Asks for an .ico file; null if the user cancels.</summary>
    string? PickIconFile(string title);

    /// <summary>Yes/no question; true for yes.</summary>
    bool Confirm(string message);

    void ShowError(string message);
}
