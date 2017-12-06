
# FFXIV TexTools 2 by Liinko
This is a more feature rich WPF implementation of FFXIV TexTools and replaced FFXIV TexTools.

# Current Version: 1.5
ChangeLog:

Application:
 - Added watermark to search box for better visibility.
 - Added open folder icon to Texture and Model tabs for easier access to the items folder.
 
3D:
 - Added emissive texture (Glow) to shader and export.
 - Models importing is now more accurate and free of issues. (Big Thanks to NeoX42)
 - Exporting .dae files with all models is now supported.
     - Requires AssetCc2.exe from Havok Content Tools (Not Provided)

Bug Fixes:
 - Fixed an issue where certain models with vfx textures would not load.
 - Fixed an issue where the models for Selene and Bishop AutoTurret were incorrect.
 - Fixed an issue where certain models were using incorrect textures.
 - Fixed issue where monk weapons that used equipment for secondary model would not display.
 - Fixed an issue where data was not being saved correctly in the modlist in certain scenarios, causing double entries, crashes, and incorrect reverts.
 - Fixed an issue where blank entries appeared in modlist window.
 - Fixed an issue where Egi's were crashing due to missing hyphen.
 - Fixed an issue where the Type dropdown would not empty after changing items.
 - Fixed an Issue where importing a model twice would cause incorrect data to be read.
 - Other minor bug fixes

Know Issues:
 - Importing faces appear correctly in TexTools, but crash the game.
 - Some astrologian weapons textures are not mapped correctly in 3D view.

Not Yet Implemented:
* Mod Importer

Using HelixToolkit:
https://ci.appveyor.com/project/objorke/helix-toolkit/build/1.0.0-unstable.1826.build.906/artifacts
