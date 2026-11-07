using ExtensionIcons.Core.Catalog;

namespace ExtensionIcons.Core.Tests.Catalog;

public class BuiltInCatalogTests
{
    [Fact]
    public void Contains_the_contested_formats_named_in_the_roadmap()
    {
        foreach (var extension in new[] { ".htm", ".html", ".xml", ".csv", ".json", ".txt" })
        {
            Assert.True(BuiltInCatalog.TryGet(extension, out _), extension);
        }
    }

    [Fact]
    public void TryGet_is_case_insensitive_and_accepts_missing_dot()
    {
        Assert.True(BuiltInCatalog.TryGet("JSON", out var entry));
        Assert.Equal(".json", entry.Extension);
    }

    [Fact]
    public void TryGet_returns_false_for_unknown_extension()
    {
        Assert.False(BuiltInCatalog.TryGet(".zzz", out _));
    }

    [Fact]
    public void Extensions_are_unique_lowercase_and_dotted()
    {
        var extensions = BuiltInCatalog.Entries.Select(e => e.Extension).ToList();

        Assert.Equal(extensions.Count, extensions.Distinct().Count());
        Assert.All(extensions, e => Assert.Matches(@"^\.[a-z0-9]+$", e));
    }

    [Fact]
    public void Htm_and_html_share_one_icon()
    {
        BuiltInCatalog.TryGet(".htm", out var htm);
        BuiltInCatalog.TryGet(".html", out var html);

        Assert.Equal(htm.IconKey, html.IconKey);
    }
}
