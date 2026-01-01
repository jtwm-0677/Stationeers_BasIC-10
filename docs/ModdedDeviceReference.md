# Modded Device Reference (Stationeers)

This document catalogs modded devices and their logic properties for use with BASIC-10 scripts.
Device information extracted from Steam Workshop mod files.

---

## Mod Sources

| Workshop ID | Mod Name | Description |
|-------------|----------|-------------|
| 3505169479 | ModularConsoleMod | Displays, inputs, indicators for modular consoles |
| 3243132734 | MorePowerMod | Nuclear battery, large omni transmitter |
| 3408132778 | LargeConsoleMod | Large console variants (2x2, 3x3) |
| 3465059322 | Advanced Computing | Stack/Queue/Memory chips, chip stacks |
| 3478434324 | KeypadMod | Numeric keypad input |
| 3608354326 | Hydroponics Grid | Wall-mountable hydroponics |
| 3271740459 | FloodLightMod | Flood lights, grow lights |
| 3452651592 | FilterCleanerMod | Filter cleaning device |
| 3348479241 | LaunchSiloMod | Launch silo structures |
| 3404482913 | CCTV | Security cameras and displays |
| 3328065049 | ForceFieldDoorMod | Force field doors |
| 467018190 | SpotlightsMod | Spotlight devices |

Workshop Path: `C:\Program Files (x86)\Steam\steamapps\workshop\content\544550\[ID]`

---

# ModularConsoleMod (3505169479)

The primary source for console displays, inputs, and indicators.

## LED Displays

### ModularDeviceLEDdisplay2 (Small LED Display)
Displays a numeric value with configurable format.

| Property | Type | Description |
|----------|------|-------------|
| `Setting` | Write | Value to display |
| `Mode` | Write | Display format mode |
| `Color` | Write | Display color |

### ModularDeviceLEDdisplay3 (Large LED Display)
Larger version with same functionality.

| Property | Type | Description |
|----------|------|-------------|
| `Setting` | Write | Value to display |
| `Mode` | Write | Display format mode |
| `Color` | Write | Display color |

**Display Modes:**
| Mode | Description |
|------|-------------|
| 0 | Default numeric |
| 1 | Percentage (0-1 as 0-100%) |
| 3 | Temperature (Kelvin) |
| 10 | String/ASCII (octet-encoded) |
| 12 | Liters (L) |
| 14 | Pressure (Pa) |

---

## Slider/Bar Displays

### ModularDeviceSliderDiode1 (Diode Slide 1)
Displays value as lit bar.

| Property | Type | Description |
|----------|------|-------------|
| `Setting` | Write | Bar position (0.0 to 1.0) |
| `Color` | Write | Bar color |

### ModularDeviceSliderDiode2 (Diode Slide 2)
Displays value as lit bar (alternate style).

| Property | Type | Description |
|----------|------|-------------|
| `Setting` | Write | Bar position (0.0 to 1.0) |
| `Color` | Write | Bar color |

---

## Gauge/Meter Displays

### ModularDeviceGauge2x2 (Gauge 2x2)
Analog gauge display. Labelable with ItemLabeller.

| Property | Type | Description |
|----------|------|-------------|
| `Setting` | Write | Gauge position (0.0 to 1.0) |
| `Color` | Write | Gauge color |

### ModularDeviceGauge3x3 (Gauge 3x3)
Larger analog gauge.

| Property | Type | Description |
|----------|------|-------------|
| `Setting` | Write | Gauge position (0.0 to 1.0) |
| `Color` | Write | Gauge color |

### ModularDeviceMeter3x3 (Meter 3x3)
Meter with user-defined range.

| Property | Type | Description |
|----------|------|-------------|
| `Setting` | Write | Value within user-defined range |
| `Color` | Write | Meter color |

---

## Indicator Lights

### ModularDeviceLight (Light Diode)
Standard indicator light.

| Property | Type | Description |
|----------|------|-------------|
| `On` | Write | Light state (0=off, 1=on) |
| `Color` | Write | Light color |

### ModularDeviceLightSmall (Light Diode Small)
Compact indicator light.

| Property | Type | Description |
|----------|------|-------------|
| `On` | Write | Light state (0=off, 1=on) |
| `Color` | Write | Light color |

### ModularDeviceLightLarge (Light Diode Large)
Large indicator light.

| Property | Type | Description |
|----------|------|-------------|
| `On` | Write | Light state (0=off, 1=on) |
| `Color` | Write | Light color |

