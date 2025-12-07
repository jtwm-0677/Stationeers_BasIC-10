# Bug Report: Incorrect Jump Target Calculation in Consecutive IF Statements

**Date Discovered:** 2025-12-06
**Severity:** Critical
**Affects:** Public Release v3.0.20 (and likely earlier versions)
**Component:** Code Generator (likely MipsGenerator.cs)

---

## Summary

The compiler generates incorrect jump targets for consecutive IF statements, causing jumps to `hcf` (halt) or beyond the end of the program instead of the intended instruction. This causes programs to halt unexpectedly or exhibit undefined behavior.

---

## Steps to Reproduce

1. Create a BASIC program with consecutive single-line IF statements near the end of the main loop
2. Compile the program
3. Observe that the second IF statement's jump target points to `hcf` or past the program end

### Minimal Reproduction Code

```basic
ALIAS sensor = IC.Device[StructureGasSensor].Name["Interior Sensor"]
ALIAS heater = IC.Device[StructureWallHeater].Name["Heater"]

var temp = 0
var lowThresh = 270.65
var highThresh = 275.65
var heatOn = 0

Main:
    temp = sensor.Temperature.Average

    IF temp < lowThresh THEN heatOn = 1
    IF temp > highThresh THEN heatOn = 0

    heater.On = heatOn
    YIELD
    GOTO Main
END
```

### Compiled Output (Problematic Section)

```ic10
bge r0 r3 36        # if temp >= lowThresh, jump to 36
move r6 1           # heatOn = 1
ble r0 r2 38        # if temp <= highThresh, jump to 38  <-- BUG: jumps to hcf!
move r6 0           # heatOn = 0
sbn ... On r6       # heater.On = heatOn
yield
j Main
hcf                 # <-- Line 38: Program halts here!
```

---

## Expected Behavior

When `temp = 267.8K` (below lowThresh of 270.65K):
1. First IF: `temp < lowThresh` is TRUE → set `heatOn = 1`
2. Second IF: `temp > highThresh` is FALSE → skip setting `heatOn = 0`
3. Apply `heater.On = heatOn` (should be 1)
4. Continue loop

The second IF's conditional jump should skip the `move r6 0` instruction and land on `sbn ... On r6`, NOT on `hcf`.

---

## Actual Behavior

When `temp <= highThresh` (which is TRUE for 267.8K <= 275.65K):
1. Jump target 38 points to `hcf`
2. Program halts immediately
3. Heater never gets turned on

---

## Analysis

The compiler appears to miscalculate jump targets in these scenarios:

1. **Consecutive single-line IF statements**
   ```basic
   IF condition1 THEN statement1
   IF condition2 THEN statement2
   ```

2. **IF/ELSEIF blocks**
   ```basic
   IF condition1 THEN
       statement1
   ELSEIF condition2 THEN
       statement2
   ENDIF
   ```

3. **Nested IF statements with multi-line blocks**

### Possible Root Causes

1. **Comment line counting**: The compiler may be counting IC10 comment lines (starting with `#`) toward jump target addresses, but IC10 ignores comments when executing

2. **Label address calculation**: Jump targets may be calculated before labels are resolved, causing off-by-N errors

3. **ENDIF jump target**: The "skip to ENDIF" logic may be calculating the wrong destination address

4. **Output buffer vs instruction counter mismatch**: The line numbers in the output string may not match the actual instruction addresses

---

## Observed Patterns

| Program Lines | Jump Target | Actual Instruction at Target |
|--------------|-------------|------------------------------|
| 40 lines     | 43          | Beyond program (undefined)   |
| 42 lines     | 38          | `hcf` (halt)                 |
| 43 lines     | 47          | Beyond program               |
| 44 lines     | 40          | `j Main` (skips heater set)  |

The jump targets are consistently 2-5 lines beyond where they should be.

---

## Workaround

Use explicit GOTO labels instead of relying on implicit IF statement jumps:

```basic
DoHeating:
    IF temp < lowThresh THEN GOTO HeatOn
    IF temp > highThresh THEN GOTO HeatOff
    GOTO ApplyHeat

HeatOn:
    heatOn = 1
    GOTO ApplyHeat

HeatOff:
    heatOn = 0
    GOTO ApplyHeat

ApplyHeat:
    heater.On = heatOn
    YIELD
    GOTO Main
```

This compiles correctly because named labels are resolved properly.

---

## Files to Investigate

- `src/CodeGen/MipsGenerator.cs` - Jump target calculation
- Look for:
  - How `bge`, `ble`, `bne` instruction targets are calculated
  - How ENDIF addresses are determined
  - Whether comment lines affect the instruction counter
  - Label resolution timing vs. jump target emission

---

## Test Cases for Fix Verification

1. **Consecutive single-line IFs at end of loop** - should not jump to hcf
2. **IF/ELSEIF/ENDIF blocks** - should jump to correct ENDIF location
3. **Nested IF statements** - inner and outer jumps should be correct
4. **IF statements with comments between them** - comments should not affect targets
5. **Programs of various lengths** - verify consistency across sizes

---

## Additional Notes

- The bug was discovered during Climate Control script development
- A simple `heater.On = 1` loop works correctly (device aliases are fine)
- The issue is specifically with conditional jump target calculation
- This affects any program using consecutive IF statements

---

## Reporter

Bug documented by Claude Code during script development session with user.
