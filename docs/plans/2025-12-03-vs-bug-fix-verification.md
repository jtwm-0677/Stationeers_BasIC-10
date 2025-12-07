# Visual Scripting Bug Fix Verification

**Test Date:** 2025-12-03
**Tester:** Claude Code (QA Instance)
**Version:** v1.9.x

---

## Test 1: Comment Character Fix

**Purpose:** Verify VS code generator uses `#` instead of `'` for comments

**Steps:**
1. Opened Visual Scripting window
2. Loaded example script with nodes
3. Checked generated BASIC code comments
4. Verified IC10 compilation

**Results:**
- All comments use `#` prefix: `# --- Variables ---`, `# --- Devices ---`, `# --- Main ---`, `# --- Program Start ---`
- IC10 compiles successfully with valid `#` comments
- **Status: PASS**

---

## Test 2: Generated Code Pane Display

**Purpose:** Verify code is generated and displayed when nodes are connected

**Steps:**
1. Cleared canvas
2. Added EntryPoint, Variable (testVar), CompoundAssign nodes
3. Connected EntryPoint -> CompoundAssign (exec)
4. Connected Variable -> CompoundAssign (value)
5. Observed generated code via MCP

**Results:**
- MCP returns valid BASIC code: `VAR testVar = 0` and `myVar += testVar`
- IC10 compiles successfully
- **Status: PARTIAL** - Code pane only updates when toggling BASIC<->IC10 view

---

## Test 3: VS to Main Editor Sync

**Purpose:** Verify VS generated code syncs to main BASIC editor

**Steps:**
1. Set main editor to: `VAR originalVar = 999` / `YIELD`
2. Cleared VS canvas
3. Created script: EntryPoint -> Variable (myVar = 42)
4. Connected nodes
5. Checked main editor content

**Results:**
- Main editor updated from `VAR originalVar = 999` to `VAR myVar = 42`
- VS code successfully synced to main editor
- **Status: PASS**

---

## Test 4: Wire Connections Affect Code Generation

**Purpose:** Verify that connecting/disconnecting wires updates generated code

**Steps:**
1. Created script with EntryPoint, Variable, Yield nodes
2. Connected and disconnected wires
3. Observed code changes

**Results:**
- Code generation updates (timestamp changes on each operation)
- **BUG FOUND:** Unconnected nodes still generate code
  - Graph with 0 wires still shows `YIELD` in generated code
  - Expected: Only nodes in execution chain should generate code
  - Actual: All nodes generate code regardless of connections
- **Status: PARTIAL**

---

## Test 5: IC10 Compilation from VS

**Purpose:** Verify generated BASIC compiles to valid IC10

**Steps:**
1. Cleared canvas
2. Created: EntryPoint -> Variable (counter) -> Label (MainLoop) -> Yield -> Goto (MainLoop)
3. Connected all 4 wires
4. Compiled via MCP

**Generated BASIC:**
```basic
# --- Variables ---
VAR counter = 0

# --- Main ---
# --- Program Start ---
MainLoop:
YIELD
GOTO MainLoop
```

**Generated IC10:**
```ic10
move r0 0
MainLoop:
yield
j MainLoop
```

**Results:**
- Compilation successful (4 IC10 lines)
- No errors or warnings
- All comments use `#`
- **Status: PASS**

---

## Summary

| Test | Description                       | Pass/Fail |
|------|-----------------------------------|-----------|
| 1    | Comment character `#` not `'`     | PASS      |
| 2    | Generated Code pane displays code | PARTIAL*  |
| 3    | VS syncs to main editor           | PASS      |
| 4    | Wire changes update code          | PARTIAL   |
| 5    | IC10 compiles successfully        | PASS      |
| 6    | Complex script reproduction       | FAIL**    |

*Code pane requires manual toggle (BASIC->IC10->BASIC) to refresh
**MCP property handling broken - nodes don't receive configured properties

---

## Issues Found for Development Instance

**Bug 1: Unconnected Nodes Generate Code (Test 4)**
- **Issue:** Nodes not in the execution chain still generate code
- **Reproduction:** Add multiple nodes without connecting them, observe generated code includes all nodes
- **Expected:** Only nodes connected to EntryPoint execution chain should generate code
- **Severity:** Low - code still compiles, but may include unintended statements

**Bug 2: Generated Code Pane Does Not Auto-Refresh (Test 2/4)**
- **Issue:** The Generated Code pane in VS window only updates when manually switching between BASIC and IC10 views
- **Reproduction:** Make changes to nodes/wires, observe code pane doesn't update until toggling "Show IC10" off and back on
- **Expected:** Code pane should auto-refresh when graph changes
- **Severity:** Medium - requires manual toggle to see updated code

**Enhancement Request: Node Colors Should Match Theme**
- **Issue:** VS node colors do not respect the syntax color theme selected in the main compiler
- **Expected:** Node colors (variables, constants, flow control, etc.) should use the same color scheme as the main editor's syntax highlighting
- **Benefit:** Consistent visual experience across text and visual editors

