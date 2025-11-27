REM Math Functions Demo
REM Demonstrates built-in math functions

LET pi = 3.14159265

REM Trigonometry
LET angle = pi / 4
LET sinVal = SIN(angle)
LET cosVal = COS(angle)
LET tanVal = TAN(angle)

PRINT "Sin(45 deg):"
PRINT sinVal

PRINT "Cos(45 deg):"
PRINT cosVal

REM Square root and power
LET x = 16
LET sqrtX = SQRT(x)
LET powX = x ^ 2

PRINT "SQRT(16):"
PRINT sqrtX

PRINT "16^2:"
PRINT powX

REM Absolute value
LET negative = -42
LET absVal = ABS(negative)

PRINT "ABS(-42):"
PRINT absVal

REM Min and Max
LET a = 10
LET b = 20
LET minVal = MIN(a, b)
LET maxVal = MAX(a, b)

PRINT "MIN(10,20):"
PRINT minVal

PRINT "MAX(10,20):"
PRINT maxVal

END
