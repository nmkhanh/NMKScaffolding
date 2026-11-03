Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;
public class WinUtil {
    public delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);
}
"@

foreach ($targetPid in @(15480, 41976)) {
    Write-Host "Checking PID $targetPid..."
    $proc = Get-Process -Id $targetPid -ErrorAction SilentlyContinue
    if (-not $proc) { continue }
    foreach ($t in $proc.Threads) {
        [WinUtil]::EnumThreadWindows($t.Id, {
            param($hwnd, $lp)
            $vis = [WinUtil]::IsWindowVisible($hwnd)
            $sb = New-Object System.Text.StringBuilder 256
            [WinUtil]::GetWindowText($hwnd, $sb, 256) | Out-Null
            $cls = New-Object System.Text.StringBuilder 256
            [WinUtil]::GetClassName($hwnd, $cls, 256) | Out-Null
            $title = $sb.ToString()
            if ($title.Length -gt 0 -or $vis) {
                Write-Host "PID $targetPid Thread $($t.Id) -> Vis: $vis | HWND: $hwnd | Class: $($cls.ToString()) | Title: $title"
            }
            return $true
        }, [IntPtr]::Zero) | Out-Null
    }
}
