"""
Basic-10 Compiler Documentation PDF Generator
Generates professional documentation for the Basic-10 BASIC to IC10 compiler
"""

from fpdf import FPDF
import os

class Basic10Manual(FPDF):
    def __init__(self):
        super().__init__()
        self.set_auto_page_break(auto=True, margin=20)

    def header(self):
        if self.page_no() > 1:
            self.set_font('Helvetica', 'I', 9)
            self.set_text_color(128, 128, 128)
            self.cell(95, 10, 'Basic-10 Compiler Documentation', align='L')
            self.cell(95, 10, f'Page {self.page_no()}', align='R')
            self.ln(10)
            self.set_x(10)  # Reset to left margin

    def footer(self):
        self.set_y(-15)
        self.set_font('Helvetica', 'I', 8)
        self.set_text_color(128, 128, 128)
        self.cell(0, 10, 'Basic-10 v1.9.1 - BASIC to IC10 Compiler for Stationeers', align='C')

    def chapter_title(self, title):
        self.set_font('Helvetica', 'B', 18)
        self.set_text_color(0, 100, 180)
        self.cell(0, 12, title, ln=True)
        self.ln(4)

    def section_title(self, title):
        self.set_font('Helvetica', 'B', 14)
        self.set_text_color(50, 50, 50)
        self.cell(0, 10, title, ln=True)
        self.ln(2)

    def subsection_title(self, title):
        self.set_font('Helvetica', 'B', 11)
        self.set_text_color(80, 80, 80)
        self.cell(0, 8, title, ln=True)
        self.ln(1)

    def body_text(self, text):
        self.set_font('Helvetica', '', 10)
        self.set_text_color(0, 0, 0)
        self.set_x(10)  # Reset to left margin
        self.multi_cell(0, 5, text)
        self.ln(2)

    def code_block(self, code):
        self.set_font('Courier', '', 9)
        self.set_fill_color(240, 240, 240)
        self.set_text_color(0, 0, 0)
        lines = code.strip().split('\n')
        for line in lines:
            self.set_x(10)  # Reset to left margin
            self.cell(0, 5, '  ' + line, ln=True, fill=True)
        self.ln(3)

    def bullet_point(self, text):
        self.set_font('Helvetica', '', 10)
        self.set_text_color(0, 0, 0)
        self.set_x(10)  # Reset to left margin
        self.multi_cell(0, 5, '  - ' + text)

    def table_header(self, cols, widths):
        self.set_font('Helvetica', 'B', 9)
        self.set_fill_color(0, 100, 180)
        self.set_text_color(255, 255, 255)
        for i, col in enumerate(cols):
            self.cell(widths[i], 7, col, border=1, fill=True, align='C')
        self.ln()

    def table_row(self, cols, widths, fill=False):
        self.set_font('Courier', '', 8)
        self.set_text_color(0, 0, 0)
        if fill:
            self.set_fill_color(248, 248, 248)
        else:
            self.set_fill_color(255, 255, 255)
        for i, col in enumerate(cols):
            self.cell(widths[i], 6, str(col), border=1, fill=True)
        self.ln()

