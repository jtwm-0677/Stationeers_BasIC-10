# IC10 MIPS Instruction Reference

This document describes the IC10 assembly instructions that the BASIC compiler generates.

## Table of Contents
1. [Registers](#registers)
2. [Arithmetic Instructions](#arithmetic-instructions)
3. [Math Functions](#math-functions)
4. [Comparison Instructions](#comparison-instructions)
5. [Branching Instructions](#branching-instructions)
6. [Bitwise Instructions](#bitwise-instructions)
7. [Stack Instructions](#stack-instructions)
8. [Device I/O Instructions](#device-io-instructions)
9. [Batch Instructions](#batch-instructions)
10. [Execution and Timing](#execution-and-timing)
11. [Memory Chip Operations](#memory-chip-operations)
12. [Miscellaneous Instructions](#miscellaneous-instructions)
13. [Device Properties](#device-properties)
14. [Common Hash Values](#common-hash-values)
15. [Example IC10 Programs](#example-ic10-programs)
16. [Units Reference](#units-reference)
17. [Common Pitfalls and Debugging](#common-pitfalls-and-debugging)
18. [Programming Patterns](#programming-patterns)

---

## Registers

### General Purpose Registers (r0-r15)

IC10 has 16 general-purpose registers for storing values:
- `r0` through `r15` (only these exist - r16, r17, etc. are **invalid**)
- All hold 64-bit floating-point values
- The compiler uses r0-r13 for variables and r14-r15 as temporary registers
- Registers persist between ticks (values are NOT reset on yield)

### Special Registers

| Register | Name | Description |
|----------|------|-------------|
| `sp` | Stack Pointer | Points to top of stack (0-511) |
| `ra` | Return Address | Stores return line for jal instruction |

### Device Registers

| Register | Description |
|----------|-------------|
| `d0` - `d5` | Device ports (connected devices) |
| `db` | Self-reference to the IC housing device |

**Important:** `db` refers to the IC housing itself (IC10 Housing, Computer, etc.), NOT the circuit board. Use `db.Setting` to display debug values on the housing's display.

### Indirect Register Addressing

IC10 supports indirect register access using double-r notation (`rr0`):

```mips
# rr0 means "use the value in r0 as the register index"
move r0 5           # r0 = 5
move r5 42          # r5 = 42
move r1 rr0         # r1 = value of r[r0] = r5 = 42

# Can also use with device registers
move r0 2           # r0 = 2
l r1 dr0 On         # r1 = d2.On (device index from r0)
```

This is useful for:
- Looping through registers dynamically
- Accessing devices by computed index
- Building lookup tables in registers

---

## Arithmetic Instructions

### Basic Arithmetic

| Instruction | Syntax | Description |
|-------------|--------|-------------|
| `add` | `add r0 r1 r2` | r0 = r1 + r2 |
| `sub` | `sub r0 r1 r2` | r0 = r1 - r2 |
| `mul` | `mul r0 r1 r2` | r0 = r1 * r2 |
| `div` | `div r0 r1 r2` | r0 = r1 / r2 (NaN if r2=0) |
| `mod` | `mod r0 r1 r2` | r0 = r1 % r2 (modulo) |

### Examples

```mips
add r0 r1 r2        # r0 = r1 + r2
add r0 r0 1         # r0 = r0 + 1 (increment)
sub r0 r1 r2        # r0 = r1 - r2
mul r0 r1 2         # r0 = r1 * 2 (double)
div r0 r1 2         # r0 = r1 / 2 (halve)
mod r0 r1 10        # r0 = r1 mod 10
```

---

## Math Functions

### Single-Argument Functions

| Instruction | Description | Range |
|-------------|-------------|-------|
| `abs` | Absolute value | All |
| `sqrt` | Square root | >= 0 |
| `floor` | Round toward -∞ | All |
| `ceil` | Round toward +∞ | All |
| `round` | Round to nearest | All |
| `trunc` | Truncate toward 0 | All |
| `sin` | Sine | Radians |
| `cos` | Cosine | Radians |
| `tan` | Tangent | Radians |
| `asin` | Arc sine | -1 to 1 |
| `acos` | Arc cosine | -1 to 1 |
| `atan` | Arc tangent | All |
| `log` | Natural log | > 0 |
| `exp` | e^x | All |

### Syntax

```mips
abs r0 r1           # r0 = |r1|
sqrt r0 r1          # r0 = √r1
floor r0 r1         # r0 = floor(r1)
ceil r0 r1          # r0 = ceil(r1)
round r0 r1         # r0 = round(r1)
trunc r0 r1         # r0 = trunc(r1)
sin r0 r1           # r0 = sin(r1)
cos r0 r1           # r0 = cos(r1)
tan r0 r1           # r0 = tan(r1)
asin r0 r1          # r0 = asin(r1)
acos r0 r1          # r0 = acos(r1)
atan r0 r1          # r0 = atan(r1)
log r0 r1           # r0 = ln(r1)
exp r0 r1           # r0 = e^r1
```

### Two-Argument Functions

| Instruction | Description |
|-------------|-------------|
| `atan2` | Two-argument arctangent |
| `max` | Maximum of two values |
| `min` | Minimum of two values |

```mips
atan2 r0 r1 r2      # r0 = atan2(r1, r2)
max r0 r1 r2        # r0 = max(r1, r2)
min r0 r1 r2        # r0 = min(r1, r2)
```

### Random Number

```mips
rand r0             # r0 = random value 0 to 1
```

---

## Comparison Instructions

### Set-on-Compare (s** instructions)

These set the destination to 1 if condition is true, 0 otherwise.

| Instruction | Description | Condition |
|-------------|-------------|-----------|
| `seq` | Set if equal | a == b |
| `sne` | Set if not equal | a != b |
| `slt` | Set if less than | a < b |
| `sgt` | Set if greater than | a > b |
| `sle` | Set if less or equal | a <= b |
| `sge` | Set if greater or equal | a >= b |

```mips
seq r0 r1 r2        # r0 = (r1 == r2) ? 1 : 0
sne r0 r1 r2        # r0 = (r1 != r2) ? 1 : 0
slt r0 r1 r2        # r0 = (r1 < r2) ? 1 : 0
sgt r0 r1 r2        # r0 = (r1 > r2) ? 1 : 0
sle r0 r1 r2        # r0 = (r1 <= r2) ? 1 : 0
sge r0 r1 r2        # r0 = (r1 >= r2) ? 1 : 0
```

### Zero-Compare (s**z instructions)

| Instruction | Description | Condition |
|-------------|-------------|-----------|
| `seqz` | Set if equal to zero | a == 0 |
| `snez` | Set if not equal to zero | a != 0 |
| `sgtz` | Set if greater than zero | a > 0 |
| `sltz` | Set if less than zero | a < 0 |
| `sgez` | Set if greater or equal to zero | a >= 0 |
| `slez` | Set if less or equal to zero | a <= 0 |

```mips
seqz r0 r1          # r0 = (r1 == 0) ? 1 : 0
snez r0 r1          # r0 = (r1 != 0) ? 1 : 0
sgtz r0 r1          # r0 = (r1 > 0) ? 1 : 0
sltz r0 r1          # r0 = (r1 < 0) ? 1 : 0
sgez r0 r1          # r0 = (r1 >= 0) ? 1 : 0
slez r0 r1          # r0 = (r1 <= 0) ? 1 : 0
```

### NaN Comparison

| Instruction | Description |
|-------------|-------------|
| `snan` | Set if NaN |
| `snaz` | Set if NaN or zero |

```mips
snan r0 r1          # r0 = isNaN(r1) ? 1 : 0
snaz r0 r1          # r0 = (isNaN(r1) || r1 == 0) ? 1 : 0
```

### Approximate Equality

| Instruction | Description |
|-------------|-------------|
| `sap` | Set if approximately equal |
| `sapz` | Set if approximately zero |

```mips
sap r0 r1 r2 r3     # r0 = (|r1-r2| <= r3 * max(|r1|,|r2|)) ? 1 : 0
sapz r0 r1 r2       # r0 = (|r1| <= r2) ? 1 : 0
```

### Select Instruction

```mips
select r0 r1 r2 r3  # r0 = (r1 != 0) ? r2 : r3
```

---

## Branching Instructions

### Unconditional Jump

| Instruction | Description |
|-------------|-------------|
| `j` | Jump to label |
| `jr` | Jump relative |
| `jal` | Jump and link (for subroutines) |

```mips
j label             # Jump to label
j 10                # Jump to line 10
jr 5                # Jump forward 5 lines
jr -3               # Jump back 3 lines
jal subroutine      # Jump to subroutine, store return in ra
```

### Conditional Branches (b** instructions)

| Instruction | Description | Condition |
|-------------|-------------|-----------|
| `beq` | Branch if equal | a == b |
| `bne` | Branch if not equal | a != b |
| `blt` | Branch if less than | a < b |
| `bgt` | Branch if greater than | a > b |
| `ble` | Branch if less or equal | a <= b |
| `bge` | Branch if greater or equal | a >= b |

```mips
beq r0 r1 label     # if r0 == r1, goto label
bne r0 r1 label     # if r0 != r1, goto label
blt r0 r1 label     # if r0 < r1, goto label
bgt r0 r1 label     # if r0 > r1, goto label
ble r0 r1 label     # if r0 <= r1, goto label
bge r0 r1 label     # if r0 >= r1, goto label
```

### Zero-Compare Branches (b**z instructions)

| Instruction | Description | Condition |
|-------------|-------------|-----------|
| `beqz` | Branch if equal to zero | a == 0 |
| `bnez` | Branch if not equal to zero | a != 0 |
| `bgtz` | Branch if greater than zero | a > 0 |
| `bltz` | Branch if less than zero | a < 0 |
| `bgez` | Branch if greater or equal to zero | a >= 0 |
| `blez` | Branch if less or equal to zero | a <= 0 |

```mips
beqz r0 label       # if r0 == 0, goto label
bnez r0 label       # if r0 != 0, goto label
bgtz r0 label       # if r0 > 0, goto label
bltz r0 label       # if r0 < 0, goto label
bgez r0 label       # if r0 >= 0, goto label
blez r0 label       # if r0 <= 0, goto label
```

### NaN Branches

```mips
bnan r0 label       # if isNaN(r0), goto label
bnaz r0 label       # if isNaN(r0) || r0 == 0, goto label
```

### Approximate Branches

```mips
bap r0 r1 r2 label  # if |r0-r1| <= r2 * max(|r0|,|r1|), goto label
bapz r0 r1 label    # if |r0| <= r1, goto label
```

### Relative Branches

All branch instructions have relative variants (suffix `al`):

```mips
beqal r0 r1 5       # if r0 == r1, jump 5 lines forward
bneal r0 r1 -3      # if r0 != r1, jump 3 lines back
```

---

## Bitwise Instructions

### Logical Operations

| Instruction | Description |
|-------------|-------------|
| `and` | Bitwise AND |
| `or` | Bitwise OR |
| `xor` | Bitwise XOR |
| `nor` | Bitwise NOR |
| `not` | Bitwise NOT |

```mips
and r0 r1 r2        # r0 = r1 & r2
or r0 r1 r2         # r0 = r1 | r2
xor r0 r1 r2        # r0 = r1 ^ r2
nor r0 r1 r2        # r0 = ~(r1 | r2)
not r0 r1           # r0 = ~r1 (actually nor r0 r1 0)
```

### Shift Operations

| Instruction | Description |
|-------------|-------------|
| `sll` | Shift left logical |
| `srl` | Shift right logical |
| `sra` | Shift right arithmetic |

```mips
sll r0 r1 2         # r0 = r1 << 2 (multiply by 4)
srl r0 r1 2         # r0 = r1 >> 2 (unsigned divide by 4)
sra r0 r1 2         # r0 = r1 >> 2 (signed divide by 4)
```

---

## Stack Instructions

| Instruction | Description |
|-------------|-------------|
| `push` | Push value onto stack |
| `pop` | Pop value from stack |
| `peek` | Read top of stack without popping |

```mips
push r0             # Stack[sp++] = r0
pop r0              # r0 = Stack[--sp]
peek r0             # r0 = Stack[sp-1] (no change to sp)
```

### Stack Limits

- Stack size: 512 values
- Stack overflow: Error when push exceeds 512
- Stack underflow: Error when pop from empty stack

---

## Device I/O Instructions

### Load (Read) Instructions

| Instruction | Syntax | Description |
|-------------|--------|-------------|
| `l` | `l r0 d0 Property` | Read device property |
| `ls` | `ls r0 d0 slot Property` | Read slot property |
| `lr` | `lr r0 d0 mode hash` | Read reagent |
| `lbn` | `lbn r0 hash name property mode` | Batch read by name |

```mips
l r0 d0 Temperature             # r0 = d0.Temperature
l r0 d0 On                      # r0 = d0.On
ls r0 d0 0 OccupantHash         # r0 = d0.Slots[0].OccupantHash
lr r0 d0 0 12345                # r0 = d0.Reagent[12345].Contents
```

### Store (Write) Instructions

| Instruction | Syntax | Description |
|-------------|--------|-------------|
| `s` | `s d0 Property r0` | Write device property |
| `ss` | `ss d0 slot Property r0` | Write slot property |
| `sbn` | `sbn hash name property value` | Batch write by name |

```mips
s d0 On 1                       # d0.On = 1
s d0 Setting r0                 # d0.Setting = r0
ss d0 0 Quantity 10             # d0.Slots[0].Quantity = 10
```

---

## Batch Instructions

### Batch Load

| Instruction | Description |
|-------------|-------------|
| `lb` | Load batch (by prefab hash) |
| `lbn` | Load batch by name hash |
| `lbs` | Load batch slot |
| `lbns` | Load batch slot by name |

```mips
lb r0 HASH Property mode        # r0 = batch read
# mode: 0=Average, 1=Sum, 2=Min, 3=Max

lb r0 -539224550 PowerGeneration 1   # Sum of all solar panel power
```

### Batch Store

| Instruction | Description |
|-------------|-------------|
| `sb` | Store batch (by prefab hash) |
| `sbn` | Store batch by name hash |
| `sbs` | Store batch slot |
| `sbns` | Store batch slot by name |

```mips
sb HASH Property r0             # Set property on all matching devices
sb -539224550 On 1              # Turn on all solar panels
```

### Batch Modes

| Mode | Value | Description |
|------|-------|-------------|
| Average | 0 | Average of all values |
| Sum | 1 | Sum of all values |
| Minimum | 2 | Minimum value |
| Maximum | 3 | Maximum value |

---

## Execution and Timing

### Execution Limits

Each IC10 chip has strict execution limits per game tick:

| Mode | Line Limit | Description |
|------|------------|-------------|
| Standard | 128 | Default IC10 behavior |
| Extended | 512 | With Extended Scripting mod |

**What happens when limit is reached:**
- Execution pauses at the current line
- Resumes from that exact line next tick
- Registers and stack are preserved
- Device values may have changed

### Timing Instructions

| Instruction | Description |
|-------------|-------------|
| `yield` | Pause execution until next tick |
| `sleep n` | Pause for n seconds (n can be register or value) |
| `hcf` | Halt execution permanently |

**Critical:** Always include `yield` in infinite loops:

```mips
main:
    # do work...
    yield           # REQUIRED - allows devices to update
    j main
```

Without `yield`, the IC will execute 128 lines then pause mid-loop, but device values won't update properly between reads.

### Tick Rate

- Game runs at variable tick rate (tied to simulation speed)
- Normal speed: approximately 2 ticks per second
- `yield` ensures clean tick boundaries
- `sleep 1` pauses for 1 real second (2 ticks at normal speed)

---

## Memory Chip Operations

IC10 can read/write to external memory devices (Logic Memory, Stack Chips, Queue Chips).

### Direct Memory Access (Pin Aliases)

For devices connected via pins (d0-d5):

| Instruction | Syntax | Description |
|-------------|--------|-------------|
| `get` | `get r0 d0 address` | Read memory at address |
| `put` | `put d0 address r0` | Write r0 to memory at address |

```mips
alias mem d0

# Read address 5 into r0
get r0 mem 5

# Write r1 to address 10
put mem 10 r1
```

### Named Memory Access

For named devices on the network:

| Instruction | Syntax | Description |
|-------------|--------|-------------|
| `getd` | `getd r0 deviceHash nameHash address` | Read from named device |
| `putd` | `putd deviceHash nameHash address r0` | Write to named device |

```mips
define MEM_HASH 1076425094          # StructureLogicMemory
define NAME_HASH 12345678           # "My Memory" (computed)

getd r0 MEM_HASH NAME_HASH 0        # Read address 0
putd MEM_HASH NAME_HASH 0 r1        # Write to address 0
```

### Memory Device Limits

| Device Type | Address Range | Notes |
|-------------|---------------|-------|
| Logic Memory | 0-31 | 32 addresses |
| Stack Utility Chip | 0-511 | 512 stack slots |
| Queue Utility Chip | 0-511 | 512 queue slots |
| Memory Utility Chip | 0-8191 | 8192 addresses |

**Note:** `.Setting` property on Logic Memory is separate from the memory array. Use `.Setting` for single values, `get`/`put` for array access.

---

## Miscellaneous Instructions

### Move

```mips
move r0 r1          # r0 = r1
move r0 42          # r0 = 42
```

### Alias

```mips
alias sensor d0     # sensor refers to d0
alias temp r0       # temp refers to r0
```

### Define

```mips
define MAX_TEMP 373.15    # Constant definition
```

### Labels

```mips
myLabel:            # Define a label (jump target)
```

### Comments

IC10 MIPS uses `#` as the **only** comment character:

```mips
# This is a comment
move r0 1           # Inline comment
# Note: ' and REM are NOT valid in IC10 (those are BASIC-only)
```

### Sleep and Yield

```mips
yield               # Wait one game tick
sleep r0            # Sleep for r0 seconds
sleep 1             # Sleep for 1 second
```

### Halt

```mips
hcf                 # Halt and Catch Fire (stop execution)
```

### Line Number

```mips
l r0 db LineNumber  # r0 = current line number (PC)
```

---

## Device Properties

### Universal Properties

| Property | Read | Write | Description |
|----------|------|-------|-------------|
| On | Yes | Yes | Power state (0 or 1) |
| Open | Yes | Yes | Open state for vents/doors |
| Lock | Yes | Yes | Lock state |
| Mode | Yes | Yes | Operating mode |
| Error | Yes | No | Error state |
| Setting | Yes | Yes | Target setting value |
| Activate | No | Yes | Trigger activation |
| Power | Yes | No | Has power (0 or 1) |
| Idle | Yes | No | Is idle |
| RequiredPower | Yes | No | Required watts |
| PrefabHash | Yes | No | Device type hash |
| NameHash | Yes | No | Device name hash |
| ReferenceId | Yes | No | Unique reference ID |

### Atmospheric Properties

| Property | Description | Units |
|----------|-------------|-------|
| Temperature | Gas temperature | Kelvin |
| TemperatureExternal | External temp | Kelvin |
| TemperatureInternal | Internal temp | Kelvin |
| TemperatureSetting | Target temp | Kelvin |
| Pressure | Gas pressure | kPa |
| PressureExternal | External pressure | kPa |
| PressureInternal | Internal pressure | kPa |
| PressureSetting | Target pressure | kPa |
| TotalMoles | Total gas amount | mol |
| TotalMolesInput | Input moles | mol |
| TotalMolesOutput | Output moles | mol |
| Volume | Container volume | L |

### Gas Ratios (0-1)

| Property | Gas |
|----------|-----|
| RatioOxygen | O2 |
| RatioCarbonDioxide | CO2 |
| RatioNitrogen | N2 |
| RatioNitrousOxide | N2O |
| RatioVolatiles | H2 |
| RatioPollutant | X (pollutant) |
| RatioWater | H2O (steam) |

### Power Properties

| Property | Description |
|----------|-------------|
| Charge | Battery charge (0-1) |
| ChargeRatio | Same as Charge |
| PowerGeneration | Power output (W) |
| PowerPotential | Max possible (W) |
| PowerRequired | Power demand (W) |
| PowerActual | Current draw (W) |

### Solar Panel Properties

| Property | Description |
|----------|-------------|
| Horizontal | Horizontal angle |
| Vertical | Vertical angle |
| SolarAngle | Current sun angle |
| SolarIrradiance | Light intensity |

### Storage/Inventory

| Property | Description |
|----------|-------------|
| Quantity | Item count |
| MaxQuantity | Maximum capacity |
| Ratio | Fill ratio (0-1) |
| ImportCount | Items imported |
| ExportCount | Items exported |

### Manufacturing

| Property | Description |
|----------|-------------|
| RecipeHash | Selected recipe hash |
| Completions | Completed items |
| Efficiency | Current efficiency |
| ClearMemory | Clear stored recipe |

### Hydroponics

| Property | Description |
|----------|-------------|
| Growth | Growth progress (0-1) |
| Health | Plant health (0-1) |
| Mature | Is mature (0 or 1) |
| Seeding | Is seeding (0 or 1) |

### Logic/Display

| Property | Description |
|----------|-------------|
| LineNumber | Current IC line |
| Channel0-7 | Communication channels |
| Color | Display color |
| ColorRed | Red component (0-255) |
| ColorGreen | Green component (0-255) |
| ColorBlue | Blue component (0-255) |

### Slot Properties (ls/ss)

| Property | Description |
|----------|-------------|
| Occupied | Slot has item |
| OccupantHash | Item type hash |
| Quantity | Stack size |
| MaxQuantity | Max stack |
| Damage | Item damage (0-1) |
| Charge | Item charge (0-1) |
| Class | Item class |
| SortingClass | For sorters |
| PrefabHash | Item prefab hash |

---

## Common Hash Values

Stationeers uses **CRC-32** for all hash calculations. The compiler and Device Hash Database both use this algorithm to generate compatible hash values.

### Device Types

| Device | Hash |
|--------|------|
| Solar Panel | -539224550 |
| Gas Sensor | 1255689925 |
| Wall Heater | -1253014094 |
| Wall Cooler | 1621028804 |
| Volume Pump | -321403609 |
| Battery (Small) | -1388288459 |
| Battery (Large) | 1729485927 |
| Active Vent | -1019769882 |
| Logic Memory | 1076425094 |

### Logic Types (for lb/sb property)

| Property | Hash |
|----------|------|
| On | 1112093520 |
| Temperature | 466104759 |
| Pressure | -838714905 |
| Charge | 810097637 |
| Setting | -1356827741 |
| Open | 1521910572 |

Use the Device Hash Database (Tools menu) for complete listings.

---

## Example IC10 Programs

### Simple Temperature Control

```mips
alias sensor d0
alias heater d1
define TARGET 293.15

main:
l r0 sensor Temperature
sub r0 r0 TARGET
bgtz r0 turnOff
s heater On 1
j end
turnOff:
s heater On 0
end:
yield
j main
```

### Solar Panel Tracker

```mips
alias panel d0

main:
l r0 panel SolarAngle
s panel Horizontal r0
move r1 60
s panel Vertical r1
yield
j main
```

### Batch Power Monitor

```mips
define SOLAR -539224550
define BATTERY -1388288459

main:
lb r0 SOLAR PowerGeneration 1
lb r1 BATTERY Charge 2
# r0 = total solar power
# r1 = minimum battery charge
yield
j main
```

---

## Units Reference

**Critical:** Stationeers uses specific units. Using wrong units is a common source of bugs.

### Temperature

| Unit | Description | Conversion |
|------|-------------|------------|
| Kelvin (K) | All temperature properties | Native unit |
| Celsius (°C) | Human-readable | K = °C + 273.15 |

```mips
# 20°C in Kelvin
define ROOM_TEMP 293.15
# Freezing point (0°C)
define FREEZING 273.15
# Boiling point (100°C)
define BOILING 373.15
```

### Pressure

| Unit | Description | Notes |
|------|-------------|-------|
| kPa | Kilopascals | **All pressure properties use kPa** |

**Common values:**
- Sea level atmosphere: ~101 kPa
- Vacuum: 0 kPa
- Safe suit pressure: ~20-101 kPa

```mips
# CORRECT - kPa values
define ATMOSPHERIC 101
define VACUUM_THRESHOLD 1

# WRONG - Pa values (1000x too large!)
# define ATMOSPHERIC 101000    # This is WRONG
```

### Ratios and Percentages

| Property | Range | Description |
|----------|-------|-------------|
| RatioOxygen | 0.0 - 1.0 | Oxygen ratio (not percentage) |
| Charge | 0.0 - 1.0 | Battery charge ratio |
| Efficiency | 0.0 - 1.0 | Device efficiency ratio |

```mips
# Check if oxygen is above 18%
define MIN_O2 0.18

l r0 d0 RatioOxygen
blt r0 MIN_O2 lowOxygen
```

### Power

| Unit | Description |
|------|-------------|
| Watts (W) | Power generation/consumption |

---

## Common Pitfalls and Debugging

### 1. Pressure Unit Confusion

```mips
# WRONG - comparing kPa to Pa
l r0 d0 Pressure
blt r0 101000 pressurize    # Sensor reads ~101, this checks 101000!

# CORRECT
l r0 d0 Pressure
blt r0 101 pressurize       # Both in kPa
```

### 2. First-Tick Device Reads

Devices may return 0 or NaN on the first tick after script load:

```mips
# Defensive coding
l r0 d0 Pressure
snan r1 r0                  # Check if NaN
bnez r1 skipAction          # Skip if NaN
beqz r0 skipAction          # Skip if zero (might be uninitialized)
# Safe to use r0 now
```

### 3. Missing Yield in Loops

```mips
# WRONG - infinite loop without yield
main:
    l r0 d0 Temperature
    j main

# CORRECT - yield allows device updates
main:
    l r0 d0 Temperature
    yield
    j main
```

### 4. Named Device Not Found (lbn/sbn)

If a named device doesn't exist on the network, `lbn` returns 0 or NaN. Always handle this:

```mips
lbn r0 DEVICE_HASH NAME_HASH Property 0
snan r1 r0
bnez r1 deviceMissing       # Handle missing device
```

### 5. Edge Detection for Buttons

Button `.Activate` is 1 for only ONE tick when pressed:

```mips
# Store last state to detect edges
l r0 d0 Activate
beqz r0 notPressed
# Button was just pressed this tick
# ... handle press ...
notPressed:
```

### 6. State Machine Initialization

Always initialize state on script load:

```mips
# Initialize at start of program
move r10 0                  # state = IDLE
s d0 Setting 0             # Clear any stored values

main:
    # ... state machine logic ...
    yield
    j main
```

### 7. Debugging with db.Setting

Use the IC housing display for debugging:

```mips
# Show current state
s db Setting r10

# Show pressure reading
l r0 d0 Pressure
s db Setting r0

# Show tick counter (proves script is running)
add r15 r15 1
s db Setting r15
```

---

## Programming Patterns

### State Machine Pattern

```mips
define STATE_IDLE 0
define STATE_WORKING 1
define STATE_DONE 2

move r10 STATE_IDLE         # Current state

main:
    beq r10 STATE_IDLE stateIdle
    beq r10 STATE_WORKING stateWorking
    beq r10 STATE_DONE stateDone
    j main

stateIdle:
    # Check for trigger condition
    l r0 d0 Activate
    beqz r0 endIdle
    move r10 STATE_WORKING
    endIdle:
    j endMain

stateWorking:
    # Do work, check completion
    l r0 d0 Progress
    blt r0 100 endWorking
    move r10 STATE_DONE
    endWorking:
    j endMain

stateDone:
    # Reset or handle completion
    move r10 STATE_IDLE
    j endMain

endMain:
    yield
    j main
```

### Hysteresis Pattern (Prevent Oscillation)

```mips
define TARGET 293.15
define TOLERANCE 2

move r11 0                  # heaterOn state

main:
    l r0 d0 Temperature

    # Only turn on if BELOW target - tolerance
    sub r1 TARGET TOLERANCE
    bgt r0 r1 checkOff
    move r11 1
    j applyState

    checkOff:
    # Only turn off if ABOVE target + tolerance
    add r1 TARGET TOLERANCE
    blt r0 r1 applyState
    move r11 0

    applyState:
    s d1 On r11
    yield
    j main
```

### Multi-IC Communication via Memory

```mips
# IC1 - Writer
alias sharedMem d0
s sharedMem Setting r0      # Write value to .Setting

# IC2 - Reader
alias sharedMem d0
l r0 sharedMem Setting      # Read value from .Setting
```
