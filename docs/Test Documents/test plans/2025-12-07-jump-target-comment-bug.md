# Bug Report: Jump Target Miscalculation Due to Comment Line Counting

**Date Discovered:** 2025-12-07
**Severity:** Critical - Causes all GOTO/loop constructs to fail
**Affected Versions:** All versions prior to fix
**Status:** FIXED (verified 2026-01-01)
**Fix Applied:** Comments no longer counted in jump target calculations

## Summary

The compiler incorrectly calculates jump target line numbers by counting comment lines (`#` prefixed lines) in the `instructionNumber` counter. In Stationeers IC10, comment lines are NOT executable instructions and are NOT counted when the IC processor calculates jump destinations. This causes all jump instructions to target incorrect lines, breaking loops, conditionals, and GOTO statements.

## Discovery Context

### Test Script Used
```basic
# Elapsed Time - Bare minimum test
# No external device reads at all

VAR counter = 0

Main:
    counter = counter + 1
    db.Setting = counter
    YIELD
    GOTO Main
END
```

### Expected Behavior
- `db.Setting` should increment by 1 every 0.5 seconds
- Counter should count: 1, 2, 3, 4, 5...

### Actual Behavior
- `db.Setting` stays static at 1
- IC shows State 1 (running) but counter never advances

## Root Cause Analysis

### Compiled IC10 Output (Buggy)
```ic10
# Elapsed Time - Bare minimum test    <- Line 0 (counted by compiler)
# No external device reads at all     <- Line 1 (counted by compiler)
move r0 0                             <- Line 2 (counted by compiler)
add r1 r0 1                           <- Line 3 (counted by compiler)
move r0 r1                            <- Line 4
s db Setting r0                       <- Line 5
yield                                 <- Line 6
j 1                                   <- Line 7 - JUMPS TO LINE 1!
hcf                                   <- Line 8
```

### Working Raw IC10 (Manual)
```ic10
move r0 0
Main:
add r0 r0 1
s db Setting r0
yield
j Main
```

### The Problem

The compiler emits `j 1` intending to jump to the `add` instruction (the start of the loop). However:

1. **Compiler's calculation:** Comments at lines 0 and 1, so `add` instruction is at line 3. But the compiler starts the loop label at what it thinks is line 1 (skipping the initialization at line 0).

2. **IC10 reality:** The IC processor SKIPS comment lines during execution but COUNTS them for line numbering. So `j 1` actually jumps to the second comment line.

3. **Execution flow after `j 1`:**
   - Line 1: `# No external device reads...` (comment - skipped)
   - Line 2: `move r0 0` (EXECUTED - resets counter to 0!)
   - Line 3: `add r1 r0 1` (r1 = 0 + 1 = 1)
   - Line 4: `move r0 r1` (r0 = 1)
   - Line 5: `s db Setting r0` (db = 1)
   - Line 6: `yield`
   - Line 7: `j 1` (back to line 1, repeat)

The counter resets to 0 every iteration, so `db.Setting` always shows 1.

## Bug Location

**File:** `src/CodeGen/MipsGenerator.cs`
**Method:** `ConvertLabelsToOffsets()` (lines 184-258)
**Specific Issue:** Lines 220-224

### Current Buggy Code (lines 220-224):
```csharp
else
{
    outputLines.Add(line);
    instructionNumber++;  // BUG: Increments for ALL lines including comments!
}
```

### Analysis
The `else` block handles all non-label lines. It adds every line to `outputLines` and increments `instructionNumber` for every line. This includes comment lines (starting with `#`), which should NOT increment the instruction counter because IC10 does not count them as executable lines.

## Fix

### Corrected Code:
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

### Alternative Fix
Strip comments from IC10 output entirely (remove lines starting with `#` before or during label resolution). This would also reduce script size in-game.

## Impact Assessment

This bug affects ALL control flow statements:
- `GOTO` statements (always jump to wrong line)
- `IF/THEN/ELSE/ENDIF` blocks
- `WHILE/WEND` loops
- `FOR/NEXT` loops
- `DO/LOOP` constructs
- `SELECT CASE` statements
- `GOSUB/RETURN`

Any script that includes a comment before the first executable instruction will have incorrect jump targets.

## Test Cases for Verification

### Test 1: Basic Counter Loop
```basic
# Comment line
VAR x = 0
Main:
    x = x + 1
    db.Setting = x
    YIELD
    GOTO Main
END
```
**Expected:** db.Setting increments: 1, 2, 3...
**Pass Criteria:** Value changes every 0.5 seconds

### Test 2: No Comments
```basic
VAR x = 0
Main:
    x = x + 1
    db.Setting = x
    YIELD
    GOTO Main
END
```
**Expected:** Should work correctly (no comments to miscount)
**Pass Criteria:** Value increments

### Test 3: IF Statement with Comments
```basic
# Test IF
VAR x = 0
Main:
    x = x + 1
    IF x > 5 THEN
        x = 0
    ENDIF
    db.Setting = x
    YIELD
    GOTO Main
END
```
**Expected:** db.Setting cycles 1-5, resets to 0, repeats
**Pass Criteria:** Values cycle correctly

## Workaround

Until the fix is applied, users can:
1. Remove all comment lines from BASIC source before compiling
2. Write raw IC10 with labels instead of using BASIC
3. Manually adjust jump targets in the IC10 output

## Related Files

- `src/CodeGen/MipsGenerator.cs` - Contains the bug
- `src/Parser/Parser.cs` - May need review for related issues
- Test script: This document

## Regression Prevention

Add unit tests that verify:
1. Jump targets are calculated correctly when comments exist
2. Jump targets are calculated correctly without comments
3. Multiple consecutive comments don't compound the offset error
4. Comments inside code blocks (not just at start) don't affect jumps
