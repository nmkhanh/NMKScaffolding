Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public class ThreadWinHelper {
    public delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
}
"@

$proc = Get-Process -Id 25324
foreach ($t in $proc.Threads) {
    [ThreadWinHelper]::EnumThreadWindows($t.Id, {
        param($hwnd, $lp)
        $sb = New-Object System.Text.StringBuilder 256
        [ThreadWinHelper]::GetWindowText($hwnd, $sb, 256) | Out-Null
        $cls = New-Object System.Text.StringBuilder 256
        [ThreadWinHelper]::GetClassName($hwnd, $cls, 256) | Out-Null
        Write-Host "Thread $($t.Id) -> HWND: $hwnd | Class: $($cls.ToString()) | Title: $($sb.ToString())"
        
        # Click 1001 / 1002
        [ThreadWinHelper]::PostMessage($hwnd, 0x0111, [IntPtr]1001, [IntPtr]0)
        [ThreadWinHelper]::PostMessage($hwnd, 0x0111, [IntPtr]1002, [IntPtr]0)
        return $true
    }, [IntPtr]::Zero) | Out-Null
}
