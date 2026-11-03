Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public class WinAPI {
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern bool EnumThreadWindows(int dwThreadId, EnumWindowsProc lpfn, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
}
"@

$proc = Get-Process -Id 15480 -ErrorAction SilentlyContinue
if (-not $proc) {
    Write-Host "Revit PID 15480 not found"
    exit
}

Write-Host "Revit 2022 PID: $($proc.Id)"

$found = 0
foreach ($thread in $proc.Threads) {
    [WinAPI]::EnumThreadWindows($thread.Id, {
        param($hwnd, $lparam)
        $sb = New-Object System.Text.StringBuilder 256
        [WinAPI]::GetWindowText($hwnd, $sb, 256) | Out-Null
        $title = $sb.ToString()
        $cls = New-Object System.Text.StringBuilder 256
        [WinAPI]::GetClassName($hwnd, $cls, 256) | Out-Null
        $className = $cls.ToString()

        if ($className -eq "#32770" -or $title -like "*Autodesk Revit*") {
            # Check child windows
            [WinAPI]::EnumChildWindows($hwnd, {
                param($childHwnd, $childLparam)
                $btnText = New-Object System.Text.StringBuilder 256
                [WinAPI]::GetWindowText($childHwnd, $btnText, 256) | Out-Null
                $bt = $btnText.ToString()
                if ($bt -eq "OK" -or $bt -eq "&OK") {
                    Write-Host "Found OK Button in HWND=$hwnd (Title='$title')! Clicking it..."
                    # BM_CLICK = 0x00F5
                    [WinAPI]::SendMessage($childHwnd, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
                    $script:found++
                }
                return $true
            }, [IntPtr]::Zero) | Out-Null
        }
        return $true
    }, [IntPtr]::Zero) | Out-Null
}

Write-Host "Dismiss check finished. Clicked count: $found"
