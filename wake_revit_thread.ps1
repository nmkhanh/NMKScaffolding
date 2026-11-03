Add-Type @"
using System;
using System.Runtime.InteropServices;

public class WinWakeup {
    [DllImport("user32.dll")]
    public static extern bool PostThreadMessage(int idThread, uint Msg, IntPtr wParam, IntPtr lParam);
}
"@

# Thread 31248 is Revit main UI thread!
$res = [WinWakeup]::PostThreadMessage(31248, 0x0000, [IntPtr]::Zero, [IntPtr]::Zero)
Write-Host "PostThreadMessage(31248, WM_NULL) = $res"
