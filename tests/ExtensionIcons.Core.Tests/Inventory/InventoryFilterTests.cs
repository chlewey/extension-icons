using ExtensionIcons.Core.Inventory;

namespace ExtensionIcons.Core.Tests.Inventory;

public class InventoryFilterTests
{
    private static ExtensionRegistration Reg(
        string extension, string? progId = null, string? userChoice = null,
        string? description = null, string? command = null) =>
        new(extension, progId, userChoice, null, description, command);

    [Fact]
    public void Default_filter_matches_everything()
    {
        Assert.True(new InventoryFilter().Matches(Reg(".zzz")));
    }

    [Theory]
    [InlineData("htm", true)]
    [InlineData("HTMLFILE", true)]
    [InlineData("hypertext", true)]
    [InlineData("msedge", true)]
    [InlineData("  htm  ", true)]
    [InlineData("nothing", false)]
    public void Search_matches_extension_progid_description_and_command_ignoring_case(string search, bool expected)
    {
        var registration = Reg(".htm", "htmlfile", description: "Hypertext Document", command: @"""C:\msedge.exe"" %1");

        Assert.Equal(expected, new InventoryFilter(Search: search).Matches(registration));
    }

    [Fact]
    public void CatalogOnly_keeps_only_catalog_extensions()
    {
        var filter = new InventoryFilter(CatalogOnly: true);

        Assert.True(filter.Matches(Reg(".json")));
        Assert.True(filter.Matches(Reg(".HTM")));
        Assert.False(filter.Matches(Reg(".zzz")));
    }

    [Fact]
    public void UserChoiceOnly_keeps_only_extensions_with_an_override()
    {
        var filter = new InventoryFilter(UserChoiceOnly: true);

        Assert.True(filter.Matches(Reg(".htm", "htmlfile", userChoice: "MSEdgeHTM")));
        Assert.False(filter.Matches(Reg(".htm", "htmlfile")));
    }

    [Fact]
    public void Conditions_combine_with_and()
    {
        var filter = new InventoryFilter(Search: "edge", CatalogOnly: true, UserChoiceOnly: true);

        Assert.True(filter.Matches(Reg(".htm", userChoice: "MSEdgeHTM")));
        Assert.False(filter.Matches(Reg(".zzz", userChoice: "MSEdgeHTM")));
        Assert.False(filter.Matches(Reg(".htm", userChoice: "Other")));
    }
}
