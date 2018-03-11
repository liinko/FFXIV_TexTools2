using FFXIV_TexTools2.Helpers;
using FFXIV_TexTools2.Model;
using FFXIV_TexTools2.Resources;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Forms = System.Windows.Forms;

namespace FFXIV_TexTools2.Views
{
    /// <summary>
    /// Interaction logic for MakeModPack.xaml
    /// </summary>
    public partial class MakeModPack : Window
    {

        JsonEntry modEntry = null;
        List<ModPackItems> packList = new List<ModPackItems>();
        string mpDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\TexTools\\ModPacks";
        int modCount = 0;
        int modSize = 0;
        ModPackItems selectedItem;
        ListSortDirection lastDirection = ListSortDirection.Ascending;


        public MakeModPack()
        {
            InitializeComponent();

            List<ModPackItems> mpiList = new List<ModPackItems>();

            InfoHeader.Content = "This tool will create a zipped TexTools Mod Pack (*.ttmp) file of the selected mods in the following directory:\n" + mpDir;
            try
            {
                using (StreamReader sr = new StreamReader(Properties.Settings.Default.Modlist_Directory))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        modEntry = JsonConvert.DeserializeObject<JsonEntry>(line);
                        var r = "-";
                        var num = "-";
                        var type = "-";

                        if (!modEntry.fullPath.Equals(""))
                        {
                            if (modEntry.fullPath.Contains("ui/"))
                            {
                                r = "UI";
                            }
                            else if (modEntry.fullPath.Contains("monster"))
                            {
                                r = "Monster";
                            }
                            else if (modEntry.fullPath.Contains(".tex") || modEntry.fullPath.Contains(".mdl") || modEntry.fullPath.Contains(".atex"))
                            {
                                if (modEntry.fullPath.Contains("accessory") || modEntry.fullPath.Contains("weapon") || modEntry.fullPath.Contains("/common/"))
                                {
                                    r = Strings.All;
                                }
                                else
                                {
                                    if (modEntry.fullPath.Contains("demihuman"))
                                    {
                                        r = "DemiHuman";

                                        type = slotAbr[modEntry.fullPath.Substring(modEntry.fullPath.LastIndexOf("_") - 3, 3)];
                                    }
                                    else if (modEntry.fullPath.Contains("/v"))
                                    {
                                        r = modEntry.fullPath.Substring(modEntry.fullPath.IndexOf("_c") + 2, 4);

                                        r = Info.IDRace[r];
                                    }
                                    else
                                    {
                                        r = modEntry.fullPath.Substring(modEntry.fullPath.IndexOf("/c") + 2, 4);

                                        r = Info.IDRace[r];
                                    }
                                }

                                if (modEntry.fullPath.Contains("/human/") && modEntry.fullPath.Contains("/body/"))
                                {
                                    var t = modEntry.fullPath.Substring(modEntry.fullPath.LastIndexOf("/b") + 2, 4);
                                    num = int.Parse(t).ToString();
                                }

                                if (modEntry.fullPath.Contains("/face/"))
                                {
                                    var t = modEntry.fullPath.Substring(modEntry.fullPath.LastIndexOf("/f") + 2, 4);
                                    num = int.Parse(t).ToString();

                                    if (modEntry.fullPath.Contains(".tex"))
                                    {
                                        type = FaceTypes[modEntry.fullPath.Substring(modEntry.fullPath.LastIndexOf("_") - 3, 3)];
                                    }
                                }

                                if (modEntry.fullPath.Contains("decal_face"))
                                {
                                    var length = modEntry.fullPath.LastIndexOf(".") - (modEntry.fullPath.LastIndexOf("_") + 1);
                                    var t = modEntry.fullPath.Substring(modEntry.fullPath.LastIndexOf("_") + 1, length);

                                    num = int.Parse(t).ToString();
                                }

                                if (modEntry.fullPath.Contains("decal_equip"))
                                {
                                    var t = modEntry.fullPath.Substring(modEntry.fullPath.LastIndexOf("_") + 1, 3);

                                    try
                                    {
                                        num = int.Parse(t).ToString();
                                    }
                                    catch
                                    {
                                        if (modEntry.fullPath.Contains("stigma"))
                                        {
                                            num = "stigma";
                                        }
                                        else
                                        {
                                            num = "Error";
                                        }
                                    }
                                }

                                if (modEntry.fullPath.Contains("/hair/"))
                                {
                                    var t = modEntry.fullPath.Substring(modEntry.fullPath.LastIndexOf("/h") + 2, 4);
                                    num = int.Parse(t).ToString();

                                    if (modEntry.fullPath.Contains(".tex"))
                                    {
                                        type = HairTypes[modEntry.fullPath.Substring(modEntry.fullPath.LastIndexOf("_") - 3, 3)];
                                    }
                                }

                                if (modEntry.fullPath.Contains("/vfx/"))
                                {
                                    type = "VFX";
                                }
                            }
                            else if (modEntry.fullPath.Contains(".avfx"))
                            {
                                r = Strings.All;
                                type = "AVFX";
                            }

                            var m = "3D";

                            if (modEntry.fullPath.Contains("ui/"))
                            {
                                var t = modEntry.fullPath.Substring(modEntry.fullPath.IndexOf("/") + 1);
                                m = t.Substring(0, t.IndexOf("/"));
                            }
                            else if (modEntry.fullPath.Contains(".tex") || modEntry.fullPath.Contains(".atex"))
                            {
                                m = GetMapName(modEntry.fullPath);
                            }
                            else if (modEntry.fullPath.Contains(".mtrl"))
                            {
                                m = "ColorSet";
                            }

                            var isActive = Helper.IsActive(modEntry.fullPath, modEntry.datFile);

                            mpiList.Add(new ModPackItems { Name = modEntry.name, Race = r, Part = type, Num = num, Map = m, Active = isActive, Entry = modEntry });
                        }
                    }
                }
                mpiList.Sort();
                listView.ItemsSource = mpiList;               
            }
            catch (Exception ex)
            {
                FlexibleMessageBox.Show("Error accessing or reading .modlist file \n" + ex.Message, "MakeModPack Error " + Info.appVersion, Forms.MessageBoxButtons.OK, Forms.MessageBoxIcon.Error);
                Debug.WriteLine(ex.StackTrace);
            }

