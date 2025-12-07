# Consolidated Backlog - All Unfinished Tasks

**Date:** 2025-12-05
**Purpose:** Master list of all unfinished features, bugs, and improvements from previous planning documents
**Status:** Active

---

## Priority 1: Critical Bug Fixes

These should be addressed before new features.

### BUG-CONST-01: Negative Constants Don't Parse
**Source:** `feature_roadmap_suggestions.md`
**Status:** Open
**Severity:** Medium

```basic
CONST X = -90    # Fails: "Unexpected token"
```

**Workaround:** `VAR X = 0 - 90`
**Fix:** Modify lexer to handle unary minus in const declarations.

---

### BUG-XOR-01: XOR Operator Generates Wrong Code
**Source:** `feature_roadmap_suggestions.md`
**Status:** Open
**Severity:** Medium

```basic
result = a XOR b    # Generates "jal b" instead of "xor"
```

**Fix:** Parser treating XOR operand as label. Add XOR to operator token list, map to IC10 `xor`.

---

## Priority 2: Language Parity with Reference Compiler

Features that exist in the reference compiler (exca/Basic-IC10) but are missing in Basic-10.

### LANG-01: Increment/Decrement Operators
**Source:** `feature_roadmap_suggestions.md`
**Status:** Open
**Effort:** Low

```basic
i++    # Should work like i = i + 1
i--    # Should work like i = i - 1
```

**Note:** `++i` and `--i` prefix versions already work.

---

### LANG-02: Unit Suffixes
**Source:** `feature_roadmap_suggestions.md`
**Status:** Open
**Effort:** Medium

```basic
CONST temperature = 25C      # Converts to 298.15 Kelvin
CONST ratio = 10%            # Converts to 0.1
CONST pressure = 59.99MPa    # Converts to 59990 kPa
```

**Why:** Huge QoL for Stationeers units.
**Implementation:** In lexer, detect NUMBER followed by unit suffix. Apply conversion during const evaluation.

---

### LANG-03: Built-in Color Constants
**Source:** `feature_roadmap_suggestions.md`
**Status:** Open
**Effort:** Low

Pre-define Stationeers color values:
```basic
Blue = 0, Grey = 1, Green = 2, Orange = 3, Red = 4
Yellow = 5, White = 6, Black = 7, Brown = 8, Khaki = 9
Pink = 10, Purple = 11
```

**Implementation:** Add to reserved constants in compiler.

---

### LANG-04: Boolean Operators || and &&
**Source:** `feature_roadmap_suggestions.md`
**Status:** Open
**Effort:** Low

```basic
IF condition1 || condition2 THEN    # Alternative to OR
IF condition1 && condition2 THEN    # Alternative to AND
```

**Implementation:** Add `||` and `&&` as aliases for OR/AND in lexer.

---

### LANG-05: wait() Function
**Source:** `feature_roadmap_suggestions.md`
**Status:** Open
**Effort:** Medium

```basic
wait(500)    # Wait 500ms (multiple yields)
```

**Implementation:** Convert to multiple `yield` statements based on tick rate.

---

### LANG-06: Reagent Access
**Source:** `feature_roadmap_suggestions.md`
**Status:** Open
**Effort:** Medium

```basic
MyDevice.Reagent[Iron].Contents
MyDevice.Reagent[Silicon].Required
```

**Implementation:** Generate `lr` (load reagent) instruction.

---

## Priority 3: Quality of Life Improvements

### QOL-01: Compound Assignment Operators
**Source:** `feature_roadmap_suggestions.md`
**Status:** Open
**Effort:** Low

```basic
x += 5    # Instead of x = x + 5
x -= 2
x *= 3
x /= 2
```

**Implementation:** Parse `IDENTIFIER OP= EXPRESSION` and transform.

---

### QOL-02: Loop EXIT/BREAK/CONTINUE
**Source:** `feature_roadmap_suggestions.md`
**Status:** Open
**Effort:** Medium

```basic
FOR i = 1 TO 100
    IF condition THEN EXIT FOR
    IF skip THEN CONTINUE
NEXT i
```

**Implementation:** Track loop end labels, EXIT jumps to end, CONTINUE jumps to increment.

---

### QOL-03: Ternary Operator
**Source:** `feature_roadmap_suggestions.md`
**Status:** Open
**Effort:** Medium

```basic
color = (delta > 0) ? COLOR_GREEN : COLOR_RED
```

**Implementation:** Parse as inline IF/ELSE expression.

---

### QOL-04: Range Check Operator
**Source:** `feature_roadmap_suggestions.md`
**Status:** Open
**Effort:** Low

```basic
IF pressure IN 25..28 THEN
```

**Implementation:** Expand to `IF pressure >= 25 AND pressure <= 28 THEN`

---

### QOL-05: CLAMP Built-in
**Source:** `feature_roadmap_suggestions.md`
**Status:** Open
**Effort:** Low

```basic
value = CLAMP(input, min, max)
```

**Implementation:** Expand to `max(min, min(input, max))`

---

### QOL-06: SWAP Statement
**Source:** `feature_roadmap_suggestions.md`
**Status:** Open
**Effort:** Low

```basic
SWAP a, b
```

**Implementation:** Generate temp variable swap sequence.

---

### QOL-07: Multiple Assignment
**Source:** `feature_roadmap_suggestions.md`
**Status:** Open
**Effort:** Medium

```basic
a, b, c = 1, 2, 3
```

**Implementation:** Parse comma-separated assignments.

---

