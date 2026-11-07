using ExtensionIcons.Core.Registry;
using ExtensionIcons.Core.Shell;
using ExtensionIcons.Core.Snapshots;

namespace ExtensionIcons.Core.Tests.Snapshots;

public class SnapshotManagerTests
{
    private const string Classes = @"Software\Classes";
    private const RegistryHive User = RegistryHive.CurrentUser;

    private readonly FakeRegistryProvider _registry = new();
    private readonly InMemorySnapshotStore _store = new();
    private readonly FakeShellNotifier _shell = new();

    private SnapshotManager Manager() => new(_registry, _store, _shell);

    private static RegistryWrite Write(string key, string value, string name = "") =>
        new(User, $@"{Classes}\{key}", name, value);

    [Fact]
    public void Apply_writes_values_notifies_the_shell_and_records_a_snapshot()
    {
        var snapshot = Manager().ApplyWithSnapshot("test", [".foo"],
            [Write(@"foofile\DefaultIcon", @"C:\a.ico"), Write(".foo", "foofile")]);

        Assert.Equal(@"C:\a.ico", _registry.GetValue(User, $@"{Classes}\foofile\DefaultIcon"));
        Assert.Equal("foofile", _registry.GetValue(User, $@"{Classes}\.foo"));
        Assert.Equal(1, _shell.NotificationCount);
        Assert.Equal([snapshot], _store.List());
        Assert.Equal([".foo"], snapshot.Extensions);
        Assert.False(snapshot.IsReverted);
    }

    [Fact]
    public void Snapshot_is_saved_before_the_first_write()
    {
        var observing = new ObservingRegistry(_registry);
        var snapshotsSeenAtFirstWrite = -1;
        observing.BeforeSetValue = call =>
        {
            if (call == 1) snapshotsSeenAtFirstWrite = _store.List().Count;
        };

        new SnapshotManager(observing, _store, _shell).ApplyWithSnapshot("test", [".foo"], [Write(".foo", "foofile")]);

        Assert.Equal(1, snapshotsSeenAtFirstWrite);
    }

    [Fact]
    public void Snapshot_records_previous_values_and_the_shallowest_key_each_write_created()
    {
        _registry.SetValue(User, $@"{Classes}\.foo", "", "oldfile");

        var snapshot = Manager().ApplyWithSnapshot("test", [".foo"],
            [Write(".foo", "foofile"), Write(@"foofile\shell\open\command", "app.exe"), Write(@"foofile\DefaultIcon", "a.ico")]);

        Assert.Equal(["oldfile", null, null], snapshot.Changes.Select(c => c.PreviousValue));
        Assert.Equal([null, $@"{Classes}\foofile", $@"{Classes}\foofile\DefaultIcon"],
            snapshot.Changes.Select(c => c.CreatedKeyPath));
    }

    [Fact]
    public void Writes_outside_the_user_hive_are_rejected_before_anything_changes()
    {
        var write = new RegistryWrite(RegistryHive.ClassesRoot, ".foo", "", "foofile");

        Assert.Throws<ArgumentException>(() => Manager().ApplyWithSnapshot("test", [".foo"], [write]));
        Assert.Empty(_store.List());
        Assert.Equal(0, _shell.NotificationCount);
    }

    [Fact]
    public void A_failure_part_way_restores_what_was_written_and_marks_the_snapshot_reverted()
    {
        _registry.SetValue(User, $@"{Classes}\.foo", "", "oldfile");
        var observing = new ObservingRegistry(_registry)
        {
            BeforeSetValue = call => { if (call == 3) throw new IOException("disk on fire"); },
        };

        var manager = new SnapshotManager(observing, _store, _shell);
        var failure = Assert.Throws<IOException>(() => manager.ApplyWithSnapshot("test", [".foo"],
            [Write(@"foofile\DefaultIcon", "a.ico"), Write(".foo", "foofile"), Write(@"foofile\shell", "open")]));

        Assert.Equal("disk on fire", failure.Message);
        Assert.Equal("oldfile", _registry.GetValue(User, $@"{Classes}\.foo"));
        Assert.False(_registry.KeyExists(User, $@"{Classes}\foofile"));
        Assert.True(Assert.Single(_store.List()).IsReverted);
    }

    [Fact]
    public void Restore_puts_back_old_values_and_removes_the_keys_it_created()
    {
        _registry.SetValue(User, $@"{Classes}\.foo", "", "oldfile");
        var manager = Manager();
        var snapshot = manager.ApplyWithSnapshot("test", [".foo"],
            [Write(@"foofile\shell\open\command", "app.exe"), Write(@"foofile\DefaultIcon", "a.ico"), Write(".foo", "foofile")]);

        manager.Restore(snapshot.Id);

        Assert.Equal("oldfile", _registry.GetValue(User, $@"{Classes}\.foo"));
        Assert.False(_registry.KeyExists(User, $@"{Classes}\foofile"));
        Assert.True(Assert.Single(_store.List()).IsReverted);
        Assert.Equal(2, _shell.NotificationCount);
    }

