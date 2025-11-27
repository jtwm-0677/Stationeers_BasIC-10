CUSTOM DEVICES FOLDER
=====================

Place your custom device JSON files in this folder.
The compiler will automatically load all .json files from here on startup.

FILE FORMAT
-----------
See ../CustomDevices.template.json for the full format with examples.

Minimal example:

{
  "devices": [
    {
      "prefabName": "StructureMyDevice",
      "category": "Custom",
      "displayName": "My Device",
      "description": "Description here"
    }
  ]
}

IMPORTANT NOTES
---------------
- Use "Structure*" prefix for placed structures (not "ItemStructure*")
- "ItemStructure*" is for items in inventory
- "Structure*" is for built/placed structures that IC10 can address

FINDING PREFAB NAMES
--------------------
1. Open Stationpedia in-game
2. Search for the device
3. Look at the PrefabHash line for the prefab name
4. For placed structures, remove "Item" prefix if present

RELOAD
------
After adding/modifying files:
- Use Tools > Reload Custom Devices in the compiler
- Or restart the application
