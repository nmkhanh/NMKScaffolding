$data = Get-Content all_4_instances_params.json | ConvertFrom-Json
$ids = ($data | Get-Member -MemberType NoteProperty).Name
foreach ($id in $ids) {
    $inst = $data.$id
    Write-Host "================ INSTANCE $id ================"
    $w = ($inst.parameters.instance | Where-Object { $_.name -eq 'W' }).displayValue
    $len = ($inst.parameters.instance | Where-Object { $_.name -eq 'Length' }).displayValue
    $bays = @()
    for ($i = 1; $i -le 10; $i++) {
        $g = ($inst.parameters.instance | Where-Object { $_.name -eq "Group_$i" }).value
        if ($g -eq 1) {
            $n = ($inst.parameters.instance | Where-Object { $_.name -eq "Group_${i}_N" }).value
            $l = ($inst.parameters.instance | Where-Object { $_.name -eq "Group_${i}_L" }).displayValue
            $bays += "${n}x${l}"
        }
    }
    Write-Host "Width: $w, Length: $len, Bays: $($bays -join ', ')"
}
