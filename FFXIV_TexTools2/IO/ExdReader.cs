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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using TreeNode = FFXIV_TexTools2.Model.TreeNode;

namespace FFXIV_TexTools2.IO
{
    public static class ExdReader
    {
        static TreeNode itemIconNode = new TreeNode() { Name = Strings.Items };

        public static Dictionary<string, List<TreeNode>> GetEXDData()
        {
            var itemList = MakeItemsList();

            if (itemList != null)
            {
                Dictionary<string, List<TreeNode>> exdDict = new Dictionary<string, List<TreeNode>>
                {
                    { Strings.Gear, itemList },
                    { Strings.Character, MakeCharacterList() },
                    { Strings.Companions, MakeCompanionsList() },
                    { "UI", MakeUIList() }
                };

                return exdDict;
            }

            return null;
        }

        private static List<TreeNode> MakeCharacterList()
        {
            List<TreeNode> nodeList = new List<TreeNode>();

            foreach(var charCategory in Info.CharCategoryList)
            {
                nodeList.Add(new TreeNode() { Name = charCategory, ItemData = new ItemData() { ItemName = charCategory, ItemCategory = "25" } });
            }

            return nodeList;
        }

        private static List<TreeNode> MakeCompanionsList()
        {
            List<TreeNode> companionNodes = new List<TreeNode>()
            {
                MakeMinionsList(), MakeMountsList(), MakePetsList()
            };

            return companionNodes;
        }

        private static List<TreeNode> MakeUIList()
        {
            List<TreeNode> UINodes = new List<TreeNode>()
            {
                MakeMapsList(), MakeActionsList(), itemIconNode, MakeStatusList(), MakeHUDList(), MakeLoadingImageList(), MakeMapSymbolList(), MakeOnlineStatusList(), MakeWeatherList()
            };

            return UINodes;
        }

        private static TreeNode MakePetsList()
        {
            TreeNode petNode = new TreeNode() { Name = Strings.Pets };
            List<string> petNames = new List<string>();

            var petFile = String.Format(Strings.PetEXD, Strings.Language);
            byte[] petBytes;
            try
            {
                petBytes =
                    Helper.GetDecompressedEXDData(
                        Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(petFile)), petFile);
            }
            catch (Exception e)
            {
                FlexibleMessageBox.Show("Error reading Pets EXD \nYou may have an older or unsupported version of the game.\n\n"
                                        + e.Message, "EXDReader (MakePetsList) Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            using (BinaryReader br = new BinaryReader(new MemoryStream(petBytes)))
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

                    br.ReadBytes(18);

                    var nameSize = (entrySize - 16);

                    if(nameSize > 2)
                    {
                        var petName = Encoding.UTF8.GetString(br.ReadBytes((entrySize - 18) + 2)).Replace("\0", "");

                        if (petName.Contains(Strings.Carbuncle))
                        {
                            petName = Strings.Carbuncle;
                        }

                        if (!petNames.Contains(petName))
                        {
                            petNames.Add(petName);
                            string petID = "";

                            if (Properties.Settings.Default.Language.Equals("ko"))
                            {
                                petID = Info.petIDKO[petName];
                            }
                            else if (Properties.Settings.Default.Language.Equals("zh"))
                            {
                                petID = Info.petIDCHS[petName];
                            }
                            else
                            {
                                petID = Info.petID[petName];
                            }

                            ItemData item = new ItemData()
                            {
                                ItemName = petName,
                                PrimaryModelID = petID,
                                PrimaryModelBody = "1",
                                PrimaryModelVariant = "1",
                                ItemCategory = "22"
                            };

                            if (petName.Equals(Strings.Selene) || petName.Equals(Strings.Bishop_Autoturret))
                            {
                                item.PrimaryModelBody = "2";
                            }

                            TreeNode itemNode = new TreeNode() { Name = petName, ItemData = item };

                            petNode._subNode.Add(itemNode);


                            for (int j = 2; j < 11; j++)
                            {
                                var MTRLFolder = String.Format(Strings.MonsterMtrlFolder, item.PrimaryModelID, item.PrimaryModelBody.ToString().PadLeft(4, '0')) + j.ToString().PadLeft(4, '0');

                                if (Helper.FolderExists(FFCRC.GetHash(MTRLFolder), Strings.ItemsDat))
                                {
                                    ItemData item2 = new ItemData()
                                    {
                                        ItemName = petName + " " + j,
                                        PrimaryModelID = petID,
                                        PrimaryModelBody = "1",
                                        PrimaryModelVariant = j.ToString(),
                                        ItemCategory = "22"
                                    };

                                    TreeNode itemNode2 = new TreeNode() { Name = item2.ItemName, ItemData = item2 };

                                    petNode._subNode.Add(itemNode2);
                                }
                            }
                        }

                    }
                }

                ItemData extraItem = new ItemData()
                {
                    ItemName = Strings.Ramuh_Egi,
                    PrimaryModelID = Info.petID[Strings.Ramuh_Egi],
                    PrimaryModelBody = "1",
                    PrimaryModelVariant = "1",
                    ItemCategory = "22"
                };

                TreeNode extraItemNode = new TreeNode() { Name = extraItem.ItemName, ItemData = extraItem };

                petNode._subNode.Add(extraItemNode);

                extraItem = new ItemData()
                {
                    ItemName = Strings.Sephirot_Egi,
                    PrimaryModelID = Info.petID[Strings.Sephirot_Egi],
                    PrimaryModelBody = "1",
                    PrimaryModelVariant = "1",
                    ItemCategory = "22"
                };

                extraItemNode = new TreeNode() { Name = extraItem.ItemName, ItemData = extraItem };

                petNode._subNode.Add(extraItemNode);

                extraItem = new ItemData()
                {
                    ItemName = Strings.Bahamut_Egi,
                    PrimaryModelID = Info.petID[Strings.Bahamut_Egi],
                    PrimaryModelBody = "1",
                    PrimaryModelVariant = "1",
                    ItemCategory = "22"
                };

                extraItemNode = new TreeNode() { Name = extraItem.ItemName, ItemData = extraItem };

                petNode._subNode.Add(extraItemNode);

                extraItem = new ItemData()
                {
                    ItemName = Strings.Placeholder_Egi,
                    PrimaryModelID = Info.petID[Strings.Placeholder_Egi],
                    PrimaryModelBody = "1",
                    PrimaryModelVariant = "1",
                    ItemCategory = "22"
                };

                extraItemNode = new TreeNode() { Name = extraItem.ItemName, ItemData = extraItem };

                petNode._subNode.Add(extraItemNode);

                return petNode;
            }
        }


