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
using System.IO;
using System.Linq;
using System.Text;

namespace FFXIV_TexTools2.Material
{
    public static class MTRL
    {
        public static List<ComboBoxInfo> GetMTRLRaces(Items item, string selectedParent, string IMCVersion)
        {
            List<ComboBoxInfo> cbiList;
            Dictionary<int, string> racesDict = new Dictionary<int, string>();
            string MTRLFolder;

            if (item.itemName.Equals(Strings.Body))
            {
                foreach (string race in Info.IDRace.Keys)
                {
                    MTRLFolder = String.Format(Strings.BodyMtrlFolder, race, "0001");

                    racesDict.Add(FFCRC.GetHash(MTRLFolder), race);
                }

                 cbiList =  Helper.FolderExistsListRace(racesDict).ToList();
            }
            else if (item.itemName.Equals(Strings.Face))
            {
                foreach (string race in Info.IDRace.Keys)
                {
                    MTRLFolder = String.Format(Strings.FaceMtrlFolder, race, "0001");

                    racesDict.Add(FFCRC.GetHash(MTRLFolder), race);
                }

                cbiList = Helper.FolderExistsListRace(racesDict).ToList();
            }
            else if (item.itemName.Equals(Strings.Hair))
            {
                foreach (string race in Info.IDRace.Keys)
                {
                    MTRLFolder = String.Format(Strings.HairMtrlFolder, race, "0001");

                    racesDict.Add(FFCRC.GetHash(MTRLFolder), race);
                }

                cbiList = Helper.FolderExistsListRace(racesDict).ToList();
            }
            else if (item.itemName.Equals(Strings.Tail))
            {
                foreach (string race in Info.IDRace.Keys)
                {
                    MTRLFolder = String.Format(Strings.TailMtrlFolder, race, "0001");

                    racesDict.Add(FFCRC.GetHash(MTRLFolder), race);
                }

                cbiList = Helper.FolderExistsListRace(racesDict).ToList();
            }
            else if (item.itemName.Equals(Strings.Face_Paint) || item.itemName.Equals(Strings.Equipment_Decals) || item.itemID.Equals("9901"))
            {
                cbiList = new List<ComboBoxInfo>();
                ComboBoxInfo cbi = new ComboBoxInfo(Strings.All, "0");
                cbiList.Add(cbi);
            }
            else if (selectedParent.Equals(Strings.Pets) || selectedParent.Equals(Strings.Mounts) || selectedParent.Equals(Strings.Minions))
            {
                cbiList = new List<ComboBoxInfo>();
                ComboBoxInfo cbi = new ComboBoxInfo(Strings.Monster, "0");
                cbiList.Add(cbi);
            }
            else
            {
                string type = Helper.GetItemType(selectedParent);
                string MTRLFile;
                int fileHash;

                List<ComboBoxInfo> cbiInfo = new List<ComboBoxInfo>();

                var FileOffsetDict = Helper.GetAllFilesInFolder(FFCRC.GetHash(item.mtrlFolder + IMCVersion));

                if (type.Equals("weapon") || type.Equals("accessory"))
                {
                    cbiInfo.Add(new ComboBoxInfo(Strings.All, Strings.All));
                }
                else
                {
                    foreach (string raceID in Info.raceID.Values)
                    {
                        MTRLFile = String.Format(Strings.EquipMtrlFile, raceID, item.itemID, Info.slotAbr[selectedParent], "a");
                    
                        fileHash = FFCRC.GetHash(MTRLFile);

                        if (FileOffsetDict.Keys.Contains(fileHash))
                        {
                            cbiInfo.Add(new ComboBoxInfo(Info.IDRace[raceID], raceID));
                        }
                    }
                }

                cbiList = cbiInfo;
            }
            return cbiList;
        }

