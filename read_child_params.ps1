$folder = 'H:\My Drive\Family\New folder\Child'
$files = @('1.rfa','2.rfa','3.rfa','4.rfa','5.rfa','6.rfa','7.rfa')

foreach ($f in $files) {
    $path = Join-Path $folder $f
    Write-Host "`n==============================================="
    Write-Host "FAMILY: $f"
    Write-Host "==============================================="
    $bytes = [System.IO.File]::ReadAllBytes($path)
    $text = [System.Text.Encoding]::UTF8.GetString($bytes)
    
    # 1. Check PartAtom XML
    $idx1 = $text.IndexOf('<part ')
    if ($idx1 -ge 0) {
        $idx2 = $text.IndexOf('</part>', $idx1)
        if ($idx2 -gt $idx1) {
            $xml = $text.Substring($idx1, $idx2 - $idx1 + 7)
            [xml]$pxml = $xml
            foreach ($prop in $pxml.part.properties.property) {
                if ($prop.name -notmatch '^FamilyType$|^OmniClass|^Default_Elevation$') {
                    Write-Host "  [Type Parameter] $($prop.name) = $($prop.val)"
                }
            }
        }
    }
    
    # 2. Extract Parameter definitions from Binary stream (h1..h15, Length, Width, etc.)
    $rx = [regex]'[\x00\x01]([a-zA-Z][a-zA-Z0-9_]{0,15})\x00\x00'
    $found = @{}
    foreach ($m in $rx.Matches($text)) {
        $name = $m.Groups[1].Value
        if ($name -match '^(h\d+|Length|Width|Height|d|d_|b|a|Thickness|Radius|Offset)$') {
            $found[$name] = $true
        }
    }
    if ($found.Keys.Count -gt 0) {
        Write-Host "  [Parameters Found in Family] $($found.Keys -join ', ')"
    }
}
