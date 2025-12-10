# Bug Report: IF/ELSE Branch Miscalculation Due to Comment Line Counting

**Date Discovered:** 2025-12-07
**Severity:** Critical - Causes all IF/ELSE/ENDIF conditionals to fail
**Affected Versions:** All versions (confirmed after GOTO label fix was applied)
**Status:** Open
**Related Bug:** 2025-12-07-jump-target-comment-bug.md (partially fixed)

## Summary

After the GOTO/label jump bug was fixed, IF/ELSE/ENDIF statements still fail when comments are present in the code. The internal branch instructions (`blt`, `bge`, `bgt`, `ble`, etc.) used by IF/ELSE blocks calculate their jump targets incorrectly because comment lines are still being counted in the offset calculations.

This is the same root cause as the GOTO bug, but affects a different code path in the compiler.

## Discovery Context

### Test Script (Fails with Comments)
```basic
ALIAS sensor = IC.Device[StructureGasSensor].Name["Interior Sensor"]
ALIAS coolingPump = IC.Device[StructureVolumePump].Name["Cooling Pump"]
ALIAS coolingValve = IC.Device[StructureDigitalValve].Name["Cooling Valve"]
ALIAS heater = IC.Device[StructureWallHeater].Name["Heater"]
ALIAS tempDisplay = IC.Device[ModularDeviceLEDdisplay3].Name["Room Temperature"]

VAR temp = 0
# 17C = 290.15K, 21C = 294.15K
VAR heaterOff = 290.15
VAR valveOff = 294.15

tempDisplay.Mode = 0

Main:
    temp = sensor.Temperature.Average
    tempDisplay.Setting = temp
    # Pump always on
    coolingPump.On = 1
    # Heaters OFF at or above 17C
    IF temp >= heaterOff THEN
        heater.On = 0
    ELSE
        heater.On = 1
    ENDIF
    # Valve OFF at or below 21C
    IF temp <= valveOff THEN
        coolingValve.On = 0
    ELSE
        coolingValve.On = 1
    ENDIF

    db.Setting = temp
    YIELD
    GOTO Main
END
```

### Expected Behavior
- At 15C (288.15K): Heaters ON (below 17C threshold)
- At 17C (290.15K): Heaters OFF
- At 21C (294.15K) and below: Valve OFF
- Above 21C: Valve ON

### Actual Behavior (With Comments)
- At 15C: Heaters remain OFF (should be ON)
- Conditional logic completely broken

### Working Version (Without Comments)
```basic
ALIAS sensor = IC.Device[StructureGasSensor].Name["Interior Sensor"]
ALIAS coolingPump = IC.Device[StructureVolumePump].Name["Cooling Pump"]
ALIAS coolingValve = IC.Device[StructureDigitalValve].Name["Cooling Valve"]
ALIAS heater = IC.Device[StructureWallHeater].Name["Heater"]
ALIAS tempDisplay = IC.Device[ModularDeviceLEDdisplay3].Name["Room Temperature"]

VAR temp = 0
VAR heaterOff = 290.15
VAR valveOff = 294.15

tempDisplay.Mode = 0

Main:
    temp = sensor.Temperature.Average
    tempDisplay.Setting = temp
    coolingPump.On = 1

    IF temp >= heaterOff THEN
        heater.On = 0
    ELSE
        heater.On = 1
    ENDIF

    IF temp <= valveOff THEN
        coolingValve.On = 0
    ELSE
        coolingValve.On = 1
    ENDIF

    db.Setting = temp
    YIELD
    GOTO Main
END
```

## Root Cause Analysis

### Compiled IC10 Output (With Comments - BUGGY)
```ic10
move r0 0
# 17C = 290.15K, 21C = 294.15K      <- Line 1 (COMMENT - counted incorrectly)
move r1 290.15                       <- Line 2
move r2 294.15                       <- Line 3
sbn -246585138 1829138044 Mode 0     <- Line 4
Main:                                <- Line 5
lbn r0 -1252983604 -974770433 Temperature 0  <- Line 6
sbn -246585138 1829138044 Setting r0         <- Line 7
# Pump always on                     <- Line 8 (COMMENT - counted incorrectly)
sbn -321403609 -1128025635 On 1      <- Line 9
# Heaters OFF at or above 17C        <- Line 10 (COMMENT - counted incorrectly)
blt r0 r1 11                         <- Line 11 - WRONG TARGET!
sbn 24258244 1979190919 On 0         <- Line 12
j 12                                 <- Line 13 - WRONG TARGET!
sbn 24258244 1979190919 On 1         <- Line 14
...
```

