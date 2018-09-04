
# FFXIV TexTools 2 by Liinko
This is a more feature rich WPF implementation of FFXIV TexTools and replaced FFXIV TexTools.

# Current Version: 1.9.7
ChangeLog:

For previous ChangeLogs visit http://ffxivtextools.dualwield.net/change_log.html

Application:
 - Guest update by Sel/Lunaretic [https://github.com/Lunaretic]
 - Advanced Import interface overhauled to support 3D Model import updates.
 
Textures:
 - Added "does not exist" message if the texture is not present in the game files.
 - Decreased image display size to save on memory, and prevent crashes.

3D:
 - Support for adding new Mesh Groups to items.
 - Support for adding new Mesh Parts to items.
 - Support for adding new Materials to items.
 - Support for adding new Attributes to items.
 - Support for new adding Bones to items.

Bug Fixes:
 - Fixed a bug where items with disparate bone sets would have their weights break on import.
 - Fixed a bug where index order would be critically broken for Items with more than two Mesh Groups.
 - Fixed a bug where icons would attempt to load and display the wrong data after Import/Enable/Disable
 
Known Issues:
 - Skeleton files may need updating for some newer items. (Ex. Some Hairstyles and Bonewicca pieces)
