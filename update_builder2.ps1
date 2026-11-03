$file = "RevitScaffoldingTool\ScaffoldingBuilder.cs"
$content = [System.IO.File]::ReadAllText($file, [System.Text.Encoding]::UTF8)

# 1. Cap nhat PlaceConfiguredPosts de check opts.IsFromData
$oldSnippet = @"
            bool hasCustomPosts = opts != null && (opts.Post1Height > 0 || opts.Post2Height > 0 || (opts.Post3Count > 0 && opts.Post3Height > 0) || opts.Post4Height > 0 || opts.Post5Height > 0);

            if (!hasCustomPosts)
            {
                var col = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(cx), MmToFt(cy), MmToFt(baseZ)), 0, null, 0);
                if (col != null) { ConfigureAlinco2700(col); placedIds.Add(col.Id.IntegerValue); }
                return;
            }
"@

$newSnippet = @"
            // Neu KHONG PHAI From Data -> Dung mac dinh ALINCO 2700
            if (opts == null || !opts.IsFromData)
            {
                var col = PlaceInstance(doc, sym3, level, new XYZ(MmToFt(cx), MmToFt(cy), MmToFt(baseZ)), 0, null, 0);
                if (col != null) { ConfigureAlinco2700(col); placedIds.Add(col.Id.IntegerValue); }
                return;
            }
"@

# Normalize line endings for replacement
$oldNorm = $oldSnippet.Replace("`r`n", "`n")
$contentNorm = $content.Replace("`r`n", "`n")

if ($contentNorm.Contains($oldNorm)) {
    $contentNorm = $contentNorm.Replace($oldNorm, $newSnippet.Replace("`r`n", "`n"))
    Write-Host "Replaced PlaceConfiguredPosts logic successfully."
} else {
    Write-Host "Warning: Could not find oldSnippet in PlaceConfiguredPosts."
}

# 2. Them ham GetTotalPostHeight truoc PlaceConfiguredPosts neu chua co
if (-not $contentNorm.Contains("GetTotalPostHeight")) {
    $func = @"
        private static double GetTotalPostHeight(BuildOptions opts)
        {
            if (opts == null || !opts.IsFromData) return 2700.0;
            double total = 0.0;
            if (opts.Post1Height > 0) total += opts.Post1Height;
            if (opts.Post2Height > 0) total += opts.Post2Height;
            if (opts.Post3Height > 0 && opts.Post3Count > 0) total += (opts.Post3Height * opts.Post3Count);
            if (opts.Post4Height > 0) total += opts.Post4Height;
            if (opts.Post5Height > 0) total += opts.Post5Height;
            return total;
        }

        private static void PlaceConfiguredPosts
"@
    $contentNorm = $contentNorm.Replace("        private static void PlaceConfiguredPosts", $func.Replace("`r`n", "`n"))
    Write-Host "Added GetTotalPostHeight function."
}

# Ghi lai file voi CRLF
$contentFinal = $contentNorm.Replace("`n", "`r`n")
[System.IO.File]::WriteAllText($file, $contentFinal, [System.Text.Encoding]::UTF8)
Write-Host "ScaffoldingBuilder.cs updated successfully."