            var fn = 1;

            if (!Directory.Exists(mpDir))
            {
                Directory.CreateDirectory(mpDir);
            }
            else
            {
                while (File.Exists(mpDir + "\\ModPack " + fn + ".ttmp"))
                {
                    fn++;
                }

                modPackName.Text = "ModPack " + fn;
            }
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
            else if (fileName.Contains(".atex"))
            {
                var atex = Path.GetFileNameWithoutExtension(fileName);
                return atex.Substring(0, 4);
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

        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (ModPackItems added in e.AddedItems)
            {
                packList.Add(added);
                modCount += 1;
                modSize += added.Entry.modSize;
            }

            foreach (ModPackItems removed in e.RemovedItems)
            {
                packList.Remove(removed);
                modCount -= 1;
                modSize -= removed.Entry.modSize;
            }

            float totalModSize = modSize;
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



            ModCountLabel.Content = modCount;
            ModSizeLabel.Content = totalModSize.ToString("0.##") + sizeSuffix;

            if (packList.Count > 0)
            {
                CreateButton.IsEnabled = true;
            }
            else
            {
                CreateButton.IsEnabled = false;
            }
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

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> modPackList = new List<string>();
            List<byte> modPackData = new List<byte>();

            var mpInfo = new ModPackInfo() { Name = modPackName.Text, Version = Info.ModPackVersion };
            modPackList.Add(JsonConvert.SerializeObject(mpInfo));


            foreach (var mpi in packList)
            {
                var mOffset = mpi.Entry.modOffset;
                var datFile = mpi.Entry.datFile;
                var datNum = int.Parse(Info.ModDatDict[mpi.Entry.datFile]);

                var datPath = string.Format(Info.datDir, datFile, datNum);

                mOffset = Helper.OffsetCorrection(datNum, mOffset);

                var mpOffset = modPackData.Count;

                var mpj = new ModPackJson()
                {
                    Name = mpi.Name,
                    Category = mpi.Entry.category,
                    FullPath = mpi.Entry.fullPath,
                    ModOffset = mpOffset,
                    ModSize = mpi.Entry.modSize,
                    DatFile = mpi.Entry.datFile
                };

                modPackList.Add(JsonConvert.SerializeObject(mpj));

                using (BinaryReader br = new BinaryReader(File.OpenRead(datPath)))
                {
                    br.BaseStream.Seek(mOffset, SeekOrigin.Begin);

                    modPackData.AddRange(br.ReadBytes(mpi.Entry.modSize));
                }
            }

            if(!File.Exists(mpDir + "\\" + modPackName.Text + ".ttmp"))
            {
                using (var zip = ZipFile.Open(mpDir + "\\" + modPackName.Text + ".ttmp", ZipArchiveMode.Create))
                {
                    var mplEntry = zip.CreateEntry("TTMPL.mpl");
                    using (StreamWriter writer = new StreamWriter(mplEntry.Open()))
                    {
                        foreach (var l in modPackList)
                        {
                            writer.WriteLine(l);
                        }
                    }

                    var mpdEntry = zip.CreateEntry("TTMPD.mpd");
                    using (BinaryWriter writer = new BinaryWriter(mpdEntry.Open()))
                    {
                        writer.Write(modPackData.ToArray(), 0, modPackData.Count);
                    }
                }
            }
            else
            {
                FlexibleMessageBox.Show(modPackName.Text + " already exists. \n\n Please choose another name.", "MakeModPack Error " + Info.appVersion, Forms.MessageBoxButtons.OK, Forms.MessageBoxIcon.Error);
            }

            ClearList();
        }

        private void ClearList()
        {
            listView.UnselectAll();
            
            var fn = 1;
            while (File.Exists(mpDir + "\\ModPack " + fn + ".ttmp"))
            {
                fn++;
            }

            modPackName.Text = "ModPack " + fn;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            listView.SelectAll();
            listView.Focus();
        }

        private void SelectActiveButton_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < listView.Items.Count; i++)
            {
                var item = (System.Windows.Controls.ListViewItem)listView.ItemContainerGenerator.ContainerFromIndex(i);
                var mpi = (ModPackItems)listView.Items[i];
                var isActive = mpi.Active;

                if(item != null)
                {
                    if (isActive)
                    {
                        item.IsSelected = true;
                    }
                    else
                    {
                        item.IsSelected = false;
                    }
                }
            }
            listView.Focus();

        }

        private void ClearSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            listView.UnselectAll();
        }

        private void Header_Click(object sender, RoutedEventArgs e)
        {
            if(lastDirection == ListSortDirection.Ascending)
            {
                lastDirection = ListSortDirection.Descending;
            }
            else
            {
                lastDirection = ListSortDirection.Ascending;
            }

            var h = e.OriginalSource as GridViewColumnHeader;

            if(h != null && !h.Content.ToString().Equals("_"))
            {
                CollectionView cv = (CollectionView)CollectionViewSource.GetDefaultView(listView.ItemsSource);
                cv.SortDescriptions.Clear();
                cv.SortDescriptions.Add(new SortDescription(h.Content.ToString(), lastDirection));
            }
        }
    }
}
