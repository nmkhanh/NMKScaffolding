Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public class Win32Dlg {
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
}
"@

[Win32Dlg]::EnumWindows({
    param($hwnd, $lp)
    $sb = New-Object System.Text.StringBuilder 256
    [Win32Dlg]::GetWindowText($hwnd, $sb, 256) | Out-Null
    $title = $sb.ToString()
    if ($title -like "*Security*" -or $title -like "*Signature*" -or $title -like "*signed*") {
        Write-Host "Found Window: $title (HWND: $hwnd). Sending Button 1001..."
        [Win32Dlg]::SetForegroundWindow($hwnd)
        [Win32Dlg]::PostMessage($hwnd, 0x0111, [IntPtr]1001, [IntPtr]0)
        # Also send Alt+L
        [Win32Dlg]::PostMessage($hwnd, 0x0104, [IntPtr]0x4C, [IntPtr]0x20000000)
    }
    return $true
}, [IntPtr]::Zero) | Out-Null
