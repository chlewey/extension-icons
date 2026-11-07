using ExtensionIcons.Core.Inventory;
using ExtensionIcons.Core.Registry;

namespace ExtensionIcons.Core.Tests.Inventory;

public class ExtensionInventoryTests
{
    private const string UserChoiceHtm =
        @"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.htm\UserChoice";

    private static FakeRegistryProvider SeedHtm(FakeRegistryProvider registry)
    {
        registry.SeedClassesRoot(".htm", "", "htmlfile");
        registry.SeedClassesRoot("htmlfile", "", "HTML Document");
        registry.SeedClassesRoot(@"htmlfile\DefaultIcon", "", @"C:\Windows\System32\shell32.dll,220");
        registry.SeedClassesRoot(@"htmlfile\shell\open\command", "", @"""C:\App\app.exe"" ""%1""");
        return registry;
    }

    [Fact]
    public void Get_resolves_progid_icon_description_and_open_command()
    {
        var inventory = new ExtensionInventory(SeedHtm(new FakeRegistryProvider()));

        var result = inventory.Get(".htm");

        Assert.NotNull(result);
        Assert.Equal(".htm", result.Extension);
        Assert.Equal("htmlfile", result.EffectiveProgId);
        Assert.Equal(@"C:\Windows\System32\shell32.dll,220", result.IconLocation);
        Assert.Equal("HTML Document", result.ProgIdDescription);
        Assert.Equal(@"""C:\App\app.exe"" ""%1""", result.OpenCommand);
    }

    [Fact]
    public void Get_accepts_extension_without_leading_dot_and_any_case()
    {
        var inventory = new ExtensionInventory(SeedHtm(new FakeRegistryProvider()));

        Assert.Equal(".htm", inventory.Get("HTM")!.Extension);
    }

    [Fact]
    public void Get_returns_null_for_unregistered_extension()
    {
        var inventory = new ExtensionInventory(new FakeRegistryProvider());

        Assert.Null(inventory.Get(".nope"));
    }

    [Fact]
    public void UserChoice_progid_takes_precedence_over_class_progid()
    {
        var registry = SeedHtm(new FakeRegistryProvider());
        registry.SeedClassesRoot("MSEdgeHTM", "", "Microsoft Edge HTML Document");
        registry.SeedClassesRoot(@"MSEdgeHTM\DefaultIcon", "", @"C:\Edge\msedge.exe,0");
        registry.SetValue(RegistryHive.CurrentUser, UserChoiceHtm, "ProgId", "MSEdgeHTM");

        var result = new ExtensionInventory(registry).Get(".htm")!;

        Assert.Equal("htmlfile", result.ClassProgId);
        Assert.Equal("MSEdgeHTM", result.UserChoiceProgId);
        Assert.Equal("MSEdgeHTM", result.EffectiveProgId);
        Assert.Equal(@"C:\Edge\msedge.exe,0", result.IconLocation);
        Assert.Equal("Microsoft Edge HTML Document", result.ProgIdDescription);
    }

    [Fact]
    public void Extension_without_progid_has_no_icon_or_command()
    {
        var registry = new FakeRegistryProvider();
        registry.SeedClassesRoot(@".orphan\OpenWithProgids", "Some.App", "");

        var result = new ExtensionInventory(registry).Get(".orphan")!;

        Assert.Null(result.EffectiveProgId);
        Assert.Null(result.IconLocation);
        Assert.Null(result.OpenCommand);
    }

    [Fact]
    public void Progid_without_default_icon_leaves_icon_null()
    {
        var registry = new FakeRegistryProvider();
        registry.SeedClassesRoot(".foo", "", "foofile");
        registry.SeedClassesRoot("foofile", "", "Foo File");

        var result = new ExtensionInventory(registry).Get(".foo")!;

        Assert.Equal("foofile", result.EffectiveProgId);
        Assert.Null(result.IconLocation);
    }

    [Fact]
    public void Enumerate_lists_only_dotted_keys_sorted_and_skips_progids()
    {
        var registry = SeedHtm(new FakeRegistryProvider());
        registry.SeedClassesRoot(".csv", "", "csvfile");
        registry.SeedClassesRoot(".", "", "junk");

        var result = new ExtensionInventory(registry).Enumerate();

        Assert.Equal([".csv", ".htm"], result.Select(r => r.Extension));
    }
}
