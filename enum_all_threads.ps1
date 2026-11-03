Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public class WinEnum {
    public delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
}
"@

$proc = Get-Process -Id 16620
foreach ($t in $proc.Threads) {
    [WinEnum]::EnumThreadWindows($t.Id, {
        param($hwnd, $lp)
        $sb = New-Object System.Text.StringBuilder 256
        [WinEnum]::GetWindowText($hwnd, $sb, 256) | Out-Null
        $cls = New-Object System.Text.StringBuilder 256
        [WinEnum]::GetClassName($hwnd, $cls, 256) | Out-Null
        
        $title = $sb.ToString()
        $cName = $cls.ToString()
        Write-Host "Thread $($t.Id) -> HWND: $hwnd | Class: $cName | Title: $title"
        
        [WinEnum]::SetForegroundWindow($hwnd)
        # Send Button 1001 (Load)
        [WinEnum]::PostMessage($hwnd, 0x0111, [IntPtr]1001, [IntPtr]0)
        return $true
    }, [IntPtr]::Zero) | Out-Null
}
