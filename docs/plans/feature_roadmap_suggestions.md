# Basic-10 Feature Roadmap & Suggestions

**Created:** 2025-12-02
**Last Updated:** 2025-12-02
**Source:** Claude Code QA session + Reference compiler analysis
**Reference Compiler:** [exca/Basic-IC10](https://github.com/exca/Stationeers-IC10-Automation/tree/main/Basic-IC10)

---

## Priority 1: Regressions from Reference Compiler

These features existed in the original compiler but are broken/missing in Basic-10:

### 1.1 128-Line Limit Protection
**Status:** ✅ FIXED (2025-12-02) - Now shows warning when exceeding 128 lines

### 1.2 Increment/Decrement Operators
**Status:** ❌ MISSING
**Reference syntax:** `i++`, `i--`
**Basic-10 result:** "Unexpected token: Newline"
**Implementation:** Parse `IDENTIFIER++` as `IDENTIFIER = IDENTIFIER + 1`

### 1.3 Unit Suffixes
**Status:** ❌ MISSING
**Reference syntax:**
```basic
CONST temperature = 25C      # Converts to 298.15 Kelvin
CONST ratio = 10%            # Converts to 0.1
CONST pressure = 59.99 MPa   # Converts to 59990 kPa
```
**Why it matters:** Huge QoL for working with Stationeers units
**Implementation:** In lexer, detect NUMBER followed by unit suffix (C, K, Pa, kPa, MPa, %). Apply conversion during const evaluation.

### 1.4 Built-in Color Constants
**Status:** ❌ MISSING (must use numbers)
**Reference provides:**
```basic
Blue = 0, Grey = 1, Green = 2, Orange = 3, Red = 4
Yellow = 5, White = 6, Black = 7, Brown = 8, Khaki = 9
Pink = 10, Purple = 11
```
**Implementation:** Pre-define these as reserved constants in compiler.

### 1.5 wait() Function
**Status:** ❌ MISSING
**Reference syntax:** `wait(milliseconds)`
**Implementation:** Convert to multiple `yield` statements or use sleep register if available.

### 1.6 Reagent Access
**Status:** ❌ MISSING
**Reference syntax:**
```basic
MyDevice.Reagent[Iron].Contents
MyDevice.Reagent[Silicon].Required
```
**Implementation:** Generate `lr` (load reagent) instruction.

### 1.7 Boolean Operators || and &&
**Status:** ❌ MISSING (uses AND/OR keywords)
**Reference syntax:** `condition1 || condition2`, `condition1 && condition2`
**Implementation:** Add `||` and `&&` as aliases for OR/AND in lexer.

### 1.8 snan() and NaN Support
**Status:** ❓ Untested
**Reference syntax:** `snan(value)` to test for NaN
**Why it matters:** Channels return NaN when unset, need to detect this.

---

## Priority 2: Bugs Found in QA Testing

### 2.1 Negative Constants
**Status:** ❌ FAIL - `const X = -90` doesn't parse
**Workaround:** `var X = 0 - 90`
**Implementation:** Modify lexer to handle unary minus in const declarations.

### 2.2 XOR Operator
**Status:** ❌ STILL BROKEN - Generates `jal b` instead of `xor` instruction (retested 2025-12-02)
**Implementation:** Parser treating XOR operand as label. Add XOR to operator token list, map to IC10 `xor`.

### 2.3 Slot Access
**Status:** ✅ FIXED (2025-12-02) - Now generates proper `ls r1 device 0 Property` instruction

### 2.4 CASE ELSE
**Status:** ✅ FIXED (2025-12-02) - Now generates DEFAULT section correctly

### 2.5 .Count Batch Mode
**Status:** ✅ FIXED (2025-12-02) - Now generates `ldcn` instruction

---

## Priority 3: Quality of Life Improvements

### 3.1 Compound Assignment Operators
```basic
x += 5    # Instead of x = x + 5
x -= 2
x *= 3
x /= 2
```
**Implementation:** Parse `IDENTIFIER OP= EXPRESSION` and transform.

### 3.2 Loop EXIT/BREAK/CONTINUE
```basic
FOR i = 1 TO 100
    IF condition THEN EXIT FOR
    IF skip THEN CONTINUE
NEXT i
```
**Implementation:** Track loop end labels, EXIT jumps to end, CONTINUE jumps to increment.

### 3.3 Ternary Operator
```basic
color = (delta > 0) ? COLOR_GREEN : COLOR_RED
```
**Implementation:** Parse as inline IF/ELSE expression.

### 3.4 Range Check Operator
```basic
IF pressure IN 25..28 THEN
```
**Implementation:** Expand to `IF pressure >= 25 AND pressure <= 28 THEN`

### 3.5 CLAMP Built-in
```basic
value = CLAMP(input, min, max)
```
**Implementation:** Expand to `max(min, min(input, max))`

### 3.6 SWAP Statement
```basic
SWAP a, b
```
**Implementation:** Generate temp variable swap sequence.

### 3.7 Multiple Assignment
```basic
a, b, c = 1, 2, 3
```
**Implementation:** Parse comma-separated assignments.

---

## Priority 4: Syntax Highlighting Improvements

| Element | Suggested Color | Notes |
|---------|-----------------|-------|
| Keywords (IF, FOR, WHILE) | Blue | Control flow |
| Device types (StructureGasSensor) | Purple | Distinct from variables |
| Property names (.Temperature) | Teal/Cyan | Italic style |
| Batch modes (.Average, .Sum) | Teal Bold | Emphasize |
| Constants | Orange | Different from variables |
| Numbers | Light Blue | |
| Strings/Device names | Green | Standard string color |
| Comments | Gray/Dimmed | |
| Operators | White/Default | |
| Built-in functions | Yellow | sin, cos, floor, etc. |
| Labels | Pink/Magenta | Stand out for GOTO |
| Unit suffixes (C, %, MPa) | Orange | Match constants |

---

## Priority 5: Differentiating Features

These would make Basic-10 truly stand out:

### 5.1 Visual IC Network Designer
- Drag-and-drop IC chips onto canvas
- Connect them visually
- Generate inter-IC communication code
- See data flow between chips

### 5.2 Live Game Connection
- Read actual device values from running game
- Test scripts against real data
- Requires game mod or memory reading

### 5.3 Automatic Optimization Pass
```
"Your code: 95 lines → Optimized: 72 lines"
```
- Dead code elimination
- Constant folding
- Register reuse optimization
- Redundant jump removal

### 5.4 Working Decompiler
- Paste IC10 MIPS code
- Get readable BASIC back
- Useful for learning from others' code

### 5.5 Script Repository Integration
- Browse community scripts
- One-click import
- Rate/comment on scripts
- Share your scripts

### 5.6 Real Physics Simulation
- Simulate actual pipe pressure dynamics
- Power network behavior
- Thermal transfer
- Not just IC10 execution

### 5.7 Multi-File Projects
- Split large systems across files
- Include/import other files
- Shared constants/aliases
- Project-level compilation

### 5.8 Macro/Template System
```basic
@MACRO StatusLight(device, delta)
    device.On = 1
    IF delta > 0 THEN
        device.Color = Green
    ELSEIF delta < 0 THEN
        device.Color = Red
    ELSE
        device.Color = Yellow
    ENDIF
@ENDMACRO

# Usage:
@StatusLight(stat1, diff1)
@StatusLight(stat2, diff2)
```
**Impact:** Would dramatically reduce repetitive code bloat.

### 5.9 Device Array References
```basic
DIM batts(5) AS Device
batts(0) = IC.Device[StructureBatteryLarge].Name["Station Battery 1"]
batts(1) = IC.Device[StructureBatteryLarge].Name["Station Battery 2"]

FOR i = 0 TO 4
    display(i).Setting = batts(i).Charge
NEXT i
```
**Impact:** This is the holy grail for reducing repetitive code.

### 5.10 PRINT Debug Statement
```basic
PRINT "Temperature: ", temp
```
- Output to debug console
- Stripped from release builds
- Invaluable for debugging

---

## Priority 6: MCP Tools for Testing

**Status:** ✅ IMPLEMENTED (2025-12-02)

All core MCP tools now available and verified working:

### Simulator Control ✅
- `simulator_start/stop/step/run/reset` - All working
- `simulator_get_state` - Shows PC, status, registers
- `simulator_set_register` - Modify register values
- `simulator_set_device/get_device` - Device property simulation

### Debugging ✅
- `simulator_add_breakpoint/clear_breakpoints` - Breakpoint management
- `add_watch/get_watches/clear_watches` - Variable monitoring
- `get_sourcemap` - BASIC to IC10 line mapping
- `get_symbols` - Symbol table access
- `find_references` - Find all uses of a symbol

### Code Analysis ✅
- `get_metrics` - Line counts, symbol counts
- `get_errors` - Compilation errors/warnings
- `get_cursor/set_cursor` - Cursor position
- `get_settings/update_setting` - App settings

### Simulator Verification Results
Tested 2025-12-02 - all calculations verified correct:
- Math operations (add, sub, mul) ✅
- WHILE loops iterate correctly ✅
- IF/ELSE conditionals branch correctly ✅
- Device read (`l` instruction) ✅
- Device write (`s` instruction) ✅

---

## Implementation Order Suggestion

### Phase 1: Critical Fixes ~~(Do First)~~ MOSTLY DONE
1. ~~128-line limit protection~~ ✅ DONE
2. Negative constants - **STILL NEEDED**
3. XOR operator fix - **STILL NEEDED**
4. ~~Slot access fix~~ ✅ DONE

### Phase 2: Reference Parity
5. `i++`/`i--` operators
6. Unit suffixes (C, %, MPa)
7. Built-in color constants
8. `||` and `&&` operators
9. ~~CASE ELSE fix~~ ✅ DONE
10. Reagent access

### Phase 3: Quality of Life
11. Compound assignments (+=, -=)
12. Loop EXIT/BREAK/CONTINUE
13. ~~.Count batch mode~~ ✅ DONE
14. wait() function

### Phase 4: Differentiators
15. Macro system (biggest impact on usability)
16. PRINT debug statement
17. Automatic optimization
18. Decompiler improvements

### Phase 5: Advanced
19. Device array references
20. Multi-file projects
21. Visual network designer
22. Live game connection

---

## Competitive Analysis

| Feature | Basic-10 | Reference (exca) | icX | Raw IC10 |
|---------|----------|------------------|-----|----------|
| Named devices | ✅ | ✅ | ✅ | ❌ |
| Batch operations | ✅ | ✅ | ✅ | Manual |
| Simulator | ✅ | ✅ | ❌ | ❌ |
| FOR/WHILE loops | ✅ | ✅ | ✅ | Manual |
| Arrays | ✅ | ✅ | ✅ | Manual |
| MCP Integration | ✅ | ❌ | ❌ | ❌ |
| Modern IDE | ✅ | Basic | ❌ | ❌ |
| Active development | ✅ | ❌ (2yr stale) | ? | N/A |
| Unit suffixes | ❌ | ✅ | ? | ❌ |
| 128-line protection | ❌ | ✅ | ? | ❌ |

**Basic-10's unique advantages:**
1. MCP integration (only one with AI pair programming)
2. Modern IDE (themes, effects, tabs)
3. Active development
4. Watch window / Variable inspector

**Marketing pitch:**
> "The most actively developed IC10 IDE with AI integration. Write BASIC, get optimized MIPS. Named device references, built-in simulator, and the only compiler that can pair-program with Claude."

---

## Sources

- [exca/Basic-IC10 Reference Compiler](https://github.com/exca/Stationeers-IC10-Automation/tree/main/Basic-IC10)
- [Basic Language Reference](https://github.com/exca/Stationeers-IC10-Automation/blob/main/Basic-IC10/Basic%20Language%20Reference.md)
- [Quick Start Guide](https://github.com/exca/Stationeers-IC10-Automation/blob/main/Basic-IC10/Quick%20Start.md)
- [Steam Workshop - Basic-IC10](https://steamcommunity.com/workshop/filedetails/?id=3108928209)

---
