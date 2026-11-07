using ExtensionIcons.Core.Inventory;

namespace ExtensionIcons.Core.Tests.Inventory;

public class OpenCommandTests
{
    [Theory]
    [InlineData(@"""C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"" --single-argument %1", "msedge.exe")]
    [InlineData(@"C:\Windows\System32\notepad.exe %1", "notepad.exe")]
    [InlineData(@"C:\Program Files\Some App\app.exe ""%1""", "app.exe")]
    [InlineData(@"%SystemRoot%\system32\NOTEPAD.EXE %1", "NOTEPAD.EXE")]
    [InlineData(@"rundll32.exe shell32.dll,OpenAs_RunDLL %1", "rundll32.exe")]
    [InlineData(@"""C:\App\run.exe""", "run.exe")]
    [InlineData(@"launcher %1", "launcher")]
    public void GetExecutableName_returns_the_program_file_name(string command, string expected)
    {
        Assert.Equal(expected, OpenCommand.GetExecutableName(command));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void GetExecutableName_returns_null_for_empty_input(string? command)
    {
        Assert.Null(OpenCommand.GetExecutableName(command));
    }
}