        public static List<ComboBoxInfo> GetMTRLParts(ComboBoxInfo race, Items item, string IMCVersion, string selectedParent)
        {
            Dictionary<int, int> MTRLDict = new Dictionary<int, int>();
            List<ComboBoxInfo> cbiList;
            string MTRLFolder;

            if (item.itemName.Equals(Strings.Body))
            {
                for (int i = 1; i < 251; i++)
                {
                    MTRLFolder = String.Format(Strings.BodyMtrlFolder, race.ID, i.ToString().PadLeft(4, '0'));

                    MTRLDict.Add(FFCRC.GetHash(MTRLFolder), i);
                }

                cbiList = Helper.FolderExistsList(MTRLDict).ToList();
            }
            else if (item.itemName.Equals(Strings.Face))
            {
                for (int i = 1; i < 251; i++)
                {
                    MTRLFolder = String.Format(Strings.FaceMtrlFolder, race.ID, i.ToString().PadLeft(4, '0'));

                    MTRLDict.Add(FFCRC.GetHash(MTRLFolder), i);
                }

                cbiList = Helper.FolderExistsList(MTRLDict).ToList();
            }
            else if (item.itemName.Equals(Strings.Hair))
            {
                for (int i = 1; i < 251; i++)
                {
                    MTRLFolder = String.Format(Strings.HairMtrlFolder, race.ID, i.ToString().PadLeft(4, '0'));

                    MTRLDict.Add(FFCRC.GetHash(MTRLFolder), i);
                }

                cbiList = Helper.FolderExistsList(MTRLDict).ToList();
            }
            else if (item.itemName.Equals(Strings.Tail))
            {
                for (int i = 1; i < 251; i++)
                {
                    MTRLFolder = String.Format(Strings.TailMtrlFolder, race.ID, i.ToString().PadLeft(4, '0'));

                    MTRLDict.Add(FFCRC.GetHash(MTRLFolder), i);
                }

                cbiList = Helper.FolderExistsList(MTRLDict).ToList();
            }
            else if (item.itemName.Equals(Strings.Face_Paint))
            {
                MTRLDict = Helper.GetAllFilesInFolder(FFCRC.GetHash(Strings.FacePaintFolder));
                cbiList = new List<ComboBoxInfo>();

                for (int i = 1; i < 100; i++)
                {
                    MTRLFolder = String.Format(Strings.FacePaintFile, i);

                    if (MTRLDict.Keys.Contains(FFCRC.GetHash(MTRLFolder)))
                    {
                        cbiList.Add(new ComboBoxInfo(i.ToString(), i.ToString()));
                    }
                }
            }
            else if (item.itemName.Equals(Strings.Equipment_Decals))
            {
                MTRLDict = Helper.GetAllFilesInFolder(FFCRC.GetHash(Strings.EquipDecalFolder));
                cbiList = new List<ComboBoxInfo>();

                for (int i = 1; i < 300; i++)
                {
                    MTRLFolder = String.Format(Strings.EquipDecalFile, i.ToString().PadLeft(3, '0'));

                    if (MTRLDict.Keys.Contains(FFCRC.GetHash(MTRLFolder)))
                    {
                        cbiList.Add(new ComboBoxInfo(i.ToString(), i.ToString()));
                    }
                }
            }
            else if (selectedParent.Equals(Strings.Pets))
            {
                int part = 1;

                if (item.itemName.Equals(Strings.Selene) || item.itemName.Equals(Strings.Bishop_Autoturret))
                {
                    part = 2;
                }

                for (int i = 1; i < 20; i++)
                {
                    MTRLFolder = String.Format(Strings.MonsterMtrlFolder, Info.petID[item.itemName], part.ToString().PadLeft(4, '0')) + i.ToString().PadLeft(4, '0');

                    MTRLDict.Add(FFCRC.GetHash(MTRLFolder), i);
                }

                cbiList = Helper.FolderExistsList(MTRLDict).ToList();
            }
            else if (selectedParent.Equals(Strings.Mounts))
            {
                cbiList = new List<ComboBoxInfo>();

                if (item.itemID.Equals("1") || item.itemID.Equals("2") || item.itemID.Equals("1011") || item.itemID.Equals("1022"))
                {
                    cbiList.Add(new ComboBoxInfo("a", "a"));
                }
                else
                {
                    Dictionary<string, int> mountMTRLDict = new Dictionary<string, int>();
                    string[] parts = { "a", "b", "c", "d", "e" };

                    MTRLDict = Helper.GetAllFilesInFolder(FFCRC.GetHash(item.mtrlFolder + IMCVersion));

                    foreach (string c in parts)
                    {
                        MTRLFolder = String.Format(Strings.MonsterMtrlFile, item.itemID.PadLeft(4, '0'), item.weaponBody.PadLeft(4, '0'), c);

                        if (MTRLDict.Keys.Contains(FFCRC.GetHash(MTRLFolder)))
                        {
                            cbiList.Add(new ComboBoxInfo(c, c));
                        }
                    }
                }
            }
            else if (selectedParent.Equals(Strings.Minions))
            {
                Dictionary<string, int> minionMTRLDict = new Dictionary<string, int>();
                string[] parts = { "a", "b", "c", "d", "e" };
                cbiList = new List<ComboBoxInfo>();

                MTRLDict = Helper.GetAllFilesInFolder(FFCRC.GetHash(item.mtrlFolder + IMCVersion));

                foreach (string c in parts)
                {
                    MTRLFolder = String.Format(Strings.MonsterMtrlFile, item.itemID.PadLeft(4, '0'), item.weaponBody.PadLeft(4, '0'), c);

                    if (MTRLDict.Keys.Contains(FFCRC.GetHash(MTRLFolder)))
                    {
                        cbiList.Add(new ComboBoxInfo(c, c));
                    }
                }
            }
            else
            {
                string type = Helper.GetItemType(selectedParent);
                string[] parts = { "a", "b", "c", "d", "e" };
                cbiList = new List<ComboBoxInfo>();

                if (item.hasSecondary)
                {
                    cbiList.Add(new ComboBoxInfo("s", "s"));
                }

                MTRLDict = Helper.GetAllFilesInFolder(FFCRC.GetHash(item.mtrlFolder + IMCVersion));

                foreach (string part in parts)
                {
                    if (type.Equals("weapon") || type.Equals("food"))
                    {
                        MTRLFolder = String.Format(Strings.WeapMtrlFile, item.itemID, item.weaponBody, part);
                    }
                    else if (type.Equals("accessory"))
                    {
                        MTRLFolder = String.Format(Strings.AccMtrlFile, item.itemID, Info.slotAbr[selectedParent], part);
                    }
                    else
                    {
                        MTRLFolder = String.Format(Strings.EquipMtrlFile, race.ID, item.itemID, Info.slotAbr[selectedParent], part);
                    }

                    if (MTRLDict.Keys.Contains(FFCRC.GetHash(MTRLFolder)))
                    {
                        cbiList.Add(new ComboBoxInfo(part, part));
                    }
                }
                cbiList.Sort();
            }

            return cbiList;
        }

