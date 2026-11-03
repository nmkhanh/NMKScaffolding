Add-Type @"
using System;
using System.Runtime.InteropServices;

public class WinEscape {
    [DllImport("user32.dll")]
    public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
}
"@

$proc = Get-Process -Id 15480 -ErrorAction SilentlyContinue
if ($proc -and $proc.MainWindowHandle -ne [IntPtr]::Zero) {
    Write-Host "Revit MainWindowHandle: $($proc.MainWindowHandle)"
    # Post WM_KEYDOWN VK_ESCAPE (0x1B)
    [WinEscape]::PostMessage($proc.MainWindowHandle, 0x0100, [IntPtr]0x1B, [IntPtr]0) | Out-Null
    [WinEscape]::PostMessage($proc.MainWindowHandle, 0x0101, [IntPtr]0x1B, [IntPtr]0) | Out-Null
    Write-Host "Posted ESC key to Revit main window!"
} else {
    Write-Host "MainWindowHandle is zero"
}