## Priority 4: Simulator Enhancements (Phase 2)

### SIM-01: Pause Button (FR-003)
**Source:** `2025-12-04-visual-scripting-phase2.md`
**Status:** Not Started
**Effort:** Low
**Priority:** High

Freeze execution without resetting state. Allow editing while paused.

---

### SIM-02: Named Devices in UI (FR-002)
**Source:** `2025-12-04-visual-scripting-phase2.md`
**Status:** Not Started
**Effort:** Medium
**Priority:** High

Display ALIAS devices in simulator panel with friendly names.

---

### SIM-03: Auto-Refresh Code (FR-001)
**Source:** `2025-12-04-visual-scripting-phase2.md`
**Status:** Not Started
**Effort:** Low
**Priority:** Medium

Reload button + optional auto-detect code changes.

---

## Priority 5: Visual Scripting Enhancements (Phase 2)

### VS-01: Example Scripts
**Source:** `2025-12-04-visual-scripting-phase2.md`
**Status:** Not Started
**Effort:** Low-Medium

12 example scripts from basic to advanced automation.

---

### VS-02: Bidirectional Sync
**Source:** `2025-12-04-visual-scripting-phase2.md`
**Status:** Not Started
**Effort:** High

Parse BASIC code and generate visual graph (reverse direction).

---

### VS-03: Load Script → Visual
**Source:** `2025-12-04-visual-scripting-phase2.md`
**Status:** Not Started
**Effort:** Medium
**Depends:** VS-02

Auto-generate visual representation when loading scripts.

---

## Priority 6: Advanced Features

### ADV-01: Macro/Template System
**Source:** `feature_roadmap_suggestions.md`
**Status:** Open
**Effort:** High
**Impact:** Very High

```basic
@MACRO StatusLight(device, delta)
    device.On = 1
    IF delta > 0 THEN device.Color = Green
    ELSE device.Color = Red
    ENDIF
@ENDMACRO

@StatusLight(stat1, diff1)
```

**Impact:** Would dramatically reduce repetitive code bloat.

---

### ADV-02: Device Array References
**Source:** `feature_roadmap_suggestions.md`
**Status:** Open
**Effort:** High
**Impact:** Very High

```basic
DIM batts(5) AS Device
FOR i = 0 TO 4
    display(i).Setting = batts(i).Charge
NEXT i
```

**Impact:** Holy grail for reducing repetitive code.

---

### ADV-03: PRINT Debug Statement
**Source:** `feature_roadmap_suggestions.md`
**Status:** Open
**Effort:** Low

```basic
PRINT "Temperature: ", temp    # Output to debug console
```

Stripped from release builds.

---

### ADV-04: Automatic Optimization Pass
**Source:** `feature_roadmap_suggestions.md`
**Status:** Open
**Effort:** High

- Dead code elimination
- Constant folding
- Register reuse optimization
- Redundant jump removal

---

### ADV-05: Multi-File Projects
**Source:** `feature_roadmap_suggestions.md`
**Status:** Open
**Effort:** High

- INCLUDE statement
- Shared constants/aliases
- Project-level compilation

---

## Completed Items (Reference)

These items from previous plans have been completed:

| Item | Version | Date |
|------|---------|------|
| 128-line limit protection | v2.x | 2025-12-02 |
| Slot access fix | v2.x | 2025-12-02 |
| CASE ELSE fix | v2.x | 2025-12-02 |
| .Count batch mode | v2.x | 2025-12-02 |
| MCP Integration | v2.x | 2025-12-02 |
| Visual Scripting (basic) | v3.0 | 2025-12-03 |
| Simulator Loop Mode | v3.0.18 | 2025-12-04 |
| VS → Editor sync fix | v3.0.20 | 2025-12-04 |

---

## Implementation Phases

### Phase A: Bug Fixes (v3.0.21)
1. SIM-01: Pause Button
2. BUG-CONST-01: Negative constants
3. BUG-XOR-01: XOR operator

### Phase B: Language Parity (v3.0.22-v3.0.25)
1. LANG-01: i++/i-- operators
2. LANG-03: Color constants
3. LANG-04: || and && operators
4. QOL-01: Compound assignments (+=, -=)

### Phase C: Simulator & VS (v3.0.26-v3.0.30)
1. SIM-02: Named devices in simulator
2. SIM-03: Auto-refresh code
3. VS-01: Example scripts (12)

### Phase D: Quality of Life (v3.1.0)
1. LANG-02: Unit suffixes
2. QOL-02: EXIT/CONTINUE
3. QOL-05: CLAMP built-in
4. ADV-03: PRINT debug

### Phase E: Visual Scripting Core (v3.2.0)
1. VS-02: Bidirectional sync
2. VS-03: Load script → visual

### Phase F: Advanced (v4.0.0)
1. ADV-01: Macro system
2. ADV-02: Device arrays
3. ADV-05: Multi-file projects

---

## Version Targets Summary

| Version | Focus | Key Features |
|---------|-------|--------------|
| v3.0.21 | Bug Fixes | Pause button, negative const, XOR |
| v3.0.22 | Language | i++/i--, colors, operators |
| v3.0.26 | Simulator | Named devices, auto-refresh |
| v3.0.30 | VS Content | 12 example scripts |
| v3.1.0 | QoL | Unit suffixes, EXIT/CONTINUE, CLAMP |
| v3.2.0 | VS Core | Bidirectional sync |
| v4.0.0 | Advanced | Macros, device arrays |

---

**End of Consolidated Backlog**
