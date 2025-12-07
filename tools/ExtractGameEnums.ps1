# ExtractGameEnums.ps1
# Extracts LogicType, LogicSlotType, and other enums from Stationeers Assembly-CSharp.dll
# Outputs JSON files matching the compiler's expected format

param(
    [string]$GamePath = "C:\Program Files (x86)\Steam\steamapps\common\Stationeers",
    [string]$OutputPath = "C:\Development\Stationeers Stuff\BASICtoMIPS_ByDogTired\Data\Generated"
)

$dllPath = Join-Path $GamePath "rocketstation_Data\Managed\Assembly-CSharp.dll"

if (-not (Test-Path $dllPath)) {
    Write-Error "Assembly-CSharp.dll not found at: $dllPath"
    Write-Host "Please check your GamePath parameter"
    exit 1
}

# CRC32 implementation using .NET (same algorithm Stationeers uses for hashes)
# Compile C# CRC32 calculator once at script start
$crc32Code = @"
using System;
using System.Text;
public class CRC32Calculator {
    private static readonly uint[] table = new uint[256];
    static CRC32Calculator() {
        for (uint i = 0; i < 256; i++) {
            uint crc = i;
            for (int j = 0; j < 8; j++) {
                crc = (crc & 1) == 1 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            }
            table[i] = crc;
        }
    }
    public static int Compute(string text) {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        uint crc = 0xFFFFFFFF;
        foreach (byte b in bytes) {
            crc = (crc >> 8) ^ table[(crc ^ b) & 0xFF];
        }
        uint result = crc ^ 0xFFFFFFFF;
        return unchecked((int)result);
    }
}
"@

if (-not ([System.Management.Automation.PSTypeName]'CRC32Calculator').Type) {
    Add-Type -TypeDefinition $crc32Code -Language CSharp
}

function Get-CRC32 {
    param([string]$text)
    return [CRC32Calculator]::Compute($text)
}

function Extract-Enum {
    param(
        [System.Type]$enumType,
        [string]$outputFile
    )

    $values = [System.Enum]::GetValues($enumType)
    $results = @()

    foreach ($value in $values) {
        $name = $value.ToString()
        $intValue = [int]$value
        $hash = Get-CRC32 -text $name

        $results += [PSCustomObject]@{
            Name = $name
            Value = $intValue
            Hash = $hash
        }
    }

    # Sort by Value
    $results = $results | Sort-Object Value

    $json = $results | ConvertTo-Json -Depth 10
    $json | Out-File $outputFile -Encoding UTF8

    Write-Host "Extracted $($results.Count) entries to $outputFile"
    return $results.Count
}

Write-Host "Loading Assembly-CSharp.dll from beta version..."
Write-Host "Path: $dllPath"
Write-Host ""

try {
    # Load the assembly
    $assembly = [System.Reflection.Assembly]::LoadFrom($dllPath)
    Write-Host "Assembly loaded successfully"
    Write-Host ""
} catch {
    Write-Error "Failed to load assembly: $_"
    exit 1
}

# Create output directory if needed
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

# Find and extract enums
$enumsToExtract = @(
    @{ TypeName = "Assets.Scripts.Objects.Motherboards.LogicType"; OutputFile = "LogicTypes.json" },
    @{ TypeName = "Assets.Scripts.Objects.Motherboards.LogicSlotType"; OutputFile = "SlotLogicTypes.json" },
    @{ TypeName = "Assets.Scripts.Objects.Motherboards.LogicBatchMethod"; OutputFile = "BatchModes.json" },
    @{ TypeName = "Assets.Scripts.Objects.Motherboards.LogicReagentMode"; OutputFile = "ReagentModes.json" },
    @{ TypeName = "Assets.Scripts.Inventory.SortingClass"; OutputFile = "SortingClasses.json" }
)

$totalExtracted = 0

foreach ($enumInfo in $enumsToExtract) {
    $typeName = $enumInfo.TypeName
    $outputFile = Join-Path $OutputPath $enumInfo.OutputFile

    Write-Host "Looking for: $typeName"

    $type = $assembly.GetType($typeName)

    if ($null -eq $type) {
        # Try alternative namespaces
        $alternativeNames = @(
            "Assets.Scripts.Objects.$($typeName.Split('.')[-1])",
            "Assets.Scripts.$($typeName.Split('.')[-1])",
            $typeName.Split('.')[-1]
        )

        foreach ($altName in $alternativeNames) {
            $type = $assembly.GetType($altName)
            if ($null -ne $type) {
                Write-Host "  Found at: $altName"
                break
            }
        }
    }

    if ($null -eq $type) {
        Write-Warning "  Could not find type: $typeName"
        Write-Host "  Searching all types for partial match..."

        $allTypes = $assembly.GetTypes()
        $matches = $allTypes | Where-Object { $_.Name -eq $typeName.Split('.')[-1] -and $_.IsEnum }

        if ($matches.Count -gt 0) {
            $type = $matches[0]
            Write-Host "  Found: $($type.FullName)"
        }
    }

    if ($null -ne $type -and $type.IsEnum) {
        $count = Extract-Enum -enumType $type -outputFile $outputFile
        $totalExtracted += $count
    } else {
        Write-Warning "  Skipping - not found or not an enum"
    }

    Write-Host ""
}

Write-Host "========================================"
Write-Host "Extraction complete!"
Write-Host "Total entries extracted: $totalExtracted"
Write-Host "Output directory: $OutputPath"
Write-Host "========================================"

# List new entries compared to what we expect
Write-Host ""
Write-Host "Verify the files were updated by checking the last entry in LogicTypes.json"