    [Fact]
    public void Restore_deletes_a_value_that_did_not_exist_but_keeps_the_key_if_it_already_existed()
    {
        _registry.SetValue(User, $@"{Classes}\foofile", "", "Foo File");
        var manager = Manager();
        var snapshot = manager.ApplyWithSnapshot("test", [".foo"], [Write("foofile", "x", "Extra")]);

        manager.Restore(snapshot.Id);

        Assert.Null(_registry.GetValue(User, $@"{Classes}\foofile", "Extra"));
        Assert.Equal("Foo File", _registry.GetValue(User, $@"{Classes}\foofile"));
    }

    [Fact]
    public void Restore_never_deletes_content_that_appeared_in_a_key_after_the_snapshot()
    {
        var manager = Manager();
        var snapshot = manager.ApplyWithSnapshot("test", [".foo"], [Write(@"foofile\DefaultIcon", "a.ico")]);
        _registry.SetValue(User, $@"{Classes}\foofile\shell\open\command", "", "someone-elses.exe");

        manager.Restore(snapshot.Id);

        Assert.Null(_registry.GetValue(User, $@"{Classes}\foofile\DefaultIcon"));
        Assert.Equal("someone-elses.exe", _registry.GetValue(User, $@"{Classes}\foofile\shell\open\command"));
    }

    [Fact]
    public void Restoring_an_already_restored_snapshot_does_nothing()
    {
        var manager = Manager();
        var snapshot = manager.ApplyWithSnapshot("test", [".foo"], [Write(".foo", "foofile")]);
        manager.Restore(snapshot.Id);
        _registry.SetValue(User, $@"{Classes}\.foo", "", "changed-later");

        manager.Restore(snapshot.Id);

        Assert.Equal("changed-later", _registry.GetValue(User, $@"{Classes}\.foo"));
    }

    [Fact]
    public void Restore_of_an_unknown_snapshot_throws()
    {
        Assert.Throws<KeyNotFoundException>(() => Manager().Restore(Guid.NewGuid()));
    }

    [Fact]
    public void Sequence_increases_with_every_snapshot()
    {
        var manager = Manager();

        var first = manager.ApplyWithSnapshot("one", [".a"], [Write(".a", "x")]);
        var second = manager.ApplyWithSnapshot("two", [".b"], [Write(".b", "y")]);

        Assert.True(second.Sequence > first.Sequence);
    }

    [Fact]
    public void RevertExtension_restores_only_snapshots_that_touched_it_newest_first()
    {
        var manager = Manager();
        manager.ApplyWithSnapshot("a1", [".a"], [Write(@"x\DefaultIcon", "one.ico")]);
        manager.ApplyWithSnapshot("b", [".b"], [Write(@"y\DefaultIcon", "b.ico")]);
        manager.ApplyWithSnapshot("a2", [".a"], [Write(@"x\DefaultIcon", "two.ico")]);

        var reverted = manager.RevertExtension(".A");

        Assert.Equal(["a2", "a1"], reverted.Select(s => s.Description));
        Assert.False(_registry.KeyExists(User, $@"{Classes}\x"));
        Assert.Equal("b.ico", _registry.GetValue(User, $@"{Classes}\y\DefaultIcon"));
        Assert.Equal(["b"], manager.Active().Select(s => s.Description));
    }

    [Fact]
    public void RevertExtension_restores_a_shared_snapshot_as_a_whole()
    {
        var manager = Manager();
        manager.ApplyWithSnapshot("shared", [".htm", ".html", ".xml"], [Write(@"MSEdgeHTM\DefaultIcon", "c.ico")]);

        var reverted = manager.RevertExtension(".html");

        Assert.Equal([".htm", ".html", ".xml"], Assert.Single(reverted).Extensions);
        Assert.False(_registry.KeyExists(User, $@"{Classes}\MSEdgeHTM"));
    }

    [Fact]
    public void RevertAll_restores_everything_newest_first_and_returns_to_the_original_state()
    {
        _registry.SetValue(User, $@"{Classes}\.a", "", "afile");
        var manager = Manager();
        manager.ApplyWithSnapshot("one", [".a"], [Write(".a", "ExtensionIcons.a"), Write(@"ExtensionIcons.a\DefaultIcon", "1.ico")]);
        manager.ApplyWithSnapshot("two", [".a"], [Write(@"ExtensionIcons.a\DefaultIcon", "2.ico")]);
        manager.ApplyWithSnapshot("three", [".b"], [Write(".b", "ExtensionIcons.b")]);

        var reverted = manager.RevertAll();

        Assert.Equal(["three", "two", "one"], reverted.Select(s => s.Description));
        Assert.Equal("afile", _registry.GetValue(User, $@"{Classes}\.a"));
        Assert.False(_registry.KeyExists(User, $@"{Classes}\.b"));
        Assert.False(_registry.KeyExists(User, $@"{Classes}\ExtensionIcons.a"));
        Assert.Empty(manager.Active());
    }

    [Fact]
    public void RevertAll_with_nothing_to_revert_does_not_notify_the_shell()
    {
        var reverted = Manager().RevertAll();

        Assert.Empty(reverted);
        Assert.Equal(0, _shell.NotificationCount);
    }
}
