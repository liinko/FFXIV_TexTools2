
# FFXIV TexTools 2 by Liinko
This is a more feature rich WPF implementation of FFXIV TexTools and replaced FFXIV TexTools.

# Current Version: 1.9.9.0
ChangeLog:

For previous ChangeLogs visit http://ffxivtextools.dualwield.net/change_log.html

Application: 
- Skin Specular maps are now editable again for non-Au Ra races, and are viewable again in the ModList view.
  - The shared skin_m texture is now always referred to as a 'specular', to match the Au Ra version.
- Added Search Bar to Make Modpack Menu
  - Search supports Wildcards via Asterisk(*), and 'Or' via Pipes(|)
- Search for Model menu is now resize-able.
- UI now defaults focus to search bars in menus where they are available.
- XIV Mod Archive added to Repos menu.
- Version Number updated to 1.9.9.0

3D:
- Vertex Color is now imported/exported correctly.
- Vertex Alpha is now imported/exported correctly.
  - As a work-around due to inconsistency with DAE parsers/exporters, Vertex Alpha is stored in the X(U) channel of UV3.
- UV2, Vertex Color, and Vertex Alpha data are now dummied up with default values, in the event that it does not exist in the incoming DAE file.
- 3D Meshes now support multiple UV Coordinates and Normals per-Vertex.
  - As a byproduct to this, the Model Import process may slow down significantly when dealing with extremely large mesh groups (50k+ Faces).  (Ex. 78,000 Face mesh group took 65 seconds to import)
- BiNormal/Tangent data is no longer required in the DAE file, since TT recalculates them anyways.
- Added error message for mesh groups exceeding maximum size.

Bugfixes:
- Fixed a bug with Modpack Import that would cause it to break if the uncompressed modpack size was greater than ~4GB.
- Fixed a bug with importing that would allow DAT files to exceed the appropriate size.
- Fixed a bug with Model Importing that would cause model indices to be thrown off if there was extraneous unused UV/Normal/Vertex Color/etc. data.
- Fixed a bug with DAE imports that would cause a crash if the meshes had extra data channels in them (ex. extra UV channels).



Known Issues:
 - Skeleton files may need updating for some newer items. (Ex. Some Hairstyles and Bonewicca pieces)
 - 3D Meshes with multiple Normals per Vertex may have extra UV Seams when Exported back out from TexTools.
