' ============================================
' EXAMPLE 11: Batch Solar Array Controller
' ============================================
' DIFFICULTY: Advanced
'
' DESCRIPTION:
' Controls ALL solar panels on the network using
' batch operations. No direct device connections
' needed - works with any number of panels.
'
' DEVICE CONNECTIONS:
' d0 = LED Display (optional, shows total power)
'
' HOW IT WORKS:
' - BATCHREAD: Reads from all devices of a type
' - BATCHWRITE: Writes to all devices of a type
' - Uses hash to identify Solar Panels
' - Sets all panels to track the sun
' - Monitors total power generation
'
' BATCH MODES:
' - 0 = Average
' - 1 = Sum
' - 2 = Minimum
' - 3 = Maximum
'
' NOTES:
' - Hash -539224550 = Solar Panel
' - Find hashes in Device Hash Database (F4)
' - More efficient than individual connections
' ============================================

ALIAS display d0

DEFINE SOLAR_HASH -539224550    ' ItemStructureSolarPanel

VAR solarAngle = 0
VAR totalPower = 0

main:
    ' Get sun angle from any panel (average)
    solarAngle = BATCHREAD(SOLAR_HASH, SolarAngle, 0)

    ' Set ALL panels to track sun
    BATCHWRITE(SOLAR_HASH, Horizontal, solarAngle)
    BATCHWRITE(SOLAR_HASH, Vertical, 60)

    ' Get total power from all panels (sum)
    totalPower = BATCHREAD(SOLAR_HASH, PowerGeneration, 1)

    ' Display total power
    display.Setting = totalPower

    YIELD
    GOTO main

END