### ModularDeviceLabelDiode2 (Label Diode 2)
Small emissive text label with blink mode.

| Property | Type | Description |
|----------|------|-------------|
| `On` | Write | Light state (0=off, 1=on) |
| `Color` | Write | Light color |
| `Mode` | Write | 0=solid, 1=blinking |

### ModularDeviceLabelDiode3 (Label Diode 3)
Large emissive text label with blink mode.

| Property | Type | Description |
|----------|------|-------------|
| `On` | Write | Light state (0=off, 1=on) |
| `Color` | Write | Light color |
| `Mode` | Write | 0=solid, 1=blinking |

### ModularDeviceAlarm (Logic Alarm)
Sound and light beacon for emergencies.

| Property | Type | Description |
|----------|------|-------------|
| `On` | Write | Alarm state (0=silent, 1=active) |

---

## Input Devices - Buttons

### ModularDeviceSquareButton (Logic Button Square)
Momentary push button.

| Property | Type | Description |
|----------|------|-------------|
| `Activate` | Read | 1 when pressed, 0 when released |
| `Color` | Write | Button color |

### ModularDeviceRoundButton (Logic Button Round)
Round momentary button.

| Property | Type | Description |
|----------|------|-------------|
| `Activate` | Read | 1 when pressed, 0 when released |
| `Color` | Write | Button color |

### ModularDeviceEmergencyButton3x3 (Emergency Button)
Large emergency/E-stop button.

| Property | Type | Description |
|----------|------|-------------|
| `Activate` | Read | 1 when pressed |

### ModularDeviceUtilityButton2x2 (Utility Button)
Large labeled button. Can be painted and labeled.

| Property | Type | Description |
|----------|------|-------------|
| `Activate` | Read | 1 when pressed |
| `Color` | Write | Button color |

---

## Input Devices - Switches

### ModularDeviceSwitch (Logic Switch)
Toggle switch that maintains state.

| Property | Type | Description |
|----------|------|-------------|
| `On` | Read | Switch state (0=off, 1=on) |
| `Color` | Write | Switch/indicator color |

### ModularDeviceFlipSwitch (Flip Switch)
Flip-style toggle switch.

| Property | Type | Description |
|----------|------|-------------|
| `On` | Read | Switch state (0=off, 1=on) |

### ModularDeviceFlipCoverSwitch (Flip Cover Switch)
Switch with protective cover.

| Property | Type | Description |
|----------|------|-------------|
| `On` | Read | Switch state (0=off, 1=on) |

### ModularDeviceBigLever (Big Lever)
Large lever for important actions. Labelable.

| Property | Type | Description |
|----------|------|-------------|
| `Open` | Read | Lever state |

---

## Input Devices - Dials & Sliders

### ModularDeviceDial (Logic Dial)
Rotary selector dial.

| Property | Type | Description |
|----------|------|-------------|
| `Setting` | Read | Current position (0 to Mode) |
| `Mode` | Write | Maximum dial position |

### ModularDeviceDialSmall (Logic Dial Small)
Compact rotary dial.

| Property | Type | Description |
|----------|------|-------------|
| `Setting` | Read | Current position (0 to Mode) |
| `Mode` | Write | Maximum dial position |

### ModularDeviceSlider (Logic Slider)
Draggable slider input.

| Property | Type | Description |
|----------|------|-------------|
| `Setting` | Read | Slider position (0.0 to 1.0) |

### ModularDeviceThrottle3x2 (Logic Throttle)
Throttle-style input.

| Property | Type | Description |
|----------|------|-------------|
| `Setting` | Read | Throttle position (0.0 to 1.0) |

---

## Input Devices - Numeric

### ModularDeviceNumpad (Logic Num Pad)
Numeric keypad with screen.

| Property | Type | Description |
|----------|------|-------------|
| `Setting` | Read/Write | Numeric value (incl. decimals) |
| `Mode` | Read | Receives pulses on digit press |
| `Color` | Write | Display color |

### ModularDeviceCardReader (Access Card Reader)
Checks access card color against Mode.

| Property | Type | Description |
|----------|------|-------------|
| `Setting` | Read | 1 if card matches Mode, 0 otherwise |
| `Mode` | Read/Write | Expected card color |
| `Color` | Write | Reader color |

---

## Console Structures

### ModularDeviceConsole (Console)
Modular equivalent to StructureConsole for hosting circuitboards.

### ModularDeviceComputer (Computer)
Modular equivalent to StructureComputer for hosting motherboards.