        public static Tuple<MTRLInfo, List<ComboBoxInfo>> GetMTRLOffset(ComboBoxInfo race, string selectedParent, Items item, string part, string IMCVersion, string type)
        {
            string MTRLFolder, MTRLFile;
            List<ComboBoxInfo> cbiList;
            int offset;
            Tuple<MTRLInfo, List<ComboBoxInfo>> info;
            MTRLInfo mtrlInfo = null;

            if (item.itemName.Equals(Strings.Face))
            {
                MTRLFolder = String.Format(Strings.FaceMtrlFolder, race.ID, part.PadLeft(4, '0'));

                var fileHashes = Helper.GetAllFilesInFolder(FFCRC.GetHash(MTRLFolder));

                cbiList = new List<ComboBoxInfo>();

                if (fileHashes.Keys.Contains(FFCRC.GetHash(String.Format(Strings.FaceMtrlFile, race.ID, part.PadLeft(4, '0'), "fac"))))
                {
                    cbiList.Add(new ComboBoxInfo(Strings.Face, ""));
                }

                if (fileHashes.Keys.Contains(FFCRC.GetHash(String.Format(Strings.FaceMtrlFile, race.ID, part.PadLeft(4, '0'), "iri"))))
                {
                    cbiList.Add(new ComboBoxInfo(Strings.Iris, ""));
                }

                if (fileHashes.Keys.Contains(FFCRC.GetHash(String.Format(Strings.FaceMtrlFile, race.ID, part.PadLeft(4, '0'), "etc"))))
                {
                    cbiList.Add(new ComboBoxInfo(Strings.Etc, ""));
                }

                info = new Tuple<MTRLInfo, List<ComboBoxInfo>>(mtrlInfo, cbiList);
            }
            else if (item.itemName.Equals(Strings.Body))
            {
                MTRLFolder = String.Format(Strings.BodyMtrlFolder, race.ID, part.PadLeft(4, '0'));
                MTRLFile = String.Format(Strings.BodyMtrlFile, race.ID, part.PadLeft(4, '0'));
                offset = Helper.GetOffset(FFCRC.GetHash(MTRLFolder), FFCRC.GetHash(MTRLFile));

                mtrlInfo = GetTEXFromMTRL(offset, true);

                info = new Tuple<MTRLInfo, List<ComboBoxInfo>>(mtrlInfo, mtrlInfo.TextureMaps);
            }
            else if (item.itemName.Equals(Strings.Hair))
            {
                MTRLFolder = String.Format(Strings.HairMtrlFolder, race.ID, part.PadLeft(4, '0'));

                var fileHashes = Helper.GetAllFilesInFolder(FFCRC.GetHash(MTRLFolder));

                cbiList = new List<ComboBoxInfo>();

                if (fileHashes.Keys.Contains(FFCRC.GetHash(String.Format(Strings.HairMtrlFile, race.ID, part.PadLeft(4, '0'), "hir", "a"))))
                {
                    cbiList.Add(new ComboBoxInfo(Strings.Hair, ""));
                }

                if (fileHashes.Keys.Contains(FFCRC.GetHash(String.Format(Strings.HairMtrlFile, race.ID, part.PadLeft(4, '0'), "acc", "b"))))
                {
                    cbiList.Add(new ComboBoxInfo(Strings.Accessory, ""));
                }

                info = new Tuple<MTRLInfo, List<ComboBoxInfo>>(mtrlInfo, cbiList);
            }
            else if (item.itemName.Equals(Strings.Face_Paint) || item.itemName.Equals(Strings.Equipment_Decals))
            {
                string texPath = "chara/common/texture/";

                if (item.itemName.Equals(Strings.Face_Paint))
                {
                    texPath = texPath + "decal_face/_decal_{0}.tex";
                }
                else
                {
                    texPath = texPath + "decal_equip/-decal_{0}.tex";
                }

                cbiList = new List<ComboBoxInfo>();
                cbiList.Add(new ComboBoxInfo(Strings.Mask, ""));
                mtrlInfo = new MTRLInfo();
                mtrlInfo.MaskPath = texPath;

                info = new Tuple<MTRLInfo, List<ComboBoxInfo>>(mtrlInfo, cbiList);
            }
            else if (item.itemName.Equals(Strings.Tail))
            {
                MTRLFolder = String.Format(Strings.TailMtrlFolder, race.ID, part.PadLeft(4, '0'));
                MTRLFile = string.Format(Strings.TailMtrlFile, race.ID, part.PadLeft(4, '0'));

                offset = Helper.GetOffset(FFCRC.GetHash(MTRLFolder), FFCRC.GetHash(MTRLFile));

                mtrlInfo = GetTEXFromMTRL(offset, true);

                info = new Tuple<MTRLInfo, List<ComboBoxInfo>>(mtrlInfo, mtrlInfo.TextureMaps);
            }
            else if (selectedParent.Equals(Strings.Pets))
            {
                int p = 1;

                if (item.itemName.Equals(Strings.Selene) || item.itemName.Equals(Strings.Bishop_Autoturret))
                {
                    p = 2;
                }

                MTRLFolder = String.Format(Strings.MonsterMtrlFolder, Info.petID[item.itemName], p.ToString().PadLeft(4, '0')) + IMCVersion.PadLeft(4, '0');
                MTRLFile = String.Format(Strings.MonsterMtrlFile, Info.petID[item.itemName], p.ToString().PadLeft(4, '0'), "a");

                offset = Helper.GetOffset(FFCRC.GetHash(MTRLFolder), FFCRC.GetHash(MTRLFile));

                mtrlInfo = GetTEXFromMTRL(offset, false);

                info = new Tuple<MTRLInfo, List<ComboBoxInfo>>(mtrlInfo, mtrlInfo.TextureMaps);
            }
            else if (selectedParent.Equals(Strings.Mounts))
            {
                if (item.itemID.Equals("1") || item.itemID.Equals("2") || item.itemID.Equals("1011") || item.itemID.Equals("1022"))
                {
                    SortedSet<ComboBoxInfo> typeSet = new SortedSet<ComboBoxInfo>();

                    var files = Helper.GetAllFilesInFolder(FFCRC.GetHash(item.mtrlFolder + IMCVersion));

                    foreach (string abr in Info.slotAbr.Values)
                    {
                        MTRLFile = String.Format(Strings.DemiMtrlFile, item.itemID.PadLeft(4, '0'), item.weaponBody.PadLeft(4, '0'), abr);

                        if (files.Keys.Contains(FFCRC.GetHash(MTRLFile)))
                        {
                            typeSet.Add(new ComboBoxInfo(abr, ""));
                        }
                    }

                    info = new Tuple<MTRLInfo, List<ComboBoxInfo>>(mtrlInfo, typeSet.ToList());
                }
                else
                {
                    MTRLFile = String.Format(Strings.MonsterMtrlFile, item.itemID.PadLeft(4, '0'), item.weaponBody.PadLeft(4, '0'), part);
                    offset = Helper.GetOffset(FFCRC.GetHash(item.mtrlFolder + IMCVersion), FFCRC.GetHash(MTRLFile));
                    mtrlInfo = GetTEXFromMTRL(offset, false);

                    info = new Tuple<MTRLInfo, List<ComboBoxInfo>>(mtrlInfo, mtrlInfo.TextureMaps);
                }
            }
            else if (selectedParent.Equals(Strings.Minions))
            {
                MTRLFile = String.Format(Strings.MonsterMtrlFile, item.itemID.PadLeft(4, '0'), item.weaponBody.PadLeft(4, '0'), part);
                offset = Helper.GetOffset(FFCRC.GetHash(item.mtrlFolder + IMCVersion), FFCRC.GetHash(MTRLFile));
                mtrlInfo = GetTEXFromMTRL(offset, false);

                info = new Tuple<MTRLInfo, List<ComboBoxInfo>>(mtrlInfo, mtrlInfo.TextureMaps);
            }
            else
            {
                type = Helper.GetItemType(selectedParent);
                string imcVersion = IMCVersion;
                MTRLFolder = item.mtrlFolder;

                if (type.Equals("weapon"))
                {
                    MTRLFile = String.Format(Strings.WeapMtrlFile, item.itemID, item.weaponBody, part);

                    if (part.Equals("s"))
                    {
                        MTRLFolder = item.sMtrlFolder;
                        imcVersion = IMC.GetVersion(selectedParent, item, true);
                        MTRLFile = String.Format(Strings.WeapMtrlFile, item.itemID1, item.weaponBody1, "a");
                    }
                }
                else if (type.Equals("accessory"))
                {
                    MTRLFile = String.Format(Strings.AccMtrlFile, item.itemID, Info.slotAbr[selectedParent], part);
                }
                else if (type.Equals("food"))
                {
                    MTRLFile = String.Format(Strings.WeapMtrlFile, item.itemID, item.weaponBody, "a");
                }
                else
                {
                    MTRLFile = String.Format(Strings.EquipMtrlFile, race.ID, item.itemID, Info.slotAbr[selectedParent], part);
                }

                offset = Helper.GetOffset(FFCRC.GetHash(MTRLFolder + imcVersion), FFCRC.GetHash(MTRLFile));

                mtrlInfo = GetTEXFromMTRL(offset, false);

                info = new Tuple<MTRLInfo, List<ComboBoxInfo>>(mtrlInfo, mtrlInfo.TextureMaps);
            }

            return info;
        }

