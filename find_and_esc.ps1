Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public class WinFindRevit {
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")]
    public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
}
"@

$targetPid = 15480
[WinFindRevit]::EnumWindows({
    param($hwnd, $lparam)
    $p = 0
    [WinFindRevit]::GetWindowThreadProcessId($hwnd, [ref]$p) | Out-Null
    if ($p -eq $targetPid) {
        $sb = New-Object System.Text.StringBuilder 256
        [WinFindRevit]::GetWindowText($hwnd, $sb, 256) | Out-Null
        $t = $sb.ToString()
        if ($t) {
            Write-Host "Found Revit Window: HWND=$hwnd, Title='$t'"
            # Send ESC
            [WinFindRevit]::PostMessage($hwnd, 0x0100, [IntPtr]0x1B, [IntPtr]0) | Out-Null
            [WinFindRevit]::PostMessage($hwnd, 0x0101, [IntPtr]0x1B, [IntPtr]0) | Out-Null
        }
    }
    return $true
}, [IntPtr]::Zero) | Out-Null
