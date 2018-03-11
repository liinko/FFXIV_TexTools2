using FFXIV_TexTools2.Helpers;
using FFXIV_TexTools2.Model;
using FFXIV_TexTools2.Resources;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace FFXIV_TexTools2.Views
{
    /// <summary>
    /// Interaction logic for ImportModPack.xaml
    /// </summary>
    public partial class ImportModPack : Window
    {
        List<ModPackItems> packList = new List<ModPackItems>();
        string packPath;

        public ImportModPack(string modPackPath)
        {
            InitializeComponent();

            packPath = modPackPath;

            InfoHeader.Content = "The following Mods will be installed.";

            List<ModPackItems> mpiList = new List<ModPackItems>();
            List<ModPackJson> mpjList = new List<ModPackJson>();

            ModPackInfo mpInfo;

            try
            {
                using(ZipArchive archive = ZipFile.OpenRead(packPath))
                {
                    foreach(var entry in archive.Entries)
                    {
                        if (entry.FullName.EndsWith(".mpl"))
                        {
                            using (StreamReader sr = new StreamReader(entry.Open()))
                            {
                                mpInfo = JsonConvert.DeserializeObject<ModPackInfo>(sr.ReadLine());

                                while(sr.Peek() >= 0)
                                {
                                    mpjList.Add(JsonConvert.DeserializeObject<ModPackJson>(sr.ReadLine()));
                                }
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                FlexibleMessageBox.Show("Error opening TexTools ModPack file. \n" + ex.Message, "ImportModPack Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Debug.WriteLine(ex.StackTrace);
            }

            var totalModSize = 0;

            foreach(var mpi in mpjList)
            {
                var r = "-";
                var num = "-";
                var type = "-";

                if (mpi.FullPath.Contains("ui/"))
                {
                    r = "UI";
                }
                else if (mpi.FullPath.Contains("monster"))
                {
                    r = "Monster";
                }
                else if (mpi.FullPath.Contains(".tex") || mpi.FullPath.Contains(".mdl"))
                {
                    if (mpi.FullPath.Contains("accessory") || mpi.FullPath.Contains("weapon") || mpi.FullPath.Contains("/common/"))
                    {
                        r = Strings.All;
                    }
                    else
                    {
                        if (mpi.FullPath.Contains("demihuman"))
                        {
                            r = "DemiHuman";
                        }
                        else if (mpi.FullPath.Contains("/v"))
                        {
                            r = mpi.FullPath.Substring(mpi.FullPath.IndexOf("_c") + 2, 4);

                            r = Info.IDRace[r];
                        }
                        else
                        {
                            r = mpi.FullPath.Substring(mpi.FullPath.IndexOf("/c") + 2, 4);

                            r = Info.IDRace[r];
                        }
                    }

                    if (mpi.FullPath.Contains("/human/") && mpi.FullPath.Contains("/body/"))
                    {
                        var t = mpi.FullPath.Substring(mpi.FullPath.LastIndexOf("/b") + 2, 4);
                        num = int.Parse(t).ToString();
                    }

                    if (mpi.FullPath.Contains("/face/"))
                    {
                        var t = mpi.FullPath.Substring(mpi.FullPath.LastIndexOf("/f") + 2, 4);
                        num = int.Parse(t).ToString();

                        if (mpi.FullPath.Contains(".tex"))
                        {
                            type = FaceTypes[mpi.FullPath.Substring(mpi.FullPath.LastIndexOf("_") - 3, 3)];
                        }
                    }

                    if (mpi.FullPath.Contains("/hair/"))
                    {
                        var t = mpi.FullPath.Substring(mpi.FullPath.LastIndexOf("/h") + 2, 4);
                        num = int.Parse(t).ToString();

                        if (mpi.FullPath.Contains(".tex"))
                        {
                            type = HairTypes[mpi.FullPath.Substring(mpi.FullPath.LastIndexOf("_") - 3, 3)];
                        }
                    }

                    if (mpi.FullPath.Contains("decal_face"))
                    {
                        var length = mpi.FullPath.LastIndexOf(".") - (mpi.FullPath.LastIndexOf("_") + 1);
                        var t = mpi.FullPath.Substring(mpi.FullPath.LastIndexOf("_") + 1, length);

                        num = int.Parse(t).ToString();
                    }

                    if (mpi.FullPath.Contains("decal_equip"))
                    {
                        var t = mpi.FullPath.Substring(mpi.FullPath.LastIndexOf("_") + 1, 3);

                        try
                        {
                            num = int.Parse(t).ToString();
                        }
                        catch
                        {
                            if (mpi.FullPath.Contains("stigma"))
                            {
                                num = "stigma";
                            }
                            else
                            {
                                num = "Error";
                            }
                        }
                    }
                }

                var m = "3D";

                if (mpi.FullPath.Contains("ui/"))
                {
                    var t = mpi.FullPath.Substring(mpi.FullPath.IndexOf("/") + 1);
                    m = t.Substring(0, t.IndexOf("/"));
                }
                else if (mpi.FullPath.Contains(".tex"))
                {
                    m = GetMapName(mpi.FullPath);
                }
                else if (mpi.FullPath.Contains(".mtrl"))
                {
                    m = "ColorSet";
                }

                totalModSize += mpi.ModSize;

                var isActive = Helper.IsActive(mpi.FullPath, mpi.DatFile);
                mpiList.Add(new ModPackItems { Name = mpi.Name, Category = mpi.Category, Race = r, Part = type, Num = num, Map = m, Active = isActive, mEntry = mpi });
            }

            mpiList.Sort();
            listView.ItemsSource = mpiList;
            listView.SelectAll();

            modCount.Content = mpiList.Count;

            string sizeSuffix = " Bytes";

            if(totalModSize > 1024 && totalModSize < 1048576)
            {
                totalModSize = totalModSize / 1024;
                sizeSuffix = " KB";
            }
            else if(totalModSize > 1048576 && totalModSize < 1073741824)
            {
                totalModSize = totalModSize / 1048576;
                sizeSuffix = " MB";
            }
            else if (totalModSize > 1073741824)
            {
                totalModSize = totalModSize / 1073741824;
                sizeSuffix = " GB";
            }

            modSize.Content = totalModSize + sizeSuffix;
        }

        private static Dictionary<string, string> FaceTypes = new Dictionary<string, string>
        {
            {"fac", Strings.Face},
            {"iri", Strings.Iris},
            {"etc", Strings.Etc},
            {"acc", Strings.Accessory}
        };

        private static Dictionary<string, string> HairTypes = new Dictionary<string, string>
        {
            {"acc", Strings.Accessory},
            {"hir", Strings.Hair},
        };

        private static Dictionary<string, string> slotAbr = new Dictionary<string, string>
        {
            {"met", Strings.Head},
            {"glv", Strings.Hands},
            {"dwn", Strings.Legs},
            {"sho", Strings.Feet},
            {"top", Strings.Body},
            {"ear", Strings.Ears},
            {"nek", Strings.Neck},
            {"rir", Strings.Rings},
            {"wrs", Strings.Wrists},
        };

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
            else if (fileName.Contains("decal"))
            {
                return Strings.Mask;
            }
            else
            {
                return "None";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var mpi in packList)
            {
                //check if they are in modlist
                //write data to dat
                //update index files
                JsonEntry modEntry = null;
                bool inModList = false;
                bool overwrite = false;
                int lineNum = 0;
                int originalOffset = 0;
                int offset = 0;

                var dataOffset = mpi.mEntry.ModOffset;
                byte[] dataBytes = new byte[mpi.mEntry.ModSize];
                List<byte> modDataList = new List<byte>();

                using (ZipArchive archive = ZipFile.OpenRead(packPath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (entry.FullName.EndsWith(".mpd"))
                        {
                            var stream = entry.Open();
                            using(var ms = new MemoryStream())
                            {
                                stream.CopyTo(ms);

                                using (var br = new BinaryReader(ms))
                                {
                                    br.BaseStream.Seek(dataOffset, SeekOrigin.Begin);
                                    modDataList.AddRange(br.ReadBytes(mpi.mEntry.ModSize));
                                }
                            }
                        }
                    }
                }

                try
                {

                }
                catch (Exception ex)
                {
                    FlexibleMessageBox.Show("Error opening TexTools ModPack file. \n" + ex.Message, "ImportModPack Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Debug.WriteLine(ex.StackTrace);

                }

                try
                {
                    using (StreamReader sr = new StreamReader(Properties.Settings.Default.Modlist_Directory))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            modEntry = JsonConvert.DeserializeObject<JsonEntry>(line);
                            if (modEntry.fullPath.Equals(mpi.mEntry.FullPath))
                            {
                                inModList = true;
                                break;
                            }
                            lineNum++;
                        }
                    }

                    var datNum = int.Parse(Info.ModDatDict[mpi.mEntry.DatFile]);

                    var modDatPath = string.Format(Info.datDir, mpi.mEntry.DatFile, datNum);

                    var datOffsetAmount = 16 * datNum;

                    using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(modDatPath)))
                    {
                        //is in modlist and size of new mod is less than or equal to existing mod size
                        if (inModList && mpi.mEntry.ModSize <= modEntry.modSize)
                        {
                            int sizeDiff = modEntry.modSize - modDataList.Count;

                            bw.BaseStream.Seek(modEntry.modOffset - datOffsetAmount, SeekOrigin.Begin);

                            bw.Write(modDataList.ToArray());

                            bw.Write(new byte[sizeDiff]);

                            Helper.UpdateIndex(modEntry.modOffset, mpi.mEntry.FullPath, mpi.mEntry.DatFile);
                            Helper.UpdateIndex2(modEntry.modOffset, mpi.mEntry.FullPath, mpi.mEntry.DatFile);

                            offset = modEntry.modOffset;

                            overwrite = true;
                        }

                        if (!overwrite)
                        {
                            bw.BaseStream.Seek(0, SeekOrigin.End);

                            while ((bw.BaseStream.Position & 0xFF) != 0)
                            {
                                bw.Write((byte)0);
                            }

                            int eof = (int)bw.BaseStream.Position + modDataList.Count;

                            while ((eof & 0xFF) != 0)
                            {
                                modDataList.AddRange(new byte[16]);
                                eof = eof + 16;
                            }

                            offset = (int)bw.BaseStream.Position + datOffsetAmount;

                            if (offset != 0)
                            {
                                bw.Write(modDataList.ToArray());
                            }
                            else
                            {
                                FlexibleMessageBox.Show("There was an issue obtaining the .dat4 offset to write data to, try importing again. " +
                                                "\n\n If the problem persists, please submit a bug report.", "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }


                            int oldOffset = Helper.UpdateIndex(offset, mpi.mEntry.FullPath, mpi.mEntry.DatFile) * 8;
                            Helper.UpdateIndex2(offset, mpi.mEntry.FullPath, mpi.mEntry.DatFile);

                            //is in modlist and size of new mod is larger than existing mod size 
                            if (inModList && mpi.mEntry.ModSize > modEntry.modSize)
                            {
                                oldOffset = modEntry.originalOffset;

                                JsonEntry replaceEntry = new JsonEntry()
                                {
                                    category = String.Empty,
                                    name = String.Empty,
                                    fullPath = String.Empty,
                                    originalOffset = 0,
                                    modOffset = modEntry.modOffset,
                                    modSize = modEntry.modSize,
                                    datFile = mpi.mEntry.DatFile
                                };

                                string[] lines = File.ReadAllLines(Properties.Settings.Default.Modlist_Directory);
                                lines[lineNum] = JsonConvert.SerializeObject(replaceEntry);
                                File.WriteAllLines(Properties.Settings.Default.Modlist_Directory, lines);
                            }

                            JsonEntry entry = new JsonEntry()
                            {
                                category = mpi.Category,
                                name = mpi.Name,
                                fullPath = mpi.mEntry.FullPath,
                                originalOffset = oldOffset,
                                modOffset = offset,
                                modSize = mpi.mEntry.ModSize,
                                datFile = mpi.mEntry.DatFile
                            };

                            try
                            {
                                using (StreamWriter modFile = new StreamWriter(Properties.Settings.Default.Modlist_Directory, true))
                                {
                                    modFile.BaseStream.Seek(0, SeekOrigin.End);
                                    modFile.WriteLine(JsonConvert.SerializeObject(entry));
                                }
                            }
                            catch (Exception ex)
                            {
                                FlexibleMessageBox.Show("Error Accessing .modlist File \n" + ex.Message, "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }

                        }
                    }

                }
                catch (Exception ex)
                {
                    FlexibleMessageBox.Show("There was an error in importing. \n" + ex.Message, "ImportModPack Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (ModPackItems added in e.AddedItems)
            {
                packList.Add(added);
                if(modCount.Content != null)
                {
                    var c = int.Parse(modCount.Content.ToString());
                    c++;
                    modCount.Content = c;

                    var mss = modSize.Content.ToString();
                    var s = int.Parse(mss.Substring(0, mss.IndexOf(" ")));
                    var ss = mss.Substring(mss.IndexOf(" "), 3);
                    var ms = added.mEntry.ModSize;

                    if (ss.Equals(" KB"))
                    {
                        ms = ms / 1024;
                    }
                    else if (ss.Equals(" MB"))
                    {
                        ms = ms / 1048576;
                    }

                    s += ms;
                    modSize.Content = s + ss;
                }

            }

            foreach (ModPackItems removed in e.RemovedItems)
            {
                packList.Remove(removed);

                if (modCount.Content != null)
                {
                    var c = int.Parse(modCount.Content.ToString());
                    c--;
                    modCount.Content = c;

                    var mss = modSize.Content.ToString();
                    var s = int.Parse(mss.Substring(0, mss.IndexOf(" ")));
                    var ss = mss.Substring(mss.IndexOf(" "), 3);
                    var ms = removed.mEntry.ModSize;

                    if (ss.Equals(" KB"))
                    {
                        ms = ms / 1024;
                    }
                    else if (ss.Equals(" MB"))
                    {
                        ms = ms / 1048576;
                    }

                    s -= ms;
                    modSize.Content = s + ss;
                }

            }
        }
    }
}
