using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

// Spike 002: how does Windows resolve a file extension's icon when (1) a UserChoice override
// exists and (2) several extensions share one ProgID? See ../002-icon-resolution.md.
//
// Part A only READS the registry/shell for real extensions.
// Part B writes only under HKCU\Software\Classes for throwaway extensions
// (.exticonspike002a/.b) and throwaway ProgIDs (ExtIcons.Spike002.*), and removes
// everything in a finally block.

const string ExtA = ".exticonspike002a";
const string ExtB = ".exticonspike002b";
const string Shared = "ExtIcons.Spike002.Shared";
const string Dedicated = "ExtIcons.Spike002.A";
const string FileExts = @"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts";

Console.WriteLine("== Part A: real extensions, read-only ==");
foreach (var ext in new[] { ".htm", ".html", ".xml", ".csv", ".txt" })
{
    var userChoice = ReadHkcu($@"{FileExts}\{ext}\UserChoice", "ProgId");
    var classProgId = ReadClasses(ext, "");
    Console.WriteLine($"{ext}: UserChoice={userChoice ?? "-"}  class={classProgId ?? "-"}");
    foreach (var (label, progId) in new[] { ("UserChoice", userChoice), ("class", classProgId) })
    {
        if (progId is not null)
            Console.WriteLine($"    {label} ProgID DefaultIcon = {ReadClasses($@"{progId}\DefaultIcon", "") ?? "(none)"}");
    }
    Report("    shell says", ext);
}

Console.WriteLine();
Console.WriteLine("== Part B: throwaway extensions ==");
try
{
    Cleanup();

    SetupShared();
    Notify();
    Report("1. shared ProgID, A", ExtA);
    Report("1. shared ProgID, B", ExtB);

    Write($@"{ExtA}\DefaultIcon", "shell32.dll,3");
    Notify();
    Report("2. + extension-key DefaultIcon on A (3), A", ExtA);
    Report("2.   B", ExtB);
    Delete(ExtA + @"\DefaultIcon");

    Write($@"SystemFileAssociations\{ExtA}\DefaultIcon", "shell32.dll,4");
    Notify();
    Report("3. + SystemFileAssociations DefaultIcon on A (4), A", ExtA);
    Report("3.   B", ExtB);
    Delete($@"SystemFileAssociations\{ExtA}");

    Write(Dedicated, "Spike dedicated");
    Write($@"{Dedicated}\DefaultIcon", "shell32.dll,1");
    Write($@"{Dedicated}\shell\open\command", "notepad.exe \"%1\"");
    Write(ExtA, Dedicated);
    Notify();
    Report("4. dedicated ProgID for A (1), A", ExtA);
    Report("4.   B", ExtB);
    Console.WriteLine($"    open executable: A={Assoc(ExtA, 2)}  B={Assoc(ExtB, 2)}");
}
finally
{
    Cleanup();
    Notify();
    Console.WriteLine();
    Console.WriteLine($"Cleanup done. Leftovers: {Leftovers()}");
}

// ---- helpers ----

void SetupShared()
{
    Write(Shared, "Spike shared");
    Write($@"{Shared}\DefaultIcon", "shell32.dll,41");
    Write($@"{Shared}\shell\open\command", "notepad.exe \"%1\"");
    Write(ExtA, Shared);
    Write(ExtB, Shared);
}

void Report(string label, string ext) =>
    Console.WriteLine($"{label}: AssocQuery={Assoc(ext, 15) ?? "(none)"}  SHGetFileInfo={FileInfoLocation(ext)}");

static string? ReadHkcu(string path, string name)
{
    using var key = Registry.CurrentUser.OpenSubKey(path);
    return key?.GetValue(name) as string;
}

static string? ReadClasses(string path, string name)
{
    using var key = Registry.ClassesRoot.OpenSubKey(path);
    return key?.GetValue(name) as string;
}

static void Write(string classesPath, string value) => WriteNamed(classesPath, "", value);

static void WriteNamed(string classesPath, string name, string value)
{
    using var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{classesPath}");
    key.SetValue(name, value);
}

static void Delete(string classesPath) =>
    Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{classesPath}", throwOnMissingSubKey: false);

void Cleanup()
{
    foreach (var path in new[]
    {
        ExtA, ExtB, Shared, Dedicated,
        $@"SystemFileAssociations\{ExtA}", $@"SystemFileAssociations\{ExtB}",
    })
    {
        Delete(path);
    }
    foreach (var ext in new[] { ExtA, ExtB })
    {
        Registry.CurrentUser.DeleteSubKeyTree($@"{FileExts}\{ext}", throwOnMissingSubKey: false);
    }
}

string Leftovers()
{
    var found = new List<string>();
    foreach (var path in new[] { ExtA, ExtB, Shared, Dedicated, $@"SystemFileAssociations\{ExtA}" })
    {
        using var key = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{path}");
        if (key is not null) found.Add(path);
    }
    return found.Count == 0 ? "none" : string.Join(", ", found);
}

static void Notify()
{
    SHChangeNotify(0x08000000 /* SHCNE_ASSOCCHANGED */, 0, IntPtr.Zero, IntPtr.Zero);
    Thread.Sleep(1500); // the first run showed SHGetFileInfo lagging AssocQueryString; give the shell time to settle
}

static string? Assoc(string ext, int what)
{
    var buffer = new StringBuilder(1024);
    uint size = (uint)buffer.Capacity;
    return AssocQueryString(0, what, ext, null, buffer, ref size) == 0 ? buffer.ToString() : null;
}

static string FileInfoLocation(string ext)
{
    var info = new SHFILEINFO();
    SHGetFileInfo("file" + ext, 0x80 /* FILE_ATTRIBUTE_NORMAL */, ref info,
        (uint)Marshal.SizeOf<SHFILEINFO>(), 0x1000 /* ICONLOCATION */ | 0x10 /* USEFILEATTRIBUTES */);
    return $"{info.szDisplayName},{info.iIcon}";
}

[DllImport("shell32.dll")]
static extern void SHChangeNotify(int eventId, uint flags, IntPtr item1, IntPtr item2);

[DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
static extern int AssocQueryString(int flags, int str, string assoc, string? extra, StringBuilder output, ref uint outputLength);

[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
static extern IntPtr SHGetFileInfo(string path, uint attributes, ref SHFILEINFO info, uint size, uint flags);

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
struct SHFILEINFO
{
    public IntPtr hIcon;
    public int iIcon;
    public uint dwAttributes;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName;
}
