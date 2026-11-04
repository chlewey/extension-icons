namespace ExtensionIcons.Core.Shell;

/// <summary>
/// Records notifications instead of calling into the shell, so tests can
/// assert that an operation refreshed associations without touching a real
/// machine.
/// </summary>
public sealed class FakeShellNotifier : IShellNotifier
{
    public int NotificationCount { get; private set; }

    public void NotifyAssociationsChanged() => NotificationCount++;
}
