' ============================================
' EXAMPLE 14: Dial-Controlled Pump
' ============================================
' DIFFICULTY: Beginner
'
' DESCRIPTION:
' Uses a dial to control pump pressure setting.
' Maps dial range (0-100) to pump pressure range.
' Simple example of analog input control.
'
' DEVICE CONNECTIONS:
' d0 = Dial (any dial or kit dial)
' d1 = Volume Pump (or any controllable pump)
' d2 = LED Display (shows target pressure)
'
' HOW IT WORKS:
' - Dial Setting is 0-100
' - Maps to pressure 0-10000 kPa
' - Updates pump and display in real-time
'
' NOTES:
' - Adjust multiplier for your pressure range
' - Can be adapted for any dial-controlled device
' ============================================

ALIAS dial d0
ALIAS pump d1
ALIAS display d2

VAR dialSetting = 0
VAR targetPressure = 0

main:
    ' Read dial position (0-100)
    dialSetting = dial.Setting

    ' Map to pressure range (0-10000 kPa)
    targetPressure = dialSetting * 100

    ' Set pump and display
    pump.Setting = targetPressure
    display.Setting = targetPressure

    YIELD
    GOTO main

END