---

# MorePowerMod (3243132734)

## Power Storage

### StationBatteryNuclear (Nuclear Battery)
High-capacity station battery with mode-based status reporting.

| Property | Type | Description |
|----------|------|-------------|
| `Charge` | Read | Current stored energy (J) |
| `Maximum` | Read | Maximum capacity (J) |
| `Ratio` | Read | Charge/Maximum (0-1) |
| `Mode` | Read | Battery status mode (0-6) |
| `Power` | Read | Power value |
| `PowerActual` | Read | Power draw FROM battery (W) - discharge rate |
| `PowerPotential` | Read | Power input TO battery (W) - charging rate |
| `RequiredPower` | Read | Required power |
| `On` | Read/Write | Power state (0=off, 1=on) |
| `Lock` | Read/Write | Lock state |
| `Error` | Read | Error state |
| `ReferenceId` | Read | Reference ID |
| `NameHash` | Read | Name hash |
| `PrefabHash` | Read | Prefab hash |

**Mode Values:**
| Mode | Status |
|------|--------|
| 0 | Empty |
| 1 | Critical |
| 2 | VeryLow |
| 3 | Low |
| 4 | Medium |
| 5 | High |
| 6 | Full |

**Capacity:** 230,400,000 J (230.4 MJ)

**Connections:**
| Pin | Type |
|-----|------|
| 0 | Logic |
| 1 | Power Input |
| 2 | Power Output |

---

## Wireless Power

### OmniTransmitterLarge (Omni Transmitter Large)
Large wireless power transmitter.

### ItemWirelessBatteryCellNuclear (Battery Wireless Cell Nuclear)
Nuclear-capacity wireless battery cell.

---

# Advanced Computing (3465059322)

## Utility Chips

### ItemUtilityChipStack (Stack Utility Chip)
LIFO stack data structure holding up to 512 values.

| Property | Type | Description |
|----------|------|-------------|
| `Mode` | Read/Write | Behavior when stack is full |
| `Setting` | Read | Top item on stack (or 0) |
| `SettingOutput` | Read | Same as Setting |
| `SettingInput` | Write | Push value to top of stack |
| `Activate` | Write | Set non-zero to pop top item |
| `Quantity` | Read | Number of items on stack |
| `ClearMemory` | Write | Set non-zero to clear all |

**Memory:** Addresses 0-511 readable. Address 0 = oldest, Quantity-1 = newest.

### ItemUtilityChipQueue (Queue Utility Chip)
FIFO queue data structure holding up to 512 values.

| Property | Type | Description |
|----------|------|-------------|
| `Mode` | Read/Write | Behavior when queue is full |
| `Setting` | Read | Front item of queue (or 0) |
| `SettingOutput` | Read | Same as Setting |
| `SettingInput` | Write | Push value to back of queue |
| `Activate` | Write | Set non-zero to dequeue front |
| `Quantity` | Read | Number of items in queue |
| `ClearMemory` | Write | Set non-zero to clear all |

**Memory:** Addresses 0-511 readable. Address 0 = front (oldest), Quantity-1 = back (newest).

### ItemUtilityChipMemory (Memory Utility Chip)
High-volume memory chip with 8192 values.

| Property | Type | Description |
|----------|------|-------------|
| `Setting` | Read | Address of first empty (zero) value, or -1 if full |
| `SettingOutput` | Read | Same as Setting |
| `Quantity` | Read | Number of non-zero values |
| `ClearMemory` | Write | Set non-zero to clear all |

**Memory:** Addresses 0-8191 readable and writable.

---

## Chip Housing

### StructureUtilityHousing (Utility Chip Housing)
Holds utility chips for data network access.

### StructureChipStackBase (Chip Stack Base)
Base unit for chip stacks with 2 rack slots + 12 data pins.
Power: 10W base + 50W per installed rack.

### StructureChipStackExtender (Chip Stack Extender)
Adds 4 slots to existing chip stack.

---

# KeypadMod (3478434324)

### StructureKeypad (Keypad)
Numeric keypad input device.

| Property | Type | Description |
|----------|------|-------------|
| `Setting` | Read | Current numeric value |
| `Mode` | Read | Pulses on keypress (for tracking) |

---

# LargeConsoleMod (3408132778)

### StructureConsole2x2 (Console 2x2)
Larger console for circuitboards.

### StructureConsole3x3 (Console 3x3)
Even larger console for circuitboards.

---

