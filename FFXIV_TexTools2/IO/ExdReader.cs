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
        /// Makes the list of minions 
        /// </summary>
        /// <param name="offset">The offset where the minion list is located</param>
        public static Dictionary<string, Items> MakeMinionsList()
        {
            string minionFile = String.Format(Strings.MinionFile, Strings.Language);

            byte[] minionsBytes = Helper.GetDecompressedBytes(Helper.GetAOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(minionFile)));
            byte[] modelchara = Helper.GetDecompressedBytes(Helper.GetAOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(Strings.ModelCharaFile)));

            Dictionary<string, Items> minionsDict = new Dictionary<string, Items>();

            using (BinaryReader br = new BinaryReader(new MemoryStream(minionsBytes)))
            {
                using (BinaryReader br1 = new BinaryReader(new MemoryStream(modelchara)))
                {

                    int duplicateCount = 1;
                    br.ReadBytes(8);
                    int offsetTableSize = BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0);

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
                            br.ReadBytes(8);

                            uint modelIndex = BitConverter.ToUInt16(br.ReadBytes(2).Reverse().ToArray(), 0);

                            br.ReadBytes(30);

                            byte[] minionNameBytes = br.ReadBytes(firstText - 1);
                            string minionName = Encoding.UTF8.GetString(minionNameBytes);
                            minionName = minionName.Replace("\0", "");

                            br1.ReadBytes(8);
                            int offsetTableSize1 = BitConverter.ToInt32(br1.ReadBytes(4).Reverse().ToArray(), 0);

                            for (int j = 0; j < offsetTableSize1; j += 8)
                            {
                                br1.BaseStream.Seek(j + 32, SeekOrigin.Begin);

                                uint index1 = BitConverter.ToUInt32(br1.ReadBytes(4).Reverse().ToArray(), 0);

                                if (index1 == modelIndex)
                                {
                                    int tableOffset1 = BitConverter.ToInt32(br1.ReadBytes(4).Reverse().ToArray(), 0);

                                    br1.BaseStream.Seek(tableOffset1, SeekOrigin.Begin);

                                    br1.ReadBytes(6);

                                    int model = BitConverter.ToInt16(br1.ReadBytes(2).Reverse().ToArray(), 0);
                                    br1.ReadBytes(3);
                                    int body = br1.ReadByte();
                                    int variant = br1.ReadByte();

                                    string minionMtrlFolder = string.Format(Strings.MonsterMtrlFolder, model.ToString().PadLeft(4, '0'), body.ToString().PadLeft(4, '0'));

                                    Items item = new Items(Helper.ToTitleCase(minionName), model.ToString(), "", "24", variant.ToString(), "", body.ToString().PadLeft(4, '0'), "", false, minionMtrlFolder, "");

                                    if(model != 0)
                                    {
                                        try
                                        {
                                            minionsDict.Add(Helper.ToTitleCase(minionName), item);
                                        }
                                        catch
                                        {
                                            minionsDict.Add(Helper.ToTitleCase(minionName) + "_" + duplicateCount, item);
                                            duplicateCount++;
                                        }
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
        /// Makes the list of mounts
        /// </summary>
        /// <param name="offset">Offset where the mounts are located</param>
        public static Dictionary<string, Items> MakeMountsList()
        {
            string mountFile = String.Format(Strings.MountFile, Strings.Language);

            byte[] mountBytes = Helper.GetDecompressedBytes(Helper.GetAOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(mountFile)));
            byte[] modelchara = Helper.GetDecompressedBytes(Helper.GetAOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(Strings.ModelCharaFile)));

            Dictionary<string, Items> mountsDict = new Dictionary<string, Items>();

            using (BinaryReader br = new BinaryReader(new MemoryStream(mountBytes)))
            {
                using (BinaryReader br1 = new BinaryReader(new MemoryStream(modelchara)))
                {
                    br.ReadBytes(8);
                    int offsetTableSize = BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0);

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
                            br.ReadBytes(70);

                            uint modelIndex = BitConverter.ToUInt16(br.ReadBytes(2).Reverse().ToArray(), 0);

                            br.ReadBytes(40);

                            byte[] mountNameBytes = br.ReadBytes(firstText - 1);
                            string mountName = Encoding.UTF8.GetString(mountNameBytes);
                            mountName = mountName.Replace("\0", "");

                            br1.ReadBytes(8);
                            int offsetTableSize1 = BitConverter.ToInt32(br1.ReadBytes(4).Reverse().ToArray(), 0);

                            for (int j = 0; j < offsetTableSize1; j += 8)
                            {
                                br1.BaseStream.Seek(j + 32, SeekOrigin.Begin);

                                uint index1 = BitConverter.ToUInt32(br1.ReadBytes(4).Reverse().ToArray(), 0);
                                if (index1 == modelIndex)
                                {
                                    int tableOffset1 = BitConverter.ToInt32(br1.ReadBytes(4).Reverse().ToArray(), 0);

                                    br1.BaseStream.Seek(tableOffset1, SeekOrigin.Begin);

                                    br1.ReadBytes(6);

                                    int model = BitConverter.ToInt16(br1.ReadBytes(2).Reverse().ToArray(), 0);
                                    br1.ReadBytes(3);
                                    int body = br1.ReadByte();
                                    int variant = br1.ReadByte();

                                    string mountMtrlFolder;

                                    if (model == 1 || model == 2 || model == 1011 || model == 1022)
                                    {
                                        mountMtrlFolder = String.Format(Strings.DemiMtrlFolder, model.ToString().PadLeft(4, '0'), body.ToString().PadLeft(4, '0'));
                                    }
                                    else
                                    {
                                        mountMtrlFolder = String.Format(Strings.MonsterMtrlFolder, model.ToString().PadLeft(4, '0'), body.ToString().PadLeft(4, '0'));

                                    }

                                    Items item = new Items(Helper.ToTitleCase(mountName), model.ToString(), "", "23", variant.ToString(), "", body.ToString().PadLeft(4, '0'), "", false, mountMtrlFolder, "");

                                    if (!mountsDict.ContainsKey(Helper.ToTitleCase(mountName)))
                                    {
                                        mountsDict.Add(Helper.ToTitleCase(mountName), item);
                                    }

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
        /// Makes the list of items
        /// </summary>
        /// <param name="offset">Offset of items list</param>
        public static Dictionary<string, Items> MakeItemsList()
        {
            List<byte> byteList = new List<byte>();
            Dictionary<string, Items> itemsDict = new Dictionary<string, Items>();
            List<int> itemOffsetList = new List<int>();

            //the smallclothes are not on the item list, so they are added manualy

            itemsDict.Add("SmallClothes Body", new Items("SmallClothes Body", "0000", "0000", "4", "1", "1", "", "", false, "chara/equipment/e0000/material/v", ""));

            itemsDict.Add("SmallClothes Legs", new Items("SmallClothes Legs", "0000", "0000", "7", "1", "1", "", "", false, "chara/equipment/e0000/material/v", ""));

            itemsDict.Add("SmallClothes Feet", new Items("SmallClothes Feet", "0000", "0000", "8", "1", "1", "", "", false, "chara/equipment/e0000/material/v", ""));

            for (int i = 0; ; i += 500)
            {
                string itemExd = String.Format(Strings.itemFile, i, Strings.Language);


                int offset = Helper.GetAOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(itemExd));

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
                using (BinaryReader br = new BinaryReader(new MemoryStream(Helper.GetDecompressedBytes(offset))))
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

                        string imcVersion = "0", imcVersion1 = "0", weaponBody = "0", weaponBody1 = "0", itemID = "0", itemID1 = "0";
                        if (lastText > 10)
                        {
                            bool hasSecondary = false;
                            br.ReadBytes(7);
                            byte[] textureDetails = br.ReadBytes(4).ToArray();
                            int itemCheck = textureDetails[3];
                            //if item has textureDetails
                            if (itemCheck != 0)
                            {
                                int weaponCheck = textureDetails[1];
                                if (weaponCheck == 0)
                                {
                                    //not a weapon
                                    imcVersion = textureDetails[3].ToString().PadLeft(2, '0');
                                }
                                else
                                {
                                    //is a weapon
                                    imcVersion = weaponCheck.ToString().PadLeft(2, '0');
                                    weaponBody = textureDetails[3].ToString().PadLeft(4, '0');
                                }

                                itemID = BitConverter.ToInt16(br.ReadBytes(2).Reverse().ToArray(), 0).ToString().PadLeft(4, '0');
                                br.ReadBytes(2);

                                textureDetails = br.ReadBytes(4).ToArray();
                                int secondaryCheck = textureDetails[3];
                                if (secondaryCheck != 0)
                                {
                                    //Secondary textureDetails
                                    hasSecondary = true;
                                    weaponCheck = textureDetails[1];
                                    if (weaponCheck == 0)
                                    {
                                        //not a weapon
                                        imcVersion1 = textureDetails[3].ToString().PadLeft(2, '0');
                                    }
                                    else
                                    {
                                        //is a weapon
                                        imcVersion1 = weaponCheck.ToString().PadLeft(2, '0');
                                        weaponBody1 = textureDetails[3].ToString().PadLeft(4, '0');
                                    }

                                    itemID1 = BitConverter.ToInt16(br.ReadBytes(2).Reverse().ToArray(), 0).ToString().PadLeft(4, '0');
                                    br.ReadBytes(2);
                                }

                                if (!hasSecondary)
                                {
                                    br.ReadBytes(118);
                                }
                                else
                                {
                                    br.ReadBytes(114);
                                }


                                byte[] slotBytes = br.ReadBytes(4).ToArray();
                                string equipSlot = slotBytes[0].ToString();

                                br.ReadBytes(lastText);

                                byte[] itemNameBytes = br.ReadBytes(entrySize - (lastText + 160));
                                string itemName = Encoding.UTF8.GetString(itemNameBytes);
                                itemName = itemName.Replace("\0", "");

                                string itemMtrlFolder;
                                string sItemMtrlFolder = "";


                                if (equipSlot.Equals("0") || equipSlot.Equals("1") || equipSlot.Equals("2") || equipSlot.Equals("13") || equipSlot.Equals("14"))
                                {
                                    itemMtrlFolder = String.Format(Strings.WeapMtrlFolder, itemID, weaponBody);
                                    if (hasSecondary)
                                    {
                                        sItemMtrlFolder = String.Format(Strings.WeapMtrlFolder, itemID1, weaponBody1);
                                    }
                                }
                                else if (equipSlot.Equals("9") || equipSlot.Equals("10") || equipSlot.Equals("11") || equipSlot.Equals("12"))
                                {
                                    itemMtrlFolder = String.Format(Strings.AccMtrlFolder, itemID);
                                }
                                else
                                {
                                    itemMtrlFolder = String.Format(Strings.EquipMtrlFolder, itemID);
                                }


                                Items items = new Items(itemName, itemID, itemID1, equipSlot, imcVersion, imcVersion1, weaponBody, weaponBody1, hasSecondary, itemMtrlFolder, sItemMtrlFolder);

                                try
                                {
                                    itemsDict.Add(itemName, items);
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
