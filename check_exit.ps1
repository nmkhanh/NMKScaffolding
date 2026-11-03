$lines = Get-Content 'C:\Users\Admin\AppData\Local\Autodesk\Revit\Autodesk Revit 2022\Journals\journal.0037.txt'
$idx = 0
for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match 'ExitNativeInstance') {
        $idx = $i
        break
    }
}
$start = [Math]::Max(0, $idx - 30)
$end = [Math]::Min($lines.Length - 1, $idx + 5)
$lines[$start..$end]
