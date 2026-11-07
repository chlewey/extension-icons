using ExtensionIcons.Core.Icons;

namespace ExtensionIcons.Core.Tests.Icons;

public sealed class FileIconStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ExtensionIconsTests", Guid.NewGuid().ToString("N"));
    private readonly string _source;
    private readonly string _store;

    public FileIconStoreTests()
    {
        _source = Path.Combine(_root, "source");
        _store = Path.Combine(_root, "store");
        Directory.CreateDirectory(_source);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string Icon(string name, params byte[] content)
    {
        var path = Path.Combine(_source, name);
        File.WriteAllBytes(path, content);
        return path;
    }

    [Fact]
    public void Install_copies_the_icon_into_the_store_and_returns_the_copy()
    {
        var store = new FileIconStore(_store);

        var installed = store.Install(Icon("html.ico", 1, 2, 3));

        Assert.StartsWith(_store, installed);
        Assert.EndsWith(".ico", installed);
        Assert.Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes(installed));
    }

    [Fact]
    public void The_copy_keeps_working_after_the_original_is_deleted()
    {
        var store = new FileIconStore(_store);
        var source = Icon("html.ico", 1, 2, 3);
        var installed = store.Install(source);

        File.Delete(source);

        Assert.True(File.Exists(installed));
    }

    [Fact]
    public void Installing_the_same_content_twice_reuses_one_file()
    {
        var store = new FileIconStore(_store);

        var first = store.Install(Icon("a.ico", 1, 2, 3));
        var second = store.Install(Icon("b.ico", 1, 2, 3));

        Assert.Single(Directory.EnumerateFiles(_store));
        Assert.Equal(File.ReadAllBytes(first), File.ReadAllBytes(second));
    }

    [Fact]
    public void Different_content_with_the_same_file_name_does_not_collide()
    {
        var store = new FileIconStore(_store);

        var first = store.Install(Icon("html.ico", 1, 2, 3));
        var second = store.Install(Icon("html.ico", 9, 9, 9));

        Assert.NotEqual(first, second);
        Assert.Equal(2, Directory.EnumerateFiles(_store).Count());
    }

    [Fact]
    public void Only_ico_files_are_accepted()
    {
        var store = new FileIconStore(_store);

        Assert.Throws<ArgumentException>(() => store.Install(Icon("picture.png", 1)));
    }

    [Fact]
    public void A_missing_source_file_is_reported()
    {
        var store = new FileIconStore(_store);

        Assert.Throws<FileNotFoundException>(() => store.Install(Path.Combine(_source, "missing.ico")));
    }
}
