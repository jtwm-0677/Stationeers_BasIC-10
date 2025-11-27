' ============================================
' EXAMPLE 13: Math Functions Demo
' ============================================
' DIFFICULTY: Beginner
'
' DESCRIPTION:
' Demonstrates all built-in math functions
' available in BASIC-IC10. Useful reference
' for calculations in your programs.
'
' DEVICE CONNECTIONS:
' d0 = Console or LED Display (optional)
'
' AVAILABLE MATH FUNCTIONS:
' - ABS(x)     : Absolute value
' - SQRT(x)    : Square root
' - SIN(x)     : Sine (radians)
' - COS(x)     : Cosine (radians)
' - TAN(x)     : Tangent (radians)
' - ASIN(x)    : Arc sine
' - ACOS(x)    : Arc cosine
' - ATAN(x)    : Arc tangent
' - ATAN2(y,x) : Arc tangent of y/x
' - LOG(x)     : Natural logarithm
' - EXP(x)     : e raised to x
' - CEIL(x)    : Round up
' - FLOOR(x)   : Round down
' - ROUND(x)   : Round to nearest
' - TRUNC(x)   : Truncate decimals
' - MIN(a,b)   : Smaller of two values
' - MAX(a,b)   : Larger of two values
' - RAND       : Random 0-1
' ============================================

ALIAS display d0

VAR pi = 3.14159265

' Trigonometry (radians)
VAR angle = 0
VAR sinVal = 0
VAR cosVal = 0

' Rounding
VAR testVal = 0
VAR rounded = 0
VAR floored = 0
VAR ceiled = 0

' Min/Max
VAR a = 0
VAR b = 0
VAR minVal = 0
VAR maxVal = 0

main:
    ' --- TRIGONOMETRY ---
    angle = pi / 4              ' 45 degrees
    sinVal = SIN(angle)         ' = 0.707...
    cosVal = COS(angle)         ' = 0.707...

    ' --- ABSOLUTE VALUE ---
    VAR negative = -42
    VAR absVal = ABS(negative)  ' = 42

    ' --- SQUARE ROOT ---
    VAR x = 16
    VAR sqrtX = SQRT(x)         ' = 4

    ' --- POWER ---
    VAR squared = x ^ 2         ' = 256

    ' --- ROUNDING ---
    testVal = 3.7
    rounded = ROUND(testVal)    ' = 4
    floored = FLOOR(testVal)    ' = 3
    ceiled = CEIL(testVal)      ' = 4

    ' --- MIN / MAX ---
    a = 10
    b = 20
    minVal = MIN(a, b)          ' = 10
    maxVal = MAX(a, b)          ' = 20

    ' --- RANDOM ---
    VAR randomNum = RAND        ' 0.0 to 1.0

    ' Display a result
    display.Setting = sqrtX

    YIELD
    GOTO main

END
