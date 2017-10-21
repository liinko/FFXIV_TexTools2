
# FFXIV TexTools 2 by Liinko
This is a more feature rich WPF implementation of FFXIV TexTools and replaced FFXIV TexTools.

# Current Version: 1.1
ChangeLog:
Updated HelixToolkit and SharpDX versions.
        
Application:
 - Now works with 4.1 Patch.
 - Added Search > Model ID option in menu.
 - (Experimenta/Beta)Importing Models is now possible.
 - Should now be more memory efficient.
 
3D:
 - Export Model + Materials now exports a Collada .dae file as well as .obj files.
 - Exported .dae files are skinned (include skeleton & blend weights).
 - Note: Not all models are exportable as .dae yet.
 - Changed culling mode.
 - A few tweaks to the shader.
 
Bug Fixes:
 - Fixed a bug that would prevent using 'Disable All Mods' and 'ReEnable All Mods'.
 - Fixed colorset crashing under certain circumstances.
 - Fixed an issue where updated image would not stay after importing or enabling/disabling colorsets.
 - Fixed an issue where certain Hyur Highlander textures/models would not appear.
 - Other minor bug fixes.

Not Yet Implemented:
* Mod Importer

Using HelixToolkit:
https://ci.appveyor.com/project/objorke/helix-toolkit/build/1.0.0-unstable.1826.build.906/artifacts
