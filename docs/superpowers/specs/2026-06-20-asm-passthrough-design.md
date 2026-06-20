# ASM/EASM Raw IC10 Passthrough — Design

**Date:** 2026-06-20
**Issue:** #8 (Stationeers_BasIC-10) — "feat: add keyword/block to allow passthrough IC10"
**Status:** Approved (pending spec review)

## Summary

Add an `ASM ... EASM` block that lets a developer drop raw IC10 assembly into BASIC
source. The lines inside the block are emitted **verbatim** into the compiled IC10
output (no rewriting, no reformatting). The block contents are **validated for
diagnostics only** — warnings never block compilation, so the feature remains usable
for IC10 instructions newer than the compiler's opcode table.

## Motivation

There are edge cases and newly-released game instructions the BASIC compiler does not
yet support. A passthrough block lets users hand-write IC10 for those cases without
abandoning BASIC for the rest of the program.

## Syntax

```basic
temp = sensor.Temperature      ' normal BASIC
ASM
    l r15 d1 Setting           # raw IC10, emitted verbatim
    add r15 r15 r0
    s d2 Setting r15
EASM
heater.On = 1                  ' back to BASIC
```

- `ASM` and `EASM` each appear on their own line.
- Case-insensitive, consistent with other BASIC keywords.
- It is a statement: allowed anywhere a statement is valid (top level, inside
  `IF`/loops/`FUNCTION`). Verbatim lines are emitted in place.
- No nesting; the first `EASM` ends the block.

## Register contract

- BASIC variables occupy **r0–r13**; the compiler uses **r14/r15** as transient scratch.
- **Safe for ASM:** `r14`, `r15`, and device pins `d0`–`d5`, `db`. These are guaranteed
  safe because compiler temps are transient *within* a statement and an ASM block sits
  *between* statements. No register-allocator change is required.
- Writing `r0`–`r13` inside ASM is allowed but risky (may clobber a live BASIC variable);
  it produces a warning (see Validation). No variable-name interpolation in v1.

## Semantics

- Lines emitted **verbatim**, unchanged, in source order.
- Instruction lines count toward the script line limit (128 default / 512 extended).
  `#` comment lines do not count (the existing line counter already excludes `#`).
- Aggressive optimization (level 2) strips `#` comments inside the block, the same as
  everywhere else. Instructions are never altered.

## Validation (diagnostics only)

Validation produces diagnostics through the existing `AnalysisWarning` pipeline with
source line numbers offset to the block's position. **All findings are warnings
(non-blocking) except an unterminated block.** Rationale: a blocking error on an unknown
opcode would make it impossible to use a genuinely-new game instruction the compiler's
table does not know yet — which is the feature's whole purpose.

Checks (v1):

1. **Unknown opcode → warning.** Reuse `IC10Parser` classification; a non-comment,
   non-label line whose opcode classifies as `IC10InstructionType.Unknown` is flagged.
   The opcode table is extended with any instructions currently missing, seeded from
   `docs/IC10Reference.md`.
2. **Write to r0–r13 → warning.** e.g. "ASM writes r3, which may hold a BASIC variable —
   prefer r14/r15 for scratch."
3. **Unterminated block (`ASM` with no `EASM`) → hard error** (lexer; unrecoverable).
4. **Malformed line (wrong operand count) → warning.** Applies only to opcodes for which
   a known arity is defined (a small operand-count table for the common instructions);
   opcodes without arity data are skipped rather than guessed at. Conservative by design,
   to avoid false alarms and to stay compatible with future instructions.

## Implementation

1. **Lexer** (`src/Lexer/`): on matching keyword `ASM`, switch to raw-capture mode and
   collect every subsequent line verbatim until a line that trims to `EASM`. Emit a single
   `TokenType.AsmBlock` token carrying the inner text. Unterminated → `LexerException`.
   (Alternative considered and rejected: tokenizing the inner IC10 as BASIC — IC10 is not
   valid BASIC.)
2. **TokenType** (`src/Lexer/TokenType.cs`): add `AsmBlock`.
3. **AST** (`src/AST/AstNode.cs`): `AsmBlockStatement : StatementNode { string RawCode; }`
   (retain the block's starting source line for diagnostics).
4. **Parser** (`src/Parser/Parser.cs`): an `AsmBlock` token becomes an `AsmBlockStatement`.
5. **Codegen** (`src/CodeGen/MipsGenerator.cs`): emit `RawCode` lines unchanged.
6. **Validation** (`src/IC10/` + `src/Analysis/StaticAnalyzer.cs`): a small `AsmValidator`
   (reusing the `IC10Parser` classifier and a known-opcode set) invoked from
   `StaticAnalyzer` so findings appear in the Problems panel before codegen.
7. **Editor** (optional, small): add `ASM`/`EASM` to completion keywords.
8. **LanguageDetector** (`src/Shared/LanguageDetector.cs`): an ASM block is a BASIC-only
   construct, but its IC10 contents would otherwise inflate the IC10 detection score and
   misroute a BASIC-with-ASM program to IC10 passthrough mode (echoing `ASM`/`EASM` into
   the output). Fix: skip lines between `ASM` and `EASM` when scoring, and treat the
   `ASM` marker as a definitive BASIC signal. (Found during implementation/testing.)

## Testing (headless harness)

- Basic block emits verbatim.
- Block between two BASIC statements; surrounding BASIC still compiles correctly.
- Empty block emits nothing.
- Unterminated block → error.
- Block inside a loop emits once at that point.
- Line-count limit includes ASM instruction lines, excludes ASM `#` comments.
- Validation: unknown opcode → warning (not error); write to r0–r13 → warning;
  valid block → no warnings.

## Out of scope (v1)

- Variable/alias name interpolation inside ASM.
- Full IC10 semantic/type validation (operand register-vs-immediate checking beyond count).
- Register-allocator reservation changes.

## Documentation

- Add an `ASM`/`EASM` section to `docs/LanguageReference.md` describing syntax, the register
  contract (use r14/r15 for scratch), and that validation is advisory.
