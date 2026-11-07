using ExtensionIcons.Core.Inventory;

namespace ExtensionIcons.Core.Tests.Inventory;

public class IconLocationTests
{
    [Theory]
    [InlineData(@"shell32.dll,41", "shell32.dll", 41)]
    [InlineData(@"C:\Windows\System32\imageres.dll,-102", @"C:\Windows\System32\imageres.dll", -102)]
    [InlineData(@"""C:\Program Files\App\app.exe"",3", @"C:\Program Files\App\app.exe", 3)]
    [InlineData(@"""C:\Program Files\App\app.exe""", @"C:\Program Files\App\app.exe", 0)]
    [InlineData(@"C:\icons\thing.ico", @"C:\icons\thing.ico", 0)]
    [InlineData(@"%SystemRoot%\system32\shell32.dll, 7", @"%SystemRoot%\system32\shell32.dll", 7)]
    public void TryParse_splits_path_and_index(string value, string path, int index)
    {
        Assert.True(IconLocation.TryParse(value, out var actualPath, out var actualIndex));
        Assert.Equal(path, actualPath);
        Assert.Equal(index, actualIndex);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(@"@{Microsoft.WindowsNotepad_11.0_x64__8wekyb3d8bbwe?ms-resource://Microsoft.WindowsNotepad/Files/Assets/txtfile.png}")]
    [InlineData(@"""unterminated,0")]
    [InlineData(@""""",0")]
    public void TryParse_rejects_values_without_a_loadable_path(string? value)
    {
        Assert.False(IconLocation.TryParse(value, out _, out _));
    }
}
