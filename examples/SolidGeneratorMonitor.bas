' ============================================
' Solid Generator Power Monitor
' Displays generator stats on a console
' ============================================

' --- Device Aliases ---
ALIAS generator = IC.Device[StructureSolidFuelGenerator].Name["Main Generator"]
ALIAS display = IC.Device[StructureConsole].Name["Power Display"]

' --- Variables ---
VAR powerOutput = 0
VAR fuelLevel = 0
VAR isOn = 0
VAR burnTime = 0

' --- Main Loop ---
Main:
    ' Read generator properties
    powerOutput = generator.PowerGeneration
    fuelLevel = generator.Fuel
    isOn = generator.On
    burnTime = generator.BurnTime

    ' Update console display
    ' Line 0: Title
    display.Setting = powerOutput

    ' Display power output (Setting shows main value)
    ' For more advanced display, use multiple consoles or LED displays

    ' Optional: Turn on status indicator based on power
    IF powerOutput > 0 THEN
        display.On = 1
        display.Color = 2    ' Green when generating
    ELSE
        IF isOn = 1 THEN
            display.Color = 5    ' Yellow when on but no output (needs fuel)
        ELSE
            display.Color = 4    ' Red when off
        ENDIF
    ENDIF

YIELD
GOTO Main

' ============================================
' Notes:
' - StructureSolidFuelGenerator properties:
'   .On - Generator enabled (0/1)
'   .PowerGeneration - Current power output (W)
'   .Fuel - Current fuel amount
'   .BurnTime - Time until fuel exhausted
'   .Activate - Start combustion
'   .Lock - Lock controls
'
' - StructureConsole properties:
'   .Setting - Displayed number
'   .On - Screen on/off
'   .Color - Display color (0-11)
'
' Colors: 0=Blue, 1=Grey, 2=Green, 3=Orange,
'         4=Red, 5=Yellow, 6=White, 7=Black,
'         8=Brown, 9=Khaki, 10=Pink, 11=Purple
' ============================================
