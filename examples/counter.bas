REM Counter Example
REM Demonstrates FOR loop and basic math

ALIAS display d0

LET count = 0

FOR i = 1 TO 10
    LET count = count + i
    PRINT count
NEXT i

PRINT "Final count:"
PRINT count

END
