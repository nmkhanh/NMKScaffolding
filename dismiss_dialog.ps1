Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public class WinHelper {
    public delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
}
"@

$proc = Get-Process -Id 10468 -ErrorAction SilentlyContinue
if (-not $proc) {
    Write-Host "Process 10468 not found"
    exit
}

foreach ($thread in $proc.Threads) {
    [WinHelper]::EnumThreadWindows($thread.Id, {
        param($hwnd, $lparam)
        $sb = New-Object System.Text.StringBuilder 256
        [WinHelper]::GetWindowText($hwnd, $sb, 256) | Out-Null
        $title = $sb.ToString()
        $cls = New-Object System.Text.StringBuilder 256
        [WinHelper]::GetClassName($hwnd, $cls, 256) | Out-Null
        $className = $cls.ToString()

        if ($title -or $className) {
            Write-Host "HWND: $hwnd | Class: $className | Title: $title"
            if ($title -like "*Security*" -or $title -like "*Add-in*" -or $title -like "*Autodesk Revit*") {
                Write-Host "Target dialog found: $title. Sending Enter / BM_CLICK..."
                [WinHelper]::SetForegroundWindow($hwnd)
                # Send WM_COMMAND with ID 1001 (Always Load) or 1002 (Load Once)
                [WinHelper]::PostMessage($hwnd, 0x0111, [IntPtr]1001, [IntPtr]0)
                [WinHelper]::PostMessage($hwnd, 0x0111, [IntPtr]1002, [IntPtr]0)
                # Also send WM_KEYDOWN VK_RETURN
                [WinHelper]::PostMessage($hwnd, 0x0100, [IntPtr]0x0D, [IntPtr]0)
            }
        }
        return $true
    }, [IntPtr]::Zero) | Out-Null
}
