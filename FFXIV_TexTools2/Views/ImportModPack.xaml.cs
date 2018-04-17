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
using System.Windows.Forms;
using System.Windows.Threading;

namespace FFXIV_TexTools2.Views
{
    /// <summary>
    /// Interaction logic for ImportModPack.xaml
    /// </summary>
    public partial class ImportModPack : Window
    {
        List<ModPackItems> packList = new List<ModPackItems>();
        string packPath;
        ProgressDialog pd;
        string currentImport;
        ListSortDirection lastDirection = ListSortDirection.Ascending;
        DispatcherTimer dt = new DispatcherTimer();
        Stopwatch sw = new Stopwatch();
        private bool loadStarted = false;

        public ImportModPack(string modPackPath)
        {
            InitializeComponent();

            dt.Tick += new EventHandler(dt_Tick);
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
                                var line = sr.ReadLine();
                                if (line.ToLower().Contains("version"))
                                {
                                    mpInfo = JsonConvert.DeserializeObject<ModPackInfo>(line);
                                }
                                else
                                {
                                    mpjList.Add(deserializeModPackJsonLine(line));
                                }

                                while(sr.Peek() >= 0)
                                {
                                    line = sr.ReadLine();
                                    mpjList.Add(deserializeModPackJsonLine(line));
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

            float totalModSize = 0;

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

            modSize.Content = totalModSize.ToString("0.##") + sizeSuffix;
        }

        private ModPackJson deserializeModPackJsonLine(string line) {
            var data = JsonConvert.DeserializeObject<ModPackJson>(line);
            data.ModOffset = (uint)data.ModOffset;
            return data;
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
            if (!Helper.IsIndexLocked(true))
            {
                BackgroundWorker backgroundWorker = new BackgroundWorker();
                backgroundWorker.WorkerReportsProgress = true;
                backgroundWorker.DoWork += BackgroundWorker_DoWork;
                backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
                backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;

                pd = new ProgressDialog();
                pd.Owner = App.Current.MainWindow;
                pd.Show();

                sw.Restart();
                dt.Start();

                backgroundWorker.RunWorkerAsync();
                Close();
            }
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            pd.Progress.Value = e.ProgressPercentage;
            pd.ProgressPercent.Content = e.ProgressPercentage.ToString() + " %";
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

            if(e.ProgressPercentage == 100)
            {
                pd.ImportingLabel.Content = "Import Complete!";
                pd.ProgressText.AppendText("\nAll Imports Completed.");
                pd.ScrollViewer.ScrollToBottom();
            }
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            pd.OKButton.IsEnabled = true;
            sw.Stop();
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var backgroundWorker = sender as BackgroundWorker;

            var packListCount = packList.Count;
            float i = 0;

            List<string> modFileLines = new List<string>(File.ReadAllLines(Properties.Settings.Default.Modlist_Directory));

            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(packPath))
                {
                    backgroundWorker.ReportProgress(0, "Opening TTMP Data File...\n");

                    foreach (var entry in archive.Entries)
                    {
                        if (entry.FullName.EndsWith(".mpd"))
                        {
                            var stream = entry.Open();
                            var remainingPack = packListCount;
                            var currentPack = 0;
                            var prevPack = 0;
                            long newOffset = 0;
                            long offsetSum = 0;
                            List<ModPackItems> pack;
                            long cursor = 0;

                            while (currentPack != packListCount)
                            {
                                prevPack = currentPack;
                                if (remainingPack > 100)
                                {
                                    pack = packList.GetRange(currentPack, 100);
                                    currentPack += 100;
                                    remainingPack -= 100;
                                }
                                else
                                {
                                    pack = packList.GetRange(currentPack, remainingPack);
                                    currentPack += remainingPack;
                                }

                                backgroundWorker.ReportProgress((int)((i / packListCount) * 100), $"\nReading Entries ({prevPack} - {currentPack}/{packListCount})\n\n");

                                long totalSize = 0;
                                var modPackBytes = new List<byte>();
                                foreach (var p in pack)
                                {
                                    if (p.mEntry.ModOffset < cursor)
                                    {
                                        backgroundWorker.ReportProgress((int)((i / packListCount) * 100), $"There was an warning in importing. \nImproper Mod Offset in ModPack for {p.mEntry.Name}. \nUnable to import {p.mEntry.Name}.");
                                        continue;
                                    }
                                    totalSize += p.mEntry.ModSize;
                                    var buf = new byte[p.mEntry.ModSize];
                                    while (p.mEntry.ModOffset > cursor)
                                    {
                                        cursor++;
                                        stream.ReadByte(); //seek forward for next offset
                                    }
                                    stream.Read(buf, 0, buf.Length);
                                    cursor += buf.Length;
                                    modPackBytes.AddRange(buf);
                                }
                                var uncompBytes = modPackBytes.ToArray();

                                offsetSum += newOffset;
                                newOffset = totalSize;

                                using (var ms = new MemoryStream(uncompBytes))
                                {
                                    //backgroundWorker.ReportProgress((int)((i / packListCount) * 100), "Reading TTMP Data...\n");
                                    var dataOffset = 0;
                                    using (var br = new BinaryReader(ms))
                                    {
                                        //backgroundWorker.ReportProgress((int)((i / packListCount) * 100), "Begining Import...\n");

                                        foreach (var mpi in pack)
                                        {
                                            currentImport = mpi.Name + "....";
                                            backgroundWorker.ReportProgress((int)((i / packListCount) * 100), currentImport);

                                            JsonEntry modEntry = null;
                                            bool inModList = false;
                                            bool overwrite = false;
                                            int lineNum = 0;
                                            int originalOffset = 0;
                                            int offset = 0;

                                            byte[] dataBytes = new byte[mpi.mEntry.ModSize];
                                            List<byte> modDataList = new List<byte>();

                                            br.BaseStream.Seek(dataOffset, SeekOrigin.Begin);
                                            modDataList.AddRange(br.ReadBytes(mpi.mEntry.ModSize));

                                            try
                                            {
                                                foreach (var line in modFileLines)
                                                {
                                                    modEntry = JsonConvert.DeserializeObject<JsonEntry>(line);
                                                    if (modEntry.fullPath.Equals(mpi.mEntry.FullPath))
                                                    {
                                                        inModList = true;
                                                        break;
                                                    }
                                                    lineNum++;
                                                }


                                                var datNum = int.Parse(Info.ModDatDict[mpi.mEntry.DatFile]);

                                                var modDatPath = string.Format(Info.datDir, mpi.mEntry.DatFile, datNum);

                                                var fileLength = new FileInfo(modDatPath).Length;
                                                while (fileLength >= 2000000000)
                                                {
                                                    datNum += 1;
                                                    modDatPath = string.Format(Info.datDir, mpi.mEntry.DatFile, datNum);
                                                    if (!File.Exists(modDatPath))
                                                    {
                                                        CreateDat.MakeNewDat(mpi.mEntry.DatFile);
                                                    }
                                                    fileLength = new FileInfo(modDatPath).Length;
                                                }

                                                //is in modlist and size of new mod is less than or equal to existing mod size
                                                if (inModList && mpi.mEntry.ModSize <= modEntry.modSize)
                                                {
                                                    int sizeDiff = modEntry.modSize - modDataList.Count;

                                                    datNum = ((modEntry.modOffset / 8) & 0x0F) / 2;
                                                    modDatPath = string.Format(Info.datDir, modEntry.datFile, datNum);
                                                    var datOffsetAmount = 16 * datNum;

                                                    using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(modDatPath)))
                                                    {
                                                        bw.BaseStream.Seek(modEntry.modOffset - datOffsetAmount, SeekOrigin.Begin);

                                                        bw.Write(modDataList.ToArray());

                                                        bw.Write(new byte[sizeDiff]);
                                                    }

                                                    Helper.UpdateIndex(modEntry.modOffset, mpi.mEntry.FullPath, mpi.mEntry.DatFile);
                                                    Helper.UpdateIndex2(modEntry.modOffset, mpi.mEntry.FullPath, mpi.mEntry.DatFile);

                                                    offset = modEntry.modOffset;

                                                    overwrite = true;
                                                }

                                                if (!overwrite)
                                                {
                                                    using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(modDatPath)))
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

                                                        var datOffsetAmount = 16 * datNum;
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


                                                        modFileLines[lineNum] = JsonConvert.SerializeObject(replaceEntry);
                                                        File.WriteAllLines(Properties.Settings.Default.Modlist_Directory, modFileLines);
                                                    }

                                                    JsonEntry jsonEntry = new JsonEntry()
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
                                                        modFileLines.Add(JsonConvert.SerializeObject(jsonEntry));
                                                        File.WriteAllLines(Properties.Settings.Default.Modlist_Directory, modFileLines);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        FlexibleMessageBox.Show("Error Accessing .modlist File \n" + ex.Message, "ImportModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                    }

                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                FlexibleMessageBox.Show("There was an error in importing. \n" + ex.Message, "ImportModPack Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                Debug.WriteLine(ex.StackTrace);
                                            }

                                            i++;

                                            backgroundWorker.ReportProgress((int)((i / packListCount) * 100), "Done.");

                                            dataOffset += mpi.mEntry.ModSize;
                                        }

                                    }

                                }
                            }
                    
                            stream.Dispose();
                            stream.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                FlexibleMessageBox.Show("Error opening TexTools ModPack file. \n" + ex.Message, "ImportModPack Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Debug.WriteLine(ex.StackTrace);
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
                    var s = float.Parse(mss.Substring(0, mss.IndexOf(" ")));
                    var ss = mss.Substring(mss.IndexOf(" "), 3);
                    float ms = added.mEntry.ModSize;

                    if (ss.Equals(" KB"))
                    {
                        ms = ms / 1024;
                    }
                    else if (ss.Equals(" MB"))
                    {
                        ms = ms / 1048576;
                    }
                    else if (ss.Equals(" GB"))
                    {
                        ms = ms / 1073741824;
                    }
                    s += ms;
                    modSize.Content = s.ToString("0.##") + ss;
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
                    var s = float.Parse(mss.Substring(0, mss.IndexOf(" ")));
                    var ss = mss.Substring(mss.IndexOf(" "), 3);
                    float ms = removed.mEntry.ModSize;

                    if (ss.Equals(" KB"))
                    {
                        ms = ms / 1024;
                    }
                    else if (ss.Equals(" MB"))
                    {
                        ms = ms / 1048576;
                    }
                    else if (ss.Equals(" GB"))
                    {
                        ms = ms / 1073741824;
                    }
                    s -= ms;
                    modSize.Content = s.ToString("0.##") + ss;
                }
            }

            if (packList.Count > 0)
            {
                ImportButton.IsEnabled = true;
                packList.Sort((a, b) => a.mEntry.ModOffset.CompareTo(b.mEntry.ModOffset));
            }
            else
            {
                ImportButton.IsEnabled = false;
            }
        }

        private void Header_Click(object sender, RoutedEventArgs e)
        {
            if (lastDirection == ListSortDirection.Ascending)
            {
                lastDirection = ListSortDirection.Descending;
            }
            else
            {
                lastDirection = ListSortDirection.Ascending;
            }

            var h = e.OriginalSource as GridViewColumnHeader;

            if (h != null && !h.Content.ToString().Equals("_"))
            {
                CollectionView cv = (CollectionView)CollectionViewSource.GetDefaultView(listView.ItemsSource);
                cv.SortDescriptions.Clear();
                cv.SortDescriptions.Add(new SortDescription(h.Content.ToString(), lastDirection));
            }
        }

        private void DeselectAllButton_Click(object sender, RoutedEventArgs e)
        {
            listView.UnselectAll();
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            listView.SelectAll();
            listView.Focus();
        }
    }
}
