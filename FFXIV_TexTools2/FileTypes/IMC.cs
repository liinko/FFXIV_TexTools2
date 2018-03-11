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
using System.IO;

namespace FFXIV_TexTools2.Material
{
    /// <summary>
    /// Handles files with imc extension
    /// </summary>
    /// <remarks>
    /// IMC files contain item version and part mask data
    /// </remarks>
    public static class IMC
    {
        /// <summary>
        /// Gets the version of the selected item.
        /// </summary>
        /// <param name="selectedCategory">The category which contains the selected item </param>
        /// <param name="item">The selected items data</param>
        /// <param name="isSecondary">Use secondary item data</param>
        /// <returns>The version of the selected item</returns>
        public static Tuple<string, string> GetVersion(string selectedCategory, ItemData item, bool isSecondary)
        {
            if(int.Parse(item.ItemCategory) < 22 || selectedCategory.Equals(Strings.Pets) || selectedCategory.Equals(Strings.Mounts) 
                || selectedCategory.Equals(Strings.Minions) || selectedCategory.Equals(Strings.Monster) || selectedCategory.Equals(Strings.DemiHuman))
            {
                var slotID = Info.slotID[selectedCategory];
                var type = Helper.GetCategoryType(selectedCategory);
                string itemID, body, variant;

                if (isSecondary)
                {
                    itemID = item.SecondaryModelID;
                    body = item.SecondaryModelBody;
                    variant = item.SecondaryModelVariant;
                }
                else
                {
                    itemID = item.PrimaryModelID;
                    body = item.PrimaryModelBody;
                    variant = item.PrimaryModelVariant;
                }

                int offset = 0;
                if (type.Equals("monster"))
                {
                    offset = Helper.GetDataOffset(FFCRC.GetHash(string.Format(Strings.MonsterIMCFolder, itemID, body)), FFCRC.GetHash(string.Format(Strings.MonsterIMCFile, body)), Strings.ItemsDat);
                }
                else if (type.Equals("food") || type.Equals("weapon"))
                {
                    offset = Helper.GetDataOffset(FFCRC.GetHash(string.Format(Strings.WeapIMCFolder, itemID, body)), FFCRC.GetHash(string.Format(Strings.WeapIMCFile, body)), Strings.ItemsDat);
                }
                else if (type.Equals("equipment"))
                {
                    offset = Helper.GetDataOffset(FFCRC.GetHash(string.Format(Strings.EquipIMCFolder, itemID)), FFCRC.GetHash(string.Format(Strings.EquipIMCFile, itemID)), Strings.ItemsDat);

                }
                else if (type.Equals("accessory"))
                {
                    offset = Helper.GetDataOffset(FFCRC.GetHash(string.Format(Strings.AccIMCFolder, itemID)), FFCRC.GetHash(string.Format(Strings.AccIMCFile, itemID)), Strings.ItemsDat);
                }
                else
                {
                    offset = Helper.GetDataOffset(FFCRC.GetHash("chara/" + type + "/" + type.Substring(0, 1) + itemID), FFCRC.GetHash(type.Substring(0, 1) + itemID + ".imc"), Strings.ItemsDat);
                }

                if (offset != 0)
                {
                    return FindVersion(offset, slotID, variant, type);
                }
                else
                {
                    return new Tuple<string, string>("0001", "0000");
                }
            }
            else
            {
                return new Tuple<string, string>("0001", "0000");
            }
        }

        /// <summary>
        /// Finds the version of the item within the imc file
        /// </summary>
        /// <param name="offset">The offset of the imc file</param>
        /// <param name="slotID">The equipment slot ID</param>
        /// <returns>The version of the item</returns>
        private static Tuple<string, string> FindVersion(int offset, int slotID, string variant, string type)
        {
            int variantOffset;
            string itemVersion;
            string vfxVersion;

            int datNum = ((offset / 8) & 0x000f) / 2;

            using (BinaryReader br = new BinaryReader(new MemoryStream(Helper.GetType2DecompressedData(offset, datNum, Strings.ItemsDat))))
            {
                if (type.Equals("weapon") || type.Equals("food") || type.Equals("monster"))
                {
                    variantOffset = 4 + (int.Parse(variant) * 6);
                }
                else
                {
                    variantOffset = 4 + (int.Parse(variant) * 30) + (6 * slotID);
                }

                br.BaseStream.Seek(variantOffset, SeekOrigin.Begin);

                int version = br.ReadInt16();
                br.ReadBytes(2);
                int vfx = br.ReadByte();

                vfxVersion = vfx.ToString().PadLeft(4, '0');

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
            return new Tuple<string, string>(itemVersion, vfxVersion);
        }
    }
}