### The Problem

The compiler calculates IF/ELSE branch targets using the same flawed logic as GOTO:

1. **Branch instruction `blt r0 r1 11`**: Intended to jump to the ELSE block (heater ON)
2. **With comments counted**: Line 11 points to the branch instruction itself or wrong location
3. **Result**: Branch logic is completely broken

The GOTO fix changed label-based jumps to use named labels (`j Main`), but IF/ELSE uses internal labels (`else_1`, `endif_1`) that still get converted to numeric offsets with the same buggy calculation.

## Bug Location

**File:** `src/CodeGen/MipsGenerator.cs`
**Method:** `ConvertLabelsToOffsets()` (same as GOTO bug)
**Specific Issue:** The fix for GOTO preserved user labels but internal IF/ELSE labels (`else_*`, `endif_*`, `while_*`, etc.) are still converted to numeric offsets using the buggy line counting.

### Current Behavior
- User labels like `Main:` → preserved as labels, `j Main` works correctly
- Internal labels like `else_1:` → converted to numeric offset, counting comments

### Required Fix
Either:
1. **Option A**: Don't count comment lines when calculating ANY numeric offsets (same fix as GOTO, applied consistently)
2. **Option B**: Preserve ALL labels (including internal ones) instead of converting to offsets
3. **Option C**: Strip comments from output entirely before calculating offsets

## Reproduction Steps

1. Create a script with IF/ELSE and at least one comment before the IF statement
2. Compile the script
3. Load into IC in-game
4. Observe that conditional logic doesn't work correctly

### Minimal Reproduction Script
```basic
# This comment breaks the IF/ELSE below
VAR x = 10
VAR threshold = 5

Main:
    IF x > threshold THEN
        db.Setting = 1
    ELSE
        db.Setting = 0
    ENDIF
    YIELD
    GOTO Main
END
```

**Expected:** db.Setting = 1 (since 10 > 5)
**Actual:** db.Setting = 0 or unpredictable behavior

## Impact Assessment

This bug affects ALL conditional statements when comments are present:
- `IF/THEN/ELSE/ENDIF`
- `IF/THEN/ENDIF` (without ELSE)
- Nested IF statements
- `WHILE/WEND` loops (if using internal branch offsets)
- `FOR/NEXT` loops
- `SELECT CASE` statements

## Workaround

Until the fix is applied, users must:
1. Remove ALL comment lines from BASIC source code
2. Or write raw IC10 assembly

## Test Cases for Verification

### Test 1: Simple IF/ELSE with Comment Before
```basic
# Comment before IF
VAR x = 10
Main:
    IF x > 5 THEN
        db.Setting = 1
    ELSE
        db.Setting = 0
    ENDIF
    YIELD
    GOTO Main
END
```
**Pass:** db.Setting = 1

### Test 2: Multiple Comments Throughout
```basic
# Top comment
VAR temp = 290
# Another comment
VAR threshold = 295
Main:
    # Loop comment
    IF temp < threshold THEN
        # Inside IF comment
        db.Setting = 1
    ELSE
        db.Setting = 0
    ENDIF
    YIELD
    GOTO Main
END
```
**Pass:** db.Setting = 1

### Test 3: No Comments (Control Test)
```basic
VAR x = 10
Main:
    IF x > 5 THEN
        db.Setting = 1
    ELSE
        db.Setting = 0
    ENDIF
    YIELD
    GOTO Main
END
```
**Pass:** db.Setting = 1 (this should work even before fix)

## Relationship to Previous Bug

| Aspect | GOTO Bug (Fixed) | IF/ELSE Bug (This Bug) |
|--------|------------------|------------------------|
| Symptoms | GOTO jumps to wrong line | IF/ELSE branches to wrong line |
| Root Cause | Comments counted in offset calculation | Same |
| Fix Applied | Labels preserved for GOTO | Not applied to IF/ELSE |
| Current Status | Fixed | Open |

## Recommended Fix Implementation

In `ConvertLabelsToOffsets()`, ensure that when calculating `instructionNumber` for ANY purpose (not just user labels), comment lines are excluded:

```csharp
else
{
    outputLines.Add(line);
    // Only increment instruction number for actual instructions, not comments
    if (!trimmed.StartsWith("#"))
    {
        instructionNumber++;
    }
}
```

This fix should have been applied universally, not just for user-facing labels.