        public static Tuple<MTRLInfo, List<ComboBoxInfo>> GetTexFromType(Items item, ComboBoxInfo race, string part, string type, string IMCVersion, string selectedParent)
        {
            string MTRLFolder, MTRLFile;
            bool isUncompressed = true;
            int offset = 0;

            if (item.itemName.Equals(Strings.Face))
            {
                MTRLFolder = String.Format(Strings.FaceMtrlFolder, race.ID, part.PadLeft(4, '0'));
                MTRLFile = String.Format(Strings.FaceMtrlFile, race.ID, part.PadLeft(4, '0'), Info.FaceTypes[type]);
                offset = Helper.GetOffset(FFCRC.GetHash(MTRLFolder), FFCRC.GetHash(MTRLFile));

                isUncompressed = true;
            }
            else if (item.itemName.Equals(Strings.Hair))
            {
                MTRLFolder = String.Format(Strings.HairMtrlFolder, race.ID, part.PadLeft(4, '0'));
                isUncompressed = true;

                if (type.Equals(Strings.Accessory))
                {
                    MTRLFile = String.Format(Strings.HairMtrlFile, race.ID, part.PadLeft(4, '0'), Info.HairTypes[type], "b");
                }
                else
                {
                    MTRLFile = String.Format(Strings.HairMtrlFile, race.ID, part.PadLeft(4, '0'), Info.HairTypes[type], "a");
                }

                offset = Helper.GetOffset(FFCRC.GetHash(MTRLFolder), FFCRC.GetHash(MTRLFile));
            }
            else if (selectedParent.Equals(Strings.Mounts))
            {
                isUncompressed = false;

                if (item.itemID.Equals("1") || item.itemID.Equals("2") || item.itemID.Equals("1011") || item.itemID.Equals("1022"))
                {
                    MTRLFile = String.Format(Strings.DemiMtrlFile, item.itemID.PadLeft(4, '0'), item.weaponBody.PadLeft(4, '0'), type);
                }
                else
                {
                    MTRLFile = String.Format(Strings.MonsterMtrlFile, item.itemID.PadLeft(4, '0'), item.weaponBody.PadLeft(4, '0'), part);
                }

                offset = Helper.GetOffset(FFCRC.GetHash(item.mtrlFolder + IMCVersion), FFCRC.GetHash(MTRLFile));
            }



            var mtrlInfo = GetTEXFromMTRL(offset, isUncompressed);

            return new Tuple<MTRLInfo, List<ComboBoxInfo>>(mtrlInfo, mtrlInfo.TextureMaps);
        }


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

            }

