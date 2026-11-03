Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public class WinEnumAll {
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")]
    public static extern bool IsWindowEnabled(IntPtr hWnd);
}
"@

$targetPid = 15480
[WinEnumAll]::EnumWindows({
    param($hwnd, $lparam)
    $pid = 0
    [WinEnumAll]::GetWindowThreadProcessId($hwnd, [ref]$pid) | Out-Null
    if ($pid -eq $targetPid) {
        $cls = New-Object System.Text.StringBuilder 256
        [WinEnumAll]::GetClassName($hwnd, $cls, 256) | Out-Null
        $className = $cls.ToString()
        $sb = New-Object System.Text.StringBuilder 256
        [WinEnumAll]::GetWindowText($hwnd, $sb, 256) | Out-Null
        $title = $sb.ToString()
        $vis = [WinEnumAll]::IsWindowVisible($hwnd)
        $en = [WinEnumAll]::IsWindowEnabled($hwnd)
        Write-Host "HWND: $hwnd | Class: $className | Visible: $vis | Enabled: $en | Title: '$title'"
    }
    return $true
}, [IntPtr]::Zero) | Out-Null
