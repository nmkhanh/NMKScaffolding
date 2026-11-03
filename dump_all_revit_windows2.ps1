Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public class WinDumper2 {
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern bool EnumThreadWindows(int dwThreadId, EnumWindowsProc lpfn, IntPtr lParam);
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

$proc = Get-Process -Id 15480 -ErrorAction SilentlyContinue
if (-not $proc) {
    Write-Host "Revit 15480 not found"
    exit
}

foreach ($thread in $proc.Threads) {
    [WinDumper2]::EnumThreadWindows($thread.Id, {
        param($hwnd, $lparam)
        $cls = New-Object System.Text.StringBuilder 256
        [WinDumper2]::GetClassName($hwnd, $cls, 256) | Out-Null
        $className = $cls.ToString()
        $sb = New-Object System.Text.StringBuilder 256
        [WinDumper2]::GetWindowText($hwnd, $sb, 256) | Out-Null
        $title = $sb.ToString()
        $en = [WinDumper2]::IsWindowEnabled($hwnd)
        $vis = [WinDumper2]::IsWindowVisible($hwnd)
        if ($title -or $className -eq "#32770") {
            Write-Host "HWND: $hwnd | Class: $className | Enabled: $en | Visible: $vis | Title: '$title'"
        }
        return $true
    }, [IntPtr]::Zero) | Out-Null
}
