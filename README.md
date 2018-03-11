
# FFXIV TexTools 2 by Liinko
This is a more feature rich WPF implementation of FFXIV TexTools and replaced FFXIV TexTools.

# Current Version: 1.8
ChangeLog:

For previous ChangeLogs visit http://ffxivtextools.dualwield.net/change_log.html

Application:
 - Added Make ModPack and Import ModPack in the Mods menu for batch export/import.
 - Added Default Race under Options menu, this will show the selected races texture in 3D view.
 - Added Mod Repos to the menu.
 - Added the ability to delete mods from Mod List.
 - Added version number to error messages
 - Mod List now supports multi-select (Ctrl + Click) (Shift + Click).
 - Pets now has separate entries for each version
 - Removed DX Version from Menu, clicking on the DX Version in the status bar now changes the DX version.
 - Monsters and DemiHumans now save to their respective folders when saved from Model Search.
 
Textures:
 - Added secondary textures for pets
 - Added Field Markers to UI

3D:
 - Added Advanced Importing for 3D models.
 - An import settings file now exports with 3D models containing extra data.
 - TexTools will now stay on the same race after importing
 - Added message when model being imported has indices in the extra data that are out of range.
 - Added Update TEX button, this will reload the textures on the model.
 - Changed reflection amount to be within button text.
 - DemiHuman parts now appear in dropdown menu when selected from Model Search.
 - Ctrl+R now resets the camera in 3D view

Bug Fixes:
 - Fixed an issue where Mounts did not appear after patch 4.2
 - Fixed an issue where the application would not read .dat files higher than 0 for 0a.
 - Fixed an issue where UI items would not appear unless alpha was checked
 - Fixed an issue where models in the DemiHuman category would not load when opened from the ModelSearch Window.
 - Fixed an issue where indices were not being read as unsigned, causing certain models to not fully load.
 - Fixed an issue where any model in the monster category would not import correctly.
 - Fixed an issue where models with more than 10 parts would not import correctly.
 - Fixed an issue where bones weights being imported would not equal to 1 in some cases. (Big thanks to Sel.)
 - Fixed an issue where bones with 0 weight would cause issue upon reimporting. (Big thanks to Sel.)
 
Known Issues:
 - Recent hair styles cannot be exported (Needs new skeleton files [Will update soon])
