
# FFXIV TexTools 2 by Liinko
This is a more feature rich WPF implementation of FFXIV TexTools and replaced FFXIV TexTools.

# Current Version: 1.2
ChangeLog:

Application:
 - Added DemiHuman category to Model Search.
 - Folder select dialog now reappears if incorrect folder is selected.
 
Textures:
 - Added _stigma.tex to Equipment Decals

3D:
 - Meshes exported in .dae file are now separated into parts to reflect MDL structure.
 - Improved importing, it should work with most equipment.
 - Alpha texture for 3D files now exports separately.
 
Bug Fixes:
 - Fixed an issue where certain model IDs would not appear in search results list.
 - Fixed an issue where other windows would stay open when main window was closed.
 - Fixed an issue where certain items with VFX files would crash due to being read incorrectly.
 - Fixed an issue where certain models for MCH weapons would not be displayed.
 - Fixed an issue where application would not check if a newer version was available.
 - Other minor bug fixes.

Know Issues:
 - Not all models are exportable as .dae.
 - Importing faces appear correctly in TexTools, but crash the game.

Not Yet Implemented:
* Mod Importer

Using HelixToolkit:
https://ci.appveyor.com/project/objorke/helix-toolkit/build/1.0.0-unstable.1826.build.906/artifacts
