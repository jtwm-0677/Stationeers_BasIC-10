# Read all language files
$files = @(
    "C:\Program Files (x86)\Steam\steamapps\common\Stationeers\rocketstation_Data\StreamingAssets\Language\english.xml",
    "C:\Program Files (x86)\Steam\steamapps\common\Stationeers\rocketstation_Data\StreamingAssets\Language\english_help.xml",
    "C:\Program Files (x86)\Steam\steamapps\common\Stationeers\rocketstation_Data\StreamingAssets\Language\english_tooltips.xml"
)

$allLogicTypes = @()
foreach ($file in $files) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        $pattern = '\{LOGICTYPE:([A-Za-z0-9_]+)\}'
        $matches = [regex]::Matches($content, $pattern)
        $allLogicTypes += $matches | ForEach-Object { $_.Groups[1].Value }
    }
}
$allLogicTypes | Sort-Object -Unique | ForEach-Object { Write-Output $_ }
