Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public class WinDumper {
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
    public static extern bool IsWindowVisible(IntPtr hWnd);
}
"@

$proc = Get-Process -Name Revit -ErrorAction SilentlyContinue | Where-Object { $_.Path -like "*2022*" }
if (-not $proc) {
    Write-Host "Revit 2022 not found"
    exit
}

Write-Host "Revit PID: $($proc.Id)"

foreach ($thread in $proc.Threads) {
    [WinDumper]::EnumThreadWindows($thread.Id, {
        param($hwnd, $lparam)
        $vis = [WinDumper]::IsWindowVisible($hwnd)
        $cls = New-Object System.Text.StringBuilder 256
        [WinDumper]::GetClassName($hwnd, $cls, 256) | Out-Null
        $className = $cls.ToString()
        $sb = New-Object System.Text.StringBuilder 256
        [WinDumper]::GetWindowText($hwnd, $sb, 256) | Out-Null
        $title = $sb.ToString()
        if ($vis -and ($title -or $className -eq "#32770")) {
            Write-Host "HWND: $hwnd | Class: $className | Visible: $vis | Title: '$title'"
            [WinDumper]::EnumChildWindows($hwnd, {
                param($ch, $lp)
                $csb = New-Object System.Text.StringBuilder 256
                [WinDumper]::GetWindowText($ch, $csb, 256) | Out-Null
                $cText = $csb.ToString()
                if ($cText) {
                    Write-Host "   Child HWND: $ch | Text: '$cText'"
                }
                return $true
            }, [IntPtr]::Zero) | Out-Null
        }
        return $true
    }, [IntPtr]::Zero) | Out-Null
}
