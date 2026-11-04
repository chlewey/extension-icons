using System.Runtime.InteropServices;

namespace ExtensionIcons.Core.Shell;

/// <summary>
/// Real shell notification via SHChangeNotify. Validated live in
/// docs/spikes/001-icon-override-mechanism.md: SHCNE_ASSOCCHANGED with
/// SHCNF_IDLIST and null item pointers refreshes icon resolution without a
/// reboot.
/// </summary>
public sealed class Win32ShellNotifier : IShellNotifier
{
    private const int SHCNE_ASSOCCHANGED = 0x08000000;
    private const uint SHCNF_IDLIST = 0x0000;

    public void NotifyAssociationsChanged() =>
        SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
}
