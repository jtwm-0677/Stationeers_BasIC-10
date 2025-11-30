$content = Get-Content "C:\Program Files (x86)\Steam\steamapps\common\Stationeers\rocketstation_Data\StreamingAssets\Language\english.xml" -Raw
$pattern = '\{THING:([A-Za-z0-9_]+)\}'
$matches = [regex]::Matches($content, $pattern)
$things = $matches | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
$things | ForEach-Object { Write-Output $_ }
