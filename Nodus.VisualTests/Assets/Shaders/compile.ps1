$shaderFiles = Get-ChildItem -Path .\* -Recurse -Include *.vert, *.frag, *.comp, *.geom, *.tesc, *.tese
$outputDir = "./compiled"

if (-not(Test-Path $outputDir)) {
    New-Item -Type Directory -Path $outputDir
}

$tempSuffix = "temp"

foreach ($shaderFile in $shaderFiles) {
    Write-Host "Compiling $($shaderFile.Name) ..." -ForegroundColor Cyan
    
    $tempfile = [System.IO.Path]::ChangeExtension($shaderFile.FullName, $tempSuffix + [System.IO.Path]::GetExtension($shaderFile.FullName))
    Get-Content $shaderFile.FullName | Set-Content -Path $tempFile -Encoding utf8

    $outputFile = Join-Path $outputDir ([System.IO.Path]::GetFileName($shaderFile.FullName) + ".spv")

    glslc $tempFile -o $outputFile

    if ($LASTEXITCODE -eq 0) {
        Write-Host "Compiled $($shaderFile.Name) successfully to $($outputFile)" -ForegroundColor Green
    } else {
        Write-Host "Failed to compile $($shaderFile.Name)" -ForegroundColor Red
    }

    Remove-Item $tempFile -Force
}