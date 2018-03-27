
# FFXIV TexTools 2 by Liinko
This is a more feature rich WPF implementation of FFXIV TexTools and replaced FFXIV TexTools.

# Current Version: 1.8.5
ChangeLog:

For previous ChangeLogs visit http://ffxivtextools.dualwield.net/change_log.html

Application:
 - [ModPack Creator]Added multi select buttons.
 - [ModPack Creator]Added multi select buttons.
 - [ModPack Creator]Added .atex and .avfx details.
 - [ModPack Creator]Added count and size.
 - [ModPack Creator & Importer]Added GB to total size of selected files.
 - [ModPack Creator & Importer]Added sort on header click.
 - [ModPack Importer]Added progress dialog on import.
 - [ModPack Importer]Moved import process to a separate thread.
 - [Directories]Added ability to change ModPack Directory
 - Added support for additional dat creation after 2GB limit is reached.
 
Textures:
 - Added "does not exist" message if the texture is not present in the game files.
 - Decreased image display size to save on memory, and prevent crashes.

3D:
 - Added more checks on Import

Bug Fixes:
 - Fixed an issue where size in ModPack Importer would not convert to MB or higher.
 - Fixed an issue where ModPack Importer window would not close on import.
 - Fixed an issue where only part a was being read for DemiHuman MTRLs
 - Fixed an issue where Miqo'te hair 104 and 109 were present but had no textures
 - Fixed an issue where the Check For Problems would show LoD settings for both DX versions
 - Other minor fixes and improvements.
 
Known Issues:
 - Recent hair styles cannot be exported (Needs new skeleton files [Will update soon])
 - ModList make take long to load if there is a lot of mods in the category selected.
