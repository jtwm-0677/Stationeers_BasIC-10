# Bug Analysis: Single-Line IF vs Multi-Line IF Block Compilation

**Date:** 2025-12-06
**Version:** v3.0.21
**Related Bug:** Jump Target Miscalculation

---

## Summary

Multi-line IF...ENDIF blocks compile correctly. Single-line IF statements with assignments do not - their skip targets are miscalculated.

---

## Working Pattern: Multi-Line IF Block

```basic
IF temp < TARGET_TEMP - TOLERANCE THEN
    heater.On = 1
    cooler.On = 0
ELSEIF temp > TARGET_TEMP + TOLERANCE THEN
    heater.On = 0
    cooler.On = 1
ELSE
    heater.On = 0
    cooler.On = 0
ENDIF
```

**Characteristics:**
- Uses `IF...THEN` with newline, statements on following lines
- Uses `ELSEIF`, `ELSE`, `ENDIF` keywords
- Multi-statement bodies
- Block structure

**Result:** Compiles correctly, works in-game.

---

## Broken Pattern: Consecutive Single-Line IFs

```basic
IF temp < lowThresh THEN heatOn = 1
IF temp > highThresh THEN heatOn = 0
heater.On = heatOn
```

**Characteristics:**
- Uses `IF condition THEN statement` all on one line
- Statement is an assignment (not a GOTO)
- Multiple consecutive single-line IFs
- No ENDIF required (single-line syntax)

**Result:** Skip targets are +4 lines too high, causing incorrect jumps.

---

## Also Working: Single-Line IF with GOTO

```basic
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
```

**Characteristics:**
- Uses `IF condition THEN GOTO label`
- Single-line format
- Uses explicit label jumps instead of assignments

**Result:** Compiles correctly, works in-game.

---

## Comparison Table

| Pattern | Syntax | Works? |
|---------|--------|--------|
| Multi-line IF...ENDIF | `IF cond THEN` (newline) `statement` (newline) `ENDIF` | YES |
| Multi-line IF...ELSEIF...ENDIF | `IF...ELSEIF...ELSE...ENDIF` | YES |
| Single-line IF with GOTO | `IF cond THEN GOTO label` | YES |
| Single-line IF with assignment | `IF cond THEN var = value` | NO |

---

## Root Cause Hypothesis

The compiler has two different code paths for IF statements:

1. **Multi-line IF blocks** - Properly tracks ENDIF location and calculates jumps
2. **Single-line IF with GOTO** - Uses label resolution (works correctly)
3. **Single-line IF with assignment** - Calculates "skip to next statement" incorrectly

The bug is isolated to case #3. When the compiler needs to calculate "jump past this assignment to the next line," it's adding an extra offset (consistently +4 lines in testing).

---

## Workarounds for Users

### Option 1: Convert to Multi-Line IF Block

Instead of:
```basic
IF temp < lowThresh THEN heatOn = 1
IF temp > highThresh THEN heatOn = 0
```

Use:
```basic
IF temp < lowThresh THEN
    heatOn = 1
ELSEIF temp > highThresh THEN
    heatOn = 0
ENDIF
```

### Option 2: Use Explicit GOTO Labels

Instead of:
```basic
IF temp < lowThresh THEN heatOn = 1
IF temp > highThresh THEN heatOn = 0
heater.On = heatOn
```

Use:
```basic
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
```

---

## Code Path to Investigate

In `MipsGenerator.cs`, look for:
- The handler for single-line IF statements
- Specifically where `IF condition THEN assignment` differs from `IF condition THEN GOTO`
- The skip target calculation for assignments
- Why multi-line IF blocks work but single-line with assignment doesn't

The fact that GOTO works but assignment doesn't suggests the issue is in how the "next statement" address is calculated after emitting the assignment instruction.
