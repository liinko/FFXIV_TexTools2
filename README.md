
# FFXIV TexTools 2 by Liinko
This is a more feature rich WPF implementation of FFXIV TexTools and replaced FFXIV TexTools.

# Current Version: 1.4
ChangeLog:
Updated HelixToolkit and SharpDX versions.
        
Application:
 - Added more translations.
 - Removed ability to turn off modlist.
 
Textures:
 - Added Loading Image, Map Symbol, Online Status, and Weather to UI.

3D:
 - Models now export at 10x their original size for easier modding.
 - Changed the datatype of Normals and Texture coordinates from Half Floats to Singles.
 - Changed up axis to Y_UP.
 - other smaller tweaks to importing and exporting

Bug Fixes:
 - Fixed an issue where certain items were using system locale instead of application language.
 - Fixed an issue where some languages would not function correctly.
 - Fixed an issue where UI texture would not be imported correctly.
 - Fixed an issue where certain head models could not be exported.
 - Fixed an issue where a check for .modlist was being called before it was created
 - Other minor bug fixes

Know Issues:
 - Not all models are exportable as .dae.
 - Importing faces appear correctly in TexTools, but crash the game.

Not Yet Implemented:
* Mod Importer

Using HelixToolkit:
https://ci.appveyor.com/project/objorke/helix-toolkit/build/1.0.0-unstable.1826.build.906/artifacts
