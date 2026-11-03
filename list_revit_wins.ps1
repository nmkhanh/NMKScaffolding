Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;
public class WinList {
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);
}
"@

$targetPid = 41976
[WinList]::EnumWindows({
    param($hwnd, $lp)
    $pidOut = 0
    [WinList]::GetWindowThreadProcessId($hwnd, [ref]$pidOut) | Out-Null
    if ($pidOut -eq $targetPid) {
        $sb = New-Object System.Text.StringBuilder 256
        [WinList]::GetWindowText($hwnd, $sb, 256) | Out-Null
        $cls = New-Object System.Text.StringBuilder 256
        [WinList]::GetClassName($hwnd, $cls, 256) | Out-Null
        $vis = [WinList]::IsWindowVisible($hwnd)
        Write-Host "HWND: $hwnd | Vis: $vis | Class: $($cls.ToString()) | Title: $($sb.ToString())"
    }
    return $true
}, [IntPtr]::Zero) | Out-Null
