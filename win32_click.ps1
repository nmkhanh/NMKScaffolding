Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public class Win32Helper {
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
}
"@

$pidTarget = 25324

[Win32Helper]::EnumWindows({
    param($hwnd, $lparam)
    $procId = 0
    [Win32Helper]::GetWindowThreadProcessId($hwnd, [ref]$procId)
    
    $sb = New-Object System.Text.StringBuilder 256
    [Win32Helper]::GetWindowText($hwnd, $sb, 256) | Out-Null
    $title = $sb.ToString()

    $cls = New-Object System.Text.StringBuilder 256
    [Win32Helper]::GetClassName($hwnd, $cls, 256) | Out-Null
    $className = $cls.ToString()

    if ($procId -eq $pidTarget -or $title -like "*Security*" -or $title -like "*Unsigned*") {
        Write-Host "Found HWND: $hwnd | PID: $procId | Class: $className | Title: $title"
        # Bring to front
        [Win32Helper]::SetForegroundWindow($hwnd)
        # Click Button 1001 (Always Load)
        [Win32Helper]::PostMessage($hwnd, 0x0111, [IntPtr]1001, [IntPtr]0)
        [Win32Helper]::PostMessage($hwnd, 0x0111, [IntPtr]1002, [IntPtr]0)
    }
    return $true
}, [IntPtr]::Zero) | Out-Null