def create_documentation():
    pdf = Basic10Manual()

    # ===== TITLE PAGE =====
    pdf.add_page()
    pdf.set_font('Helvetica', 'B', 36)
    pdf.set_text_color(0, 100, 180)
    pdf.ln(60)
    pdf.cell(0, 20, 'Basic-10', align='C', ln=True)

    pdf.set_font('Helvetica', '', 18)
    pdf.set_text_color(80, 80, 80)
    pdf.cell(0, 10, 'BASIC to IC10 MIPS Compiler', align='C', ln=True)
    pdf.cell(0, 10, 'for Stationeers', align='C', ln=True)

    pdf.ln(20)
    pdf.set_font('Helvetica', 'B', 14)
    pdf.set_text_color(0, 100, 180)
    pdf.cell(0, 10, 'User Manual & Language Reference', align='C', ln=True)

    pdf.ln(40)
    pdf.set_font('Helvetica', '', 12)
    pdf.set_text_color(100, 100, 100)
    pdf.cell(0, 8, 'Version 1.9.1', align='C', ln=True)
    pdf.cell(0, 8, 'December 2025', align='C', ln=True)

    # ===== TABLE OF CONTENTS =====
    pdf.add_page()
    pdf.chapter_title('Table of Contents')
    pdf.set_font('Helvetica', '', 11)
    pdf.set_text_color(0, 0, 0)

    toc = [
        ('1. Introduction', 3),
        ('2. Getting Started', 4),
        ('3. Language Reference', 6),
        ('   3.1 Variables & Constants', 6),
        ('   3.2 Operators', 7),
        ('   3.3 Control Flow', 9),
        ('   3.4 Loops', 10),
        ('   3.5 Subroutines', 11),
        ('4. Device Operations', 12),
        ('5. Built-in Functions', 14),
        ('6. IC10 MIPS Reference', 16),
        ('7. Example Programs', 19),
        ('8. Tips & Best Practices', 22),
        ('Appendix A: Device Properties', 24),
        ('Appendix B: Color Constants', 25),
    ]

    for item, page in toc:
        pdf.cell(0, 7, f'{item}', ln=True)

    # ===== CHAPTER 1: INTRODUCTION =====
    pdf.add_page()
    pdf.chapter_title('1. Introduction')

    pdf.section_title('What is Basic-10?')
    pdf.body_text(
        'Basic-10 is a BASIC to IC10 compiler designed for the game Stationeers. '
        'It allows you to write programs in a beginner-friendly BASIC dialect, '
        'which are then compiled to IC10 MIPS assembly code that runs on the '
        "game's programmable Integrated Circuit (IC) chips."
    )

    pdf.section_title('Why Use Basic-10?')
    pdf.bullet_point('Write readable, maintainable code instead of low-level assembly')
    pdf.bullet_point('Automatic register allocation - no manual register management')
    pdf.bullet_point('Built-in functions for math, timing, and device operations')
    pdf.bullet_point('Real-time syntax checking and error highlighting')
    pdf.bullet_point('Integrated simulator for testing without the game')
    pdf.bullet_point('One-click deployment to Stationeers scripts folder')

    pdf.section_title('IC10 Overview')
    pdf.body_text(
        'IC10 is the assembly language used by Integrated Circuits in Stationeers. '
        'Each IC chip has:'
    )
    pdf.bullet_point('16 general-purpose registers (r0-r15)')
    pdf.bullet_point('6 device connection pins (d0-d5)')
    pdf.bullet_point('A 512-value stack for temporary storage')
    pdf.bullet_point('A maximum of 128 lines of code')
    pdf.body_text(
        'Basic-10 handles all the complexity of register allocation and generates '
        'optimized IC10 code that fits within these constraints.'
    )

    # ===== CHAPTER 2: GETTING STARTED =====
    pdf.add_page()
    pdf.chapter_title('2. Getting Started')

    pdf.section_title('Your First Program')
    pdf.body_text('Here is a simple program that blinks a light on and off:')
    pdf.code_block('''# My first IC10 program - Blink a light
ALIAS light d0    # Connect a light to pin d0

main:
    light.On = 1  # Turn light on
    SLEEP 1       # Wait 1 second
    light.On = 0  # Turn light off
    SLEEP 1       # Wait 1 second
    GOTO main     # Repeat forever
END''')

    pdf.section_title('Understanding the Code')
    pdf.bullet_point('Lines starting with # are comments (ignored by compiler)')
    pdf.bullet_point('ALIAS creates a friendly name for device pins (d0-d5)')
    pdf.bullet_point('main: is a label - a named location in your code')
    pdf.bullet_point('SLEEP pauses execution for the specified seconds')
    pdf.bullet_point('GOTO jumps to a label')
    pdf.bullet_point('END marks the end of your program')

    pdf.section_title('The Main Loop Pattern')
    pdf.body_text(
        'Almost every IC10 program uses a main loop that runs continuously. '
        'This is essential because IC chips need to constantly monitor sensors '
        'and update device states.'
    )
    pdf.code_block('''main:
    # Read sensors and make decisions
    VAR temp = sensor.Temperature
    IF temp > 300 THEN
        heater.On = 0
    ENDIF

    YIELD         # IMPORTANT: Let game process
    GOTO main     # Loop back
END''')

    pdf.body_text(
        'IMPORTANT: Always include YIELD or SLEEP in your loops! Without it, '
        'your program will freeze the game due to infinite loop detection.'
    )

    pdf.section_title('Connecting Devices')
    pdf.body_text(
        'IC chips have 6 device pins (d0-d5). Connect devices in-game using '
        'cables, then reference them in your code with ALIAS:'
    )
    pdf.code_block('''ALIAS sensor d0      # Gas sensor on pin 0
ALIAS heater d1      # Wall heater on pin 1
ALIAS display d2     # LED display on pin 2

# Now use the friendly names
VAR temp = sensor.Temperature
heater.On = 1
display.Setting = 42''')

    # ===== CHAPTER 3: LANGUAGE REFERENCE =====
    pdf.add_page()
    pdf.chapter_title('3. Language Reference')

    pdf.section_title('3.1 Variables & Constants')

    pdf.subsection_title('Variable Declaration')
    pdf.code_block('''VAR temperature = 0     # Declare with initial value
VAR count               # Declare (defaults to 0)
LET x = 5               # Alternative syntax
x = 42                  # Reassign value''')

    pdf.subsection_title('Constants')
    pdf.code_block('''CONST MAX_TEMP = 373.15    # Named constant (cannot change)
DEFINE TARGET_PRESSURE 101  # IC10-style define (no = sign)''')

    pdf.subsection_title('Arrays')
    pdf.code_block('''DIM values(10)         # Declare array of 10 elements
values(0) = 100        # Set first element
VAR x = values(0)      # Read element''')

    pdf.add_page()
    pdf.section_title('3.2 Operators')

    pdf.subsection_title('Arithmetic Operators')
    widths = [30, 50, 60, 50]
    pdf.table_header(['Operator', 'Description', 'Example', 'IC10'], widths)
    ops = [
        ('a + b', 'Addition', 'x = 5 + 3', 'add'),
        ('a - b', 'Subtraction', 'x = 10 - 4', 'sub'),
        ('a * b', 'Multiplication', 'x = 3 * 4', 'mul'),
        ('a / b', 'Division', 'x = 10 / 2', 'div'),
        ('a MOD b', 'Modulo', 'x = 10 MOD 3', 'mod'),
        ('a ^ b', 'Power', 'x = 2 ^ 3', 'exp+log'),
        ('-a', 'Negation', 'x = -value', 'sub'),
    ]
    for i, row in enumerate(ops):
        pdf.table_row(row, widths, i % 2 == 0)
    pdf.ln(3)

    pdf.subsection_title('Compound Assignment Operators (v1.9.0)')
    widths = [30, 50, 60, 50]
    pdf.table_header(['Operator', 'Equivalent', 'Description', 'IC10'], widths)
    ops = [
        ('x += n', 'x = x + n', 'Add and assign', 'add'),
        ('x -= n', 'x = x - n', 'Subtract and assign', 'sub'),
        ('x *= n', 'x = x * n', 'Multiply and assign', 'mul'),
        ('x /= n', 'x = x / n', 'Divide and assign', 'div'),
    ]
    for i, row in enumerate(ops):
        pdf.table_row(row, widths, i % 2 == 0)
    pdf.ln(3)

    pdf.subsection_title('Increment/Decrement Operators (v1.9.0)')
    widths = [30, 80, 80]
    pdf.table_header(['Operator', 'Description', 'Example'], widths)
    ops = [
        ('++x', 'Prefix increment (returns new)', 'y = ++x  # x=11, y=11'),
        ('x++', 'Postfix increment (returns old)', 'y = x++  # x=11, y=10'),
        ('--x', 'Prefix decrement (returns new)', 'y = --x  # x=9, y=9'),
        ('x--', 'Postfix decrement (returns old)', 'y = x--  # x=9, y=10'),
    ]
    for i, row in enumerate(ops):
        pdf.table_row(row, widths, i % 2 == 0)
    pdf.ln(3)

    pdf.subsection_title('Comparison Operators')
    widths = [40, 70, 80]
    pdf.table_header(['Operator', 'Description', 'Example'], widths)
    ops = [
        ('= or ==', 'Equal to', 'IF x = 5 THEN'),
        ('<> or !=', 'Not equal to', 'IF x <> 0 THEN'),
        ('<', 'Less than', 'IF temp < 300 THEN'),
        ('>', 'Greater than', 'IF pressure > 100 THEN'),
        ('<=', 'Less than or equal', 'IF charge <= 0.2 THEN'),
        ('>=', 'Greater than or equal', 'IF ratio >= 0.21 THEN'),
    ]
    for i, row in enumerate(ops):
        pdf.table_row(row, widths, i % 2 == 0)
    pdf.ln(3)

    pdf.subsection_title('Logical Operators')
    widths = [50, 70, 70]
    pdf.table_header(['Operator', 'Description', 'Example'], widths)
    ops = [
        ('a AND b', 'Logical AND', 'IF a > 0 AND b > 0'),
        ('a OR b', 'Logical OR', 'IF error OR warning'),
        ('NOT a', 'Logical NOT', 'IF NOT active THEN'),
    ]
    for i, row in enumerate(ops):
        pdf.table_row(row, widths, i % 2 == 0)
    pdf.ln(3)

    pdf.add_page()
    pdf.subsection_title('Bitwise Operators')
    widths = [50, 70, 70]
    pdf.table_header(['Operator', 'Description', 'IC10'], widths)
    ops = [
        ('a & b or BAND(a,b)', 'Bitwise AND', 'and'),
        ('a | b or BOR(a,b)', 'Bitwise OR', 'or'),
        ('a ^ b or BXOR(a,b)', 'Bitwise XOR', 'xor'),
        ('~a or BNOT(a)', 'Bitwise NOT', 'nor'),
        ('a << n or SHL(a,n)', 'Shift left', 'sll'),
        ('a >> n or SHR(a,n)', 'Shift right', 'srl'),
    ]
    for i, row in enumerate(ops):
        pdf.table_row(row, widths, i % 2 == 0)
    pdf.ln(3)

    pdf.body_text('Bit shift example:')
    pdf.code_block('''VAR a = 1
VAR b = a << 4    # b = 16 (shift left 4 bits)
VAR c = 16 >> 2   # c = 4 (shift right 2 bits)
VAR d = 5 ^ 3     # d = 6 (XOR: 101 ^ 011 = 110)''')

    pdf.section_title('3.3 Control Flow')

    pdf.subsection_title('IF...THEN...ELSE...ENDIF')
    pdf.code_block('''IF temperature > 300 THEN
    heater.On = 0
ELSEIF temperature < 290 THEN
    heater.On = 1
ELSE
    # Temperature is acceptable
ENDIF''')

    pdf.subsection_title('SELECT CASE')
    pdf.code_block('''SELECT CASE mode
    CASE 0
        # Handle mode 0
    CASE 1
        # Handle mode 1
    DEFAULT
        # Default handling
END SELECT''')

    pdf.subsection_title('Labels and GOTO')
    pdf.code_block('''main:
    # Main program code
    YIELD
    GOTO main      # Jump back to main

errorHandler:
    # Handle errors
    GOTO main''')

    pdf.add_page()
    pdf.section_title('3.4 Loops')

    pdf.subsection_title('WHILE...WEND')
    pdf.code_block('''WHILE temperature > 300
    heater.On = 0
    YIELD
WEND''')

    pdf.subsection_title('FOR...NEXT')
    pdf.code_block('''FOR i = 1 TO 10
    display.Setting = i
    YIELD
NEXT i

FOR j = 10 TO 0 STEP -1
    # Countdown
NEXT j''')

    pdf.subsection_title('DO...LOOP')
    pdf.code_block('''DO
    pump.On = 1
    YIELD
LOOP UNTIL pressure > 100''')

    pdf.subsection_title('Loop Control: BREAK and CONTINUE')
    pdf.code_block('''# BREAK - exit loop immediately
WHILE 1
    IF done THEN BREAK
    YIELD
WEND

# CONTINUE - skip to next iteration
FOR i = 1 TO 10
    IF i MOD 2 = 0 THEN CONTINUE
    # Only processes odd numbers
NEXT i''')

    pdf.section_title('3.5 Subroutines')

    pdf.subsection_title('GOSUB and RETURN')
    pdf.code_block('''main:
    GOSUB ReadSensors
    GOSUB UpdateOutputs
    YIELD
    GOTO main

ReadSensors:
    temp = sensor.Temperature
    pressure = sensor.Pressure
    RETURN

UpdateOutputs:
    display.Setting = temp
    RETURN''')

    pdf.subsection_title('SUB and FUNCTION')
    pdf.code_block('''SUB UpdateDisplay
    display.Setting = temp
END SUB

FUNCTION Clamp(val, minVal, maxVal)
    IF val < minVal THEN RETURN minVal
    IF val > maxVal THEN RETURN maxVal
    RETURN val
END FUNCTION

# Usage
CALL UpdateDisplay
VAR safe = Clamp(input, 0, 100)''')

    # ===== CHAPTER 4: DEVICE OPERATIONS =====
    pdf.add_page()
    pdf.chapter_title('4. Device Operations')

    pdf.section_title('Device Pins')
    pdf.body_text(
        'Each IC chip has 6 device pins (d0-d5) plus a self-reference (db). '
        'Use ALIAS to give devices friendly names:'
    )
    pdf.code_block('''ALIAS sensor d0      # Device on pin 0
ALIAS pump d1        # Device on pin 1
ALIAS chip THIS      # The IC chip itself (db)''')

    pdf.section_title('Reading Device Properties')
    pdf.code_block('''VAR temp = sensor.Temperature
VAR pressure = sensor.Pressure
VAR isOn = heater.On
VAR charge = battery.Charge''')

    pdf.section_title('Writing Device Properties')
    pdf.code_block('''heater.On = 1           # Turn on
pump.Setting = 100      # Set target
display.Setting = temp  # Show value
door.Open = 0           # Close door''')

    pdf.section_title('Slot Operations')
    pdf.body_text('Many devices have slots (inventories) you can access:')
    pdf.code_block('''VAR hash = device.Slot(0).OccupantHash
VAR qty = device.Slot(0).Quantity
VAR occupied = device.Slot(0).Occupied''')

    pdf.section_title('Named Device References')
    pdf.body_text(
        'Bypass the 6-pin limit by referencing devices by their prefab name. '
        'This uses batch operations to find devices on the network:'
    )
    pdf.code_block('''DEVICE sensor "StructureGasSensor"
DEVICE furnace "StructureFurnace"

# Use like regular aliases
VAR temp = sensor.Temperature
furnace.On = 1''')

    pdf.section_title('Batch Operations')
    pdf.body_text('Read from or write to ALL devices of a type at once:')
    pdf.code_block('''DEFINE SENSOR_HASH -1234567890

# Batch read modes: 0=Average, 1=Sum, 2=Min, 3=Max
VAR avgTemp = BATCHREAD(SENSOR_HASH, Temperature, 0)
VAR totalPower = BATCHREAD(BATTERY_HASH, PowerGeneration, 1)

# Write to all devices
BATCHWRITE(LIGHT_HASH, On, 1)  # Turn on all lights''')

    # ===== CHAPTER 5: BUILT-IN FUNCTIONS =====
    pdf.add_page()
    pdf.chapter_title('5. Built-in Functions')

    pdf.section_title('Math Functions')
    widths = [40, 70, 80]
    pdf.table_header(['Function', 'Description', 'Example'], widths)
    funcs = [
        ('ABS(x)', 'Absolute value', 'ABS(-5) = 5'),
        ('SQRT(x)', 'Square root', 'SQRT(16) = 4'),
        ('MIN(a,b)', 'Minimum value', 'MIN(5, 3) = 3'),
        ('MAX(a,b)', 'Maximum value', 'MAX(5, 3) = 5'),
        ('CEIL(x)', 'Round up', 'CEIL(3.2) = 4'),
        ('FLOOR(x)', 'Round down', 'FLOOR(3.8) = 3'),
        ('ROUND(x)', 'Round nearest', 'ROUND(3.5) = 4'),
        ('TRUNC(x)', 'Truncate', 'TRUNC(3.9) = 3'),
        ('SGN(x)', 'Sign (-1,0,1)', 'SGN(-5) = -1'),
        ('RND()', 'Random 0-1', 'RND() = 0.xxx'),
    ]
    for i, row in enumerate(funcs):
        pdf.table_row(row, widths, i % 2 == 0)
    pdf.ln(3)

    pdf.section_title('Trigonometry (angles in radians)')
    widths = [40, 70, 80]
    pdf.table_header(['Function', 'Description', 'Example'], widths)
    funcs = [
        ('SIN(x)', 'Sine', 'SIN(0) = 0'),
        ('COS(x)', 'Cosine', 'COS(0) = 1'),
        ('TAN(x)', 'Tangent', 'TAN(0) = 0'),
        ('ASIN(x)', 'Arc sine', 'ASIN(1) = 1.57'),
        ('ACOS(x)', 'Arc cosine', 'ACOS(0) = 1.57'),
        ('ATAN(x)', 'Arc tangent', 'ATAN(1) = 0.785'),
        ('ATAN2(y,x)', '2-arg arctangent', 'ATAN2(1, 1) = 0.785'),
    ]
    for i, row in enumerate(funcs):
        pdf.table_row(row, widths, i % 2 == 0)
    pdf.ln(3)

    pdf.section_title('Exponential & Logarithmic')
    widths = [40, 70, 80]
    pdf.table_header(['Function', 'Description', 'Example'], widths)
    funcs = [
        ('EXP(x)', 'e raised to x', 'EXP(1) = 2.718'),
        ('LOG(x)', 'Natural logarithm', 'LOG(2.718) = 1'),
    ]
    for i, row in enumerate(funcs):
        pdf.table_row(row, widths, i % 2 == 0)
    pdf.ln(3)

    pdf.section_title('Control Functions')
    widths = [50, 60, 80]
    pdf.table_header(['Function', 'Description', 'Example'], widths)
    funcs = [
        ('YIELD', 'Pause 1 game tick', 'YIELD'),
        ('SLEEP n', 'Pause n seconds', 'SLEEP 0.5'),
        ('WAIT(n)', 'Same as SLEEP', 'WAIT(1)'),
        ('END', 'Stop execution', 'IF error THEN END'),
    ]
    for i, row in enumerate(funcs):
        pdf.table_row(row, widths, i % 2 == 0)
    pdf.ln(3)

    pdf.section_title('Stack Operations')
    pdf.code_block('''PUSH value      # Push to stack
POP variable    # Pop from stack
PEEK variable   # Read top without removing''')

    # ===== CHAPTER 6: IC10 MIPS REFERENCE =====
    pdf.add_page()
    pdf.chapter_title('6. IC10 MIPS Reference')

    pdf.body_text(
        'This reference shows the IC10 assembly instructions that your BASIC code '
        'compiles to. Understanding these helps with debugging and optimization.'
    )

    pdf.section_title('Registers')
    pdf.bullet_point('r0-r15: General purpose registers (16 total)')
    pdf.bullet_point('sp: Stack pointer')
    pdf.bullet_point('ra: Return address (for subroutines)')
    pdf.bullet_point('d0-d5: Device references')
    pdf.bullet_point('db: IC housing device (self)')

    pdf.section_title('Math Operations')
    widths = [50, 60, 80]
    pdf.table_header(['Instruction', 'Meaning', 'Description'], widths)
    ops = [
        ('add r0 r1 r2', 'r0 = r1 + r2', 'Addition'),
        ('sub r0 r1 r2', 'r0 = r1 - r2', 'Subtraction'),
        ('mul r0 r1 r2', 'r0 = r1 * r2', 'Multiplication'),
        ('div r0 r1 r2', 'r0 = r1 / r2', 'Division'),
        ('mod r0 r1 r2', 'r0 = r1 % r2', 'Modulo'),
        ('sqrt r0 r1', 'r0 = sqrt(r1)', 'Square root'),
        ('abs r0 r1', 'r0 = |r1|', 'Absolute value'),
        ('round r0 r1', 'r0 = round(r1)', 'Round'),
        ('floor r0 r1', 'r0 = floor(r1)', 'Round down'),
        ('ceil r0 r1', 'r0 = ceil(r1)', 'Round up'),
        ('min r0 r1 r2', 'r0 = min(r1,r2)', 'Minimum'),
        ('max r0 r1 r2', 'r0 = max(r1,r2)', 'Maximum'),
    ]
    for i, row in enumerate(ops):
        pdf.table_row(row, widths, i % 2 == 0)
    pdf.ln(3)

    pdf.section_title('Logic & Bitwise')
    widths = [50, 60, 80]
    pdf.table_header(['Instruction', 'Meaning', 'Description'], widths)
    ops = [
        ('and r0 r1 r2', 'r0 = r1 & r2', 'Bitwise AND'),
        ('or r0 r1 r2', 'r0 = r1 | r2', 'Bitwise OR'),
        ('xor r0 r1 r2', 'r0 = r1 ^ r2', 'Bitwise XOR'),
        ('nor r0 r1 r2', 'r0 = ~(r1|r2)', 'NOR'),
        ('sll r0 r1 r2', 'r0 = r1 << r2', 'Shift left'),
        ('srl r0 r1 r2', 'r0 = r1 >> r2', 'Shift right'),
        ('sra r0 r1 r2', 'r0 = r1 >>> r2', 'Arithmetic shift'),
    ]
    for i, row in enumerate(ops):
        pdf.table_row(row, widths, i % 2 == 0)
    pdf.ln(3)

    pdf.add_page()
    pdf.section_title('Comparison (Set Instructions)')
    widths = [50, 60, 80]
    pdf.table_header(['Instruction', 'Meaning', 'Description'], widths)
    ops = [
        ('slt r0 r1 r2', 'r0 = (r1 < r2)', 'Set if less than'),
        ('sgt r0 r1 r2', 'r0 = (r1 > r2)', 'Set if greater'),
        ('sle r0 r1 r2', 'r0 = (r1 <= r2)', 'Set if less/equal'),
        ('sge r0 r1 r2', 'r0 = (r1 >= r2)', 'Set if greater/equal'),
        ('seq r0 r1 r2', 'r0 = (r1 == r2)', 'Set if equal'),
        ('sne r0 r1 r2', 'r0 = (r1 != r2)', 'Set if not equal'),
        ('seqz r0 r1', 'r0 = (r1 == 0)', 'Set if zero'),
        ('snez r0 r1', 'r0 = (r1 != 0)', 'Set if not zero'),
    ]
    for i, row in enumerate(ops):
        pdf.table_row(row, widths, i % 2 == 0)
    pdf.ln(3)

    pdf.section_title('Branching & Jumps')
    widths = [55, 55, 80]
    pdf.table_header(['Instruction', 'Meaning', 'Description'], widths)
    ops = [
        ('j label', 'goto label', 'Unconditional jump'),
        ('jal label', 'call label', 'Jump and link'),
        ('jr r0', 'goto r0', 'Jump to register'),
        ('beq r0 r1 lbl', 'if r0==r1 goto', 'Branch if equal'),
        ('bne r0 r1 lbl', 'if r0!=r1 goto', 'Branch if not equal'),
        ('blt r0 r1 lbl', 'if r0<r1 goto', 'Branch if less'),
        ('bgt r0 r1 lbl', 'if r0>r1 goto', 'Branch if greater'),
        ('beqz r0 lbl', 'if r0==0 goto', 'Branch if zero'),
        ('bnez r0 lbl', 'if r0!=0 goto', 'Branch if not zero'),
    ]
    for i, row in enumerate(ops):
        pdf.table_row(row, widths, i % 2 == 0)
    pdf.ln(3)

    pdf.section_title('Device Operations')
    widths = [60, 55, 75]
    pdf.table_header(['Instruction', 'Meaning', 'Description'], widths)
    ops = [
        ('l r0 d0 Prop', 'r0 = d0.Prop', 'Load from device'),
        ('s d0 Prop r0', 'd0.Prop = r0', 'Store to device'),
        ('ls r0 d0 s Prop', 'r0=d0.Slot(s).P', 'Load slot prop'),
        ('lb r0 h Prop m', 'batch read', 'Load batch'),
        ('sb h Prop r0', 'batch write', 'Store batch'),
    ]
    for i, row in enumerate(ops):
        pdf.table_row(row, widths, i % 2 == 0)
    pdf.ln(3)

    pdf.section_title('Special Instructions')
    widths = [50, 60, 80]
    pdf.table_header(['Instruction', 'Meaning', 'Description'], widths)
    ops = [
        ('move r0 r1', 'r0 = r1', 'Copy value'),
        ('yield', 'pause 1 tick', 'Yield execution'),
        ('sleep r0', 'pause r0 sec', 'Sleep for time'),
        ('push r0', 'stack.push(r0)', 'Push to stack'),
        ('pop r0', 'r0=stack.pop()', 'Pop from stack'),
        ('hcf', 'halt', 'Halt and catch fire'),
    ]
    for i, row in enumerate(ops):
        pdf.table_row(row, widths, i % 2 == 0)

    # ===== CHAPTER 7: EXAMPLES =====
    pdf.add_page()
    pdf.chapter_title('7. Example Programs')

    pdf.section_title('Thermostat with Hysteresis')
    pdf.body_text('Maintains temperature with dead-band to prevent rapid cycling:')
    pdf.code_block('''ALIAS sensor d0
ALIAS heater d1

CONST TARGET = 293    # 20C in Kelvin
CONST TOLERANCE = 2

main:
    VAR temp = sensor.Temperature

    IF temp < TARGET - TOLERANCE THEN
        heater.On = 1
    ELSEIF temp > TARGET + TOLERANCE THEN
        heater.On = 0
    ENDIF

    YIELD
    GOTO main
END''')

    pdf.section_title('Solar Panel Tracker')
    pdf.body_text('Automatically positions solar panels to track the sun:')
    pdf.code_block('''ALIAS panel d0

main:
    VAR angle = panel.SolarAngle
    panel.Horizontal = angle
    panel.Vertical = 60

    YIELD
    GOTO main
END''')

    pdf.section_title('Battery Monitor with Backup')
    pdf.body_text('Activates generator when battery is low:')
    pdf.code_block('''ALIAS battery d0
ALIAS generator d1
ALIAS display d2

CONST LOW = 0.20
CONST HIGH = 0.90

main:
    VAR charge = battery.Charge

    IF charge < LOW THEN
        generator.On = 1
    ELSEIF charge > HIGH THEN
        generator.On = 0
    ENDIF

    display.Setting = charge * 100
    YIELD
    GOTO main
END''')

    pdf.add_page()
    pdf.section_title('Counter with Compound Assignment')
    pdf.body_text('Demonstrates v1.9.0 compound operators:')
    pdf.code_block('''ALIAS display d0
ALIAS button d1

VAR count = 0
VAR lastBtn = 0

main:
    VAR btn = button.Setting

    IF btn = 1 AND lastBtn = 0 THEN
        count += 1    # Compound assignment
    ENDIF

    lastBtn = btn
    display.Setting = count

    YIELD
    GOTO main
END''')

    pdf.section_title('Bit Flags for Status Display')
    pdf.body_text('Uses bit shifts for compact status:')
    pdf.code_block('''ALIAS sensor d0
ALIAS display d1

VAR status = 0

main:
    status = 0

    IF sensor.Power > 0 THEN
        status = status | (1 << 0)
    ENDIF
    IF sensor.Temperature > 250 THEN
        status = status | (1 << 1)
    ENDIF
    IF sensor.Pressure > 80 THEN
        status = status | (1 << 2)
    ENDIF

    display.Setting = status
    YIELD
    GOTO main
END''')

    # ===== CHAPTER 8: TIPS =====
    pdf.add_page()
    pdf.chapter_title('8. Tips & Best Practices')

    pdf.section_title('Always Use YIELD')
    pdf.body_text(
        'Every loop must contain YIELD or SLEEP. Without it, the game will '
        'detect an infinite loop and halt your program.'
    )
    pdf.code_block('''# BAD - will crash
WHILE 1
    # no yield!
WEND

# GOOD
WHILE 1
    YIELD
WEND''')

    pdf.section_title('Cache Device Reads')
    pdf.body_text('Each device read takes time. Read once and reuse:')
    pdf.code_block('''# BAD - reads 3 times
IF sensor.Temperature > 100 THEN
ELSEIF sensor.Temperature > 50 THEN
ENDIF

# GOOD - reads once
VAR temp = sensor.Temperature
IF temp > 100 THEN
ELSEIF temp > 50 THEN
ENDIF''')

    pdf.section_title('Use Hysteresis')
    pdf.body_text(
        'Prevent rapid on/off switching by adding a dead-band around your '
        'target value:'
    )
    pdf.code_block('''CONST TARGET = 100
CONST TOLERANCE = 5

IF value < TARGET - TOLERANCE THEN
    device.On = 1
ELSEIF value > TARGET + TOLERANCE THEN
    device.On = 0
ENDIF''')

    pdf.section_title('Use Constants for Magic Numbers')
    pdf.code_block('''# BAD
IF temp > 373.15 THEN

# GOOD
CONST BOILING_POINT = 373.15
IF temp > BOILING_POINT THEN''')

    pdf.section_title('Bit Shifts for Efficiency')
    pdf.body_text('Use bit shifts for power-of-2 multiplication/division:')
    pdf.code_block('''x = x << 1    # Same as x * 2
x = x >> 2    # Same as x / 4

# Set/clear/check flags
flags = flags | (1 << n)      # Set bit n
flags = flags & ~(1 << n)     # Clear bit n
isSet = (flags >> n) & 1      # Check bit n''')

    # ===== APPENDIX A =====
    pdf.add_page()
    pdf.chapter_title('Appendix A: Common Device Properties')

    pdf.section_title('Universal Properties')
    widths = [45, 30, 25, 90]
    pdf.table_header(['Property', 'Type', 'R/W', 'Description'], widths)
    props = [
        ('On', '0/1', 'R/W', 'Power state'),
        ('Setting', 'Number', 'R/W', 'Target/display value'),
        ('Mode', 'Integer', 'R/W', 'Operating mode'),
        ('Lock', '0/1', 'R/W', 'Lock state'),
        ('Error', '0/1', 'R', 'Error state'),
        ('Power', 'Watts', 'R', 'Power consumption'),
        ('PrefabHash', 'Integer', 'R', 'Device type hash'),
    ]
    for i, row in enumerate(props):
        pdf.table_row(row, widths, i % 2 == 0)
    pdf.ln(5)

    pdf.section_title('Atmosphere Properties')
    widths = [55, 30, 25, 80]
    pdf.table_header(['Property', 'Type', 'R/W', 'Description'], widths)
    props = [
        ('Temperature', 'Kelvin', 'R', 'Gas temperature'),
        ('Pressure', 'kPa', 'R', 'Total pressure'),
        ('RatioOxygen', '0-1', 'R', 'O2 ratio'),
        ('RatioCarbonDioxide', '0-1', 'R', 'CO2 ratio'),
        ('RatioNitrogen', '0-1', 'R', 'N2 ratio'),
        ('RatioVolatiles', '0-1', 'R', 'H2 ratio'),
        ('RatioWater', '0-1', 'R', 'Steam ratio'),
        ('TotalMoles', 'Moles', 'R', 'Total gas quantity'),
    ]
    for i, row in enumerate(props):
        pdf.table_row(row, widths, i % 2 == 0)
    pdf.ln(5)

    pdf.section_title('Power Properties')
    widths = [50, 30, 25, 85]
    pdf.table_header(['Property', 'Type', 'R/W', 'Description'], widths)
    props = [
        ('Charge', '0-1', 'R', 'Battery charge ratio'),
        ('PowerGeneration', 'Watts', 'R', 'Power output'),
        ('PowerRequired', 'Watts', 'R', 'Power demand'),
        ('SolarAngle', 'Degrees', 'R', 'Sun angle'),
        ('Horizontal', 'Degrees', 'R/W', 'Panel horizontal'),
        ('Vertical', 'Degrees', 'R/W', 'Panel vertical'),
    ]
    for i, row in enumerate(props):
        pdf.table_row(row, widths, i % 2 == 0)

    # ===== APPENDIX B =====
    pdf.add_page()
    pdf.chapter_title('Appendix B: Color Constants')

    pdf.body_text('Built-in color constants for lights and displays:')

    widths = [40, 40, 60, 50]
    pdf.table_header(['Name', 'Value', 'RGB Hex', 'Usage'], widths)
    colors = [
        ('Blue', '0', '#0000FF', 'light.Color = Blue'),
        ('Gray', '1', '#808080', 'light.Color = Gray'),
        ('Green', '2', '#00FF00', 'light.Color = Green'),
        ('Orange', '3', '#FFA500', 'light.Color = Orange'),
        ('Red', '4', '#FF0000', 'light.Color = Red'),
        ('Yellow', '5', '#FFFF00', 'light.Color = Yellow'),
        ('White', '6', '#FFFFFF', 'light.Color = White'),
        ('Black', '7', '#000000', 'light.Color = Black'),
        ('Brown', '8', '#8B4513', 'light.Color = Brown'),
        ('Khaki', '9', '#F0E68C', 'light.Color = Khaki'),
        ('Pink', '10', '#FFC0CB', 'light.Color = Pink'),
        ('Purple', '11', '#800080', 'light.Color = Purple'),
    ]
    for i, row in enumerate(colors):
        pdf.table_row(row, widths, i % 2 == 0)

    pdf.ln(10)
    pdf.section_title('Custom RGB Colors')
    pdf.body_text('For custom colors, use decimal RGB values:')
    pdf.code_block('''# RGB to decimal: R*65536 + G*256 + B
DEFINE RED 16711680       # FF0000
DEFINE GREEN 65280        # 00FF00
DEFINE BLUE 255           # 0000FF
DEFINE YELLOW 16776960    # FFFF00
DEFINE CYAN 65535         # 00FFFF
DEFINE MAGENTA 16711935   # FF00FF
DEFINE WHITE 16777215     # FFFFFF

light.Color = RED''')

    pdf.ln(10)
    pdf.section_title('Slot Type Constants')
    pdf.body_text('For slot operations:')
    widths = [50, 50, 90]
    pdf.table_header(['Name', 'Value', 'Description'], widths)
    slots = [
        ('Import', '0', 'Input slot (also: Input)'),
        ('Export', '1', 'Output slot (also: Output)'),
        ('Content', '2', 'Content/storage slot'),
        ('Fuel', '3', 'Fuel slot'),
    ]
    for i, row in enumerate(slots):
        pdf.table_row(row, widths, i % 2 == 0)

    # Save PDF
    output_path = os.path.join(os.path.dirname(__file__), 'docs', 'Basic-10_Manual.pdf')
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    pdf.output(output_path)
    print(f"PDF generated: {output_path}")
    return output_path

if __name__ == '__main__':
    create_documentation()