**Dead Code to Remove (Assistant Window Removed):**
- `Basic10.Mcp/McpServer.cs`: Lines 207-218, 1063-1064 - `basic10_get_messages` and `basic10_send_response` MCP tools
- `Services/HttpApiServer.cs`: Line 1438 - Comment referencing "Claude Assistant chat"

---

## Test 6: Complex Script Reproduction via VS

**Purpose:** Verify that a complex BASIC program can be built visually

**Test Script:** `SolarTrackerHeavyPanelsV1` (Solar panel sun tracker)

**Original Script Features:**
- 3 named device aliases (panels, sensor, sunLight)
- 3 constants (COLOR_BLACK, COLOR_YELLOW, HOME_VERT)
- 4 variables (solarHoriz, solarVert, sunUp, homeHoriz)
- IF/ELSE conditional logic
- Device property reads (Activate, Horizontal, Vertical)
- Device property writes (Horizontal, Vertical, On, Color)
- Math operations (addition, subtraction)
- Main loop with YIELD and GOTO

**Steps:**
1. Loaded script from scripts folder
2. Opened VS window and cleared canvas
3. Added 27 nodes:
   - 3 NamedDevice nodes
   - 3 Const nodes
   - 4 Variable nodes
   - 1 EntryPoint, 1 Label, 1 If, 1 Yield, 1 Goto
   - 3 ReadProperty, 4 WriteProperty
   - 2 Math (Add, Subtract)
   - 2 Constant, 1 Compare
4. Connected execution flow (EntryPoint → Label → If → Yield → Goto)
5. Connected data flow (Sensor → ReadProperty → Compare → If condition)
6. Checked generated code

**Results:**
- **Execution flow connected successfully** - nodes wire together correctly
- **Data flow connected successfully** - values can be piped between nodes

**CRITICAL BUGS FOUND:**

**Bug 3: Node Properties Not Applied via MCP**
- **Issue:** Properties passed in `add_node` are ignored
- **Evidence:** All NamedDevice nodes show "StructureActiveVent" instead of specified device types
- **Evidence:** All Const nodes show "MY_CONST = 0" instead of specified names/values
- **Severity:** HIGH - Makes MCP-based VS scripting non-functional

**Bug 4: Property Updates Fail**
- **Issue:** `update_property` returns "Property not found" for all properties
- **Evidence:** Attempted to update AliasName, DeviceType, DeviceName on NamedDevice - all failed
- **Evidence:** `get_node` shows no properties in response, only pins
- **Severity:** HIGH - Cannot correct node configurations via MCP

**Generated Code (Broken):**
```basic
# --- Variables ---
VAR solarHoriz = 0
VAR solarVert = 0
VAR sunUp = 0
VAR homeHoriz = -90

# --- Constants ---
CONST MY_CONST = 0   # Should be COLOR_BLACK = 7
CONST MY_CONST = 0   # Should be COLOR_YELLOW = 3
CONST MY_CONST = 0   # Should be HOME_VERT = 0

# --- Devices ---
DEVICE device "StructureActiveVent"  # Should be panels
DEVICE device "StructureActiveVent"  # Should be sensor
DEVICE device "StructureActiveVent"  # Should be sunLight
```

**Conclusion:**
- **Status: FAIL** - Complex scripts CANNOT be built via MCP due to property bugs
- The visual scripting SYSTEM may work fine via manual UI interaction
- The MCP INTEGRATION is broken for nodes requiring property configuration
- Only simple scripts with default-property nodes can be built via MCP

**Recommendation:**
Fix MCP property handling:
1. `add_node` should apply the `properties` parameter
2. `get_node` should return all editable properties
3. `update_property` should work for all exposed properties

---

## Additional Issues (User-Reported)

**Bug 5: NamedDevice Node Has No Configuration UI**
- **Issue:** The "Named Device" node has no way to name or select the device in the visual UI
- **Evidence:** Node shows default "StructureActiveVent" with no way to change it
- **Suggestion:** Add dropdown with all devices listed, OR text box with auto-complete
- **Severity:** HIGH - Makes NamedDevice nodes unusable

**Bug 6: Zoom/Pan Breaks at Grid Edges**
- **Issue:** Lose all ability to drag or zoom when moving too far from center of grid
- **Evidence:** Must click "Reset View" to regain control of the visual programming area
- **Reproduction:** Pan/zoom away from center until controls stop responding
- **Severity:** Medium - Frustrating UX, workaround exists (Reset View)

---

## Complete Bug Summary

| # | Type | Description | Severity |
|---|------|-------------|----------|
| 1 | Bug | Unconnected nodes still generate code | Low |
| 2 | Bug | Code pane only refreshes on BASIC↔IC10 toggle | Medium |
| 3 | Bug | MCP `add_node` ignores properties parameter | HIGH |
| 4 | Bug | MCP `update_property` fails - properties not exposed | HIGH |
| 5 | Bug | NamedDevice node has no configuration UI | HIGH |
| 6 | Bug | Zoom/pan breaks at grid edges | Medium |
| 7 | Enhancement | Node colors should match syntax theme | - |
| 8 | Dead Code | Remove Assistant window MCP tools | - |
