Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public class WinDesktop {
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
}
"@

$found = 0
[WinDesktop]::EnumWindows({
    param($hwnd, $lparam)
    $cls = New-Object System.Text.StringBuilder 256
    [WinDesktop]::GetClassName($hwnd, $cls, 256) | Out-Null
    $className = $cls.ToString()
    if ($className -eq "#32770") {
        $sb = New-Object System.Text.StringBuilder 256
        [WinDesktop]::GetWindowText($hwnd, $sb, 256) | Out-Null
        $title = $sb.ToString()
        Write-Host "Found #32770 dialog HWND=$hwnd, Title='$title'"
        if ($title -like "*Revit*") {
            Write-Host "   -> Revit dialog found! Clicking OK..."
            $btn = [WinDesktop]::FindWindowEx($hwnd, [IntPtr]::Zero, "Button", "OK")
            if ($btn -ne [IntPtr]::Zero) {
                [WinDesktop]::SendMessage($btn, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
                Write-Host "   -> Clicked OK button!"
            }
        }
        $script:found++
    }
    return $true
}, [IntPtr]::Zero) | Out-Null

Write-Host "Total dialogs found on desktop: $found"
