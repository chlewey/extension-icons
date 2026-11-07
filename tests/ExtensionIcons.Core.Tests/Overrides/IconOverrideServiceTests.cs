using ExtensionIcons.Core.Inventory;
using ExtensionIcons.Core.Overrides;
using ExtensionIcons.Core.Registry;
using ExtensionIcons.Core.Shell;
using ExtensionIcons.Core.Snapshots;

namespace ExtensionIcons.Core.Tests.Overrides;

public class IconOverrideServiceTests
{
    private const string NewIcon = @"C:\Icons\new.ico";
    private const string Classes = @"Software\Classes";
    private const string UserChoicePath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts";

    private readonly FakeRegistryProvider _registry = new();
    private readonly FakeShellNotifier _shell = new();
    private readonly SnapshotManager _snapshots;
    private readonly ExtensionInventory _inventory;
    private readonly IconOverrideService _service;

    public IconOverrideServiceTests()
    {
        _snapshots = new SnapshotManager(_registry, new InMemorySnapshotStore(), _shell);
        _inventory = new ExtensionInventory(_registry);
        _service = new IconOverrideService(_inventory, _registry, _snapshots);

        // .foo and .bar share one plain ProgID with two verbs, no UserChoice.
        _registry.SeedClassesRoot(".foo", "", "foofile");
        _registry.SeedClassesRoot(".bar", "", "foofile");
        _registry.SeedClassesRoot("foofile", "", "Foo File");
        _registry.SeedClassesRoot("foofile\\DefaultIcon", "", @"C:\Old\foo.ico");
        _registry.SeedClassesRoot("foofile\\shell", "", "open");
        _registry.SeedClassesRoot("foofile\\shell\\open", "", "&Open");
        _registry.SeedClassesRoot("foofile\\shell\\open\\command", "", @"""C:\App\foo.exe"" ""%1""");
        _registry.SeedClassesRoot("foofile\\shell\\edit\\command", "", @"""C:\App\edit.exe"" ""%1""");

        // .htm/.html/.xml all resolve to MSEdgeHTM through UserChoice.
        foreach (var (ext, classProgId) in new[] { (".htm", "htmlfile"), (".html", "htmlfile"), (".xml", "xmlfile") })
        {
            _registry.SeedClassesRoot(ext, "", classProgId);
            _registry.SetValue(RegistryHive.CurrentUser, $@"{UserChoicePath}\{ext}\UserChoice", "ProgId", "MSEdgeHTM");
        }
        _registry.SeedClassesRoot("MSEdgeHTM\\DefaultIcon", "", @"C:\Edge\msedge.exe,0");
        _registry.SeedClassesRoot("MSEdgeHTM\\shell\\open\\command", "", @"""C:\Edge\msedge.exe"" %1");
    }

    // ---- planning ----

    [Fact]
    public void Plan_without_UserChoice_gives_the_extension_its_own_ProgId_and_affects_only_it()
    {
        var plan = _service.Plan(".foo", NewIcon);

        Assert.Equal(IconOverrideStrategy.DedicatedProgId, plan.Strategy);
        Assert.Equal("ExtensionIcons.foo", plan.TargetProgId);
        Assert.Equal([".foo"], plan.AffectedExtensions);
        Assert.False(plan.AffectsOtherExtensions);
    }

    [Fact]
    public void Plan_writes_the_extension_key_last_and_only_under_the_user_hive()
    {
        var plan = _service.Plan(".foo", NewIcon);

        Assert.All(plan.Writes, w => Assert.Equal(RegistryHive.CurrentUser, w.Hive));
        var last = plan.Writes[^1];
        Assert.Equal($@"{Classes}\.foo", last.KeyPath);
        Assert.Equal("ExtensionIcons.foo", last.Value);
    }

    [Fact]
    public void Plan_for_an_extension_with_UserChoice_targets_the_shared_ProgId_and_lists_everyone_it_affects()
    {
        var plan = _service.Plan(".htm", NewIcon);

        Assert.Equal(IconOverrideStrategy.ProgIdDefaultIcon, plan.Strategy);
        Assert.Equal("MSEdgeHTM", plan.TargetProgId);
        Assert.Equal([".htm", ".html", ".xml"], plan.AffectedExtensions);
        Assert.True(plan.AffectsOtherExtensions);
        Assert.Equal(new RegistryWrite(RegistryHive.CurrentUser, $@"{Classes}\MSEdgeHTM\DefaultIcon", "", NewIcon),
            Assert.Single(plan.Writes));
    }

    [Fact]
    public void Plan_accepts_the_extension_without_a_dot_and_in_any_case()
    {
        Assert.Equal(".foo", _service.Plan("FOO", NewIcon).Extension);
    }

    [Fact]
    public void Plan_rejects_an_unregistered_extension()
    {
        var ex = Assert.Throws<IconOverrideNotSupportedException>(() => _service.Plan(".nope", NewIcon));

        Assert.Equal(IconOverrideProblem.NotRegistered, ex.Problem);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(@"@{Package?ms-resource://x/y.png}")]
    public void Plan_rejects_an_unusable_icon_location(string icon)
    {
        var ex = Assert.Throws<IconOverrideNotSupportedException>(() => _service.Plan(".foo", icon));

        Assert.Equal(IconOverrideProblem.InvalidIconLocation, ex.Problem);
    }

    [Fact]
    public void Plan_rejects_an_extension_that_has_no_ProgId_at_all()
    {
        _registry.SeedClassesRoot(".noprog\\OpenWithProgids", "Some.App", "");

        var ex = Assert.Throws<IconOverrideNotSupportedException>(() => _service.Plan(".noprog", NewIcon));

        Assert.Equal(IconOverrideProblem.NoProgId, ex.Problem);
    }

    // ---- ProgIDs that cannot be copied safely fall back to changing the existing ProgID's icon ----

    private void SeedProgId(string ext, string progId, Action<string> addExtras)
    {
        _registry.SeedClassesRoot(ext, "", progId);
        _registry.SeedClassesRoot(progId, "", $"{progId} description");
        _registry.SeedClassesRoot($@"{progId}\DefaultIcon", "", @"C:\Old\x.ico");
        _registry.SeedClassesRoot($@"{progId}\shell\open\command", "", @"""C:\App\x.exe"" ""%1""");
        addExtras(progId);
    }

    [Theory]
    [InlineData("ddeexec", "shell\\open\\ddeexec")]
    [InlineData("shell extension", "shellex\\ContextMenuHandlers\\Thing")]
    [InlineData("CurVer", "CurVer")]
    [InlineData("drop target", "shell\\open\\DropTarget")]
    public void A_ProgId_with_behavior_a_copy_would_lose_falls_back_to_changing_its_own_icon(string label, string subKey)
    {
        SeedProgId(".odd", "oddfile", progId => _registry.SeedClassesRoot($@"{progId}\{subKey}", "", "x"));

        var plan = _service.Plan(".odd", NewIcon);

        Assert.True(plan.Strategy == IconOverrideStrategy.ProgIdDefaultIcon, label);
        Assert.Equal("oddfile", plan.TargetProgId);
        Assert.Equal(new RegistryWrite(RegistryHive.CurrentUser, $@"{Classes}\oddfile\DefaultIcon", "", NewIcon),
            Assert.Single(plan.Writes));
    }

    [Fact]
    public void A_ProgId_whose_open_verb_uses_DelegateExecute_falls_back_to_changing_its_own_icon()
    {
        SeedProgId(".deleg", "delegfile", progId =>
            _registry.SeedClassesRoot($@"{progId}\shell\open\command", "DelegateExecute", "{GUID}"));

        Assert.Equal(IconOverrideStrategy.ProgIdDefaultIcon, _service.Plan(".deleg", NewIcon).Strategy);
    }

    [Fact]
    public void A_ProgId_without_a_plain_open_command_falls_back_to_changing_its_own_icon()
    {
        _registry.SeedClassesRoot(".nocmd", "", "nocmdfile");
        _registry.SeedClassesRoot("nocmdfile", "", "No command");

        var plan = _service.Plan(".nocmd", NewIcon);

        Assert.Equal(IconOverrideStrategy.ProgIdDefaultIcon, plan.Strategy);
        Assert.Equal("nocmdfile", plan.TargetProgId);
    }

    [Fact]
    public void The_fallback_changes_only_the_icon_so_opening_is_untouched_and_it_lists_who_shares_it()
    {
        SeedProgId(".odd", "oddfile", progId => _registry.SeedClassesRoot($@"{progId}\shell\open\ddeexec", "", "[open]"));
        _registry.SeedClassesRoot(".odd2", "", "oddfile");

        var plan = _service.Plan(".odd", NewIcon);
        Assert.Equal([".odd", ".odd2"], plan.AffectedExtensions);

        _service.Apply(plan, acknowledgeSharedExtensions: true);

        Assert.Equal(NewIcon, _inventory.Get(".odd")!.IconLocation);
        Assert.Equal(@"""C:\App\x.exe"" ""%1""", _inventory.Get(".odd")!.OpenCommand);
        Assert.Equal("[open]", _registry.GetValue(RegistryHive.ClassesRoot, @"oddfile\shell\open\ddeexec"));
    }

    [Fact]
    public void A_simple_ProgId_is_copied_including_extra_text_values_on_ProgId_and_verbs()
    {
        SeedProgId(".rich", "richfile", progId =>
        {
            _registry.SeedClassesRoot(progId, "FriendlyTypeName", "@shell32.dll,-10");
            _registry.SeedClassesRoot($@"{progId}\shell\open", "MUIVerb", "&Open it");
            _registry.SeedClassesRoot($@"{progId}\shell\open\command", "Extra", "value");
        });

        var plan = _service.Plan(".rich", NewIcon);
        _service.Apply(plan);

        Assert.Equal(IconOverrideStrategy.DedicatedProgId, plan.Strategy);
        Assert.Equal("@shell32.dll,-10", _registry.GetValue(RegistryHive.ClassesRoot, @"ExtensionIcons.rich", "FriendlyTypeName"));
        Assert.Equal("&Open it", _registry.GetValue(RegistryHive.ClassesRoot, @"ExtensionIcons.rich\shell\open", "MUIVerb"));
        Assert.Equal("value", _registry.GetValue(RegistryHive.ClassesRoot, @"ExtensionIcons.rich\shell\open\command", "Extra"));
    }

    // ---- applying: dedicated ProgID ----

    [Fact]
    public void Applying_a_dedicated_override_changes_the_icon_but_keeps_opening_and_verbs()
    {
        _service.Apply(_service.Plan(".foo", NewIcon));

        var foo = _inventory.Get(".foo")!;
        Assert.Equal("ExtensionIcons.foo", foo.ClassProgId);
        Assert.Equal(NewIcon, foo.IconLocation);
        Assert.Equal("Foo File", foo.ProgIdDescription);
        Assert.Equal(@"""C:\App\foo.exe"" ""%1""", foo.OpenCommand);
        Assert.Equal("open", _registry.GetValue(RegistryHive.ClassesRoot, @"ExtensionIcons.foo\shell"));
        Assert.Equal("&Open", _registry.GetValue(RegistryHive.ClassesRoot, @"ExtensionIcons.foo\shell\open"));
        Assert.Equal(@"""C:\App\edit.exe"" ""%1""",
            _registry.GetValue(RegistryHive.ClassesRoot, @"ExtensionIcons.foo\shell\edit\command"));
    }

    [Fact]
    public void A_dedicated_override_leaves_the_other_extension_on_the_shared_ProgId_untouched()
    {
        _service.Apply(_service.Plan(".foo", NewIcon));

        var bar = _inventory.Get(".bar")!;
        Assert.Equal("foofile", bar.ClassProgId);
        Assert.Equal(@"C:\Old\foo.ico", bar.IconLocation);
        Assert.Equal(@"C:\Old\foo.ico", _registry.GetValue(RegistryHive.ClassesRoot, @"foofile\DefaultIcon"));
    }

    [Fact]
    public void Overriding_again_only_replaces_the_icon_and_does_not_recopy_the_ProgId()
    {
        _service.Apply(_service.Plan(".foo", NewIcon));

        var second = _service.Plan(".foo", @"C:\Icons\second.ico");
        _service.Apply(second);

        Assert.DoesNotContain(second.Writes, w => w.KeyPath.EndsWith(@"\shell\open\command"));
        Assert.Equal(@"C:\Icons\second.ico", _inventory.Get(".foo")!.IconLocation);
    }

    // ---- applying: shared ProgID ----

    [Fact]
    public void Applying_a_plan_that_affects_other_extensions_requires_acknowledgment_and_writes_nothing_without_it()
    {
        var plan = _service.Plan(".htm", NewIcon);

        Assert.Throws<InvalidOperationException>(() => _service.Apply(plan));

        Assert.False(_registry.KeyExists(RegistryHive.CurrentUser, $@"{Classes}\MSEdgeHTM"));
        Assert.Empty(_snapshots.List());
    }

    [Fact]
    public void Applying_a_shared_override_changes_the_icon_of_every_extension_that_shares_the_ProgId()
    {
        _service.Apply(_service.Plan(".htm", NewIcon), acknowledgeSharedExtensions: true);

        Assert.All(new[] { ".htm", ".html", ".xml" }, ext => Assert.Equal(NewIcon, _inventory.Get(ext)!.IconLocation));
        Assert.Equal(@"""C:\Edge\msedge.exe"" %1", _inventory.Get(".htm")!.OpenCommand);
    }

    // ---- reverting ----

    [Fact]
    public void Reverting_a_dedicated_override_leaves_no_trace_in_the_user_hive()
    {
        _service.Apply(_service.Plan(".foo", NewIcon));

        _snapshots.RevertExtension(".foo");

        Assert.False(_registry.KeyExists(RegistryHive.CurrentUser, $@"{Classes}\.foo"));
        Assert.False(_registry.KeyExists(RegistryHive.CurrentUser, $@"{Classes}\ExtensionIcons.foo"));
        var foo = _inventory.Get(".foo")!;
        Assert.Equal("foofile", foo.ClassProgId);
        Assert.Equal(@"C:\Old\foo.ico", foo.IconLocation);
    }

    [Fact]
    public void Reverting_a_shared_override_restores_every_sharing_extension()
    {
        _service.Apply(_service.Plan(".htm", NewIcon), acknowledgeSharedExtensions: true);

        var reverted = _snapshots.RevertExtension(".xml");

        Assert.Equal([".htm", ".html", ".xml"], Assert.Single(reverted).Extensions);
        Assert.All(new[] { ".htm", ".html", ".xml" },
            ext => Assert.Equal(@"C:\Edge\msedge.exe,0", _inventory.Get(ext)!.IconLocation));
        Assert.False(_registry.KeyExists(RegistryHive.CurrentUser, $@"{Classes}\MSEdgeHTM"));
    }

    [Fact]
    public void Reverting_everything_after_several_overrides_restores_the_original_registry()
    {
        _service.Apply(_service.Plan(".foo", NewIcon));
        _service.Apply(_service.Plan(".foo", @"C:\Icons\second.ico"));
        _service.Apply(_service.Plan(".bar", @"C:\Icons\bar.ico"));
        _service.Apply(_service.Plan(".htm", @"C:\Icons\edge.ico"), acknowledgeSharedExtensions: true);

        _snapshots.RevertAll();

        Assert.Empty(_registry.GetSubKeyNames(RegistryHive.CurrentUser, Classes));
        Assert.Equal(@"C:\Old\foo.ico", _inventory.Get(".foo")!.IconLocation);
        Assert.Equal(@"C:\Old\foo.ico", _inventory.Get(".bar")!.IconLocation);
        Assert.Equal(@"C:\Edge\msedge.exe,0", _inventory.Get(".htm")!.IconLocation);
    }
}
