# VS Complex Script Testing

**Test Date:** 2025-12-03
**Tester:** Claude Code (QA Instance)
**Purpose:** Test Visual Scripting with complex scripts to identify limitations

---

## Test Summary

Attempted to create complex scripts via MCP to test VS capabilities. Found significant limitations in node property configuration.

---

## Script 1: Temperature Controller

**Goal:** Read temperature from sensor, compare to target, control cooler

**Nodes Used:**
- Const (TARGET_TEMP = 295)
- NamedDevice (sensor, cooler)
- Variable (temp)
- EntryPoint, Label, If, WriteProperty x2, Yield, Goto
- ReadProperty, Compare, Constant x3

**Generated BASIC:**
```basic
# --- Variables ---
VAR temp = 0

# --- Constants ---
CONST TARGET_TEMP = 295

# --- Devices ---
DEVICE sensor "StructureGasSensor"
DEVICE cooler "StructureWallCooler"

# --- Main ---
MainLoop:
IF (sensor.On = 295) THEN
    cooler.On = 1
ELSE
    cooler.On = 0
ENDIF
YIELD
GOTO MainLoop
```

**Issues Found:**
1. ReadProperty uses `sensor.On` instead of `sensor.Temperature` - PropertyName not configurable
2. Compare uses `=` instead of `>` - Operator not configurable
3. WriteProperty generates correct `On` property (lucky default match)

**Status:** Compiles but logic is wrong

---

## Script 2: Simple Counter Loop

**Goal:** Increment a counter in a loop

**Nodes Used:**
- Variable (counter = 0)
- EntryPoint, Label, Increment, Yield, Goto

**Generated BASIC:**
```basic
VAR counter = 0
Loop:
++myVar
YIELD
GOTO Loop
```

**Issues Found:**
1. Increment uses `++myVar` instead of `++counter` - VariableName not configurable
2. Compilation fails because `myVar` is undefined

**Status:** Does not compile

---

## Missing Node Properties (via MCP)

| Node Type | Missing Property | Default Value | Impact |
|-----------|-----------------|---------------|--------|
| ReadProperty | PropertyName | "On" | Cannot read Temperature, Pressure, etc. |
| WriteProperty | PropertyName | "On" | Cannot write specific properties |
| Compare | Operator | "=" (Equal) | Cannot use >, <, >=, <=, != |
| Increment | VariableName | "myVar" | Cannot specify target variable |
| CompoundAssign | VariableName | "myVar" | Cannot specify target variable |
| CompoundAssign | Operator | "+=" | Cannot use -=, *=, /= |
| Const | (no output pin) | N/A | Cannot wire values, only declare |

---

## Working Features

These features work correctly:
- ✅ Node creation with positions
- ✅ Execution flow wiring (EntryPoint → Label → If → etc.)
- ✅ Data flow wiring (Device → ReadProperty, Constant → Compare)
- ✅ NamedDevice: AliasName and PrefabName configurable
- ✅ Const: ConstName and Value configurable
- ✅ Variable: VariableName and InitialValue configurable
- ✅ Label: LabelName configurable
- ✅ Goto: TargetLabel configurable
- ✅ Constant: Value configurable
- ✅ Code generation from connected graph
- ✅ IC10 compilation (when BASIC is valid)

---

## Recommendations

### High Priority - Property Exposure

The following nodes need their properties exposed for MCP configuration:

1. **ReadProperty**
   - Add `PropertyName` property (dropdown of valid properties)

2. **WriteProperty**
   - Add `PropertyName` property

3. **Compare**
   - Add `Operator` property (=, !=, <, >, <=, >=)

4. **Increment / Decrement**
   - Add `VariableName` property
   - OR require wiring to a Variable node's output

5. **CompoundAssign**
   - Add `VariableName` property
   - Add `Operator` property (+=, -=, *=, /=)

### Medium Priority

6. **Const Node**
   - Add output pin to wire constant value to other nodes
   - Currently only useful for declaration, not data flow

---

## UI vs MCP Comparison

Some of these properties may be editable in the UI (now that TextBox editing works). The MCP layer may just need to expose what's already available in the UI.

**Recommend:** Check each node in UI to see if properties are editable, then ensure MCP exposes the same properties.

---

**End of Test Report**
