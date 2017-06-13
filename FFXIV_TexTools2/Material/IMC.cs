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
using FFXIV_TexTools2.Model;
using FFXIV_TexTools2.Resources;
using System;
using System.Diagnostics;
using System.IO;

namespace FFXIV_TexTools2.Material
{
    public static class IMC
    {

        public static string type, itemID, body, variant;

        public static string GetVersion(string selectedParent, Items item, bool isSecondary)
        {
            if(int.Parse(item.itemSlot) < 22 || selectedParent.Equals(Strings.Pets) || selectedParent.Equals(Strings.Mounts) || selectedParent.Equals(Strings.Minions))
            {
                int slot = Info.slotID[selectedParent];
                type = Helper.GetItemType(selectedParent);
                itemID = item.itemID;
                body = item.weaponBody;
                variant = item.imcVariant;

                if (isSecondary)
                {
                    itemID = item.itemID1;
                    body = item.weaponBody1;
                    variant = item.imcVariant1;
                }


                int offset = IMCOffset();

                if (offset != 0)
                {
                    return FindVersion(offset, slot);
                }
                else
                {
                    return "0001";
                }
            }
            else
            {
                return "0";
            }
        }

        private static int IMCOffset()
        {
            int IMCOffset;

            if (type.Equals("weapon") || type.Equals("monster"))
            {
                IMCOffset = Helper.GetOffset(FFCRC.GetHash("chara/" + type + "/" + type.Substring(0, 1) + itemID + "/obj/body/b" + body), FFCRC.GetHash("b" + body + ".imc"));
            }
            else if (type.Equals("food"))
            {
                IMCOffset = Helper.GetOffset(FFCRC.GetHash("chara/weapon/w" + itemID + "/obj/body/b" + body), FFCRC.GetHash("b" + body + ".imc"));
            }
            else
            {
                IMCOffset = Helper.GetOffset(FFCRC.GetHash("chara/" + type + "/" + type.Substring(0, 1) + itemID), FFCRC.GetHash(type.Substring(0, 1) + itemID + ".imc"));
            }

            return IMCOffset;
        }

        private static string FindVersion(int offset, int slot)
        {
            int variantLoc;
            string itemVersion;

            int datNum = ((offset / 8) & 0x000f) / 2;

            using (BinaryReader br = new BinaryReader(new MemoryStream(Helper.GetDecompressedIMCBytes(offset, datNum))))
            {
                if (type.Equals("weapon") || type.Equals("food") || type.Equals("monster"))
                {
                    variantLoc = 4 + (int.Parse(variant) * 6);
                }
                else
                {
                    variantLoc = 4 + (int.Parse(variant) * 30) + (6 * slot);
                }

                br.BaseStream.Seek(variantLoc, SeekOrigin.Begin);

                int version = br.ReadInt16();

                if (version <= 0)
                {
                    itemVersion = "0001";
                }
                else if (version > 100)
                {
                    itemVersion = "0001";
                }
                else
                {
                    itemVersion = version.ToString().PadLeft(4, '0');
                }
            }
            return itemVersion;
        }
    }
}
