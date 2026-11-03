<#
Icon-override spike: validate, on a throwaway extension that has never been
seen by Windows (so no UserChoice exists and nothing real is touched), that
the extension -> ProgID -> DefaultIcon chain resolves the way the roadmap
assumes, that it can be changed and refreshed without a reboot, and that
doing so does not disturb the registered "open" command.
Cleans up all created keys/files at the end, including on error.
Findings recorded in 001-icon-override-mechanism.md.
#>

$ext = ".exticonspike123"
$progId = "ExtIcons.SpikeTest"
$extKey = "HKCU:\Software\Classes\$ext"
$progKey = "HKCU:\Software\Classes\$progId"

function Get-ShellIconLocation($path) {
    Add-Type -Namespace Spike -Name Shell32 -MemberDefinition @'
[DllImport("shell32.dll", EntryPoint = "SHGetFileInfoW", CharSet = CharSet.Unicode)]
public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct SHFILEINFO {
    public IntPtr hIcon;
    public int iIcon;
    public uint dwAttributes;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName;
}
'@ -ErrorAction SilentlyContinue

    $SHGFI_ICONLOCATION = 0x1000
    $SHGFI_USEFILEATTRIBUTES = 0x10
    $info = New-Object Spike.Shell32+SHFILEINFO
    [Spike.Shell32]::SHGetFileInfo($path, 0x80, [ref]$info, [System.Runtime.InteropServices.Marshal]::SizeOf($info), $SHGFI_ICONLOCATION -bor $SHGFI_USEFILEATTRIBUTES) | Out-Null
    return "$($info.szDisplayName),$($info.iIcon)"
}

function Notify-AssocChanged {
    Add-Type -Namespace Spike -Name Shell32Notify -MemberDefinition @'
[DllImport("shell32.dll")]
public static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
'@ -ErrorAction SilentlyContinue
    $SHCNE_ASSOCCHANGED = 0x08000000
    $SHCNF_IDLIST = 0x0000
    [Spike.Shell32Notify]::SHChangeNotify($SHCNE_ASSOCCHANGED, $SHCNF_IDLIST, [IntPtr]::Zero, [IntPtr]::Zero)
}

try {
    Write-Host "== Baseline: resolved icon location for an unregistered extension ==" -ForegroundColor Cyan
    $testFile = Join-Path $env:TEMP "spike-test$ext"
    "spike" | Out-File -FilePath $testFile -Encoding ascii
    Write-Host "Before registration: $(Get-ShellIconLocation $testFile)"

    Write-Host "`n== Registering throwaway extension -> ProgID -> DefaultIcon + open command ==" -ForegroundColor Cyan
    New-Item -Path $extKey -Force | Out-Null
    Set-ItemProperty -Path $extKey -Name "(Default)" -Value $progId

    New-Item -Path $progKey -Force | Out-Null
    Set-ItemProperty -Path $progKey -Name "(Default)" -Value "Extension Icons Spike Test File"

    New-Item -Path "$progKey\DefaultIcon" -Force | Out-Null
    Set-ItemProperty -Path "$progKey\DefaultIcon" -Name "(Default)" -Value "%SystemRoot%\System32\shell32.dll,41"

    New-Item -Path "$progKey\shell\open\command" -Force | Out-Null
    Set-ItemProperty -Path "$progKey\shell\open\command" -Name "(Default)" -Value '"%SystemRoot%\System32\notepad.exe" "%1"'

    Notify-AssocChanged
    Start-Sleep -Milliseconds 300

    Write-Host "After registration (expect shell32.dll,41): $(Get-ShellIconLocation $testFile)"

    Write-Host "`n== Changing ONLY the icon, leaving open command untouched ==" -ForegroundColor Cyan
    Set-ItemProperty -Path "$progKey\DefaultIcon" -Name "(Default)" -Value "%SystemRoot%\System32\shell32.dll,1"
    Notify-AssocChanged
    Start-Sleep -Milliseconds 300
    Write-Host "After icon-only change (expect shell32.dll,1): $(Get-ShellIconLocation $testFile)"

    $openCmd = (Get-ItemProperty -Path "$progKey\shell\open\command" -Name "(Default)").'(Default)'
    Write-Host "Open command still registered as: $openCmd"
}
finally {
    Write-Host "`n== Cleanup ==" -ForegroundColor Cyan
    Remove-Item -Path $extKey -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $progKey -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $testFile -Force -ErrorAction SilentlyContinue
    Notify-AssocChanged
    $stillThere = (Test-Path $extKey) -or (Test-Path $progKey) -or (Test-Path $testFile)
    Write-Host "Any spike artifacts remaining: $stillThere"
}
