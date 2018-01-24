// FFXIV TexTools
// Copyright © 2017 Rafael Gonzalez - All Rights Reserved
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using FFXIV_TexTools2.Helpers;
using FFXIV_TexTools2.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace FFXIV_TexTools2.Model
{
    public class VFXData
    {
        public List<string> VFXPaths = new List<string>();

        public static Tuple<string, byte[]> GetAVFX(string category, ItemData item, string AVFXVersion, bool secondary)
        {
            var fullPath = GetAVFXPath(category, item, AVFXVersion, secondary);

            var folderHash = FFCRC.GetHash(fullPath.Substring(0, fullPath.LastIndexOf("/")));
            var fileHash = FFCRC.GetHash(Path.GetFileName(fullPath));

            var offset = Helper.GetDataOffset(folderHash, fileHash, Strings.ItemsDat);

            if(offset != 0)
            {
                int datNum = ((offset / 8) & 0x000f) / 2;

                return new Tuple<string, byte[]>(fullPath, Helper.GetType2DecompressedData(offset, datNum, Strings.ItemsDat));
            }
            else
            {
                MessageBox.Show("Could not find AVFX Data for " + item.ItemName, "AVFX Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        public static string GetAVFXPath(string category, ItemData item, string AVFXVersion, bool secondary)
        {
            var cat = Helper.GetCategoryType(category);
            var folder = "";
            var file = "";

            if (cat.Equals("weapon"))
            {
                if (secondary)
                {
                    folder = "chara/weapon/w" + item.SecondaryModelID.PadLeft(4, '0') + "/obj/body/b" + item.SecondaryModelBody.PadLeft(4, '0') + "/vfx/eff";
                }
                else
                {
                    folder = "chara/weapon/w" + item.PrimaryModelID.PadLeft(4, '0') + "/obj/body/b" + item.PrimaryModelBody.PadLeft(4, '0') + "/vfx/eff";
                }

                file = "vw" + AVFXVersion.PadLeft(4, '0') + ".avfx";
            }
            else if (cat.Equals("accessory"))
            {
                folder = "chara/accessory/a" + item.PrimaryModelID.PadLeft(4, '0') + "/vfx/eff";
                file = "va" + AVFXVersion.PadLeft(4, '0') + ".avfx";
            }
            else if (cat.Equals("monster"))
            {
                folder = "chara/monster/m" + item.PrimaryModelID.PadLeft(4, '0') + "/obj/body/b" + item.PrimaryModelBody.PadLeft(4, '0') + "/vfx/eff";
                file = "vm" + AVFXVersion.PadLeft(4, '0') + ".avfx";
            }
            else
            {
                folder = "chara/equipment/e" + item.PrimaryModelID.PadLeft(4, '0') + "/vfx/eff";
                file = "ve" + AVFXVersion.PadLeft(4, '0') + ".avfx";
            }

            return folder + "/" + file;
        }
    }
}
