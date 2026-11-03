
$file = "C:\Users\Admin\Documents\antigravity\nifty-chandrasekhar\RevitScaffoldingTool\ScaffoldingBuilder.cs"
$lines = [System.IO.File]::ReadAllLines($file)

# We want lines 0..896 (up to end of ExecuteMultiBay)
# Then lines 1153..1254 (ConfigureAlinco2700 down to end of GetOrLoadFamilySymbol)
# Then closing braces `    }` and `}`
$clean = [System.Collections.Generic.List[string]]::new()

# Part 1: from line 0 to line 896 (1-based line 897)
for ($i = 0; $i -lt 897; $i++) {
    $clean.Add($lines[$i])
}

# Part 2: from line 1153 (1-based line 1154: private static void ConfigureAlinco2700) to line 1254 (1-based line 1255)
for ($i = 1153; $i -lt 1255; $i++) {
    $clean.Add($lines[$i])
}

# Closing braces
$clean.Add("    }")
$clean.Add("}")

[System.IO.File]::WriteAllLines($file, $clean)
Write-Output ("Cleaned file, total lines: " + $clean.Count)
