Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public class WinFind {
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
}
"@

$w = [WinFind]::FindWindow("#32770", "Autodesk Revit 2022")
Write-Host "FindWindow('#32770', 'Autodesk Revit 2022') = $w"
if ($w -ne [IntPtr]::Zero) {
    # Send Enter key
    Write-Host "Sending Enter..."
    [WinFind]::PostMessage($w, 0x0100, [IntPtr]0x0D, [IntPtr]0) | Out-Null
    # Also find OK button
    $btn = [WinFind]::FindWindowEx($w, [IntPtr]::Zero, "Button", "OK")
    Write-Host "OK button HWND = $btn"
    if ($btn -ne [IntPtr]::Zero) {
        [WinFind]::SendMessage($btn, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
    }
} else {
    # Try finding any window titled "Autodesk Revit 2022"
    $w2 = [WinFind]::FindWindow($null, "Autodesk Revit 2022")
    Write-Host "FindWindow(null, 'Autodesk Revit 2022') = $w2"
}
