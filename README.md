
# FFXIV TexTools 2 by Liinko
This is a more feature rich WPF implementation of FFXIV TexTools and replaced FFXIV TexTools.

# Current Version: 1.7.5
ChangeLog:

For previous ChangeLogs visit http://ffxivtextools.dualwield.net/change_log.html

Application:
 - Added a scrollable message box so that it does not run off the screen. (Thanks to Soralin)

3D:
 - Camera now retains its position when switching between models in the same category. (Thanks to Soralin)

Bug Fixes:
 - Fixed an issue where TexTools was unable to access .dat1 for 0a, and would crash.
 - Fixed an issue where the decimal separator for different languages would change to a comma, which made it import/export incorrectly.
 - Fixed an issue where if two consecutive parts of a mesh had been deleted, it would cause TexTools to crash on import.
 - Fixed an issue where TexTools would crash if some meshes had secondary texture coordinates for one mesh but not another.
 - Fixed an issue where MeshParts for LoDs other than the primary were being written incorrectly to the MDL file, causing the DX9 client to crash.
 - Fixed an issue where Start Over would place the modlist in the default location even if a custom directory had been set.

Not Yet Implemented:
* Mod Importer
