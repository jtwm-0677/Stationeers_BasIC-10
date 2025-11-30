$content = Get-Content "C:\Program Files (x86)\Steam\steamapps\common\Stationeers\rocketstation_Data\StreamingAssets\Language\english.xml" -Raw

# Extract all Key values from Record elements
$pattern = '<Key>([^<]+)</Key>'
$matches = [regex]::Matches($content, $pattern)
$keys = $matches | ForEach-Object { $_.Groups[1].Value } | Where-Object {
    $_ -match '^(Structure|Item|Dynamic|Circuitboard|Appliance|Device|Modular|Logic|Sensor|Kit)' -or
    $_ -match '(Console|Display|Light|LED|Switch|Button|Memory|Dial|Reader|Writer|Vent|Pump|Sensor|Tank|Door|Airlock)'
} | Sort-Object -Unique
$keys | ForEach-Object { Write-Output $_ }
