' ============================================
' EXAMPLE 15: Item Sorter
' ============================================
' DIFFICULTY: Intermediate
'
' DESCRIPTION:
' Sorts items based on their hash values.
' Routes different materials to different
' outputs using a sorter device.
'
' DEVICE CONNECTIONS:
' d0 = Sorter
' d1 = LED Display (shows current item hash)
'
' HOW IT WORKS:
' - Reads item hash from sorter slot 0
' - Matches against known material hashes
' - Sets sorter mode to route to correct output
' - Unknown items go to default output
'
' SORTER MODES:
' - Mode 0 = Default output
' - Mode 1-4 = Sorted outputs 1-4
'
' NOTES:
' - Find item hashes in Device Hash Database
' - Add more ELSEIF blocks for more materials
' ============================================

ALIAS sorter d0
ALIAS display d1

' Common ore hashes
DEFINE IRON_HASH 226410516
DEFINE COPPER_HASH -707307845
DEFINE GOLD_HASH -929742000
DEFINE SILICON_HASH 1179041605

VAR itemHash = 0

main:
    ' Read item in sorter input slot
    itemHash = sorter[0].OccupantHash

    ' Display current hash (for debugging)
    display.Setting = itemHash

    ' Sort based on item type
    IF itemHash = IRON_HASH THEN
        sorter.Mode = 1
    ELSEIF itemHash = COPPER_HASH THEN
        sorter.Mode = 2
    ELSEIF itemHash = GOLD_HASH THEN
        sorter.Mode = 3
    ELSEIF itemHash = SILICON_HASH THEN
        sorter.Mode = 4
    ELSE
        ' Unknown item - default output
        sorter.Mode = 0
    ENDIF

    YIELD
    GOTO main

END
