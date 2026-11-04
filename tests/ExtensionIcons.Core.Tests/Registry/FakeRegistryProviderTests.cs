using ExtensionIcons.Core.Registry;

namespace ExtensionIcons.Core.Tests.Registry;

public class FakeRegistryProviderTests
{
    [Fact]
    public void SetValue_then_GetValue_round_trips()
    {
        var registry = new FakeRegistryProvider();

        registry.SetValue(RegistryHive.CurrentUser, @"Software\Classes\.htm", "", "ExtIcons.htm");

        Assert.Equal("ExtIcons.htm", registry.GetValue(RegistryHive.CurrentUser, @"Software\Classes\.htm"));
    }

    [Fact]
    public void GetValue_returns_null_for_missing_key_or_value()
    {
        var registry = new FakeRegistryProvider();

        Assert.Null(registry.GetValue(RegistryHive.CurrentUser, @"Software\Classes\.missing"));

        registry.SetValue(RegistryHive.CurrentUser, @"Software\Classes\.htm", "", "ExtIcons.htm");
        Assert.Null(registry.GetValue(RegistryHive.CurrentUser, @"Software\Classes\.htm", "NoSuchValue"));
    }

    [Fact]
    public void SetValue_creates_intermediate_keys_like_real_registry()
    {
        var registry = new FakeRegistryProvider();

        registry.SetValue(RegistryHive.CurrentUser, @"Software\Classes\ExtIcons.htm\DefaultIcon", "", "shell32.dll,41");

        Assert.True(registry.KeyExists(RegistryHive.CurrentUser, @"Software\Classes"));
        Assert.True(registry.KeyExists(RegistryHive.CurrentUser, @"Software\Classes\ExtIcons.htm"));
        Assert.True(registry.KeyExists(RegistryHive.CurrentUser, @"Software\Classes\ExtIcons.htm\DefaultIcon"));
    }

    [Fact]
    public void KeyExists_is_false_before_creation()
    {
        var registry = new FakeRegistryProvider();

        Assert.False(registry.KeyExists(RegistryHive.CurrentUser, @"Software\Classes\.htm"));
    }

    [Fact]
    public void GetSubKeyNames_returns_only_direct_children()
    {
        var registry = new FakeRegistryProvider();
        registry.SetValue(RegistryHive.CurrentUser, @"Software\Classes\ExtIcons.htm\DefaultIcon", "", "shell32.dll,41");
        registry.SetValue(RegistryHive.CurrentUser, @"Software\Classes\ExtIcons.htm\shell\open\command", "", "notepad.exe \"%1\"");

        var children = registry.GetSubKeyNames(RegistryHive.CurrentUser, @"Software\Classes\ExtIcons.htm");

        Assert.Equal(new[] { "DefaultIcon", "shell" }, children.OrderBy(n => n));
    }

    [Fact]
    public void DeleteKey_recursive_removes_key_and_descendants()
    {
        var registry = new FakeRegistryProvider();
        registry.SetValue(RegistryHive.CurrentUser, @"Software\Classes\ExtIcons.htm\DefaultIcon", "", "shell32.dll,41");

        registry.DeleteKey(RegistryHive.CurrentUser, @"Software\Classes\ExtIcons.htm", recursive: true);

        Assert.False(registry.KeyExists(RegistryHive.CurrentUser, @"Software\Classes\ExtIcons.htm"));
        Assert.False(registry.KeyExists(RegistryHive.CurrentUser, @"Software\Classes\ExtIcons.htm\DefaultIcon"));
    }

    [Fact]
    public void DeleteKey_non_recursive_throws_when_subkeys_exist()
    {
        var registry = new FakeRegistryProvider();
        registry.SetValue(RegistryHive.CurrentUser, @"Software\Classes\ExtIcons.htm\DefaultIcon", "", "shell32.dll,41");

        Assert.Throws<InvalidOperationException>(() =>
            registry.DeleteKey(RegistryHive.CurrentUser, @"Software\Classes\ExtIcons.htm", recursive: false));
    }

    [Fact]
    public void DeleteKey_non_recursive_succeeds_on_leaf_key()
    {
        var registry = new FakeRegistryProvider();
        registry.SetValue(RegistryHive.CurrentUser, @"Software\Classes\ExtIcons.htm\DefaultIcon", "", "shell32.dll,41");

        registry.DeleteKey(RegistryHive.CurrentUser, @"Software\Classes\ExtIcons.htm\DefaultIcon", recursive: false);

        Assert.False(registry.KeyExists(RegistryHive.CurrentUser, @"Software\Classes\ExtIcons.htm\DefaultIcon"));
        Assert.True(registry.KeyExists(RegistryHive.CurrentUser, @"Software\Classes\ExtIcons.htm"));
    }

    [Fact]
    public void ClassesRoot_is_read_only()
    {
        var registry = new FakeRegistryProvider();

        Assert.Throws<NotSupportedException>(() =>
            registry.SetValue(RegistryHive.ClassesRoot, @".htm", "", "ExtIcons.htm"));
        Assert.Throws<NotSupportedException>(() =>
            registry.DeleteKey(RegistryHive.ClassesRoot, @".htm"));
    }

    [Fact]
    public void Key_paths_are_case_insensitive()
    {
        var registry = new FakeRegistryProvider();

        registry.SetValue(RegistryHive.CurrentUser, @"Software\Classes\.HTM", "", "ExtIcons.htm");

        Assert.Equal("ExtIcons.htm", registry.GetValue(RegistryHive.CurrentUser, @"software\classes\.htm"));
    }
}
