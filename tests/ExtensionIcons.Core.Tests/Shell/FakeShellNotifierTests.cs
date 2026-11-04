using ExtensionIcons.Core.Shell;

namespace ExtensionIcons.Core.Tests.Shell;

public class FakeShellNotifierTests
{
    [Fact]
    public void NotificationCount_starts_at_zero()
    {
        var notifier = new FakeShellNotifier();

        Assert.Equal(0, notifier.NotificationCount);
    }

    [Fact]
    public void NotifyAssociationsChanged_increments_count()
    {
        var notifier = new FakeShellNotifier();

        notifier.NotifyAssociationsChanged();
        notifier.NotifyAssociationsChanged();

        Assert.Equal(2, notifier.NotificationCount);
    }
}
