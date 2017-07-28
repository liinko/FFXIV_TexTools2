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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace FFXIV_TexTools2.IO
{
    public static class ExdReader
    {
        /// <summary>
        /// Creates a list of minions contained in companion_(num)_(language).exd 
        /// </summary>
        /// <returns>List<Items> Items:Item data associated with Minion</returns>
        public static List<ItemData> MakeMinionsList()
        {
            string minionFile = String.Format(Strings.MinionFile, Strings.Language);

            byte[] minionsBytes = Helper.GetDecompressedEXDData(Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(minionFile)));
            byte[] modelChara = Helper.GetDecompressedEXDData(Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(Strings.ModelCharaFile)));

            List<ItemData> minionsDict = new List<ItemData>();

            using (BinaryReader br = new BinaryReader(new MemoryStream(minionsBytes)))
            {
                using (BinaryReader br1 = new BinaryReader(new MemoryStream(modelChara)))
                {
                    br.ReadBytes(8);
                    int offsetTableSize = BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0);

                    br1.ReadBytes(8);
                    int offsetTableSize1 = BitConverter.ToInt32(br1.ReadBytes(4).Reverse().ToArray(), 0);

                    for (int i = 0; i < offsetTableSize; i += 8)
                    {
                        br.BaseStream.Seek(i + 32, SeekOrigin.Begin);
                        int index = BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0);
                        int tableOffset = BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0);

                        br.BaseStream.Seek(tableOffset, SeekOrigin.Begin);

                        br.ReadBytes(13);

                        int firstText = br.ReadByte();

                        if (firstText >= 2)
                        {
                            ItemData item = new ItemData();

                            br.ReadBytes(8);

                            uint modelIndex = BitConverter.ToUInt16(br.ReadBytes(2).Reverse().ToArray(), 0);

                            br.ReadBytes(30);

                            byte[] minionNameBytes = br.ReadBytes(firstText - 1);
                            item.ItemName = Helper.ToTitleCase((Encoding.UTF8.GetString(minionNameBytes)).Replace("\0", ""));
                            item.ItemCategory = Strings.Minion_Category;

                            for (int j = 0; j < offsetTableSize1; j += 8)
                            {
                                br1.BaseStream.Seek(j + 32, SeekOrigin.Begin);

                                uint index1 = BitConverter.ToUInt32(br1.ReadBytes(4).Reverse().ToArray(), 0);

                                if (index1 == modelIndex)
                                {
                                    int tableOffset1 = BitConverter.ToInt32(br1.ReadBytes(4).Reverse().ToArray(), 0);

                                    br1.BaseStream.Seek(tableOffset1, SeekOrigin.Begin);

                                    br1.ReadBytes(6);

                                    item.PrimaryModelID = (BitConverter.ToInt16(br1.ReadBytes(2).Reverse().ToArray(), 0)).ToString().PadLeft(4, '0');
                                    br1.ReadBytes(3);
                                    item.PrimaryModelBody = (br1.ReadByte()).ToString().PadLeft(4, '0');
                                    item.PrimaryModelVariant = (br1.ReadByte()).ToString();

                                    item.PrimaryMTRLFolder = string.Format(Strings.MonsterMtrlFolder, item.PrimaryModelID, item.PrimaryModelBody);

                                    if(!item.PrimaryModelID.Equals("0000"))
                                    {
                                        minionsDict.Add(item);
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            return minionsDict;
        }

        /// <summary>
        /// Creates a list of Mounts contained in mount_(num)_(language).exd 
        /// </summary>
        /// <returns>List<Items> Items:Item data associated with Mount</returns>
        public static SortedSet<ItemData> MakeMountsList()
        {
            string mountFile = String.Format(Strings.MountFile, Strings.Language);

            byte[] mountBytes = Helper.GetDecompressedEXDData(Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(mountFile)));
            byte[] modelchara = Helper.GetDecompressedEXDData(Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(Strings.ModelCharaFile)));

            SortedSet<ItemData> mountsDict = new SortedSet<ItemData>();

            using (BinaryReader br = new BinaryReader(new MemoryStream(mountBytes)))
            {
                using (BinaryReader br1 = new BinaryReader(new MemoryStream(modelchara)))
                {
                    br.ReadBytes(8);
                    int offsetTableSize = BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0);

                    br1.ReadBytes(8);
                    int offsetTableSize1 = BitConverter.ToInt32(br1.ReadBytes(4).Reverse().ToArray(), 0);

                    for (int i = 0; i < offsetTableSize; i += 8)
                    {
                        br.BaseStream.Seek(i + 32, SeekOrigin.Begin);
                        int index = BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0);
                        int tableOffset = BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0);

                        br.BaseStream.Seek(tableOffset, SeekOrigin.Begin);

                        br.ReadBytes(13);

                        int firstText = br.ReadByte();

                        if (firstText >= 2)
                        {
                            ItemData item = new ItemData();

                            br.ReadBytes(70);

                            uint modelIndex = BitConverter.ToUInt16(br.ReadBytes(2).Reverse().ToArray(), 0);

                            br.ReadBytes(40);

                            item.ItemName = Helper.ToTitleCase(Encoding.UTF8.GetString(br.ReadBytes(firstText - 1)).Replace("\0", ""));
                            item.ItemCategory = Strings.Mount_Category;

                            for (int j = 0; j < offsetTableSize1; j += 8)
                            {
                                br1.BaseStream.Seek(j + 32, SeekOrigin.Begin);

                                uint index1 = BitConverter.ToUInt32(br1.ReadBytes(4).Reverse().ToArray(), 0);
                                if (index1 == modelIndex)
                                {
                                    int tableOffset1 = BitConverter.ToInt32(br1.ReadBytes(4).Reverse().ToArray(), 0);

                                    br1.BaseStream.Seek(tableOffset1, SeekOrigin.Begin);

                                    br1.ReadBytes(6);

                                    item.PrimaryModelID = BitConverter.ToInt16(br1.ReadBytes(2).Reverse().ToArray(), 0).ToString().PadLeft(4, '0');
                                    br1.ReadBytes(3);
                                    item.PrimaryModelBody = br1.ReadByte().ToString().PadLeft(4, '0');
                                    item.PrimaryModelVariant = br1.ReadByte().ToString();

                                    if (item.PrimaryModelID.Equals("0001") || item.PrimaryModelID.Equals("0002") || item.PrimaryModelID.Equals("1011") || item.PrimaryModelID.Equals("1022"))
                                    {
                                        item.PrimaryMTRLFolder = String.Format(Strings.DemiMtrlFolder, item.PrimaryModelID, item.PrimaryModelBody);
                                    }
                                    else
                                    {
                                        item.PrimaryMTRLFolder = String.Format(Strings.MonsterMtrlFolder, item.PrimaryModelID, item.PrimaryModelBody);
                                    }

                                    mountsDict.Add(item);

                                    break;
                                }
                            }
                        }
                    }
                }
            }
            return mountsDict;
        }

        /// <summary>
        /// Creates a list of Items contained in item_(num)_(language).exd 
        /// </summary>
        /// <returns>Dictionary<string, Items> String:Item Name  Items:Item data associated with Item</returns>
        public static List<ItemData> MakeItemsList()
        {
            List<ItemData> itemsDict = new List<ItemData>();
            List<int> itemOffsetList = new List<int>();

            var smallClothesMTRL = "chara/equipment/e0000/material/v";

            //smallclothes are not in the item list, so they are added manualy
            ItemData item = new ItemData()
            {
                ItemName = "SmallClothes Body",
                ItemCategory = "4",
                PrimaryModelID = "0000",
                PrimaryModelVariant = "1",
                PrimaryMTRLFolder = smallClothesMTRL
            };
            itemsDict.Add(item);

            item = new ItemData()
            {
                ItemName = "SmallClothes Legs",
                ItemCategory = "7",
                PrimaryModelID = "0000",
                PrimaryModelVariant = "1",
                PrimaryMTRLFolder = smallClothesMTRL
            };
            itemsDict.Add(item);

            item = new ItemData()
            {
                ItemName = "SmallClothes Feet",
                ItemCategory = "8",
                PrimaryModelID = "0000",
                PrimaryModelVariant = "1",
                PrimaryMTRLFolder = smallClothesMTRL
            };
            itemsDict.Add(item);

            //searches item files which increase in increments of 500 in 0a0000 index until one does not exist
            for (int i = 0; ; i += 500)
            {
                string itemExd = String.Format(Strings.itemFile, i, Strings.Language);

                int offset = Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(itemExd));

                if(offset != 0)
                {
                    itemOffsetList.Add(offset);
                }
                else
                {
                    break;
                }
            }

            foreach(int offset in itemOffsetList)
            {
                using (BinaryReader br = new BinaryReader(new MemoryStream(Helper.GetDecompressedEXDData(offset))))
                {
                    br.ReadBytes(8);
                    int offsetTableSize = BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0);

                    for (int i = 0; i < offsetTableSize; i += 8)
                    {
                        br.BaseStream.Seek(i + 32, SeekOrigin.Begin);
                        int index = BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0);
                        int tableOffset = BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0);

                        br.BaseStream.Seek(tableOffset, SeekOrigin.Begin);
                        int entrySize = BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0);
                        br.ReadBytes(16);
                        int lastText = BitConverter.ToInt16(br.ReadBytes(2).Reverse().ToArray(), 0);
                        br.ReadBytes(3);

                        if (lastText > 10)
                        {
                            item = new ItemData();

                            bool hasSecondary = false;
                            br.ReadBytes(7);
                            byte[] textureDetails = br.ReadBytes(4).ToArray();
                            int itemCheck = textureDetails[3];

                            if (itemCheck != 0)
                            {
                                int weaponCheck = textureDetails[1];
                                if (weaponCheck == 0)
                                {
                                    item.PrimaryModelVariant = textureDetails[3].ToString().PadLeft(2, '0');
                                }
                                else
                                {
                                    item.PrimaryModelVariant = weaponCheck.ToString().PadLeft(2, '0');
                                    item.PrimaryModelBody = textureDetails[3].ToString().PadLeft(4, '0');
                                }

                                item.PrimaryModelID = BitConverter.ToInt16(br.ReadBytes(2).Reverse().ToArray(), 0).ToString().PadLeft(4, '0');
                                br.ReadBytes(2);

                                textureDetails = br.ReadBytes(4).ToArray();
                                int secondaryCheck = textureDetails[3];
                                if (secondaryCheck != 0)
                                {
                                    hasSecondary = true;
                                    weaponCheck = textureDetails[1];
                                    if (weaponCheck == 0)
                                    {
                                        item.SecondaryModelVariant = textureDetails[3].ToString().PadLeft(2, '0');
                                    }
                                    else
                                    {
                                        item.SecondaryModelVariant = weaponCheck.ToString().PadLeft(2, '0');
                                        item.SecondaryModelBody = textureDetails[3].ToString().PadLeft(4, '0');
                                    }

                                    item.SecondaryModelID = BitConverter.ToInt16(br.ReadBytes(2).Reverse().ToArray(), 0).ToString().PadLeft(4, '0');
                                    br.ReadBytes(2);
                                }

                                if (!hasSecondary)
                                {
                                    br.ReadBytes(110);
                                }
                                else
                                {
                                    br.ReadBytes(106);
                                }

                                byte[] slotBytes = br.ReadBytes(4).ToArray();
                                item.ItemCategory = slotBytes[0].ToString();

                                br.ReadBytes(lastText);

                                item.ItemName = Encoding.UTF8.GetString(br.ReadBytes(entrySize - (lastText + 152))).Replace("\0", "");


                                if (item.ItemCategory.Equals("0") || item.ItemCategory.Equals("1") || item.ItemCategory.Equals("2") || item.ItemCategory.Equals("13") || item.ItemCategory.Equals("14"))
                                {
                                    item.PrimaryMTRLFolder = String.Format(Strings.WeapMtrlFolder, item.PrimaryModelID, item.PrimaryModelBody);
                                    if (hasSecondary)
                                    {
                                        item.SecondaryMTRLFolder = String.Format(Strings.WeapMtrlFolder, item.SecondaryModelID, item.SecondaryModelBody);
                                    }
                                }
                                else if (item.ItemCategory.Equals("9") || item.ItemCategory.Equals("10") || item.ItemCategory.Equals("11") || item.ItemCategory.Equals("12"))
                                {
                                    item.PrimaryMTRLFolder = String.Format(Strings.AccMtrlFolder, item.PrimaryModelID);
                                }
                                else
                                {
                                    item.PrimaryMTRLFolder = String.Format(Strings.EquipMtrlFolder, item.PrimaryModelID);
                                }

                                try
                                {
                                    itemsDict.Add(item);
                                }
                                catch (Exception e)
                                {
                                    Debug.WriteLine("EXD_MakeItemListError " + e);
                                }
                            }

                        }
                    }

                }
            }

            return itemsDict;
        }
    }
}
