Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public class AutoDismisser {
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

$proc = Get-Process -Name Revit -ErrorAction SilentlyContinue | Where-Object { $_.Path -like "*2022*" }
if (-not $proc) {
    Write-Host "Revit 2022 not found"
    exit
}

Write-Host "AutoDismisser active for Revit 2022 (PID: $($proc.Id))"

while ($true) {
    try {
        foreach ($thread in $proc.Threads) {
            [AutoDismisser]::EnumThreadWindows($thread.Id, {
                param($hwnd, $lparam)
                $cls = New-Object System.Text.StringBuilder 256
                [AutoDismisser]::GetClassName($hwnd, $cls, 256) | Out-Null
                $className = $cls.ToString()

                if ($className -eq "#32770") {
                    $sb = New-Object System.Text.StringBuilder 256
                    [AutoDismisser]::GetWindowText($hwnd, $sb, 256) | Out-Null
                    $title = $sb.ToString()
                    
                    # If this is a Revit dialog (Warning, Error, Duplicate instances)
                    if ($title -like "*Autodesk Revit*" -or $title -like "*Warning*") {
                        [AutoDismisser]::EnumChildWindows($hwnd, {
                            param($childHwnd, $childLparam)
                            $btnText = New-Object System.Text.StringBuilder 256
                            [AutoDismisser]::GetWindowText($childHwnd, $btnText, 256) | Out-Null
                            $bt = $btnText.ToString()
                            if ($bt -eq "OK" -or $bt -eq "&OK" -or $bt -eq "Close" -or $bt -eq "&Close") {
                                [AutoDismisser]::SendMessage($childHwnd, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
                                [AutoDismisser]::PostMessage($hwnd, 0x0111, [IntPtr]1, [IntPtr]0) | Out-Null
                            }
                            return $true
                        }, [IntPtr]::Zero) | Out-Null
                    }
                }
                return $true
            }, [IntPtr]::Zero) | Out-Null
        }
    } catch {}
    Start-Sleep -Milliseconds 200
}
