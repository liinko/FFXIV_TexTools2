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
using System.Windows.Threading;
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
        string mpDir = Properties.Settings.Default.ModPack_Directory;
        int modCount = 0;
        int modSize = 0;
        private string mpName = "";
        ModPackItems selectedItem;
        ListSortDirection lastDirection = ListSortDirection.Ascending;
        ProgressDialog pd;
        DispatcherTimer dt = new DispatcherTimer();
        Stopwatch sw = new Stopwatch();
        private string tempMPL, tempMPD;


        public MakeModPack()
        {
            InitializeComponent();

            dt.Tick += new EventHandler(dt_Tick);
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
            mpName = modPackName.Text;

            if (File.Exists(mpDir + "\\" + mpName + ".ttmp"))
            {
                if (FlexibleMessageBox.Show(mpName + " already exists.\n\nWould you like to Overwrite the file?",
                        "Overwrite Confimration", Forms.MessageBoxButtons.YesNo, Forms.MessageBoxIcon.Question) ==
                    Forms.DialogResult.Yes)
                {
                    File.Delete(mpDir + "\\" + mpName + ".ttmp");
                }
                else
                {
                    FlexibleMessageBox.Show("Please choose a different name.", "Overwrite Declined",
                        Forms.MessageBoxButtons.OK, Forms.MessageBoxIcon.Information);
                    return;
                }
            }


            BackgroundWorker backgroundWorker = new BackgroundWorker();
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.DoWork += BackgroundWorker_DoWork;
            backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
            backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;

            pd = new ProgressDialog();
            pd.Title = "ModPack Maker";
            pd.ImportingLabel.Content = "Creating ModPack...";
            pd.Owner = App.Current.MainWindow;
            pd.Show();

            sw.Restart();
            dt.Start();

            CreateButton.IsEnabled = false;
            backgroundWorker.RunWorkerAsync();          
        }

        private void dt_Tick(object sender, EventArgs e)
        {
            if (sw.IsRunning)
            {
                TimeSpan ts = sw.Elapsed;
                var currentTime = String.Format("{0:00}:{1:00}:{2:00}",
                    ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                pd.TimeElapsedLabel.Content = currentTime;
            }
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage > 0 && e.ProgressPercentage < 100)
            {
                pd.Progress.Value = e.ProgressPercentage;
                pd.ProgressPercent.Content = e.ProgressPercentage + " %";
            }

            var name = (string)e.UserState;
            if (name.Equals("Done."))
            {
                pd.ProgressText.AppendText(name + "\n");
            }
            else
            {
                pd.ProgressText.AppendText(name);
                pd.ScrollViewer.ScrollToBottom();
            }

            if (e.ProgressPercentage == 100)
            {
                pd.ProgressText.AppendText("\nFinished Reading Data.\n");
                pd.ScrollViewer.ScrollToBottom();
            }
            else if (e.ProgressPercentage == -2)
            {
                pd.Progress.Value = 100;
                pd.ProgressPercent.Content = "100 %";

                pd.ImportingLabel.Content = "ModPack Creation Complete!";
            }

        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            pd.OKButton.IsEnabled = true;
            sw.Stop();
            ClearList();

            File.Delete(tempMPL);
            File.Delete(tempMPD);
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var backgroundWorker = sender as BackgroundWorker;

            var packCount = packList.Count;
            var packRemaining = packCount;
            var currentPackCount = 0;
            long offset = 0;
            float i = 0;
            List<byte> modPackData = new List<byte>();

            tempMPL = Path.GetTempFileName();
            tempMPD = Path.GetTempFileName();

            using (StreamWriter sw = new StreamWriter(tempMPL))
            {
                using (BinaryWriter bw = new BinaryWriter(File.Open(tempMPD, FileMode.Open)))
                {
                    while (currentPackCount < packCount)
                    {
                        List<ModPackItems> pack;

                        if (packRemaining > 100)
                        {
                            pack = packList.GetRange(currentPackCount, 100);
                            packRemaining -= 100;
                            currentPackCount += 100;
                        }
                        else
                        {
                            pack = packList.GetRange(currentPackCount, packRemaining);
                            currentPackCount += packRemaining;
                        }

                        backgroundWorker.ReportProgress((int)((i / packCount) * 100), "\nReading " + pack.Count + " Entries (" + currentPackCount + "/" + packList.Count + ")\n\n");

                        foreach (var mpi in pack)
                        {
                            List<string> modPackList = new List<string>();

                            var currentImport = mpi.Name + "....";
                            backgroundWorker.ReportProgress((int)((i / packCount) * 100), currentImport);

                            var mOffset = mpi.Entry.modOffset;
                            var datFile = mpi.Entry.datFile;
                            int datNum = ((mOffset / 8) & 0x0F) / 2;

                            var datPath = string.Format(Info.datDir, datFile, datNum);

                            mOffset = Helper.OffsetCorrection(datNum, mOffset);

                            offset += modPackData.Count;

                            var mpj = new ModPackJson()
                            {
                                Name = mpi.Name,
                                Category = mpi.Entry.category,
                                FullPath = mpi.Entry.fullPath,
                                ModOffset = offset,
                                ModSize = mpi.Entry.modSize,
                                DatFile = mpi.Entry.datFile
                            };

                            modPackList.Add(JsonConvert.SerializeObject(mpj));

                            modPackData.Clear();

                            using (BinaryReader br = new BinaryReader(File.OpenRead(datPath)))
                            {
                                br.BaseStream.Seek(mOffset, SeekOrigin.Begin);

                                modPackData.AddRange(br.ReadBytes(mpi.Entry.modSize));
                            }

                            foreach (var l in modPackList)
                            {
                                sw.WriteLine(l);
                            }

                            bw.Write(modPackData.ToArray());

                            i++;
                            backgroundWorker.ReportProgress((int)((i / packCount) * 100), "Done.");
                        }
                    }
                }
            }


            backgroundWorker.ReportProgress(-1, "\nCreating TTMP File, this may take some time...");

            using (var zip = ZipFile.Open(mpDir + "\\" + mpName + ".ttmp", ZipArchiveMode.Create))
            {
                zip.CreateEntryFromFile(tempMPL, "TTMPL.mpl");
                zip.CreateEntryFromFile(tempMPD, "TTMPD.mpd");
            }

            backgroundWorker.ReportProgress(-2, "Done.");
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