            return Helper.GetOffset(FFCRC.GetHash(decalFolder), FFCRC.GetHash(decalFile));
        }


        public static MTRLInfo GetTEXFromMTRL(int offset, bool isUncompressed)
        {
            int datNum = ((offset / 8) & 0x000f) / 2;

            MTRLInfo info = new MTRLInfo();

            Dictionary<string, int> texOffset = new Dictionary<string, int>();

            using (BinaryReader br = new BinaryReader(new MemoryStream(Helper.GetDecompressedIMCBytes(offset, datNum))))
            {
                br.BaseStream.Seek(6, SeekOrigin.Begin);
                short clrSize = br.ReadInt16();
                short texNameSize = br.ReadInt16();
                br.ReadBytes(2);
                byte texNum = br.ReadByte();
                byte mapNum = br.ReadByte();
                byte clrNum = br.ReadByte();
                byte unkNum = br.ReadByte();

                int headerEnd = 16 + ((texNum + mapNum + clrNum) * 4);

                int[] texPathOffsets = new int[texNum + 1];

                for(int i = 0; i < texNum + 1; i++)
                {
                    texPathOffsets[i] = br.ReadInt16();
                    br.ReadBytes(2);
                }

                br.ReadBytes((mapNum - 1) * 4);

                for (int i = 0; i < texNum; i++)
                {
                    br.BaseStream.Seek(headerEnd + texPathOffsets[i], SeekOrigin.Begin);
                    byte[] bitName = br.ReadBytes(texPathOffsets[i + 1] - texPathOffsets[i]);
                    string fullPath = Encoding.ASCII.GetString(bitName);
                    fullPath = fullPath.Replace("\0", "");
                    string fileName = fullPath.Substring(fullPath.LastIndexOf("/") + 1);

                    if (Properties.Settings.Default.DX_Ver.Equals("DX11") && isUncompressed)
                    {
                        if(texNameSize > 50)
                        {
                            fileName = fileName.Insert(0, "--");

                            int offsetTest = Helper.GetOffset(FFCRC.GetHash(fullPath.Substring(0, fullPath.LastIndexOf("/"))), FFCRC.GetHash(fileName));

                            if (offsetTest == 0)
                            {
                                fileName = fileName.Substring(2);
                            }
                        }
                    }

                    int texHash = FFCRC.GetHash(fileName);

                    string mapName = GetMapName(fileName);


                    if (fileName.Contains("_s.tex"))
                    {
                        info.SpecularPath = fullPath;
                        info.TextureMaps.Add(new ComboBoxInfo(mapName, ""));
                        info.SpecularOffset =  Helper.GetOffset(FFCRC.GetHash(fullPath.Substring(0, fullPath.LastIndexOf("/"))), FFCRC.GetHash(fileName));
                    }
                    else if (fileName.Contains("_d.tex"))
                    {
                        info.DiffusePath = fullPath;
                        info.TextureMaps.Add(new ComboBoxInfo(mapName, ""));
                        info.DiffuseOffset = Helper.GetOffset(FFCRC.GetHash(fullPath.Substring(0, fullPath.LastIndexOf("/"))), FFCRC.GetHash(fileName));
                    }
                    else if (fileName.Contains("_n.tex"))
                    {
                        info.NormalPath = fullPath;
                        info.TextureMaps.Add(new ComboBoxInfo(mapName, ""));
                        info.NormalOffset = Helper.GetOffset(FFCRC.GetHash(fullPath.Substring(0, fullPath.LastIndexOf("/"))), FFCRC.GetHash(fileName));
                    }
                    else if (fileName.Contains("_m.tex"))
                    {
                        info.MaskPath = fullPath;
                        info.TextureMaps.Add(new ComboBoxInfo(mapName, ""));
                        info.MaskOffset = Helper.GetOffset(FFCRC.GetHash(fullPath.Substring(0, fullPath.LastIndexOf("/"))), FFCRC.GetHash(fileName));
                    }
                }

                if(clrNum > 0 && clrSize > 0)
                {
                    br.BaseStream.Seek((16 + (texNum*4) + (mapNum*4) + (clrNum*4) + texNameSize + 4), SeekOrigin.Begin);
                    info.TextureMaps.Add(new ComboBoxInfo(Strings.ColorSet, ""));
                    info.ColorData = br.ReadBytes(clrSize);
                }
            }

            return info;
        }

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