        /// <summary>
        /// Creates a list of minions contained in companion_(num)_(language).exd 
        /// </summary>
        /// <returns>List<Items> Items:Item data associated with Minion</returns>
        public static TreeNode MakeMinionsList()
        {
            string minionFile = String.Format(Strings.MinionFile, Strings.Language);

            byte[] minionsBytes;
            byte[] modelChara;
            try
            {
                minionsBytes = Helper.GetDecompressedEXDData(Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(minionFile)), minionFile);
                modelChara = Helper.GetDecompressedEXDData(Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(Strings.ModelCharaFile)), Strings.ModelCharaFile);
            }
            catch (Exception e)
            {
                FlexibleMessageBox.Show("Error reading Minions EXD \nYou may have an older or unsupported version of the game.\n\n"
                                        + e.Message, "EXDReader (MakeMinionsList) Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            TreeNode minionNode = new TreeNode() { Name = Strings.Minions};

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
                                        var itemNode = new TreeNode() { Name = item.ItemName, ItemData = item };
                                        minionNode._subNode.Add(itemNode);
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            minionNode._subNode.Sort();
            return minionNode;
        }

        public static TreeNode MakeMapsList()
        {
            string placeNameFile = String.Format(Strings.PlaceName, Strings.Language);

            byte[] placeNameBytes = Helper.GetDecompressedEXDData(Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(placeNameFile)), placeNameFile);
            byte[] mapBytes = Helper.GetDecompressedEXDData(Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(Strings.MapExd)), Strings.MapExd);

            //SortedSet<ItemData> mapList = new SortedSet<ItemData>();
            TreeNode mapNode = new TreeNode() { Name = Strings.Maps };

            Dictionary<string, TreeNode> regionNodes = new Dictionary<string, TreeNode>();

            using (BinaryReader br = new BinaryReader(new MemoryStream(mapBytes)))
            {
                using (BinaryReader br1 = new BinaryReader(new MemoryStream(placeNameBytes)))
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

                        int entrySize = BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0);

                        br.ReadBytes(14);

                        int regionIndex = BitConverter.ToInt16(br.ReadBytes(2).Reverse().ToArray(), 0);
                        int mapIndex = BitConverter.ToInt16(br.ReadBytes(2).Reverse().ToArray(), 0);

                        if(mapIndex > 1)
                        {
                            br.ReadBytes(16);

                            ItemData item = new ItemData();

                            byte[] mapPathNameBytes = br.ReadBytes(entrySize - 32);
                            if(mapPathNameBytes.Length > 2)
                            {
                                item.UIPath = Encoding.UTF8.GetString(mapPathNameBytes).Replace("\0", "");
                                item.ItemCategory = Strings.Maps;
                                int mapNum = int.Parse(item.UIPath.Substring(item.UIPath.LastIndexOf("/") + 1, 2));

                                bool mName = false;
                                bool rName = false;

                                TreeNode itemNode = null;

                                for (int j = 0; j < offsetTableSize1; j += 8)
                                {
                                    br1.BaseStream.Seek(j + 32, SeekOrigin.Begin);

                                    uint index1 = BitConverter.ToUInt32(br1.ReadBytes(4).Reverse().ToArray(), 0);

                                    if (index1 == mapIndex)
                                    {
                                        int tableOffset1 = BitConverter.ToInt32(br1.ReadBytes(4).Reverse().ToArray(), 0);

                                        br1.BaseStream.Seek(tableOffset1, SeekOrigin.Begin);

                                        br1.ReadBytes(12);

                                        int nameStringOffset = BitConverter.ToInt16(br1.ReadBytes(2).Reverse().ToArray(), 0);

                                        if (nameStringOffset > 1)
                                        {
                                            br1.ReadBytes(16);

                                            byte[] mapNameBytes = br1.ReadBytes(nameStringOffset);
                                            if (mapNum > 0)
                                            {
                                                item.ItemName = Helper.ToTitleCase((Encoding.UTF8.GetString(mapNameBytes)).Replace("\0", "")) + " [" + mapNum + "]";
                                            }
                                            else
                                            {
                                                item.ItemName = Helper.ToTitleCase((Encoding.UTF8.GetString(mapNameBytes)).Replace("\0", ""));
                                            }
                                        }
                                        else
                                        {
                                            item.ItemName = item.UIPath;
                                        }

                                        itemNode = new TreeNode() { Name = item.ItemName };
                                        mName = true;
                                    }
                                    else if(index1 == regionIndex)
                                    {
                                        int tableOffset1 = BitConverter.ToInt32(br1.ReadBytes(4).Reverse().ToArray(), 0);

                                        br1.BaseStream.Seek(tableOffset1, SeekOrigin.Begin);

                                        br1.ReadBytes(12);

                                        int nameStringOffset = BitConverter.ToInt16(br1.ReadBytes(2).Reverse().ToArray(), 0);

                                        if (nameStringOffset > 1)
                                        {
                                            br1.ReadBytes(16);

                                            byte[] mapNameBytes = br1.ReadBytes(nameStringOffset);

                                            item.ItemSubCategory = Helper.ToTitleCase((Encoding.UTF8.GetString(mapNameBytes)).Replace("\0", ""));

                                            if (!regionNodes.ContainsKey(item.ItemSubCategory))
                                            {
                                                regionNodes.Add(item.ItemSubCategory, new TreeNode() { Name = item.ItemSubCategory });
                                            }
                                        }
                                        rName = true;
                                    }
                                    
                                    if(mName && rName)
                                    {
                                        itemNode.ItemData = item;
                                        regionNodes[item.ItemSubCategory]._subNode.Add(itemNode);
                                        break;
                                    }

                                }
                            }
                        }
                    }
                }
            }

            foreach(var regionNode in regionNodes.Values)
            {
                regionNode._subNode.Sort();
                mapNode._subNode.Add(regionNode);
            }

            mapNode._subNode.Sort();

            return mapNode;
        }

        public static TreeNode MakeHUDList()
        {
            var uldList = Properties.Resources.uldpaths.Split(new string[] { Environment.NewLine, "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

            var uldFolder = "ui/uld/";

            TreeNode HUDNode = new TreeNode() { Name = "HUD" };
            Dictionary<string, TreeNode> HUDDict = new Dictionary<string, TreeNode>();

            foreach (var uld in uldList)
            {
                ItemData item = new ItemData();

                item.ItemCategory = "HUD";
                if (uld.ToLower().Contains("subtitle"))
                {
                    item.ItemSubCategory = "Subtitles";
                }
                else if (uld.ToLower().Contains("title"))
                {
                    item.ItemSubCategory = "Titles";
                }
                else if (uld.ToLower().Contains("window"))
                {
                    item.ItemSubCategory = "Windows";
                }
                else if (uld.ToLower().Contains("gcarmy"))
                {
                    item.ItemSubCategory = "GC Army";
                }
                else if (uld.ToLower().Contains("job"))
                {
                    item.ItemSubCategory = "Jobs";
                }
                else
                {
                    item.ItemSubCategory = "Other";
                }


                item.UIPath = uldFolder + uld.ToLower();
                item.ItemName = Path.GetFileNameWithoutExtension(item.UIPath);
                TreeNode itemNode = new TreeNode() { Name = item.ItemName, ItemData = item };


                if (HUDDict.ContainsKey(item.ItemSubCategory))
                {
                    HUDDict[item.ItemSubCategory]._subNode.Add(itemNode);
                }
                else
                {
                    HUDDict.Add(item.ItemSubCategory, new TreeNode() { Name = item.ItemSubCategory, _subNode = { itemNode } });
                }
            }

            foreach(var i in HUDDict.Values)
            {
                HUDNode._subNode.Add(i);
            }

            return HUDNode;
        }

        public static TreeNode MakeLoadingImageList()
        {
            var loadingImageFolder = "ui/loadingimage/";
            var loadingImageFile = string.Format(Strings.LoadingImage, "");

            var folders = Helper.GetAllFilesInFolder(FFCRC.GetHash(loadingImageFolder.Substring(0, 15)), Strings.UIDat);

            TreeNode LINode = new TreeNode() { Name = "Loading Image" };

            for (int i = 0; i < 20; i++)
            {
                ItemData item = new ItemData();

                loadingImageFile = string.Format(Strings.LoadingImage, i.ToString().PadLeft(2, '0'));

                if (i == 0)
                {
                    loadingImageFile = string.Format(Strings.LoadingImage, "");
                }

                if (folders.Contains(FFCRC.GetHash(loadingImageFile)))
                {
                    item.UIPath = loadingImageFolder + loadingImageFile;
                    item.ItemName = Path.GetFileNameWithoutExtension(item.UIPath);
                    item.ItemCategory = "LoadingImage";
                    TreeNode itemNode = new TreeNode() { Name = item.ItemName, ItemData = item };
                    LINode._subNode.Add(itemNode);
                }
            }

            return LINode;
        }


        public static TreeNode MakeMapSymbolList()
        {
            TreeNode mapSymbolNode = new TreeNode() { Name = Strings.MapSymbol };

            string mapSymbolFile = "mapsymbol_0.exd";
            byte[] mapSymbolBytes = Helper.GetDecompressedEXDData(Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(mapSymbolFile)), mapSymbolFile);

            string placeNameFile = String.Format(Strings.PlaceName, Strings.Language);
            byte[] placeNameBytes = Helper.GetDecompressedEXDData(Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(placeNameFile)), placeNameFile);

            using (BinaryReader br = new BinaryReader(new MemoryStream(mapSymbolBytes)))
            {
                using (BinaryReader br1 = new BinaryReader(new MemoryStream(placeNameBytes)))
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

                        int entrySize = BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0);

                        br.ReadBytes(2);

                        int iconNum = BitConverter.ToUInt16(br.ReadBytes(4).Reverse().ToArray(), 0);

                        int pnIndex = BitConverter.ToUInt16(br.ReadBytes(4).Reverse().ToArray(), 0);

                        if (iconNum != 0)
                        {
                            ItemData item = new ItemData();

                            item.Icon = iconNum.ToString();

                            for (int j = 0; j < offsetTableSize1; j += 8)
                            {
                                br1.BaseStream.Seek(j + 32, SeekOrigin.Begin);

                                uint index1 = BitConverter.ToUInt32(br1.ReadBytes(4).Reverse().ToArray(), 0);

                                if (index1 == pnIndex)
                                {
                                    int tableOffset1 = BitConverter.ToInt32(br1.ReadBytes(4).Reverse().ToArray(), 0);

                                    br1.BaseStream.Seek(tableOffset1, SeekOrigin.Begin);

                                    br1.ReadBytes(12);

                                    int nameStringOffset = BitConverter.ToInt16(br1.ReadBytes(2).Reverse().ToArray(), 0);

                                    if (nameStringOffset > 1)
                                    {
                                        br1.ReadBytes(16);

                                        byte[] mapNameBytes = br1.ReadBytes(nameStringOffset);

                                        item.ItemName = Helper.ToTitleCase((Encoding.UTF8.GetString(mapNameBytes)).Replace("\0", ""));
                                        item.ItemCategory = Strings.MapSymbol;
                                        TreeNode itemNode = new TreeNode() { Name = item.ItemName, ItemData = item };
                                        mapSymbolNode._subNode.Add(itemNode);
                                    }
                                    break;
                                }
                            }
                        }

                    }
                    mapSymbolNode._subNode.Sort();
                    return mapSymbolNode;
                }

            }
        }


        public static TreeNode MakeOnlineStatusList()
        {
            TreeNode OnlineStatusNode = new TreeNode() { Name = Strings.OnlineStatus };

            string onlineStatusFile = String.Format(Strings.OnlineStatusEXD, Strings.Language);
            byte[] onlineStatusBytes = Helper.GetDecompressedEXDData(Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(onlineStatusFile)), onlineStatusFile);

            using (BinaryReader br = new BinaryReader(new MemoryStream(onlineStatusBytes)))
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

                    br.ReadBytes(6);

                    int iconNum = BitConverter.ToUInt16(br.ReadBytes(4).Reverse().ToArray(), 0);

                    if(iconNum != 0)
                    {
                        ItemData item = new ItemData();
                        item.Icon = iconNum.ToString();

                        br.ReadBytes(4);

                        byte[] osNameBytes = br.ReadBytes(entrySize - 12);

                        item.ItemName = Helper.ToTitleCase((Encoding.UTF8.GetString(osNameBytes)).Replace("\0", ""));
                        item.ItemCategory = Strings.OnlineStatus;
                        TreeNode itemNode = new TreeNode() { Name = item.ItemName, ItemData = item };
                        OnlineStatusNode._subNode.Add(itemNode);
                    }




                }
                OnlineStatusNode._subNode.Sort();
                return OnlineStatusNode;
            }
        }


        public static TreeNode MakeWeatherList()
        {
            TreeNode weatherNode = new TreeNode() { Name = Strings.Weather };

            string weatherFile = String.Format(Strings.WeatherEXD, Strings.Language);
            byte[] weatherBytes = Helper.GetDecompressedEXDData(Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(weatherFile)), weatherFile);

            List<int> weatherIcons = new List<int>();

            using (BinaryReader br = new BinaryReader(new MemoryStream(weatherBytes)))
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

                    br.ReadBytes(6);

                    int nameSize = BitConverter.ToUInt16(br.ReadBytes(4).Reverse().ToArray(), 0);

                    int iconNum = BitConverter.ToUInt16(br.ReadBytes(4).Reverse().ToArray(), 0);

                    if (iconNum != 0)
                    {
                        if (!weatherIcons.Contains(iconNum))
                        {
                            ItemData item = new ItemData();
                            item.Icon = iconNum.ToString();

                            byte[] wNameBytes = br.ReadBytes(nameSize);

                            item.ItemName = Helper.ToTitleCase((Encoding.UTF8.GetString(wNameBytes)).Replace("\0", ""));
                            item.ItemCategory = Strings.Weather;
                            TreeNode itemNode = new TreeNode() { Name = item.ItemName, ItemData = item };
                            weatherNode._subNode.Add(itemNode);
                            weatherIcons.Add(iconNum);
                        }

                    }
                }
                weatherNode._subNode.Sort();
                return weatherNode;
            }
        }


        public static TreeNode MakeActionsList()
        {
            var ActionCategoryDict = ActionCategory();

            TreeNode actionsNode = new TreeNode() { Name = Strings.Actions };

            string actionFile = String.Format(Strings.Action, Strings.Language);

            byte[] actionBytes = Helper.GetDecompressedEXDData(Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(actionFile)), actionFile);

            TreeNode actionNode = new TreeNode();

            using (BinaryReader br = new BinaryReader(new MemoryStream(actionBytes)))
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

                    br.ReadBytes(10);

                    int iconNum = BitConverter.ToUInt16(br.ReadBytes(2).Reverse().ToArray(), 0);

                    if(iconNum != 405)
                    {
                        if(iconNum != 0)
                        {
                            ItemData item = new ItemData();

                            item.Icon = iconNum.ToString();

                            br.ReadBytes(16);
                            var subcat = br.ReadByte();
                            if (subcat != 0)
                            {
                                try
                                {
                                    item.ItemSubCategory = ActionCategoryDict[subcat];
                                }
                                catch
                                {
                                    item.ItemSubCategory = ActionCategoryDict[0];
                                }
                            }
                            else
                            {
                                item.ItemSubCategory = "None";
                            }

                            actionNode.Name = item.ItemSubCategory;


                            br.ReadBytes(25);

                            var actionName = Encoding.UTF8.GetString(br.ReadBytes(entrySize - 52)).Replace("\0", "");

                            if(actionName.Length > 1)
                            {
                                item.ItemName = actionName;
                                item.ItemCategory = Strings.Actions;

                                TreeNode itemNode = new TreeNode() { Name = item.ItemName, ItemData = item };
                                actionNode._subNode.Add(itemNode);

                            }
                        }

                    }

                }
                actionNode._subNode.Sort();
                actionsNode._subNode.Add(actionNode);
            }

            actionFile = String.Format(Strings.GeneralAction, Strings.Language);
            actionBytes = Helper.GetDecompressedEXDData(Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(actionFile)), actionFile);

            TreeNode generalNode = new TreeNode() { Name = Strings.General };

            using (BinaryReader br = new BinaryReader(new MemoryStream(actionBytes)))
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

                    br.ReadBytes(9);

                    int stringSize = br.ReadByte();

                    br.ReadBytes(2);

                    int iconNum = BitConverter.ToUInt16(br.ReadBytes(2).Reverse().ToArray(), 0);

                    if (iconNum != 0)
                    {
                        ItemData item = new ItemData();

                        item.Icon = iconNum.ToString();

                        br.ReadBytes(8);

                        item.ItemSubCategory = Strings.General;

                        var actionName = Encoding.UTF8.GetString(br.ReadBytes(stringSize)).Replace("\0", "");

                        if (actionName.Length > 1)
                        {
                            item.ItemName = actionName;
                            item.ItemCategory = Strings.Actions;

                            TreeNode itemNode = new TreeNode() { Name = item.ItemName, ItemData = item };
                            generalNode._subNode.Add(itemNode);
                        }
                    }
                }

            }
            generalNode._subNode.Sort();
            actionsNode._subNode.Add(generalNode);


            actionFile = String.Format(Strings.BuddyAction, Strings.Language);
            actionBytes = Helper.GetDecompressedEXDData(Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(actionFile)), actionFile);

            TreeNode buddyNode = new TreeNode() { Name = Strings.Buddy };
            using (BinaryReader br = new BinaryReader(new MemoryStream(actionBytes)))
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

                    br.ReadBytes(9);

                    int stringSize = br.ReadByte();

                    br.ReadBytes(2);

                    int iconNum = BitConverter.ToUInt16(br.ReadBytes(2).Reverse().ToArray(), 0);

                    if (iconNum != 0)
                    {
                        ItemData item = new ItemData();

                        item.Icon = iconNum.ToString();

                        br.ReadBytes(8);

                        item.ItemSubCategory = Strings.Buddy;

                        var actionName = Encoding.UTF8.GetString(br.ReadBytes(stringSize)).Replace("\0", "");

                        if (actionName.Length > 1)
                        {
                            item.ItemName = actionName;
                            item.ItemCategory = Strings.Actions;
                            TreeNode itemNode = new TreeNode() { Name = item.ItemName, ItemData = item };

                            buddyNode._subNode.Add(itemNode);
                        }
                    }
                }
            }
            buddyNode._subNode.Sort();
            actionsNode._subNode.Add(buddyNode);

            actionFile = String.Format(Strings.CompanyAction, Strings.Language);
            actionBytes = Helper.GetDecompressedEXDData(Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(actionFile)), actionFile);

            TreeNode companyNode = new TreeNode() { Name = Strings.Company };

            using (BinaryReader br = new BinaryReader(new MemoryStream(actionBytes)))
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

                    br.ReadBytes(9);

                    int stringSize = br.ReadByte();

                    br.ReadBytes(6);

                    int iconNum = BitConverter.ToUInt16(br.ReadBytes(2).Reverse().ToArray(), 0);

                    if (iconNum != 0)
                    {
                        ItemData item = new ItemData();

                        item.Icon = iconNum.ToString();

                        br.ReadBytes(4);

                        item.ItemSubCategory = Strings.Company;

                        var actionName = Encoding.UTF8.GetString(br.ReadBytes(stringSize)).Replace("\0", "");

                        if (actionName.Length > 1)
                        {
                            item.ItemName = actionName;
                            item.ItemCategory = Strings.Actions;
                            TreeNode itemNode = new TreeNode() { Name = item.ItemName, ItemData = item };

                            companyNode._subNode.Add(itemNode);
                        }
                    }
                }
            }
            companyNode._subNode.Sort();
            actionsNode._subNode.Add(companyNode);

            actionFile = String.Format(Strings.CraftAction, Strings.Language);
            actionBytes = Helper.GetDecompressedEXDData(Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(actionFile)), actionFile);
            TreeNode craftNode = new TreeNode() { Name = Strings.Craft };

            using (BinaryReader br = new BinaryReader(new MemoryStream(actionBytes)))
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

                    br.ReadBytes(9);

                    int stringSize = br.ReadByte();

                    br.ReadBytes(40);

                    int iconNum = BitConverter.ToUInt16(br.ReadBytes(2).Reverse().ToArray(), 0);

                    if (iconNum != 0)
                    {
                        ItemData item = new ItemData();

                        item.Icon = iconNum.ToString();

                        br.ReadBytes(10);

                        item.ItemSubCategory = Strings.Craft;

                        var actionName = Encoding.UTF8.GetString(br.ReadBytes(stringSize)).Replace("\0", "");

                        if (actionName.Length > 1)
                        {
                            item.ItemName = actionName;
                            item.ItemCategory = Strings.Actions;
                            TreeNode itemNode = new TreeNode() { Name = item.ItemName, ItemData = item };

                            craftNode._subNode.Add(itemNode);
                        }
                    }
                }
            }
            craftNode._subNode.Sort();
            actionsNode._subNode.Add(craftNode);


            actionFile = String.Format(Strings.EventAction, Strings.Language);
            actionBytes = Helper.GetDecompressedEXDData(Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(actionFile)), actionFile);

            TreeNode eventNode = new TreeNode() { Name = Strings.Event };

            using (BinaryReader br = new BinaryReader(new MemoryStream(actionBytes)))
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

                    br.ReadBytes(6);

                    int iconNum = BitConverter.ToUInt16(br.ReadBytes(2).Reverse().ToArray(), 0);

                    if (iconNum != 0)
                    {
                        ItemData item = new ItemData();

                        item.Icon = iconNum.ToString();

                        br.ReadBytes(10);

                        item.ItemSubCategory = Strings.Event;

                        var actionName = Encoding.UTF8.GetString(br.ReadBytes(entrySize - 16)).Replace("\0", "");

                        if (actionName.Length > 1)
                        {
                            item.ItemName = actionName;
                            item.ItemCategory = Strings.Actions;
                            TreeNode itemNode = new TreeNode() { Name = item.ItemName, ItemData = item };

                            eventNode._subNode.Add(itemNode);
                        }
                    }
                }
            }
            eventNode._subNode.Sort();
            actionsNode._subNode.Add(eventNode);

            actionFile = String.Format(Strings.EmoteEXD, Strings.Language);
            actionBytes = Helper.GetDecompressedEXDData(Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(actionFile)), actionFile);

            TreeNode emoteNode = new TreeNode() { Name = Strings.Emote };

            using (BinaryReader br = new BinaryReader(new MemoryStream(actionBytes)))
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

                    br.ReadBytes(28);

                    int iconNum = BitConverter.ToUInt16(br.ReadBytes(2).Reverse().ToArray(), 0);

                    if (iconNum != 0)
                    {
                        ItemData item = new ItemData();

                        item.Icon = iconNum.ToString();

                        br.ReadBytes(8);

                        item.ItemSubCategory = Strings.Emote;

                        var actionName = Encoding.UTF8.GetString(br.ReadBytes(entrySize - 36)).Replace("\0", "");

                        if (actionName.Length > 1)
                        {
                            item.ItemName = actionName;
                            item.ItemCategory = Strings.Actions;
                            TreeNode itemNode = new TreeNode() { Name = item.ItemName, ItemData = item };

                            emoteNode._subNode.Add(itemNode);
                        }
                    }
                }
            }
            emoteNode._subNode.Sort();
            actionsNode._subNode.Add(emoteNode);

            actionFile = String.Format(Strings.MarkerEXD, Strings.Language);
            actionBytes = Helper.GetDecompressedEXDData(Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(actionFile)), actionFile);

            TreeNode markerNode = new TreeNode() { Name = Strings.Marker };

            using (BinaryReader br = new BinaryReader(new MemoryStream(actionBytes)))
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

                    br.ReadBytes(8);

                    int iconNum = BitConverter.ToUInt16(br.ReadBytes(2).Reverse().ToArray(), 0);

                    if (iconNum != 0)
                    {
                        ItemData item = new ItemData();

                        item.Icon = iconNum.ToString();

                        item.ItemSubCategory = Strings.Marker;

                        var actionName = Encoding.UTF8.GetString(br.ReadBytes(entrySize - 8)).Replace("\0", "");

                        if (actionName.Length > 1)
                        {
                            item.ItemName = actionName;
                            item.ItemCategory = Strings.Actions;
                            TreeNode itemNode = new TreeNode() { Name = item.ItemName, ItemData = item };

                            markerNode._subNode.Add(itemNode);
                        }

                        item = new ItemData();

                        item.Icon = (iconNum + 100).ToString();

                        item.ItemSubCategory = Strings.Marker;

                        actionName = actionName + " (Large)";

                        if (actionName.Length > 1)
                        {
                            item.ItemName = actionName;
                            item.ItemCategory = Strings.Actions;
                            TreeNode itemNode = new TreeNode() { Name = item.ItemName, ItemData = item };

                            markerNode._subNode.Add(itemNode);
                        }
                    }
                }
            }
            markerNode._subNode.Sort();
            actionsNode._subNode.Add(markerNode);


            actionFile = String.Format(Strings.FieldMarkerEXD, Strings.Language);
            actionBytes = Helper.GetDecompressedEXDData(Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(actionFile)), actionFile);

            TreeNode fieldMarkerNode = new TreeNode() { Name = Strings.FieldMarker };

            using (BinaryReader br = new BinaryReader(new MemoryStream(actionBytes)))
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

                    br.ReadBytes(10);

                    int iconNum = BitConverter.ToUInt16(br.ReadBytes(2).Reverse().ToArray(), 0);

                    br.ReadBytes(2);
                    if (iconNum != 0)
                    {
                        ItemData item = new ItemData();

                        item.Icon = iconNum.ToString();

                        item.ItemSubCategory = Strings.FieldMarker;

                        var actionName = Encoding.UTF8.GetString(br.ReadBytes(entrySize - 12)).Replace("\0", "");

                        if (actionName.Length > 1)
                        {
                            item.ItemName = actionName;
                            item.ItemCategory = Strings.Actions;
                            TreeNode itemNode = new TreeNode() { Name = item.ItemName, ItemData = item };

                            fieldMarkerNode._subNode.Add(itemNode);
                        }

                        if (!actionName.Contains("Clear"))
                        {
                            item = new ItemData();

                            item.Icon = (iconNum + 100).ToString();

                            item.ItemSubCategory = Strings.FieldMarker;

                            actionName = actionName + " (Large)";

                            if (actionName.Length > 1)
                            {
                                item.ItemName = actionName;
                                item.ItemCategory = Strings.Actions;
                                TreeNode itemNode = new TreeNode() { Name = item.ItemName, ItemData = item };

                                fieldMarkerNode._subNode.Add(itemNode);
                            }
                        }
                    }
                }
            }
            fieldMarkerNode._subNode.Sort();
            actionsNode._subNode.Add(fieldMarkerNode);

            return actionsNode;
        }

        public static TreeNode MakeStatusList()
        {
            var statusFile = String.Format(Strings.StatusExd, Strings.Language);
            var statusBytes = Helper.GetDecompressedEXDData(Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(statusFile)), statusFile);
            Dictionary<string, TreeNode> statusDict = new Dictionary<string, TreeNode>();

            TreeNode statusNode = new TreeNode() { Name = Strings.Status };

            using (BinaryReader br = new BinaryReader(new MemoryStream(statusBytes)))
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

                    br.ReadBytes(8);

                    int statusNameSize = BitConverter.ToUInt16(br.ReadBytes(2).Reverse().ToArray(), 0);

                    int statusIconNum = BitConverter.ToUInt16(br.ReadBytes(2).Reverse().ToArray(), 0);

                    br.ReadBytes(3);

                    int statusType = br.ReadByte();

                    br.ReadBytes(10);

                    if (statusIconNum != 0)
                    {
                        ItemData item = new ItemData();

                        item.Icon = statusIconNum.ToString();

                        if (statusType == 0)
                        {
                            item.ItemSubCategory = Strings.General;
                        }
                        else if (statusType == 1)
                        {
                            item.ItemSubCategory = Strings.Beneficial;
                        }
                        else if (statusType == 2)
                        {
                            item.ItemSubCategory = Strings.Detrimental;
                        }
                        else
                        {
                            item.ItemSubCategory = "Unknown";
                        }

                        var statusName = Encoding.UTF8.GetString(br.ReadBytes(statusNameSize)).Replace("\0", "");

                        if (statusName.Length > 1)
                        {
                            item.ItemName = statusName;
                            item.ItemCategory = Strings.Status;

                            TreeNode itemNode = new TreeNode() { Name = item.ItemName, ItemData = item };

                            if (statusDict.ContainsKey(item.ItemSubCategory))
                            {
                                statusDict[item.ItemSubCategory]._subNode.Add(itemNode);
                            }
                            else
                            {
                                statusDict.Add(item.ItemSubCategory, new TreeNode() { Name = item.ItemSubCategory, _subNode = { itemNode } });
                            }
                        }
                    }
                }
                foreach(var i in statusDict.Values)
                {
                    statusNode._subNode.Add(i);
                }
                return statusNode;
            }
        }


        /// <summary>
        /// Creates a list of Mounts contained in mount_(num)_(language).exd 
        /// </summary>
        /// <returns>List<Items> Items:Item data associated with Mount</returns>
        public static TreeNode MakeMountsList()
        {
            string mountFile = String.Format(Strings.MountFile, Strings.Language);

            byte[] mountBytes = Helper.GetDecompressedEXDData(Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(mountFile)), mountFile);
            byte[] modelchara = Helper.GetDecompressedEXDData(Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(Strings.ModelCharaFile)), Strings.ModelCharaFile);

            TreeNode mountNode = new TreeNode() { Name = Strings.Mounts };

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

                            br.ReadBytes(22);

                            uint modelIndex = BitConverter.ToUInt16(br.ReadBytes(2).Reverse().ToArray(), 0);

                            br.ReadBytes(44);

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

                                    var itemNode = new TreeNode() { Name = item.ItemName, ItemData = item };
                                    mountNode._subNode.Add(itemNode);

                                    break;
                                }
                            }
                        }
                    }
                }
            }
            mountNode._subNode.Sort();
            return mountNode;
        }

        public static Dictionary<int, string> ItemUICategory()
        {
            string itemUICatFile = String.Format(Strings.ItemUICategory, Strings.Language);

            byte[] itemUICatBytes;
            try
            {
                itemUICatBytes = Helper.GetDecompressedEXDData(
                    Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(itemUICatFile)), itemUICatFile);
            }
            catch (Exception e)
            {
                FlexibleMessageBox.Show("Error reading ItemUICategory EXD \nYou may have an older or unsupported version of the game.\n\n"
                                        + e.Message, "EXDReader (ItemUICategory) Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            Dictionary<int, string> UICategories = new Dictionary<int, string>();

            using (BinaryReader br = new BinaryReader(new MemoryStream(itemUICatBytes)))
            {
                br.ReadBytes(8);
                int offsetTableSize = 0;
                try
                {
                    offsetTableSize = BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0);
                }
                catch (Exception e)
                {
                    FlexibleMessageBox.Show("Error reading Offset Table Size for ItemUICategory\nYou may have an older or unsupported version of the game.\n\n"
                        + e.Message, "EXDReader (ItemUICategory) Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }

                int errorCount = 0;
                for (int i = 0; i < offsetTableSize; i += 8)
                {
                    br.BaseStream.Seek(i + 32, SeekOrigin.Begin);
                    try
                    {
                        int index = BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0);
                        int tableOffset = BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0);

                        br.BaseStream.Seek(tableOffset, SeekOrigin.Begin);

                        int entrySize = BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0);

                        br.ReadBytes(14);

                        var catName = Encoding.UTF8.GetString(br.ReadBytes(entrySize - 12)).Replace("\0", "");

                        if (catName.Length > 0 && !catName.Equals("  "))
                        {
                            UICategories.Add(index, catName);
                        }
                    }
                    catch (Exception e)
                    {
                        if(errorCount < 4)
                        {
                            FlexibleMessageBox.Show("Error reading UI Category entry for ItemUICategory\nYou may have an older or unsupported version of the game.\n\n"
                                + e.Message, "EXDReader (ItemUICategory) Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            errorCount++;
                        }
                    }

                }

            }
            return UICategories;
        }

        public static Dictionary<int, string> ActionCategory()
        {
            string actionCatFile = String.Format(Strings.ActionCategory, Strings.Language);

            byte[] actionCatBytes = Helper.GetDecompressedEXDData(Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(actionCatFile)), actionCatFile);

            Dictionary<int, string> actionCategories = new Dictionary<int, string>();

            actionCategories.Add(0, "None");
            using (BinaryReader br = new BinaryReader(new MemoryStream(actionCatBytes)))
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

                    br.ReadBytes(6);

                    var catName = Encoding.UTF8.GetString(br.ReadBytes(entrySize - 4)).Replace("\0", "");

                    if (catName.Length > 0 && !catName.Equals("  "))
                    {
                        actionCategories.Add(index, catName);
                    }
                }

            }
            return actionCategories;
        }

        /// <summary>
        /// Creates a list of Items contained in item_(num)_(language).exd 
        /// </summary>
        /// <returns>Dictionary<string, Items> String:Item Name  Items:Item data associated with Item</returns>
        public static List<TreeNode> MakeItemsList()
        {
            SortedSet<ItemData> itemIconList = new SortedSet<ItemData>();
            Dictionary<int, string> itemOffsetDict = new Dictionary<int, string>();
            Dictionary<string, TreeNode> itemDict = new Dictionary<string, TreeNode>();
            Dictionary<string, TreeNode> itemIconDict = new Dictionary<string, TreeNode>();

            string testItemExd = String.Format(Strings.itemFile, 0, Strings.Language);

            if (!Helper.FileExists(FFCRC.GetHash(testItemExd), FFCRC.GetHash(Strings.ExdFolder), Strings.EXDDat))
            {
                if (Info.otherClientSupport)
                {
                    testItemExd = String.Format(Strings.itemFile, 0, "ko");
                    Properties.Settings.Default.Language = "ko";
                    Properties.Settings.Default.Save();

                    if (!Helper.FileExists(FFCRC.GetHash(testItemExd), FFCRC.GetHash(Strings.ExdFolder), Strings.EXDDat))
                    {
                        testItemExd = String.Format(Strings.itemFile, 0, "chs");
                        Properties.Settings.Default.Language = "zh";
                        Properties.Settings.Default.Save();

                        if (!Helper.FileExists(FFCRC.GetHash(testItemExd), FFCRC.GetHash(Strings.ExdFolder), Strings.EXDDat))
                        {
                            return null;
                        }
                    }

                    CultureInfo ci = new CultureInfo(Properties.Settings.Default.Language);
                    ci.NumberFormat.NumberDecimalSeparator = ".";
                    CultureInfo.DefaultThreadCurrentCulture = ci;
                    CultureInfo.DefaultThreadCurrentUICulture = ci;
                }
                else
                {
                    FlexibleMessageBox.Show(Dialogs.ClientSupportWarning, "Client Support OFF " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }

            }

            var UICategoryDict = ItemUICategory();

            var smallClothesMTRL = "chara/equipment/e0000/material/v";

            int errorCount = 0;

            //smallclothes are not in the item list, so they are added manualy
            ItemData item = new ItemData()
            {
                ItemName = "SmallClothes Body",
                ItemCategory = "4",
                PrimaryModelID = "0000",
                PrimaryModelVariant = "1",
                PrimaryMTRLFolder = smallClothesMTRL
            };

            var catName = "";
            try
            {
                catName = Info.IDSlotName[item.ItemCategory];
            }
            catch(Exception e)
            {
                FlexibleMessageBox.Show("Error at .\nItem: " + item.ItemName + "\nCategory: " + item.ItemCategory  + "\n\n" + e.Message, "EXDReader (Item) Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            var scNode = new TreeNode() { Name = item.ItemName, ItemData = item };
            itemDict.Add(catName, new TreeNode() { Name = catName, _subNode = new List<TreeNode>() { scNode } });

            item = new ItemData()
            {
                ItemName = "SmallClothes Legs",
                ItemCategory = "7",
                PrimaryModelID = "0000",
                PrimaryModelVariant = "1",
                PrimaryMTRLFolder = smallClothesMTRL
            };


            try
            {
                catName = Info.IDSlotName[item.ItemCategory];
            }
            catch
            {
                errorCount++;
            }
            scNode = new TreeNode() { Name = item.ItemName, ItemData = item };
            itemDict.Add(catName, new TreeNode() { Name = catName, _subNode = new List<TreeNode>() { scNode } });

            item = new ItemData()
            {
                ItemName = "SmallClothes Feet",
                ItemCategory = "8",
                PrimaryModelID = "0000",
                PrimaryModelVariant = "1",
                PrimaryMTRLFolder = smallClothesMTRL
            };

            try
            {
                catName = Info.IDSlotName[item.ItemCategory];
            }
            catch
            {
                errorCount++;
            }

            scNode = new TreeNode() { Name = item.ItemName, ItemData = item };
            itemDict.Add(catName, new TreeNode() { Name = catName, _subNode = new List<TreeNode>() { scNode } });


            //searches item files which increase in increments of 500 in 0a0000 index until one does not exist
            for (int i = 0; ; i += 500)
            {
                string itemExd = String.Format(Strings.itemFile, i, Strings.Language);

                int offset = Helper.GetEXDOffset(FFCRC.GetHash(Strings.ExdFolder), FFCRC.GetHash(itemExd));

                if(offset != 0)
                {
                    itemOffsetDict.Add(offset, itemExd);
                }
                else
                {
                    break;
                }
            }

            foreach(int offset in itemOffsetDict.Keys)
            {
                if(offset != 0)
                {
                    using (BinaryReader br = new BinaryReader(new MemoryStream(Helper.GetDecompressedEXDData(offset, itemOffsetDict[offset]))))
                    {
                        br.ReadBytes(8);
                        int offsetTableSize = 0;
                        try
                        {
                            offsetTableSize = BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0);
                        }
                        catch (Exception e)
                        {
                            FlexibleMessageBox.Show("Error reading Offset Table Size for " + itemOffsetDict[offset] + "\nYou may have an older or unsupported version of the game.\n\n"
                                + e.Message, "EXDReader (Item) Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            continue;
                        }

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
                                TreeNode category = new TreeNode();

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

                                    int icon = 0;

                                    if (!hasSecondary)
                                    {
                                        br.ReadBytes(90);
                                        icon = BitConverter.ToUInt16(br.ReadBytes(2).Reverse().ToArray(), 0);
                                    }
                                    else
                                    {
                                        br.ReadBytes(86);
                                        icon = BitConverter.ToUInt16(br.ReadBytes(2).Reverse().ToArray(), 0);
                                    }

                                    item.Icon = icon.ToString();

                                    br.ReadBytes(16);

                                    byte[] slotBytes = br.ReadBytes(4).ToArray();
                                    item.ItemCategory = slotBytes[0].ToString();

                                    br.ReadBytes(2);
                                    br.ReadBytes(lastText);

                                    var name = Encoding.UTF8.GetString(br.ReadBytes(entrySize - (lastText + 152))).Replace("\0", "");
                                    item.ItemName = new string(name.Where(c => !char.IsControl(c)).ToArray());

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
                                        var itemNode = new TreeNode { Name = item.ItemName, ItemData = item };
                                        var itemCatName = "";

                                        try
                                        {
                                            itemCatName = Info.IDSlotName[item.ItemCategory];
                                        }
                                        catch
                                        {
                                            errorCount++;
                                        }

                                        if (!item.ItemCategory.Equals("6"))
                                        {
                                            if (itemDict.ContainsKey(itemCatName))
                                            {
                                                itemDict[itemCatName]._subNode.Add(itemNode);
                                            }
                                            else
                                            {
                                                itemDict.Add(itemCatName, new TreeNode() { Name = itemCatName, _subNode = new List<TreeNode>() { itemNode } });
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.WriteLine("EXD_MakeItemListError " + e);
                                    }
                                }
                                else
                                {
                                    br.ReadBytes(98);

                                    item.Icon = BitConverter.ToUInt16(br.ReadBytes(2).Reverse().ToArray(), 0).ToString();

                                    br.ReadBytes(14);

                                    var someByte = br.ReadByte();

                                    try
                                    {
                                        item.ItemSubCategory = UICategoryDict[someByte];
                                    }
                                    catch
                                    {
                                        errorCount++;
                                    }

                                    br.ReadBytes(7);

                                    br.ReadBytes(lastText);

                                    var name = Encoding.UTF8.GetString(br.ReadBytes(entrySize - (lastText + 152))).Replace("\0", "");
                                    item.ItemName = new string(name.Where(c => !char.IsControl(c)).ToArray());

                                    item.ItemCategory = Strings.Items;

                                    try
                                    {
                                        TreeNode itemNode = new TreeNode() { Name = item.ItemName, ItemData = item };

                                        itemIconList.Add(item);
                                        if (itemIconDict.ContainsKey(item.ItemSubCategory))
                                        {
                                            itemIconDict[item.ItemSubCategory]._subNode.Add(itemNode);
                                        }
                                        else
                                        {
                                            itemIconDict.Add(item.ItemSubCategory, new TreeNode() { Name = item.ItemSubCategory, _subNode = { itemNode } });
                                        }

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
            }

            if(errorCount > 0)
            {
                FlexibleMessageBox.Show("TexTools ran into errors reading the items list.\n\nError Count: " + errorCount, "EXDReader Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            foreach (var i in itemDict.Values)
            {
                i._subNode.Sort();
            }

            foreach(var i in itemIconDict.Values)
            {
                i._subNode.Sort();
                itemIconNode._subNode.Add(i);
            }
            itemIconNode._subNode.Sort();

            return itemDict.Values.ToList();
        }
    }
}
