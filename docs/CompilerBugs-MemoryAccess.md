# Compiler Bug: Named Device Memory Access

**Date Discovered:** 2026-01-01
**Severity:** High - Generates invalid IC10 code that fails at runtime
**Status:** FIXED (2026-01-01)
**Fix Applied:** Compiler now emits clear error when Memory access used with named devices

## Summary

When using named device syntax (`IC.Device[Type].Name["name"]`) for memory access operations (`device.Memory[address]`), the compiler generates incorrect IC10 instructions with wrong argument counts.

## Affected Operations

### 1. Memory Write (`putd`)

**BASIC Code:**
```basic
ALIAS StateMem = IC.Device["StructureLogicMemory"].Name["System State"]
StateMem.Memory[0] = mode
```

**Generated IC10 (BROKEN):**
```ic10
putd -851746783 -724041079 0 r0
```
- **Problem:** `putd` only accepts 3 arguments: `putd deviceHash address value`
- **Actual:** Compiler generates 4 arguments: `putd deviceHash nameHash address value`
- **Error:** `incorrectArgumentCount` at runtime

### 2. Memory Read (`getd`)

**BASIC Code:**
```basic
ALIAS TargetMem = IC.Device["StructureLogicMemory"].Name["Gas Targets"]
targetO2 = TargetMem.Memory[0]
```

**Generated IC10 (BROKEN):**
```ic10
getd r10 -851746783 1729869689 0
```
- **Problem:** `getd` only accepts 3 arguments: `getd register deviceHash address`
- **Actual:** Compiler generates 4 arguments: `getd register deviceHash nameHash address`
- **Error:** `incorrectArgumentCount` at runtime

## Root Cause Analysis

The compiler treats Memory access like a regular property access (using `sbn`/`lbn` syntax with 4 arguments), but IC10's memory instructions (`get`/`put`/`getd`/`putd`) have different argument structures:

| Instruction | Purpose | Arguments |
|-------------|---------|-----------|
| `sbn` | Set property by name | `sbn deviceHash nameHash property value` (4 args) |
| `lbn` | Load property by name | `lbn register deviceHash nameHash property batchMode` (5 args) |
| `put` | Write to memory (pin) | `put d# address value` (3 args) |
| `get` | Read from memory (pin) | `get register d# address` (3 args) |
| `putd` | Write to memory (hash) | `putd deviceHash address value` (3 args) |
| `getd` | Read from memory (hash) | `getd register deviceHash address` (3 args) |

The compiler incorrectly uses the `sbn`/`lbn` pattern (with nameHash) for memory instructions, but `putd`/`getd` don't support name hashes - they only work with device type hashes.

## Solution

Pin aliases (`d0`-`d5`) must be used for memory access:

```basic
# Instead of:
# ALIAS StateMem = IC.Device["StructureLogicMemory"].Name["System State"]

# Use pin alias:
ALIAS StateMem = d0

# Memory access compiles correctly:
StateMem.Memory[0] = mode    # Generates: put d0 0 r0
mode = StateMem.Memory[0]    # Generates: get r0 d0 0
```

## Fix Implemented (Option B)

The compiler now emits a clear error when `device.Memory[n]` is used with a named device:

```
[error] Memory access (.Memory[]) requires pin alias (d0-d5) at line X.
Named device 'StateMem' cannot be used with .Memory[] due to IC10 limitations.
Use: ALIAS StateMem = d0
```

This prevents generating invalid IC10 code and guides users to the correct approach.

## Files Changed

- `src/CodeGen/MipsGenerator.cs` - Added validation for memory access with named devices

## Related Bug: DIM Initializer with CONST/Variables

**Status:** FIXED (2026-01-01)

A separate bug existed where `DIM` initializers containing CONST names or variable expressions generated incorrect `jal` calls:

```basic
CONST MODE_AUTO = 1
DIM mode = MODE_AUTO    # Was generating: jal MODE_AUTO (WRONG)

DIM slot = activePreset - 17   # Was generating: jal activePreset (WRONG)
```

**Fix Applied:** DIM initializers now correctly resolve CONST names to their values and properly evaluate expressions:
```basic
# Now works correctly:
CONST MODE_AUTO = 1
DIM mode = MODE_AUTO           # Generates: move r0 1 (CORRECT)
DIM slot = activePreset - 17   # Generates: sub r1 r0 17 (CORRECT)
```

## Scripts Using Pin Aliases for Memory Access

The following scripts use pin aliases for memory access. With the compiler fix, this is now the required approach (named devices will produce a helpful error message):

All greenhouse controller scripts updated 2026-01-01:
- Greenhouse Master Controller (d0 = System State)
- Greenhouse Safety Monitor (d0 = System State)
- Greenhouse Preset Library (d0 = System State, d1 = Gas Targets, d2 = Custom Presets)
- Greenhouse O2 Controller (d0 = System State, d1 = Gas Targets)
- Greenhouse N2 Controller (d0 = System State, d1 = Gas Targets)
- Greenhouse CO2 Controller (d0 = System State, d1 = Gas Targets)

**Note:** Pin aliases remain the correct approach for memory access since IC10's `get`/`put` instructions require device pins, not name hashes.
