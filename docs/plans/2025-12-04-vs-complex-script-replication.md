# VS Complex Script Replication Test

**Test Date:** 2025-12-04
**Tester:** Claude Code (QA Instance)
**Version:** v3.0.13

---

## Overview

Attempted to replicate a complex helmet controller script using Visual Scripting to identify limitations and missing features.

---

## Original Script

```basic
VAR INIT = true

ALIAS helm = IC.Pin[0]
ALIAS suit = THIS

helm.SoundAlert = 6
helm.On = true
suit.Filtration = false
helm.Open = true
WAIT(0.25)
helm.Open = false
suit.Filtration = true
helm.SoundAlert = 9
helm.On = false
WAIT(1)
suit.Filtration = false
helm.Open = true
helm.SoundAlert = 0
INIT = false

Main:
IF suit.PressureExternal > 85 THEN
    INIT = true
    IF suit.TemperatureExternal > 313.15 THEN
        suit.Filtration = true
        helm.Open = false
        helm.Lock = true
    ELSEIF suit.TemperatureExternal < 278.15 THEN
        suit.Filtration = true
        helm.Open = false
        helm.Lock = true
    ELSE
        helm.Lock = false
        helm.Open = true
        suit.Filtration = false
    ENDIF
ELSEIF suit.PressureExternal > 220 THEN
    IF INIT == true THEN
        helm.SoundAlert = 6
        INIT = false
    ENDIF
    suit.Filtration = true
    helm.Open = false
    helm.Lock = true
    WAIT(0.5)
    helm.SoundAlert = 0
ENDIF

IF suit.PressureExternal < 40 THEN
    IF INIT == true THEN
        helm.SoundAlert = 6
        INIT = false
    ENDIF
    suit.Filtration = true
    helm.Open = false
    helm.Lock = true
    WAIT(0.5)
    helm.SoundAlert = 0
ENDIF

yield()
GOTO Main
```

---

## VS Replication Attempt

### Nodes Created (38 total)

| Category | Count | Nodes |
|----------|-------|-------|
| Entry/Flow | 4 | EntryPoint, Label, Yield, Goto |
| Devices | 3 | Variable (INIT), PinDevice (helm), ThisDevice (suit) |
| WriteProperty | 11 | SoundAlert x3, On x2, Filtration x3, Open x3 |
| Constants | 12 | Values: 0, 1, 6, 9, 0.25, 1, 85 |
| Sleep | 2 | 0.25s, 1s |
| Condition | 3 | If, Compare, ReadProperty |
| Assignment | 1 | CompoundAssign |

### Generated BASIC (Partial)

```basic
VAR INIT = 1
ALIAS helm d0
ALIAS chip db

helm.SoundAlert = 6
helm.On = 1
chip.Filtration = 0
helm.Open = 1
SLEEP 0.25
helm.Open = 0
chip.Filtration = 1
helm.SoundAlert = 9
helm.On = 0
SLEEP 1
chip.Filtration = 0
helm.Open = 1
helm.SoundAlert = 0
INIT += 0
Main:
IF (chip.PressureExternal > 85) THEN
ENDIF
YIELD
GOTO Main
```

### Generated IC10

```ic10
move r0 1
s d0 SoundAlert 6
s d0 On 1
s db Filtration 0
s d0 Open 1
sleep 0.25
s d0 Open 0
s db Filtration 1
s d0 SoundAlert 9
s d0 On 0
sleep 1
s db Filtration 0
s d0 Open 1
s d0 SoundAlert 0
add r1 r0 0
move r0 r1
Main:
l r1 db PressureExternal
ble r1 85 19
yield
j Main
```

---

## What Works

| Feature | Status | Notes |
|---------|--------|-------|
| PinDevice alias | ✅ WORKS | `helm d0` correctly generated |
| WriteProperty | ✅ WORKS | All device.Property = value statements work |
| Device wiring | ✅ WORKS | Multiple WriteProperty nodes can share one device |
| Sleep node | ✅ WORKS | Duration wiring works correctly |
| Execution flow | ✅ WORKS | Linear sequence wires properly |
| Label node | ✅ WORKS | Label names configurable |
| Goto node | ✅ WORKS | Target label configurable |
| If node | ✅ WORKS | Condition input works |
| Compare node | ✅ WORKS | Operator and value comparison works |
| ReadProperty | ✅ WORKS | Property name configurable |
| Constant values | ✅ WORKS | Integer, decimal values work |

---

## Issues Found

### CRITICAL: ThisDevice Alias Not Configurable

| Expected | Actual |
|----------|--------|
| `ALIAS suit = THIS` | `ALIAS chip db` |

**Problem:** ThisDevice node hardcodes the alias as "chip" instead of allowing user to specify a custom name like "suit".

**Impact:** Scripts using ThisDevice cannot match intended alias names.

**Fix Required:** Add `AliasName` property to ThisDevice node (similar to PinDevice and NamedDevice).

---

### CRITICAL: No Simple Assignment Operator

| Expected | Actual |
|----------|--------|
| `INIT = 0` | `INIT += 0` |

**Problem:** CompoundAssign node only has compound operators (+=, -=, *=, /=). Setting "Assign (=)" via MCP appeared to work but reverted to "Add (+=)".

**Impact:** Cannot directly assign values to variables. Must use workarounds like `VAR = VAR * 0` or declare new variables.

**Fix Required:** Either:
1. Add "Assign (=)" as a true operator option in CompoundAssign
2. Create a separate "Assign" node for simple variable assignment

---

### LIMITATION: Nested IF/ELSEIF/ELSE Complexity

**Problem:** The original script has deeply nested conditionals:
- IF with nested IF/ELSEIF/ELSE
- ELSEIF branches
- Multiple independent IF blocks

**Impact:** Building this in VS would require:
- Multiple If nodes (one per condition)
- Careful wiring of True/False branches
- Many more nodes for the nested logic
- Significant canvas space

**Suggestion:** Consider adding:
1. ELSEIF pin on If node
2. SelectCase node for multi-branch scenarios
3. Node grouping/collapsing for complex scripts

---

## Missing Features Identified

### High Priority

| Feature | Description | Impact |
|---------|-------------|--------|
| ThisDevice.AliasName | Allow custom alias for THIS device | Cannot name self-reference |
| Simple Assignment | `variable = value` without compound operator | Cannot reset variables |
| ELSEIF support | If node needs ELSEIF branch pin | Complex conditionals require many nodes |

### Medium Priority

| Feature | Description | Impact |
|---------|-------------|--------|
| Node collapsing | Group nodes into collapsible regions | Large scripts hard to navigate |
| Variable read node | Read variable value without assignment | Need to wire variable output everywhere |
| Boolean constants | TRUE/FALSE constants | Currently use 1/0 |

### Low Priority

| Feature | Description | Impact |
|---------|-------------|--------|
| Preset device properties | Common property dropdowns per device type | Easier property selection |
| Script templates | Pre-built common patterns | Faster script creation |

---

## Conclusion

Visual Scripting can handle **moderately complex scripts** with:
- Multiple devices
- Sequential operations
- Sleep/Wait timing
- Simple conditionals
- Loops

However, **advanced scripts** with:
- Nested conditionals
- Variable reassignment
- Self-referencing devices (THIS)

...require additional features or significant workarounds.

### Recommended Priority Fixes

1. **ThisDevice.AliasName** - Add property to customize alias
2. **Simple Assignment node** - Create dedicated Assign node or fix CompoundAssign
3. **ELSEIF support** - Add ELSEIF pin to If node

---

**End of Report**
