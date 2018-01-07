
# FFXIV TexTools 2 by Liinko
This is a more feature rich WPF implementation of FFXIV TexTools and replaced FFXIV TexTools.

# Current Version: 1.6.1
ChangeLog:

Application:
 - Updated to latest version of Helix Toolkit.
 - Added new steam directory name "FINAL FANTASY XIV Online" to common install directories.
 - Help > Check For Problems now does a more thorough check.
 - Changed .modlist and saved directory to MyDocument/TexTools so that it is shared by all newer version of TexTools.
 - User settings from this version onward should now persist as new versions are released.
 - Help > Report Bug now opens a new link to a public issue tracker.
 - Added a whole bunch of additional safeguards and checks for importing.
 - Added a crash dialog with information that can be copied to clipboard for bug reports.
 
3D:
 - Meshes in the .dae file no longer have to be in order when importing.
 - Secondary Texture coorindates now export and import (used for decals/face paints).
 - Extra data used to hide mesh parts when overlaid by another mesh now imports and updates (this should fix a lot of issues).
 - Faces now import correctly.
 - Slight increase in emissive intensity in 3D viewer.
 - Added a notice for the model in Character > Body stating that it is not the default model when unequipped.
 - Added initial support for importing from blender (must use "Better Collada exporter" from here https://github.com/godotengine/collada-exporter)

Bug Fixes:
 - Fixed an issue where textures would not appear correctly on 3D model when the texture width was greater than its height.
 - Fixed an issue where the model for Hyur Midlander Female in Charater > Body would not display.
 - Fixed an issue where imported model indices could be read incorrectly under certain circumstances.
 - Fixed an issue where offset was being set to 0 when importing an item with a larger data size than that already in the modlist.
 - Fixed an issue where some exported models had incorrect skinning (eg. Yotsuyu body[9130]), also fixes incorrect skinning on import.
 - Fixed an issue where certain models would not display in 3D (eg. Raubahn[9095]).
 - Fixed an issue where under certain circumstances information would be saved incorrectly to the modlist causing file pointer issues.
 - Other minor bug fixes

Version 1.6.1 Bug Fixes:
 - Fixed an issue where dae files with TexTools authoring tool were not able to be imported.
 - Fixed an issue where certain models with no extra data in mesh 0, but extra data in mesh 1 would not import and cause the application to crash. (eg. Makai Moon Guide's Quartertights)

Not Yet Implemented:
* Mod Importer