# FloodLightMod (3271740459)

### StructureGlowLight2 (Flood Light Large)
Large flood light. Standard light properties.

| Property | Type | Description |
|----------|------|-------------|
| `On` | Read/Write | Light state |
| `Color` | Write | Light color |

### StructureGlowLight (Flood Light)
Standard flood light.

### StructureGlowLightSmall (Flood Light Small)
Compact flood light.

### StructureGrowlightLarge (Grow Light Large)
Large grow light for plants.

---

# CCTV (3404482913)

### Security Cameras
- StructureSecurityCameraStraight
- StructureSecurityCameraLeft
- StructureSecurityCameraRight
- StructureSecurityCameraPanning
- StructureSecurityCameraFishEye

Link to StructureMotionSensor and console with CCTV circuitboard.

### Circuitboards
- CircuitboardCameraDisplayModLD (Low Definition)
- CircuitboardCameraDisplayModSD (Standard Definition)
- CircuitboardCameraDisplayModHD (High Definition)
- CircuitboardCameraDisplayModSCAN (Scanner - for low light)

---

# ForceFieldDoorMod (3328065049)

### StructureForceFieldDoor (Force-Field Door)
Energy-based door. Standard door properties expected.

| Property | Type | Description |
|----------|------|-------------|
| `On` | Read/Write | Power state |
| `Open` | Read/Write | Door state |

---

# Hydroponics Grid (3608354326)

### StructureHydroponicsGrid
Wall-mountable hydroponics using Root Padlock foam.
**Note:** Not automatable (manual interaction only).

### StructureHydroponicsGridExtended
Extended version.

### StructureHydroponicsGridArched
Arched version.

---

# Color Reference

Standard color values used across all devices:

| Value | Color |
|-------|-------|
| 0 | Blue |
| 1 | Grey |
| 2 | Green |
| 3 | Orange |
| 4 | Red |
| 5 | Yellow |
| 6 | White |
| 7 | Black |
| 8 | Brown |
| 9 | Khaki |
| 10 | Pink |
| 11 | Purple |

---

# String Encoding (Mode 10)

For LED displays in Mode 10 (string mode), text is encoded as octet ASCII values.

**Common Gas Name Encodings:**
| String | Encoded Value |
|--------|---------------|
| "O2" | 20274 |
| "N2" | 20018 |
| "CO2" | 4411186 |
| "VOL" | 5656396 |
| "POL" | 5263180 |
| "N2O" | 5124687 |
| "H2O" | 4731471 |
| "---" | 2960685 |

---

# Button Edge Detection Pattern

For momentary buttons, use edge detection for toggle behavior:

```basic
currBtn = button.Activate
IF currBtn = 1 THEN
    IF prevBtn = 0 THEN
        # Rising edge - button just pressed
        state = 1 - state  # Toggle
    ENDIF
ENDIF
prevBtn = currBtn
```

---

# Example: Complete Console Panel

```basic
# Status Panel with LED display, slider, and button control

ALIAS display = IC.Device[ModularDeviceLEDdisplay3].Name["Status Display"]
ALIAS slider = IC.Device[ModularDeviceSliderDiode2].Name["Level Bar"]
ALIAS statusLight = IC.Device[ModularDeviceLabelDiode3].Name["STATUS"]
ALIAS controlBtn = IC.Device[ModularDeviceSquareButton].Name["Toggle"]

# Setup
display.Mode = 1  # Percentage mode
statusLight.Mode = 0  # Solid (not blinking)

prevBtn = 0
systemOn = 0

Main:
    # Button edge detection
    currBtn = controlBtn.Activate
    IF currBtn = 1 THEN
        IF prevBtn = 0 THEN
            systemOn = 1 - systemOn
        ENDIF
    ENDIF
    prevBtn = currBtn

    # Update button color
    IF systemOn = 1 THEN
        controlBtn.Color = 2  # Green
    ELSE
        controlBtn.Color = 4  # Red
    ENDIF

    # Update status light
    statusLight.On = systemOn
    IF systemOn = 1 THEN
        statusLight.Color = 2  # Green
    ELSE
        statusLight.Color = 4  # Red
    ENDIF

    # Display and slider show same value
    value = 0.75
    display.Setting = value
    slider.Setting = value

    YIELD
    GOTO Main
END
```

---

*Document generated from Steam Workshop mod files.*
*Workshop content path: C:\Program Files (x86)\Steam\steamapps\workshop\content\544550*
*Last updated: December 2024*
