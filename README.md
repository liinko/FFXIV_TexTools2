
# FFXIV TexTools 2 by Liinko
This is a more feature rich WPF implementation of FFXIV TexTools and replaced FFXIV TexTools.

# Current Version: 1.9.8
ChangeLog:

For previous ChangeLogs visit http://ffxivtextools.dualwield.net/change_log.html

Application:
 - Guest update by Sel/Lunaretic [https://github.com/Lunaretic]
 - Added an option to change the model viewer background color (Requires restart)
 - Closing the Modlist menu now automatically refreshes the current model and textures.
 
Textures:
 - Icons with multiple stacks/versions are now editable.

3D:
 - All body technical assets under character-body are now editable (Don't play with them unless you know what you're doing; standard nude bodies are under Smallclothes)
- 3D Models now import with correct LoD Data.  (Does not apply to old .ttmps; they must be manually reimported & repackaged)
  - Higher LoD levels have extra mesh data automatically disabled for now.
- An option has been added to the Advanced Import menu to use the original Bones from an item.
  - This is an edge-case handling for items with more than 64 bones, and some animated items. (ex. Antecedent's Attire, Astro Globes)
  - Items using Original Bones cannot have Mesh Groups or Bones added to them.  Mesh Parts may be added, but can only use bones already in their mesh group.
  
Bug Fixes:
- Fixed importing DX9 Models (Does not apply to old .ttmps; they must be manually reimported & repackaged)
- Fixed issues with very large/complex models exceeding the header length limit and crashing with a Model 3 Data error.
- The 3D Model viewer now crashes more gracefully.

Known Issues:
 - Skeleton files may need updating for some newer items. (Ex. Some Hairstyles and Bonewicca pieces)
