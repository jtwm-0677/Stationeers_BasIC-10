# FindEnums.ps1 - Find all enum types in the assembly
$dllPath = "C:\Program Files (x86)\Steam\steamapps\common\Stationeers\rocketstation_Data\Managed\Assembly-CSharp.dll"
$assembly = [System.Reflection.Assembly]::LoadFrom($dllPath)

Write-Host "=== All Logic-related Enums ==="
$assembly.GetTypes() | Where-Object {
    $_.IsEnum -and ($_.Name -like '*Logic*' -or $_.Name -like '*Batch*' -or $_.Name -like '*Reagent*')
} | ForEach-Object {
    Write-Host $_.FullName
}
