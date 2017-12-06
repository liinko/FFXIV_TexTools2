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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace FFXIV_TexTools2.Material
{
    /// <summary>
    /// Handles files with mtrl extension
    /// </summary>
    /// <remarks>
    /// mtrl files contain the texture file paths and colorset data
    /// </remarks>
    public static class MTRL
    {
        /// <summary>
        /// Gets the races that have an mtrl file for the given item
        /// </summary>
        /// <remarks>
        /// Goes through a list of all races to see if an mtrl file exists within the items material folder
        /// </remarks>
        /// <param name="item">Selected item to check</param>
        /// <param name="selectedCategory">The category of the selected item</param>
        /// <param name="IMCVersion">The items version from its imc file</param>
        /// <returns>ObservableCollection of ComboBoxInfo classes containing race and race ID</returns>
        public static ObservableCollection<ComboBoxInfo> GetMTRLRaces(ItemData item, string selectedCategory, string IMCVersion)
        {
            ObservableCollection<ComboBoxInfo> cbiList;
            Dictionary<int, string> racesDict = new Dictionary<int, string>();
            string MTRLFolder;

            if (item.ItemName.Equals(Strings.Body))
            {
                foreach (string race in Info.IDRace.Keys)
                {
                    MTRLFolder = String.Format(Strings.BodyMtrlFolder, race, "0001");

                    racesDict.Add(FFCRC.GetHash(MTRLFolder), race);
                }

                 cbiList = new ObservableCollection<ComboBoxInfo>(Helper.FolderExistsListRace(racesDict, Strings.ItemsDat));
            }
            else if (item.ItemName.Equals(Strings.Face))
            {
                foreach (string race in Info.IDRace.Keys)
                {
                    MTRLFolder = String.Format(Strings.FaceMtrlFolder, race, "0001");
                    if(race.Equals("0301") || race.Equals("0304") || race.Equals("0401") || race.Equals("0404"))
                    {
                        MTRLFolder = String.Format(Strings.FaceMtrlFolder, race, "0101");

                    }

                    racesDict.Add(FFCRC.GetHash(MTRLFolder), race);
                }

                cbiList = new ObservableCollection<ComboBoxInfo>(Helper.FolderExistsListRace(racesDict, Strings.ItemsDat));
            }
            else if (item.ItemName.Equals(Strings.Hair))
            {
                foreach (string race in Info.IDRace.Keys)
                {
                    MTRLFolder = String.Format(Strings.HairMtrlFolder, race, "0001");

                    racesDict.Add(FFCRC.GetHash(MTRLFolder), race);
                }

                cbiList = new ObservableCollection<ComboBoxInfo>(Helper.FolderExistsListRace(racesDict, Strings.ItemsDat));
            }
            else if (item.ItemName.Equals(Strings.Tail))
            {
                foreach (string race in Info.IDRace.Keys)
                {
                    MTRLFolder = String.Format(Strings.TailMtrlFolder, race, "0001");

                    racesDict.Add(FFCRC.GetHash(MTRLFolder), race);
                }

                cbiList = new ObservableCollection<ComboBoxInfo>(Helper.FolderExistsListRace(racesDict, Strings.ItemsDat));
            }
            else if (item.ItemName.Equals(Strings.Face_Paint) || item.ItemName.Equals(Strings.Equipment_Decals) || item.ItemCategory.Equals("9901"))
            {
                cbiList = new ObservableCollection<ComboBoxInfo>();
                ComboBoxInfo cbi = new ComboBoxInfo() { Name = Strings.All, ID = "0",  IsNum = true };
                cbiList.Add(cbi);
            }
            else if (selectedCategory.Equals(Strings.Pets) || selectedCategory.Equals(Strings.Mounts) || selectedCategory.Equals(Strings.Minions) || selectedCategory.Equals(Strings.Monster))
            {
                cbiList = new ObservableCollection<ComboBoxInfo>();
                ComboBoxInfo cbi = new ComboBoxInfo() { Name = Strings.Monster, ID = "0", IsNum = true };
                cbiList.Add(cbi);
            }
            else
            {
                string type = Helper.GetCategoryType(selectedCategory);

                ObservableCollection<ComboBoxInfo> cbiInfo = new ObservableCollection<ComboBoxInfo>();

                var fileHashList = Helper.GetAllFilesInFolder(FFCRC.GetHash(item.PrimaryMTRLFolder + IMCVersion), Strings.ItemsDat);

                if (type.Equals("weapon") || type.Equals("food"))
                {
                    cbiInfo.Add(new ComboBoxInfo() { Name = Strings.All, ID = Strings.All, IsNum = false });
                }
                else
                {
                    foreach (string raceID in Info.raceID.Values)
                    {
                        var MTRLFile = String.Format(Strings.EquipMtrlFile, raceID, item.PrimaryModelID, Info.slotAbr[selectedCategory], "a");

                        if (type.Equals("accessory"))
                        {
                            MTRLFile = String.Format(Strings.AccMtrlFile, raceID, item.PrimaryModelID, Info.slotAbr[selectedCategory], "a");
                        }
                        var fileHash = FFCRC.GetHash(MTRLFile);

                        if (fileHashList.Contains(fileHash))
                        {
                            cbiInfo.Add(new ComboBoxInfo() { Name = Info.IDRace[raceID], ID = raceID, IsNum = false });
                        }
                    }
                }

                cbiList = cbiInfo;
            }
            return cbiList;
        }

        /// <summary>
        /// Gets the parts for the selected item
        /// </summary>
        /// <param name="item">currently selected item</param>
        /// <param name="raceID">currently selected race</param>
        /// <param name="IMCVersion">version of selected item</param>
        /// <param name="selectedCategory">The category of the selected item</param>
        /// <returns></returns>
        public static ObservableCollection<ComboBoxInfo> GetMTRLParts(ItemData item, string raceID, string IMCVersion, string selectedCategory)
        {
            Dictionary<int, int> MTRLDict = new Dictionary<int, int>();
            List<ComboBoxInfo> cbiList;
            List<int> fileHashList;
            string MTRLFolder;

            if (item.ItemName.Equals(Strings.Body))
            {
                for (int i = 1; i < 251; i++)
                {
                    MTRLFolder = String.Format(Strings.BodyMtrlFolder, raceID, i.ToString().PadLeft(4, '0'));

                    MTRLDict.Add(FFCRC.GetHash(MTRLFolder), i);
                }

                cbiList = new List<ComboBoxInfo>(Helper.FolderExistsList(MTRLDict, Strings.ItemsDat));
            }
            else if (item.ItemName.Equals(Strings.Face))
            {
                for (int i = 1; i < 251; i++)
                {
                    MTRLFolder = String.Format(Strings.FaceMtrlFolder, raceID, i.ToString().PadLeft(4, '0'));

                    MTRLDict.Add(FFCRC.GetHash(MTRLFolder), i);
                }

                cbiList = new List<ComboBoxInfo>(Helper.FolderExistsList(MTRLDict, Strings.ItemsDat));
            }
            else if (item.ItemName.Equals(Strings.Hair))
            {
                for (int i = 1; i < 251; i++)
                {
                    MTRLFolder = String.Format(Strings.HairMtrlFolder, raceID, i.ToString().PadLeft(4, '0'));

                    MTRLDict.Add(FFCRC.GetHash(MTRLFolder), i);
                }

                cbiList = new List<ComboBoxInfo>(Helper.FolderExistsList(MTRLDict, Strings.ItemsDat));
            }
            else if (item.ItemName.Equals(Strings.Tail))
            {
                for (int i = 1; i < 251; i++)
                {
                    MTRLFolder = String.Format(Strings.TailMtrlFolder, raceID, i.ToString().PadLeft(4, '0'));

                    MTRLDict.Add(FFCRC.GetHash(MTRLFolder), i);
                }

                cbiList = new List<ComboBoxInfo>(Helper.FolderExistsList(MTRLDict, Strings.ItemsDat));
            }
            else if (item.ItemName.Equals(Strings.Face_Paint))
            {
                fileHashList = Helper.GetAllFilesInFolder(FFCRC.GetHash(Strings.FacePaintFolder), Strings.ItemsDat);
                cbiList = new List<ComboBoxInfo>();

                for (int i = 1; i < 100; i++)
                {
                    MTRLFolder = String.Format(Strings.FacePaintFile, i);

                    if (fileHashList.Contains(FFCRC.GetHash(MTRLFolder)))
                    {
                        cbiList.Add(new ComboBoxInfo() { Name = i.ToString(), ID = i.ToString(), IsNum = true });
                    }
                }
            }
            else if (item.ItemName.Equals(Strings.Equipment_Decals))
            {
                fileHashList = Helper.GetAllFilesInFolder(FFCRC.GetHash(Strings.EquipDecalFolder), Strings.ItemsDat);
                cbiList = new List<ComboBoxInfo>();

                for (int i = 1; i < 300; i++)
                {
                    MTRLFolder = String.Format(Strings.EquipDecalFile, i.ToString().PadLeft(3, '0'));

                    if (fileHashList.Contains(FFCRC.GetHash(MTRLFolder)))
                    {
                        cbiList.Add(new ComboBoxInfo() { Name = i.ToString(), ID = i.ToString(), IsNum = true });
                    }
                }

                cbiList.Add(new ComboBoxInfo() { Name = "_stigma", ID = "_stigma", IsNum = false });
            }
            else if (selectedCategory.Equals(Strings.Pets))
            {
                int part = 1;

                if (item.ItemName.Equals(Strings.Selene) || item.ItemName.Equals(Strings.Bishop_Autoturret))
                {
                    part = 2;
                }

                for (int i = 1; i < 20; i++)
                {
                    MTRLFolder = String.Format(Strings.MonsterMtrlFolder, Info.petID[item.ItemName], part.ToString().PadLeft(4, '0')) + i.ToString().PadLeft(4, '0');

                    MTRLDict.Add(FFCRC.GetHash(MTRLFolder), i);
                }

                cbiList = new List<ComboBoxInfo>(Helper.FolderExistsList(MTRLDict, Strings.ItemsDat));
            }
            else if (selectedCategory.Equals(Strings.Mounts))
            {
                cbiList = new List<ComboBoxInfo>();

                if (item.PrimaryMTRLFolder.Contains("demihuman"))
                {
                    cbiList.Add(new ComboBoxInfo() { Name = "a", ID = "a", IsNum = false });
                }
                else
                {
                    Dictionary<string, int> mountMTRLDict = new Dictionary<string, int>();
                    string[] parts = { "a", "b", "c", "d", "e" };

                    fileHashList = Helper.GetAllFilesInFolder(FFCRC.GetHash(item.PrimaryMTRLFolder + IMCVersion), Strings.ItemsDat);

                    foreach (string c in parts)
                    {
                        MTRLFolder = String.Format(Strings.MonsterMtrlFile, item.PrimaryModelID.PadLeft(4, '0'), item.PrimaryModelBody.PadLeft(4, '0'), c);

                        if (fileHashList.Contains(FFCRC.GetHash(MTRLFolder)))
                        {
                            cbiList.Add(new ComboBoxInfo() { Name = c, ID = c, IsNum = false });
                        }
                    }
                }
            }
            else if (selectedCategory.Equals(Strings.Minions))
            {
                Dictionary<string, int> minionMTRLDict = new Dictionary<string, int>();
                string[] parts = { "a", "b", "c", "d", "e" };
                cbiList = new List<ComboBoxInfo>();

                fileHashList = Helper.GetAllFilesInFolder(FFCRC.GetHash(item.PrimaryMTRLFolder + IMCVersion), Strings.ItemsDat);

                foreach (string c in parts)
                {
                    MTRLFolder = String.Format(Strings.MonsterMtrlFile, item.PrimaryModelID.PadLeft(4, '0'), item.PrimaryModelBody.PadLeft(4, '0'), c);

                    if (fileHashList.Contains(FFCRC.GetHash(MTRLFolder)))
                    {
                        cbiList.Add(new ComboBoxInfo() { Name = c, ID = c, IsNum = false });
                    }
                }
            }
            else
            {
                string type = Helper.GetCategoryType(selectedCategory);
                string[] parts = { "a", "b", "c", "d", "e" };
                cbiList = new List<ComboBoxInfo>();

                fileHashList = Helper.GetAllFilesInFolder(FFCRC.GetHash(item.PrimaryMTRLFolder + IMCVersion), Strings.ItemsDat);

                foreach (string part in parts)
                {
                    if (type.Equals("weapon") || type.Equals("food"))
                    {
                        MTRLFolder = String.Format(Strings.WeapMtrlFile, item.PrimaryModelID, item.PrimaryModelBody, part);
                    }
                    else if (type.Equals("accessory"))
                    {
                        MTRLFolder = String.Format(Strings.AccMtrlFile, raceID, item.PrimaryModelID, Info.slotAbr[selectedCategory], part);
                    }
                    else
                    {
                        MTRLFolder = String.Format(Strings.EquipMtrlFile, raceID, item.PrimaryModelID, Info.slotAbr[selectedCategory], part);
                    }

                    if (fileHashList.Contains(FFCRC.GetHash(MTRLFolder)))
                    {
                        cbiList.Add(new ComboBoxInfo() { Name = part, ID = part, IsNum = false });
                    }
                }

                if (item.SecondaryModelID != null)
                {
                    cbiList.Add(new ComboBoxInfo() { Name = "s", ID = "s", IsNum = false });
                }

                cbiList.Sort();
            }

            return new ObservableCollection<ComboBoxInfo>(cbiList);
        }


        /// <summary>
        /// Parses the data of the items MTRL file
        /// </summary>
        /// <param name="item">currently selected item</param>
        /// <param name="raceID">currently selected race</param>
        /// <param name="selectedCategory">the category of the item</param>
        /// <param name="part">currently selected part</param>
        /// <param name="IMCVersion">version of selected item</param>
        /// <param name="body">the items body</param>
        /// <returns>A tuple containing the MTRLInfo and Observable Collection containing texture map names</returns>
        public static Tuple<MTRLData, ObservableCollection<ComboBoxInfo>> GetMTRLData(ItemData item, string raceID, string selectedCategory, string part, string IMCVersion, string body, string modelID, string VFXVersion)
        {
            string MTRLFolder = "";
            string MTRLFile = "";
            int offset;
            ObservableCollection<ComboBoxInfo> cbiList;
            Tuple<MTRLData, ObservableCollection<ComboBoxInfo>> info;
            MTRLData mtrlInfo = null;
            string[] partList = new string[] { "b", "c", "d" };

            if (item.ItemName.Equals(Strings.Face))
            {
                MTRLFolder = String.Format(Strings.FaceMtrlFolder, raceID, part.PadLeft(4, '0'));

                var fileHashList = Helper.GetAllFilesInFolder(FFCRC.GetHash(MTRLFolder), Strings.ItemsDat);

                cbiList = new ObservableCollection<ComboBoxInfo>();

                if (fileHashList.Contains(FFCRC.GetHash(String.Format(Strings.FaceMtrlFile, raceID, part.PadLeft(4, '0'), "fac", "a"))))
                {
                    cbiList.Add(new ComboBoxInfo() { Name = Strings.Face, ID = "", IsNum = false });
                }
                else
                {
                    foreach(var p in partList)
                    {
                        var file = String.Format(Strings.FaceMtrlFile, raceID, part.PadLeft(4, '0'), "fac", p);

                        if (fileHashList.Contains(FFCRC.GetHash(file)))
                        {
                            cbiList.Add(new ComboBoxInfo() { Name = Strings.Face, ID = "", IsNum = false });
                            break;
                        }
                    }
                }

                if (fileHashList.Contains(FFCRC.GetHash(String.Format(Strings.FaceMtrlFile, raceID, part.PadLeft(4, '0'), "iri", "a"))))
                {
                    cbiList.Add(new ComboBoxInfo() { Name = Strings.Iris, ID = "", IsNum = false });
                }
                else
                {
                    foreach (var p in partList)
                    {
                        var file = String.Format(Strings.FaceMtrlFile, raceID, part.PadLeft(4, '0'), "iri", p);

                        if (fileHashList.Contains(FFCRC.GetHash(file)))
                        {
                            cbiList.Add(new ComboBoxInfo() { Name = Strings.Iris, ID = "", IsNum = false });
                            break;
                        }
                    }
                }

                if (fileHashList.Contains(FFCRC.GetHash(String.Format(Strings.FaceMtrlFile, raceID, part.PadLeft(4, '0'), "etc", "a"))))
                {
                    cbiList.Add(new ComboBoxInfo() { Name = Strings.Etc, ID = "", IsNum = false });
                }
                else
                {
                    foreach (var p in partList)
                    {
                        var file = String.Format(Strings.FaceMtrlFile, raceID, part.PadLeft(4, '0'), "etc", p);

                        if (fileHashList.Contains(FFCRC.GetHash(file)))
                        {
                            cbiList.Add(new ComboBoxInfo() { Name = Strings.Etc, ID = "", IsNum = false });
                            break;
                        }
                    }
                }

                info = new Tuple<MTRLData, ObservableCollection<ComboBoxInfo>>(mtrlInfo, cbiList);
            }
            else if (item.ItemName.Equals(Strings.Body))
            {
                MTRLFolder = String.Format(Strings.BodyMtrlFolder, raceID, part.PadLeft(4, '0'));
                MTRLFile = String.Format(Strings.BodyMtrlFile, raceID, part.PadLeft(4, '0'));

                if(Helper.FileExists(FFCRC.GetHash(MTRLFile), FFCRC.GetHash(MTRLFolder), Strings.ItemsDat))
                {
                    offset = Helper.GetDataOffset(FFCRC.GetHash(MTRLFolder), FFCRC.GetHash(MTRLFile), Strings.ItemsDat);

                    mtrlInfo = GetMTRLInfo(offset, true);

                    info = new Tuple<MTRLData, ObservableCollection<ComboBoxInfo>>(mtrlInfo, mtrlInfo.TextureMaps);
                }
                else
                {
                    return null;
                }

            }
            else if (item.ItemName.Equals(Strings.Hair))
            {
                MTRLFolder = String.Format(Strings.HairMtrlFolder, raceID, part.PadLeft(4, '0'));

                var fileHashList = Helper.GetAllFilesInFolder(FFCRC.GetHash(MTRLFolder), Strings.ItemsDat);

                cbiList = new ObservableCollection<ComboBoxInfo>();

                if (fileHashList.Contains(FFCRC.GetHash(String.Format(Strings.HairMtrlFile, raceID, part.PadLeft(4, '0'), "hir", "a"))))
                {
                    cbiList.Add(new ComboBoxInfo() { Name = Strings.Hair, ID = "", IsNum = false });
                }

                if (fileHashList.Contains(FFCRC.GetHash(String.Format(Strings.HairMtrlFile, raceID, part.PadLeft(4, '0'), "acc", "b"))))
                {
                    cbiList.Add(new ComboBoxInfo() { Name = Strings.Accessory, ID = "", IsNum = false });
                }

                info = new Tuple<MTRLData, ObservableCollection<ComboBoxInfo>>(mtrlInfo, cbiList);
            }
            else if (item.ItemName.Equals(Strings.Face_Paint) || item.ItemName.Equals(Strings.Equipment_Decals))
            {
                string texPath = "chara/common/texture/";

                if (item.ItemName.Equals(Strings.Face_Paint))
                {
                    texPath = texPath + "decal_face/_decal_{0}.tex";
                }
                else
                {
                    if (!part.Contains("stigma"))
                    {
                        texPath = texPath + "decal_equip/-decal_{0}.tex";
                    }
                    else
                    {
                        texPath = texPath + "decal_equip/_stigma.tex";
                    }

                }

                cbiList = new ObservableCollection<ComboBoxInfo>
                {
                    new ComboBoxInfo() { Name = Strings.Mask, ID = "", IsNum = false }
                }
                ;
                mtrlInfo = new MTRLData()
                {
                    MaskPath = texPath
                };
                info = new Tuple<MTRLData, ObservableCollection<ComboBoxInfo>>(mtrlInfo, cbiList);
            }
            else if (item.ItemName.Equals(Strings.Tail))
            {
                MTRLFolder = String.Format(Strings.TailMtrlFolder, raceID, part.PadLeft(4, '0'));
                MTRLFile = string.Format(Strings.TailMtrlFile, raceID, part.PadLeft(4, '0'));

                if (Helper.FileExists(FFCRC.GetHash(MTRLFile), FFCRC.GetHash(MTRLFolder), Strings.ItemsDat))
                {
                    offset = Helper.GetDataOffset(FFCRC.GetHash(MTRLFolder), FFCRC.GetHash(MTRLFile), Strings.ItemsDat);

                    mtrlInfo = GetMTRLInfo(offset, true);

                    info = new Tuple<MTRLData, ObservableCollection<ComboBoxInfo>>(mtrlInfo, mtrlInfo.TextureMaps);
                }
                else
                {
                    return null;
                }
            }
            else if (selectedCategory.Equals(Strings.Pets))
            {
                int p = 1;

                if (item.ItemName.Equals(Strings.Selene) || item.ItemName.Equals(Strings.Bishop_Autoturret))
                {
                    p = 2;
                }

                MTRLFolder = String.Format(Strings.MonsterMtrlFolder, Info.petID[item.ItemName], p.ToString().PadLeft(4, '0')) + part.PadLeft(4, '0');
                MTRLFile = String.Format(Strings.MonsterMtrlFile, Info.petID[item.ItemName], p.ToString().PadLeft(4, '0'), "a");

                if (Helper.FileExists(FFCRC.GetHash(MTRLFile), FFCRC.GetHash(MTRLFolder), Strings.ItemsDat))
                {
                    offset = Helper.GetDataOffset(FFCRC.GetHash(MTRLFolder), FFCRC.GetHash(MTRLFile), Strings.ItemsDat);

                    mtrlInfo = GetMTRLInfo(offset, false);
                    mtrlInfo.MTRLPath = MTRLFolder + "/" + MTRLFile;
                    mtrlInfo.MTRLOffset = offset;

                    info = new Tuple<MTRLData, ObservableCollection<ComboBoxInfo>>(mtrlInfo, mtrlInfo.TextureMaps);
                }
                else
                {
                    return null;
                }
            }
            else if (selectedCategory.Equals(Strings.Mounts))
            {
                if (item.PrimaryMTRLFolder.Contains("demihuman"))
                {
                    SortedSet<ComboBoxInfo> typeSet = new SortedSet<ComboBoxInfo>();

                    var fileHashList = Helper.GetAllFilesInFolder(FFCRC.GetHash(item.PrimaryMTRLFolder + IMCVersion), Strings.ItemsDat);

                    foreach (string abr in Info.slotAbr.Values)
                    {
                        MTRLFile = String.Format(Strings.DemiMtrlFile, item.PrimaryModelID.PadLeft(4, '0'), item.PrimaryModelBody.PadLeft(4, '0'), abr);

                        if (fileHashList.Contains(FFCRC.GetHash(MTRLFile)))
                        {
                            typeSet.Add(new ComboBoxInfo() { Name = Info.slotAbr.FirstOrDefault(x => x.Value == abr).Key, ID = "", IsNum = false });
                            
                        }
                    }
                    info = new Tuple<MTRLData, ObservableCollection<ComboBoxInfo>>(mtrlInfo, new ObservableCollection<ComboBoxInfo>(typeSet.ToList()));
                }
                else
                {
                    MTRLFile = String.Format(Strings.MonsterMtrlFile, item.PrimaryModelID.PadLeft(4, '0'), item.PrimaryModelBody.PadLeft(4, '0'), part);

                    if (Helper.FileExists(FFCRC.GetHash(MTRLFile), FFCRC.GetHash(item.PrimaryMTRLFolder + IMCVersion), Strings.ItemsDat))
                    {
                        offset = Helper.GetDataOffset(FFCRC.GetHash(item.PrimaryMTRLFolder + IMCVersion), FFCRC.GetHash(MTRLFile), Strings.ItemsDat);
                        mtrlInfo = GetMTRLInfo(offset, false);
                        mtrlInfo.MTRLPath = item.PrimaryMTRLFolder + IMCVersion + "/" + MTRLFile;
                        mtrlInfo.MTRLOffset = offset;

                        info = new Tuple<MTRLData, ObservableCollection<ComboBoxInfo>>(mtrlInfo, mtrlInfo.TextureMaps);
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else if (selectedCategory.Equals(Strings.Minions))
            {
                MTRLFile = String.Format(Strings.MonsterMtrlFile, item.PrimaryModelID.PadLeft(4, '0'), item.PrimaryModelBody.PadLeft(4, '0'), part);

                if (Helper.FileExists(FFCRC.GetHash(MTRLFile), FFCRC.GetHash(item.PrimaryMTRLFolder + IMCVersion), Strings.ItemsDat))
                {
                    offset = Helper.GetDataOffset(FFCRC.GetHash(item.PrimaryMTRLFolder + IMCVersion), FFCRC.GetHash(MTRLFile), Strings.ItemsDat);
                    mtrlInfo = GetMTRLInfo(offset, false);
                    mtrlInfo.MTRLPath = item.PrimaryMTRLFolder + IMCVersion + "/" + MTRLFile;
                    mtrlInfo.MTRLOffset = offset;

                    info = new Tuple<MTRLData, ObservableCollection<ComboBoxInfo>>(mtrlInfo, mtrlInfo.TextureMaps);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                var categoryType = Helper.GetCategoryType(selectedCategory);
                string imcVersion = IMCVersion;
                MTRLFolder = item.PrimaryMTRLFolder;

                if (categoryType.Equals("weapon"))
                {
                    if (!modelID.Equals(""))
                    {
                        var isNumber = int.TryParse(body, out var n);
                        if (isNumber)
                        {
                            MTRLFolder = string.Format(Strings.WeapMtrlFolder, modelID, body);
                            MTRLFile = String.Format(Strings.WeapMtrlFile, modelID, n.ToString().PadLeft(4, '0'), part);
                        }
                        else
                        {
                            MTRLFolder = string.Format(Strings.WeapMtrlFolder, modelID, body);
                            MTRLFile = String.Format(Strings.WeapMtrlFile, modelID, item.PrimaryModelBody, part);
                        }
                    }
                    else
                    {
                        MTRLFile = String.Format(Strings.WeapMtrlFile, item.PrimaryModelID, item.PrimaryModelBody, part);
                    }

                    //if (part.Equals("s") || type.Equals("Secondary"))
                    //{

                    //    MTRLFolder = item.SecondaryMTRLFolder;
                    //    imcVersion = IMC.GetVersion(selectedCategory, item, true).Item1;
                    //    MTRLFile = String.Format(Strings.WeapMtrlFile, item.SecondaryModelID, item.SecondaryModelBody, "a");
                    //}
                }
                else if (categoryType.Equals("accessory"))
                {
                    MTRLFile = String.Format(Strings.AccMtrlFile, raceID, item.PrimaryModelID, Info.slotAbr[selectedCategory], part);
                }
                else if (categoryType.Equals("food"))
                {
                    MTRLFile = String.Format(Strings.WeapMtrlFile, item.PrimaryModelID, item.PrimaryModelBody, "a");
                }
                else
                {
                    MTRLFile = String.Format(Strings.EquipMtrlFile, raceID, item.PrimaryModelID, Info.slotAbr[selectedCategory], part);
                }

                string VFXFolder = "";
                string VFXFile = "";

                ObservableCollection<ComboBoxInfo> cbi = new ObservableCollection<ComboBoxInfo>();

                if (item.PrimaryModelID.Equals("9982"))
                {
                    MTRLFile = "guide_model.mtrl";
                }

                if (Helper.FileExists(FFCRC.GetHash(MTRLFile), FFCRC.GetHash(MTRLFolder + imcVersion), Strings.ItemsDat))
                {
                    offset = Helper.GetDataOffset(FFCRC.GetHash(MTRLFolder + imcVersion), FFCRC.GetHash(MTRLFile), Strings.ItemsDat);

                    mtrlInfo = GetMTRLInfo(offset, false);
                    mtrlInfo.MTRLPath = MTRLFolder + imcVersion + "/" + MTRLFile;
                    mtrlInfo.MTRLOffset = offset;

                    foreach(var texMap in mtrlInfo.TextureMaps)
                    {
                        cbi.Add(texMap);
                    }
                }
                else
                {
                    if (Helper.FileExists(FFCRC.GetHash(MTRLFile), FFCRC.GetHash(MTRLFolder + "0001"), Strings.ItemsDat))
                    {
                        offset = Helper.GetDataOffset(FFCRC.GetHash(MTRLFolder + "0001"), FFCRC.GetHash(MTRLFile), Strings.ItemsDat);

                        mtrlInfo = GetMTRLInfo(offset, false);
                        mtrlInfo.MTRLPath = MTRLFolder + imcVersion + "/" + MTRLFile;
                        mtrlInfo.MTRLOffset = offset;

                        foreach (var texMap in mtrlInfo.TextureMaps)
                        {
                            cbi.Add(texMap);
                        }
                    }
                    else if (categoryType.Equals("weapon"))
                    {
                        MTRLFolder = string.Format(Strings.EquipMtrlFolder, modelID);
                        MTRLFile = String.Format(Strings.EquipMtrlFile, "0101", modelID, Info.slotAbr[Strings.Hands], part);

                        if (Helper.FileExists(FFCRC.GetHash(MTRLFile), FFCRC.GetHash(MTRLFolder + "0001"), Strings.ItemsDat))
                        {
                            offset = Helper.GetDataOffset(FFCRC.GetHash(MTRLFolder + "0001"), FFCRC.GetHash(MTRLFile), Strings.ItemsDat);

                            mtrlInfo = GetMTRLInfo(offset, false);
                            mtrlInfo.MTRLPath = MTRLFolder + imcVersion + "/" + MTRLFile;
                            mtrlInfo.MTRLOffset = offset;

                            foreach (var texMap in mtrlInfo.TextureMaps)
                            {
                                cbi.Add(texMap);
                            }
                        }
                        else
                        {
                            return null;
                        }

                    }
                    else
                    {
                        return null;
                    }
                }

                if(item.Icon != null)
                {
                    var iconBaseNum = item.Icon.Substring(0, 2).PadRight(item.Icon.Length, '0');
                    var iconFolder = string.Format(Strings.IconFolder, iconBaseNum.PadLeft(6, '0'));
                    var iconHQFolder = string.Format(Strings.IconHQFolder, iconBaseNum.PadLeft(6, '0'));
                    var iconFile = string.Format(Strings.UIFile, item.Icon.PadLeft(6, '0'));

                    mtrlInfo.UIPath = iconFolder + "/" + iconFile;
                    mtrlInfo.UIHQPath = iconHQFolder + "/" + iconFile;

                    mtrlInfo.UIOffset = Helper.GetDataOffset(FFCRC.GetHash(iconFolder), FFCRC.GetHash(iconFile), Strings.UIDat);
                    mtrlInfo.UIHQOffset = Helper.GetDataOffset(FFCRC.GetHash(iconHQFolder), FFCRC.GetHash(iconFile), Strings.UIDat);

                    if (mtrlInfo.UIOffset != 0)
                    {
                        cbi.Add(new ComboBoxInfo() { Name = "Icon", ID = "Icon", IsNum = false });
                    }

                    if (mtrlInfo.UIHQOffset != 0)
                    {
                        cbi.Add(new ComboBoxInfo() { Name = "HQ Icon", ID = "HQIcon", IsNum = false });
                    }
                }


                if (!VFXVersion.Equals("0000"))
                {
                    if (categoryType.Equals("equipment"))
                    {
                        VFXFolder = string.Format(Strings.EquipVFXFolder, item.PrimaryModelID);
                        VFXFile = string.Format(Strings.EquipVFXFile, VFXVersion);
                    }
                    else if (categoryType.Equals("weapon"))
                    {
                        VFXFolder = string.Format(Strings.WeapVFXFolder, item.PrimaryModelID, item.PrimaryModelBody);
                        VFXFile = string.Format(Strings.WeapVFXFile, VFXVersion);
                    }

                    if (Helper.FileExists(FFCRC.GetHash(VFXFile), FFCRC.GetHash(VFXFolder), Strings.ItemsDat))
                    {
                        offset = Helper.GetDataOffset(FFCRC.GetHash(VFXFolder), FFCRC.GetHash(VFXFile), Strings.ItemsDat);

                        var vfxData = GetVFXData(offset);

                        foreach (var vfx in vfxData.VFXPaths)
                        {
                            cbi.Add(new ComboBoxInfo() { Name = Path.GetFileNameWithoutExtension(vfx), ID = vfx, IsNum = false });
                        }
                    }
                }

                info = new Tuple<MTRLData, ObservableCollection<ComboBoxInfo>>(mtrlInfo, cbi);
            }

            return info;
        }


        /// <summary>
        /// Parses the MTRL file for items that contains types
        /// </summary>
        /// <param name="item">currently selected item</param>
        /// <param name="race">currently selected race</param>
        /// <param name="partNum">currently selected part</param>
        /// <param name="type">currently selected type</param>
        /// <param name="IMCVersion">version of the selected item</param>
        /// <param name="selectedCategory">The category of the item</param>
        /// <returns>A tuple containing the MTRLInfo and Observable Collection containing texture map names</returns>
        public static Tuple<MTRLData, ObservableCollection<ComboBoxInfo>> GetMTRLDatafromType(ItemData item, string race, string partNum, string type, string IMCVersion, string selectedCategory, string part)
        {
            string MTRLFolder, MTRLFile;
            string MTRLPath = "";
            bool isUncompressed = true;
            int offset = 0;
            string[] partList = new string[] { "a", "b", "c", "d" };

            if (item.ItemName.Equals(Strings.Face))
            {
                MTRLFolder = String.Format(Strings.FaceMtrlFolder, race, partNum.PadLeft(4, '0'));
                MTRLFile = String.Format(Strings.FaceMtrlFile, race, partNum.PadLeft(4, '0'), Info.FaceTypes[type], part);
                offset = Helper.GetDataOffset(FFCRC.GetHash(MTRLFolder), FFCRC.GetHash(MTRLFile), Strings.ItemsDat);
                if(offset == 0)
                {
                    foreach(var p in partList)
                    {
                        MTRLFile = String.Format(Strings.FaceMtrlFile, race, partNum.PadLeft(4, '0'), Info.FaceTypes[type], p);
                        offset = Helper.GetDataOffset(FFCRC.GetHash(MTRLFolder), FFCRC.GetHash(MTRLFile), Strings.ItemsDat);

                        if(offset != 0)
                        {
                            break;
                        }
                    }
                }

                MTRLPath = MTRLFolder + "/" + MTRLFile;

                isUncompressed = true;
            }
            else if (item.ItemName.Equals(Strings.Hair))
            {
                MTRLFolder = String.Format(Strings.HairMtrlFolder, race, partNum.PadLeft(4, '0'));
                isUncompressed = true;

                if (type.Equals(Strings.Accessory))
                {
                    MTRLFile = String.Format(Strings.HairMtrlFile, race, partNum.PadLeft(4, '0'), Info.HairTypes[type], "b");
                }
                else
                {
                    MTRLFile = String.Format(Strings.HairMtrlFile, race, partNum.PadLeft(4, '0'), Info.HairTypes[type], "a");
                }

                MTRLPath = MTRLFolder + "/" + MTRLFile;

                offset = Helper.GetDataOffset(FFCRC.GetHash(MTRLFolder), FFCRC.GetHash(MTRLFile), Strings.ItemsDat);
            }
            else if (selectedCategory.Equals(Strings.Mounts))
            {
                isUncompressed = false;

                if (item.PrimaryMTRLFolder.Contains("demihuman"))
                {
                    MTRLFile = String.Format(Strings.DemiMtrlFile, item.PrimaryModelID.PadLeft(4, '0'), item.PrimaryModelBody.PadLeft(4, '0'), type);
                }
                else
                {
                    MTRLFile = String.Format(Strings.MonsterMtrlFile, item.PrimaryModelID.PadLeft(4, '0'), item.PrimaryModelBody.PadLeft(4, '0'), partNum);
                }

                MTRLPath = item.PrimaryMTRLFolder + IMCVersion + "/" + MTRLFile;

                offset = Helper.GetDataOffset(FFCRC.GetHash(item.PrimaryMTRLFolder + IMCVersion), FFCRC.GetHash(MTRLFile), Strings.ItemsDat);
            }

            var mtrlInfo = GetMTRLInfo(offset, isUncompressed);
            mtrlInfo.MTRLPath = MTRLPath;
            mtrlInfo.MTRLOffset = offset;

            return new Tuple<MTRLData, ObservableCollection<ComboBoxInfo>>(mtrlInfo, mtrlInfo.TextureMaps);
        }


        /// <summary>
        /// Gets the MTRL offset of decal files 
        /// </summary>
        /// <param name="decalType">Type of decal</param>
        /// <param name="decalNum">The decal number</param>
        /// <returns>The mtrl offset of the decal file</returns>
        public static int GetDecalOffset(string decalType, string decalNum)
        {
            string decalFolder, decalFile;

            if (decalType.Equals(Strings.Face_Paint))
            {
                decalFolder = Strings.FacePaintFolder;
                decalFile = String.Format(Strings.FacePaintFile, decalNum);
            }
            else
            {
                decalFolder = Strings.EquipDecalFolder;
                decalFile = String.Format(Strings.EquipDecalFile, decalNum.PadLeft(3, '0'));

                if (decalFile.Contains("stigma"))
                {
                    decalFile = "_stigma.tex";
                }
            }

            return Helper.GetDataOffset(FFCRC.GetHash(decalFolder), FFCRC.GetHash(decalFile), Strings.ItemsDat);
        }


        /// <summary>
        /// Gets the data from the MTRL file
        /// </summary>
        /// <param name="offset">MTRL file offset</param>
        /// <param name="isUncompressed">DX compression</param>
        /// <returns>Data from MTRL file</returns>
        public static MTRLData GetMTRLInfo(int offset, bool isUncompressed)
        {
            int datNum = ((offset / 8) & 0x000f) / 2;

            MTRLData mtrlInfo = new MTRLData();

            var decompData = Helper.GetType2DecompressedData(offset, datNum, Strings.ItemsDat);

            using (BinaryReader br = new BinaryReader(new MemoryStream(decompData)))
            {
                br.BaseStream.Seek(6, SeekOrigin.Begin);
                short colorDataSize = br.ReadInt16();
                short textureNameSize = br.ReadInt16();
                br.ReadBytes(2);
                byte numOfTextures = br.ReadByte();
                byte numOfMaps = br.ReadByte();
                byte numOfColorSets = br.ReadByte();
                byte unknown = br.ReadByte();

                int headerEnd = 16 + ((numOfTextures + numOfMaps + numOfColorSets) * 4);

                int[] texPathOffsets = new int[numOfTextures + 1];

                for(int i = 0; i < numOfTextures + 1; i++)
                {
                    texPathOffsets[i] = br.ReadInt16();
                    br.ReadBytes(2);
                }

                br.ReadBytes((numOfMaps - 1) * 4);

                for (int i = 0; i < numOfTextures; i++)
                {
                    br.BaseStream.Seek(headerEnd + texPathOffsets[i], SeekOrigin.Begin);

                    string fullPath = Encoding.ASCII.GetString(br.ReadBytes(texPathOffsets[i + 1] - texPathOffsets[i])).Replace("\0", "");

                    string fileName = fullPath.Substring(fullPath.LastIndexOf("/") + 1);

                    if (Properties.Settings.Default.DX_Ver.Equals("DX11") && isUncompressed)
                    {
                        if(textureNameSize > 50)
                        {
                            fileName = fileName.Insert(0, "--");

                            int mtrlOffset = Helper.GetDataOffset(FFCRC.GetHash(fullPath.Substring(0, fullPath.LastIndexOf("/"))), FFCRC.GetHash(fileName), Strings.ItemsDat);

                            if (mtrlOffset == 0)
                            {
                                fileName = fileName.Substring(2);
                            }
                        }
                    }

                    int texHash = FFCRC.GetHash(fileName);

                    string mapName = GetMapName(fileName);

                    //if (fileName.Contains("9982"))
                    //{
                    //    fileName = fileName.Replace("1301", "0101").Replace("met", "model");
                    //}

                    if (fileName.Contains("_s.tex"))
                    {
                        mtrlInfo.SpecularPath = fullPath.Substring(0, fullPath.LastIndexOf("/")) + "/" + fileName;
                        mtrlInfo.TextureMaps.Add(new ComboBoxInfo() { Name = mapName, ID = "", IsNum = false });
                        mtrlInfo.SpecularOffset =  Helper.GetDataOffset(FFCRC.GetHash(fullPath.Substring(0, fullPath.LastIndexOf("/"))), FFCRC.GetHash(fileName), Strings.ItemsDat);
                    }
                    else if (fileName.Contains("_d.tex"))
                    {
                        mtrlInfo.DiffusePath = fullPath.Substring(0, fullPath.LastIndexOf("/")) + "/" + fileName;
                        mtrlInfo.TextureMaps.Add(new ComboBoxInfo() { Name = mapName, ID = "", IsNum = false });
                        mtrlInfo.DiffuseOffset = Helper.GetDataOffset(FFCRC.GetHash(fullPath.Substring(0, fullPath.LastIndexOf("/"))), FFCRC.GetHash(fileName), Strings.ItemsDat);
                    }
                    else if (fileName.Contains("_n.tex"))
                    {
                        mtrlInfo.NormalPath = fullPath.Substring(0, fullPath.LastIndexOf("/")) + "/" + fileName;
                        mtrlInfo.TextureMaps.Add(new ComboBoxInfo() { Name = mapName, ID = "", IsNum = false });
                        mtrlInfo.NormalOffset = Helper.GetDataOffset(FFCRC.GetHash(fullPath.Substring(0, fullPath.LastIndexOf("/"))), FFCRC.GetHash(fileName), Strings.ItemsDat);
                    }
                    else if (fileName.Contains("_m.tex"))
                    {
                        mtrlInfo.MaskPath = fullPath.Substring(0, fullPath.LastIndexOf("/")) + "/" + fileName;
                        mtrlInfo.TextureMaps.Add(new ComboBoxInfo() { Name = mapName, ID = "", IsNum = false });
                        mtrlInfo.MaskOffset = Helper.GetDataOffset(FFCRC.GetHash(fullPath.Substring(0, fullPath.LastIndexOf("/"))), FFCRC.GetHash(fileName), Strings.ItemsDat);
                    }
                }

                if(numOfColorSets > 0 && colorDataSize > 0)
                {
                    br.BaseStream.Seek((16 + (numOfTextures * 4) + (numOfMaps * 4) + (numOfColorSets * 4) + textureNameSize + 4), SeekOrigin.Begin);
                    mtrlInfo.TextureMaps.Add(new ComboBoxInfo() { Name = Strings.ColorSet, ID = "", IsNum = false });

                    if (colorDataSize == 544)
                    {
                        mtrlInfo.ColorData = br.ReadBytes(colorDataSize - 32);
                        mtrlInfo.ColorFlags = br.ReadBytes(32);
                    }
                    else
                    {
                        mtrlInfo.ColorData = br.ReadBytes(colorDataSize);
                    }
                }
            }

            return mtrlInfo;
        }


        public static VFXData GetVFXData(int offset)
        {
            VFXData vfxData = new VFXData();

            int datNum = ((offset / 8) & 0x000f) / 2;

            var decompBytes = Helper.GetType2DecompressedData(offset, datNum, Strings.ItemsDat);

            using (BinaryReader br = new BinaryReader(new MemoryStream(decompBytes)))
            {
                int data = br.ReadInt32();

                while(data != 5531000)
                {
                    data = br.ReadInt32();
                }

                bool eol = false;
                while(data == 5531000)
                {
                    var pathLength = br.ReadInt32();

                    string fullPath = Encoding.ASCII.GetString(br.ReadBytes(pathLength)).Replace("\0", "");

                    vfxData.VFXPaths.Add(fullPath);

                    var space = br.ReadByte();


                    while (space == 0)
                    {
                        try
                        {
                            space = br.ReadByte();
                        }
                        catch
                        {
                            eol = true;
                            break;
                        }
                    }
                    
                    if(!eol && br.ReadInt16() == 21605)
                    {
                        br.ReadByte();
                    }
                    else
                    {
                        data = 0;
                        break;
                    }
                }
            }
            return vfxData;
        }

        public static Tuple<MTRLData, ObservableCollection<ComboBoxInfo>> GetUIData(ItemData item)
        {
            MTRLData mtrlData = new MTRLData();
            ObservableCollection<ComboBoxInfo> cbi = new ObservableCollection<ComboBoxInfo>();

            if (item.ItemCategory.Equals(Strings.Items))
            {
                var iconBaseNum = item.Icon.Substring(0, 2).PadRight(item.Icon.Length, '0');
                var iconFolder = string.Format(Strings.IconFolder, iconBaseNum.PadLeft(6, '0'));
                var iconHQFolder = string.Format(Strings.IconHQFolder, iconBaseNum.PadLeft(6, '0'));
                var iconFile = string.Format(Strings.UIFile, item.Icon.PadLeft(6, '0'));

                mtrlData.UIPath = iconFolder + "/" + iconFile;
                mtrlData.UIHQPath = iconHQFolder + "/" + iconFile;

                mtrlData.UIOffset = Helper.GetDataOffset(FFCRC.GetHash(iconFolder), FFCRC.GetHash(iconFile), Strings.UIDat);
                mtrlData.UIHQOffset = Helper.GetDataOffset(FFCRC.GetHash(iconHQFolder), FFCRC.GetHash(iconFile), Strings.UIDat);

                if (mtrlData.UIOffset != 0)
                {
                    cbi.Add(new ComboBoxInfo() { Name = "Icon", ID = "Icon", IsNum = false });
                }

                if (mtrlData.UIHQOffset != 0)
                {
                    cbi.Add(new ComboBoxInfo() { Name = "HQ Icon", ID = "HQIcon", IsNum = false });
                }
            }
            else if (item.ItemCategory.Equals(Strings.Maps))
            {
                var mapFolder = string.Format(Strings.MapFolder, item.UIPath);
                var mapName = item.UIPath.Replace("/", "");

                var fileHashList = Helper.GetAllFilesInFolder(FFCRC.GetHash(mapFolder), Strings.UIDat);

                mtrlData.UIPath = mapFolder + "/" + mapName + "{0}.tex" ;

                int offset = 0;

                var mapFile = string.Format(Strings.MapFile1, mapName, "m");

                if (fileHashList.Contains(FFCRC.GetHash(mapFile)))
                {
                    offset = Helper.GetDataOffset(FFCRC.GetHash(mapFolder), FFCRC.GetHash(mapFile), Strings.UIDat);
                    cbi.Add(new ComboBoxInfo() { Name = "HighRes Map", ID = offset.ToString(), IsNum = false });
                }

                mapFile = string.Format(Strings.MapFile1, mapName, "s");

                if (fileHashList.Contains(FFCRC.GetHash(mapFile)))
                {
                    offset = Helper.GetDataOffset(FFCRC.GetHash(mapFolder), FFCRC.GetHash(mapFile), Strings.UIDat);
                    cbi.Add(new ComboBoxInfo() { Name = "LowRes Map", ID = offset.ToString(), IsNum = false });
                }

                if (fileHashList.Count > 2)
                {
                    mapFile = string.Format(Strings.MapFile2, mapName);

                    if (fileHashList.Contains(FFCRC.GetHash(mapFile)))
                    {
                        offset = Helper.GetDataOffset(FFCRC.GetHash(mapFolder), FFCRC.GetHash(mapFile), Strings.UIDat);
                        cbi.Add(new ComboBoxInfo() { Name = "PoI", ID = offset.ToString(), IsNum = false });
                    }

                    mapFile = string.Format(Strings.MapFile3, mapName, "m");

                    if (fileHashList.Contains(FFCRC.GetHash(mapFile)))
                    {
                        offset = Helper.GetDataOffset(FFCRC.GetHash(mapFolder), FFCRC.GetHash(mapFile), Strings.UIDat);
                        cbi.Add(new ComboBoxInfo() { Name = "HighRes Mask", ID = offset.ToString(), IsNum = false });
                    }

                    mapFile = string.Format(Strings.MapFile3, mapName, "s");

                    if (fileHashList.Contains(FFCRC.GetHash(mapFile)))
                    {
                        offset = Helper.GetDataOffset(FFCRC.GetHash(mapFolder), FFCRC.GetHash(mapFile), Strings.UIDat);
                        cbi.Add(new ComboBoxInfo() { Name = "LowRes Mask", ID = offset.ToString(), IsNum = false });
                    }
                }
            }
            else if (item.ItemCategory.Equals(Strings.Actions) || item.ItemCategory.Equals(Strings.MapSymbol) || item.ItemCategory.Equals(Strings.OnlineStatus) || item.ItemCategory.Equals(Strings.Weather))
            {
                var iconBaseNum = "0";
                if(int.Parse(item.Icon) > 1000)
                {
                    if(int.Parse(item.Icon) > 10000)
                    {
                        iconBaseNum = item.Icon.Substring(0, 2).PadRight(item.Icon.Length, '0');
                    }
                    else
                    {
                        iconBaseNum = item.Icon.Substring(0, 1).PadRight(item.Icon.Length, '0');
                    }
                }

                var iconFolder = string.Format(Strings.IconFolder, iconBaseNum.PadLeft(6, '0'));
                var iconFile = string.Format(Strings.UIFile, item.Icon.PadLeft(6, '0'));

                mtrlData.UIPath = iconFolder + "/" + iconFile;

                mtrlData.UIOffset = Helper.GetDataOffset(FFCRC.GetHash(iconFolder), FFCRC.GetHash(iconFile), Strings.UIDat);

                if (mtrlData.UIOffset != 0)
                {
                    cbi.Add(new ComboBoxInfo() { Name = "Icon", ID = "Icon", IsNum = false });
                }
            }
            else if (item.ItemCategory.Equals("HUD"))
            {
                mtrlData.UIPath = item.UIPath;
                var HUDFolder = "ui/uld";
                var HUDFile = Path.GetFileName(item.UIPath);

                mtrlData.UIOffset = Helper.GetDataOffset(FFCRC.GetHash(HUDFolder), FFCRC.GetHash(HUDFile), Strings.UIDat);

                if (mtrlData.UIOffset != 0)
                {
                    cbi.Add(new ComboBoxInfo() { Name = "Texture", ID = "Texture", IsNum = false });
                }
            }
            else if (item.ItemCategory.Equals("LoadingImage"))
            {
                mtrlData.UIPath = item.UIPath;
                var LIFolder = "ui/loadingimage";
                var LIFile = Path.GetFileName(item.UIPath);

                mtrlData.UIOffset = Helper.GetDataOffset(FFCRC.GetHash(LIFolder), FFCRC.GetHash(LIFile), Strings.UIDat);

                if (mtrlData.UIOffset != 0)
                {
                    cbi.Add(new ComboBoxInfo() { Name = "Texture", ID = "Texture", IsNum = false });
                }
            }
            else if (item.ItemCategory.Equals(Strings.Status))
            {
                var iconBaseNum = item.Icon.Substring(0, 2).PadRight(item.Icon.Length, '0');
                var iconFolder = string.Format(Strings.IconFolder, iconBaseNum.PadLeft(6, '0'));
                var iconFile = string.Format(Strings.UIFile, item.Icon.PadLeft(6, '0'));

                mtrlData.UIPath = iconFolder + "/" + iconFile;

                mtrlData.UIOffset = Helper.GetDataOffset(FFCRC.GetHash(iconFolder), FFCRC.GetHash(iconFile), Strings.UIDat);

                if (mtrlData.UIOffset != 0)
                {
                    cbi.Add(new ComboBoxInfo() { Name = "Icon", ID = "Icon", IsNum = false });
                }
            }


            return new Tuple<MTRLData, ObservableCollection<ComboBoxInfo>>(mtrlData, cbi);
        }

        /// <summary>
        /// Gets the bitmap from the colorset data
        /// </summary>
        /// <param name="offset">The offset of the MTRL file</param>
        /// <returns>Bitmap from the colorset</returns>
        public static Tuple<Bitmap, byte[]> GetColorBitmap(int offset)
        {
            int datNum = ((offset / 8) & 0x000f) / 2;
            byte[] colorData = null;

            using (BinaryReader br = new BinaryReader(new MemoryStream(Helper.GetType2DecompressedData(offset, datNum, Strings.ItemsDat))))
            {
                br.BaseStream.Seek(6, SeekOrigin.Begin);
                short colorDataSize = br.ReadInt16();
                short textureNameSize = br.ReadInt16();
                br.ReadBytes(2);
                byte numOfTextures = br.ReadByte();
                byte numOfMaps = br.ReadByte();
                byte numOfColorSets = br.ReadByte();
                byte unknown = br.ReadByte();

                if (numOfColorSets > 0 && colorDataSize > 0)
                {
                    br.BaseStream.Seek((16 + (numOfTextures * 4) + (numOfMaps * 4) + (numOfColorSets * 4) + textureNameSize + 4), SeekOrigin.Begin);

                    if (colorDataSize == 544)
                    {
                        colorData = br.ReadBytes(colorDataSize - 32);
                    }
                    else
                    {
                        colorData = br.ReadBytes(colorDataSize);
                    }
                }
                else
                {
                    return null;
                }
            }

            return new Tuple<Bitmap, byte[]>(TEX.TextureToBitmap(colorData, 9312, 4, 16), colorData);
        }

        /// <summary>
        /// Gets the name of the texture map
        /// </summary>
        /// <param name="fileName">The name of the file</param>vfx
        /// <returns>The texture map name</returns>
        private static string GetMapName(string fileName)
        {
            if (fileName.Contains("_s.tex"))
            {
                return Strings.Specular;
            }
            else if (fileName.Contains("_d.tex"))
            {
                return Strings.Diffuse;
            }
            else if (fileName.Contains("_n.tex"))
            {
                return Strings.Normal;
            }
            else if (fileName.Contains("_m.tex"))
            {
                if (fileName.Contains("skin"))
                {
                    return Strings.Skin;
                }
                else
                {
                    return Strings.Mask;
                }
            }
            else
            {
                return "None";
            }
        }

    }
}