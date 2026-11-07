using ExtensionIcons.Core.Registry;

namespace ExtensionIcons.Core.Tests.Registry;

public class FakeRegistryProviderMergeTests
{
    [Fact]
    public void DeleteValue_removes_only_that_value()
    {
        var registry = new FakeRegistryProvider();
        registry.SetValue(RegistryHive.CurrentUser, @"Software\Classes\.foo", "", "foofile");
        registry.SetValue(RegistryHive.CurrentUser, @"Software\Classes\.foo", "Content Type", "text/foo");

        registry.DeleteValue(RegistryHive.CurrentUser, @"Software\Classes\.foo", "");

        Assert.Null(registry.GetValue(RegistryHive.CurrentUser, @"Software\Classes\.foo"));
        Assert.Equal("text/foo", registry.GetValue(RegistryHive.CurrentUser, @"Software\Classes\.foo", "Content Type"));
        Assert.True(registry.KeyExists(RegistryHive.CurrentUser, @"Software\Classes\.foo"));
    }

    [Fact]
    public void DeleteValue_is_a_no_op_for_missing_key_or_value()
    {
        var registry = new FakeRegistryProvider();

        registry.DeleteValue(RegistryHive.CurrentUser, @"Software\Classes\.nope", "");
        registry.SetValue(RegistryHive.CurrentUser, @"Software\Classes\.foo", "", "foofile");
        registry.DeleteValue(RegistryHive.CurrentUser, @"Software\Classes\.foo", "Missing");

        Assert.Equal("foofile", registry.GetValue(RegistryHive.CurrentUser, @"Software\Classes\.foo"));
    }

    [Fact]
    public void DeleteValue_on_ClassesRoot_is_refused()
    {
        Assert.Throws<NotSupportedException>(() =>
            new FakeRegistryProvider().DeleteValue(RegistryHive.ClassesRoot, ".foo", ""));
    }

    [Fact]
    public void GetValueNames_lists_values_and_is_empty_for_missing_key()
    {
        var registry = new FakeRegistryProvider();
        registry.SetValue(RegistryHive.CurrentUser, @"Software\Classes\.foo", "", "foofile");
        registry.SetValue(RegistryHive.CurrentUser, @"Software\Classes\.foo", "Content Type", "text/foo");

        Assert.Equal(["", "Content Type"], registry.GetValueNames(RegistryHive.CurrentUser, @"Software\Classes\.foo").Order());
        Assert.Empty(registry.GetValueNames(RegistryHive.CurrentUser, @"Software\Classes\.nope"));
    }

    [Fact]
    public void ClassesRoot_prefers_a_user_value_over_the_seeded_one()
    {
        var registry = new FakeRegistryProvider();
        registry.SeedClassesRoot(@"foofile\DefaultIcon", "", @"C:\old.ico");

        registry.SetValue(RegistryHive.CurrentUser, @"Software\Classes\foofile\DefaultIcon", "", @"C:\new.ico");

        Assert.Equal(@"C:\new.ico", registry.GetValue(RegistryHive.ClassesRoot, @"foofile\DefaultIcon"));
    }

    [Fact]
    public void ClassesRoot_falls_back_to_the_seeded_value_when_the_user_key_has_no_such_value()
    {
        var registry = new FakeRegistryProvider();
        registry.SeedClassesRoot("foofile", "", "Foo File");
        registry.SetValue(RegistryHive.CurrentUser, @"Software\Classes\foofile\DefaultIcon", "", @"C:\new.ico");

        Assert.Equal("Foo File", registry.GetValue(RegistryHive.ClassesRoot, "foofile"));
    }

    [Fact]
    public void ClassesRoot_sees_keys_and_subkeys_created_only_in_the_user_hive()
    {
        var registry = new FakeRegistryProvider();
        registry.SeedClassesRoot(".old", "", "oldfile");
        registry.SetValue(RegistryHive.CurrentUser, @"Software\Classes\.new", "", "newfile");

        Assert.True(registry.KeyExists(RegistryHive.ClassesRoot, ".new"));
        Assert.Equal(["", ".new", ".old"], registry.GetSubKeyNames(RegistryHive.ClassesRoot, "")
            .Where(n => n.StartsWith('.')).Prepend("").Order());
    }

    [Fact]
    public void ClassesRoot_subkeys_are_the_union_without_duplicates()
    {
        var registry = new FakeRegistryProvider();
        registry.SeedClassesRoot(@"foofile\shell\open\command", "", "a");
        registry.SetValue(RegistryHive.CurrentUser, @"Software\Classes\foofile\DefaultIcon", "", "b");
        registry.SetValue(RegistryHive.CurrentUser, @"Software\Classes\foofile\shell\edit\command", "", "c");

        Assert.Equal(["DefaultIcon", "shell"], registry.GetSubKeyNames(RegistryHive.ClassesRoot, "foofile").Order());
        Assert.Equal(["edit", "open"], registry.GetSubKeyNames(RegistryHive.ClassesRoot, @"foofile\shell").Order());
    }
}
