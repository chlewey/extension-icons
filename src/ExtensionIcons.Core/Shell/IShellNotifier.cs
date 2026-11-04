namespace ExtensionIcons.Core.Shell;

/// <summary>
/// Tells the shell a file-association change happened, so Explorer picks up
/// the new icon/command without the user having to sign out or reboot.
/// </summary>
public interface IShellNotifier
{
    void NotifyAssociationsChanged();
}
