
# FFXIV TexTools 2 by Liinko
This is a more feature rich WPF implementation of FFXIV TexTools and replaced FFXIV TexTools.

# Current Version: 1.7
ChangeLog:

For previous ChangeLogs visit http://ffxivtextools.dualwield.net/change_log.html

Application:
 - Added the ability to change directory for Index Backup folder and Modlist file.
 - Added error checking when reading the games items list.
 - Added missing Egi (Sephirot & Bahamut).
 - Added discord link.
 - Added tutorials link to Help menu.
 - Added additional checks to prevent writing an offset of 0 to the modlist.
 - Added additional checks to make sure settings are set correctly

3D:
 - Removed warning for having extra texture coordinates as that is now taken care of internally.
 - Added an error message when the AssetCc2.exe may be an unsupported version.

Bug Fixes:
 - Fixed an issue where models were not able to be imported if there was a gap in part numbers.
 - Fixed an issue where data was being read incorrectly on importing a model for a second time.
 - Fixed an issue where DX version setting would not save, and would be DX11 after restarting the application every time.
 - Fixed an issue where RGBA toggles were not selectable on UI items.
 - Fixed an issue where texture variations of Pets were not displaying in 3D view.
 - Fixed an issue where extra vertex data was not being read correctly on import causing parts of gear to go flying off into space somewhere. 
 - Fixed an issue where importing a model with a mesh that is referencing a bone that is included in the model but not referenced by the original would cause TexTools to crash.
 - Other minor bug fixes.

Known Issues:
 - Some or All model imports crash the game when using the DX9 client.
 - UI textures do not show up unless A in RGBA is checked.

Not Yet Implemented:
* Mod Importer
