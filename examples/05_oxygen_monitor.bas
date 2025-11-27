' ============================================
' EXAMPLE 05: Oxygen Monitor with Alarm
' ============================================
' DIFFICULTY: Intermediate
'
' DESCRIPTION:
' Monitors oxygen levels in a room and displays
' status using a colored light. Changes color
' based on oxygen safety levels.
'
' DEVICE CONNECTIONS:
' d0 = Gas Sensor
' d1 = Wall Light (status indicator)
' d2 = LED Display (shows O2 percentage)
'
' HOW IT WORKS:
' - Reads oxygen ratio from sensor
' - Converts to percentage for display
' - Sets light color based on safety:
'   * Green  = Safe (18-23%)
'   * Yellow = Warning (high O2, fire risk)
'   * Red    = Danger (low O2, suffocation)
'
' COLOR VALUES:
' - Red:    16711680 (0xFF0000)
' - Yellow: 16776960 (0xFFFF00)
' - Green:  65280    (0x00FF00)
'
' NOTES:
' - Normal breathable O2 is 18-23%
' - Below 16% causes suffocation
' - Above 25% increases fire risk
' ============================================

ALIAS sensor d0
ALIAS alarm d1
ALIAS display d2

DEFINE MIN_OXYGEN 0.18      ' 18% minimum safe
DEFINE MAX_OXYGEN 0.23      ' 23% maximum safe

VAR oxygenRatio = 0
VAR oxygenPercent = 0

main:
    oxygenRatio = sensor.RatioOxygen
    oxygenPercent = oxygenRatio * 100

    ' Update display with percentage
    display.Setting = oxygenPercent

    ' Set alarm color based on safety
    IF oxygenRatio < MIN_OXYGEN THEN
        ' DANGER - Low oxygen (red)
        alarm.On = 1
        alarm.Color = 16711680
    ELSEIF oxygenRatio > MAX_OXYGEN THEN
        ' WARNING - High oxygen (yellow)
        alarm.On = 1
        alarm.Color = 16776960
    ELSE
        ' SAFE - Normal levels (green)
        alarm.On = 1
        alarm.Color = 65280
    ENDIF

    YIELD
    GOTO main

END
