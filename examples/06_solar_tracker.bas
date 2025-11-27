' ============================================
' EXAMPLE 06: Solar Panel Tracker
' ============================================
' DIFFICULTY: Beginner
'
' DESCRIPTION:
' Automatically rotates solar panels to face the
' sun for optimal power generation. One IC10 can
' control all connected panels.
'
' DEVICE CONNECTIONS:
' d0 = Solar Panel (any one panel in array)
'
' HOW IT WORKS:
' - Reads the sun's angle from the panel
' - Sets panel horizontal rotation to match
' - Fixed vertical angle (adjust for latitude)
'
' NOTES:
' - SolarAngle property gives sun position
' - Horizontal = rotation (0-360 degrees)
' - Vertical = tilt (0 = flat, 90 = upright)
' - All panels on same network follow settings
' - Vertical angle of 60° works for most bases
' ============================================

ALIAS panel d0

VAR solarAngle = 0

main:
    ' Get current sun position
    solarAngle = panel.SolarAngle

    ' Point panels at the sun
    panel.Horizontal = solarAngle

    ' Set tilt (adjust for your base location)
    panel.Vertical = 60

    YIELD
    GOTO main

END
