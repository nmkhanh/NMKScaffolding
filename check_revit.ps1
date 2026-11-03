$journals = Get-ChildItem "C:\Users\Admin\AppData\Local\Autodesk\Revit\Autodesk Revit 2022\Journals\journal.*.txt" | Sort-Object LastWriteTime -Descending
if ($journals.Count -gt 0) {
    $latest = $journals[0].FullName
    Write-Host "Latest Journal: $latest"
    Get-Content $latest -Tail 25
}
