$data = Get-Content all_4_instances_params.json | ConvertFrom-Json
$inst = $data.'87965'
foreach ($p in $inst.parameters.instance) {
    if ($p.value -ne $null -and $p.value -ne 0 -and $p.value -ne "" -and $p.value -ne -1) {
        Write-Host "$($p.name): $($p.displayValue) (raw: $($p.value))"
    }
}
