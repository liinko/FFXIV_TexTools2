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
using FFXIV_TexTools2.IO;
using FFXIV_TexTools2.Model;
using FFXIV_TexTools2.Resources;
using FFXIV_TexTools2.Views;
using FolderSelect;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using System.Windows.Input;
using System.Xml;
using MessageBox = System.Windows.Forms.MessageBox;
using TreeNode = FFXIV_TexTools2.Model.TreeNode;

namespace FFXIV_TexTools2.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        string searchText, modelText, variantText;
        TextureViewModel TVM = new TextureViewModel();
        ModelViewModel MVM = new ModelViewModel();
        ObservableCollection<CategoryViewModel> _categories;
        ObservableCollection<CategoryViewModel> oCategory;
        List<string> categoryList = new List<string>();

        public bool IsEnglish { get { return Properties.Settings.Default.Language.Equals("en"); } }
        public bool IsJapanese { get { return Properties.Settings.Default.Language.Equals("ja"); } }
        public bool IsGerman { get { return Properties.Settings.Default.Language.Equals("de"); } }
        public bool IsFrench { get { return Properties.Settings.Default.Language.Equals("fr"); } }

        public bool EnglishEnabled { get { return !Properties.Settings.Default.Language.Equals("en"); } } 
        public bool JapaneseEnabled { get { return !Properties.Settings.Default.Language.Equals("ja"); } } 
        public bool GermanEnabled { get { return !Properties.Settings.Default.Language.Equals("de"); } }
        public bool FrenchEnabled { get { return !Properties.Settings.Default.Language.Equals("fr"); } }

        public TextureViewModel TextureVM { get { return TVM; } set { TVM = value; NotifyPropertyChanged("TextureVM"); } }
        public ModelViewModel ModelVM { get { return MVM; } set { MVM = value; NotifyPropertyChanged("ModelVM"); } }

        public string ModelText { get { return modelText; } set { modelText = value; } }
        public string VariantText { get { return variantText; } set { variantText = value; } }

        /// <summary>
        /// View Model Constructor for Main Window
        /// </summary>
        public MainViewModel()
        {
            CultureInfo ci = new CultureInfo(Properties.Settings.Default.Language);
            ci.NumberFormat.NumberDecimalSeparator = ".";
            CultureInfo.DefaultThreadCurrentCulture = ci;
            CultureInfo.DefaultThreadCurrentUICulture = ci;



            if (!Properties.Settings.Default.FFXIV_Directory.Contains("ffxiv"))
            {
                SetDirectories(true);
            }
            else
            {
                var indexCheck = Properties.Settings.Default.FFXIV_Directory + "\\0a0000.win32.index";
                if (File.Exists(indexCheck))
                {
                    var fileDate = File.GetLastWriteTime(indexCheck);
                    if (fileDate.Year >= 2018)
                    {
                        runStartup();
                    }
                    else
                    {
                        SetDirectories(false);
                    }
                }

            }


        }

        /// <summary>
        /// Command for the ModList Menu
        /// </summary>
        public ICommand IDSearchCommand
        {
            get { return new RelayCommand(IDSearch); }
        }


        private void runStartup()
        {
            var applicationVersion = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion;

            CheckForModList();
            CheckVersion();
            MakeModContainers();

            var gameDir = "";
            string[] versionFile = new string[] { "" };
            Version ffxivVersion = null;
            try
            {
                gameDir = Properties.Settings.Default.FFXIV_Directory.Substring(0, Properties.Settings.Default.FFXIV_Directory.LastIndexOf("sqpack"));

            }
            catch (Exception e)
            {
                FlexibleMessageBox.Show("Could not find sqpack folder in game directory.\nDirectory: " + Properties.Settings.Default.FFXIV_Directory + "\n\nError: " + e.Message, "Version Check Error " + applicationVersion, MessageBoxButtons.OK, MessageBoxIcon.None);
            }

            try
            {
                versionFile = File.ReadAllLines(gameDir + "/ffxivgame.ver");
                ffxivVersion = new Version(versionFile[0].Substring(0, versionFile[0].LastIndexOf(".")));
            }
            catch (Exception e)
            {
                FlexibleMessageBox.Show("Could not determine FFXIV Version.\nData read: " + versionFile[0] + "\n\nError: " + e.Message, "Version Check Error " + applicationVersion, MessageBoxButtons.OK, MessageBoxIcon.None);

            }

            var indexBackupDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/TexTools/Index_Backups";
            try
            {
                indexBackupDir = Properties.Settings.Default.IndexBackups_Directory;
            }
            catch
            {
                FlexibleMessageBox.Show("TexTools was unable to read Index Backups Directory setting\n\n" +
                     "The following default will be used:\n" + indexBackupDir, "Settings Read Error " + applicationVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (indexBackupDir.Equals(""))
            {
                indexBackupDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/TexTools/Index_Backups";
                Properties.Settings.Default.IndexBackups_Directory = indexBackupDir;
                Properties.Settings.Default.Save();
            }

            if (Properties.Settings.Default.Save_Directory.Equals(""))
            {
                var md = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/TexTools/Saved";
                Directory.CreateDirectory(md);
                Properties.Settings.Default.Save_Directory = md;
                Properties.Settings.Default.Save();
            }

            if (!Directory.Exists(indexBackupDir))
            {
                Properties.Settings.Default.FFXIV_Ver = ffxivVersion.ToString();
                Properties.Settings.Default.Save();

                var backupMessage = "No index file backups were detected. \nWould you like to create a backup now? \n\nWarning:\nIn order to create a clean backup, all active modifications will be set to disabled, they will have to be re-enabled manually.";

                if (MessageBox.Show(backupMessage, "Create Backup", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
                {
                    Directory.CreateDirectory(indexBackupDir);

                    if (!Helper.IsIndexLocked(true))
                    {
                        if (!CheckIndexValues())
                        {
                            try
                            {
                                File.Copy(Properties.Settings.Default.FFXIV_Directory + "/040000.win32.index", indexBackupDir + "/040000.win32.index", true);
                                File.Copy(Properties.Settings.Default.FFXIV_Directory + "/040000.win32.index2", indexBackupDir + "/040000.win32.index2", true);
                                File.Copy(Properties.Settings.Default.FFXIV_Directory + "/060000.win32.index", indexBackupDir + "/060000.win32.index", true);
                                File.Copy(Properties.Settings.Default.FFXIV_Directory + "/060000.win32.index2", indexBackupDir + "/060000.win32.index2", true);
                            }
                            catch (Exception e)
                            {
                                FlexibleMessageBox.Show("There was an issue creating backup files. \n" + e.Message, "Backup Error " + applicationVersion, MessageBoxButtons.OK, MessageBoxIcon.None);
                            }
                        }
                        else
                        {
                            FlexibleMessageBox.Show("There are still modified offsets in the index files, disable all mods and reopen TexTools to try again.\n\n" +
                                 "If this issue persits, obtain new index backups to place in the index_backups folder. (TexTools Discord is a good place to ask)", "Backup Error", MessageBoxButtons.OK, MessageBoxIcon.None);
                        }
                    }
                }
            }
            else
            {
                if (Properties.Settings.Default.FFXIV_Ver == "")
                {
                    Properties.Settings.Default.FFXIV_Ver = ffxivVersion.ToString();
                    Properties.Settings.Default.Save();
                }

                var checkVer = new Version(Properties.Settings.Default.FFXIV_Ver);

                if (ffxivVersion > checkVer)
                {
                    Helper.FixIndex();
                    var backupMessage = "A newer version of FFXIV was detected. \nWould you like to create a new backup of your index files now? (Recommended) \n\nWarning:\nIn order to create a clean backup, all active modifications will be set to disabled, they will have to be re-enabled manually.";
                    if (MessageBox.Show(backupMessage, "Create Backup", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
                    {
                        if (!Helper.IsIndexLocked(true))
                        {
                            Properties.Settings.Default.FFXIV_Ver = ffxivVersion.ToString();
                            Properties.Settings.Default.Save();

                            RevertAllMods();

                            if (!CheckIndexValues())
                            {
                                try
                                {
                                    File.Copy(Properties.Settings.Default.FFXIV_Directory + "/040000.win32.index", indexBackupDir + "/040000.win32.index", true);
                                    File.Copy(Properties.Settings.Default.FFXIV_Directory + "/040000.win32.index2", indexBackupDir + "/040000.win32.index2", true);
                                    File.Copy(Properties.Settings.Default.FFXIV_Directory + "/060000.win32.index", indexBackupDir + "/060000.win32.index", true);
                                    File.Copy(Properties.Settings.Default.FFXIV_Directory + "/060000.win32.index2", indexBackupDir + "/060000.win32.index2", true);
                                }
                                catch (Exception e)
                                {
                                    FlexibleMessageBox.Show("There was an issue creating backup files. \n" + e.Message, "Backup Error " + applicationVersion, MessageBoxButtons.OK, MessageBoxIcon.None);
                                }
                            }
                            else
                            {
                                FlexibleMessageBox.Show("There are still modified offsets in the index files, disable all mods and reopen TexTools to try again.\n\n" +
                                     "If this issue persits, obtain new index backups to place in the index_backups folder. (TexTools Discord is a good place to ask)", "Backup Error " + applicationVersion, MessageBoxButtons.OK, MessageBoxIcon.None);
                            }
                        }
                    }
                }
            }

            FillTree();
        }

        private void CheckForModList()
        {
            var oldModListDir = Properties.Settings.Default.FFXIV_Directory + "/040000.modlist";

            try
            {
                if (File.Exists(oldModListDir))
                {
                    string[] lines = File.ReadAllLines(oldModListDir);
                    List<string> lineList = new List<string>();

                    if (lines.Length > 0)
                    {
                        foreach (var l in lines)
                        {
                            var modEntry = JsonConvert.DeserializeObject<JsonEntry>(l);
                            modEntry.datFile = Strings.ItemsDat;

                            lineList.Add(JsonConvert.SerializeObject(modEntry));
                        }
                        File.WriteAllLines(Properties.Settings.Default.Modlist_Directory, lineList);
                    }

                    File.Delete(oldModListDir);

                   FlexibleMessageBox.Show("TexTools discovered an old modlist in the ffxiv directory. \n\n" +
                        "A new modlist (TexTools.modlist) with the same data has been created and is located in the TexTools folder. ", "ModList Change.",MessageBoxButtons.OK,MessageBoxIcon.Information);
                }
            }
            catch(Exception e)
            {
               FlexibleMessageBox.Show("There was an error converting the old modlist.  \n\n" +
                 "A new modlist will be created, you may remove the old modlist from the ffxiv folder. \n\n" + e.Message, "ModList Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            string mpDir = Properties.Settings.Default.ModPack_Directory;
            if (mpDir.Equals(""))
            {
                mpDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\TexTools\\ModPacks";
                if (!Directory.Exists(mpDir))
                {
                    Directory.CreateDirectory(mpDir);
                    Properties.Settings.Default.ModPack_Directory = mpDir;
                    Properties.Settings.Default.Save();
                }
                else
                {
                    Properties.Settings.Default.ModPack_Directory = mpDir;
                    Properties.Settings.Default.Save();

                }
            }
        }


        private void IDSearch(object obj)
        {
            ModelSearch modelSearch = new ModelSearch(this);
            modelSearch.Owner = App.Current.MainWindow;
            modelSearch.Show();
        }


        /// <summary>
        /// Asks for game directory and sets default save directory
        /// </summary>
        private void SetDirectories(bool valid)
        {
            if (valid)
            {
                string[] commonInstallDirectories = new string[]
{
                "C:/Program Files (x86)/SquareEnix/FINAL FANTASY XIV - A Realm Reborn/game/sqpack/ffxiv",
                "C:/Program Files/SquareEnix/FINAL FANTASY XIV - A Realm Reborn/game/sqpack/ffxiv",
                "C:/Program Files/Steam/SteamApps/common/FINAL FANTASY XIV - A Realm Reborn/game/sqpack/ffxiv",
                "C:/Program Files/Steam/SteamApps/common/FINAL FANTASY XIV Online/game/sqpack/ffxiv",
                "C:/Program Files (x86)/Steam/SteamApps/common/FINAL FANTASY XIV - A Realm Reborn/game/sqpack/ffxiv",
                "C:/Program Files (x86)/Steam/SteamApps/common/FINAL FANTASY XIV Online/game/sqpack/ffxiv"
};

                if (Properties.Settings.Default.FFXIV_Directory.Equals(""))
                {
                    var md = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/TexTools/Saved";
                    Directory.CreateDirectory(md);
                    Properties.Settings.Default.Save_Directory = md;
                    Properties.Settings.Default.Save();

                    var installDirectory = "";
                    foreach (var i in commonInstallDirectories)
                    {
                        if (Directory.Exists(i))
                        {
                            if (FlexibleMessageBox.Show("FFXIV install directory found at \n\n" + i + "\n\nUse this directory? ", "Install Directory Found", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                            {
                                installDirectory = i;
                                Properties.Settings.Default.FFXIV_Directory = installDirectory;
                                Properties.Settings.Default.Save();
                            }
                            break;
                        }
                    }

                    if (installDirectory.Equals(""))
                    {
                        if (FlexibleMessageBox.Show("Please locate the following directory. \n\n .../FINAL FANTASY XIV - A Realm Reborn/game/sqpack/ffxiv", "Install Directory Not Found", MessageBoxButtons.OK, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.OK)
                        {
                            while (!installDirectory.Contains("ffxiv"))
                            {
                                FolderSelectDialog folderSelect = new FolderSelectDialog()
                                {
                                    Title = "Select sqpack/ffxiv Folder"
                                };
                                bool result = folderSelect.ShowDialog();
                                if (result)
                                {
                                    installDirectory = folderSelect.FileName;
                                }
                                else
                                {
                                    Environment.Exit(0);
                                }
                            }

                            Properties.Settings.Default.FFXIV_Directory = installDirectory;
                            Properties.Settings.Default.Save();
                        }
                        else
                        {
                            Environment.Exit(0);
                        }
                    }
                }

                if (Properties.Settings.Default.Save_Directory.Equals(""))
                {
                    var md = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/TexTools/Saved";
                    Directory.CreateDirectory(md);
                    Properties.Settings.Default.Save_Directory = md;
                    Properties.Settings.Default.Save();
                }
                runStartup();
            }
            else
            {
                if (FlexibleMessageBox.Show("The install location chosen is out of date \n\nPlease locate the following directory. \n\n " +
                                            ".../FINAL FANTASY XIV - A Realm Reborn/game/sqpack/ffxiv", "Install Directory Not Found", MessageBoxButtons.OK, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.OK)
                {
                    var installDirectory = "";

                    while (!installDirectory.Contains("ffxiv"))
                    {
                        FolderSelectDialog folderSelect = new FolderSelectDialog()
                        {
                            Title = "Select sqpack/ffxiv Folder"
                        };
                        bool result = folderSelect.ShowDialog();
                        if (result)
                        {
                            installDirectory = folderSelect.FileName;
                        }
                        else
                        {
                            Environment.Exit(0);
                        }
                    }

                    var indexCheck = installDirectory + "\\040000.win32.index";
                    if (File.Exists(indexCheck))
                    {
                        var fileDate = File.GetLastWriteTime(indexCheck);
                        if (fileDate.Year >= 2018)
                        {
                            Properties.Settings.Default.FFXIV_Directory = installDirectory;
                            Properties.Settings.Default.Save();

                            runStartup();
                        }
                        else
                        {
                            SetDirectories(false);
                        }
                    }
                }
                else
                {
                    Environment.Exit(0);
                }
            }

        }

        /// <summary>
        /// Checks for application update
        /// </summary>
        private void CheckVersion()
        {
            string xmlURL = "https://raw.githubusercontent.com/liinko/FFXIVTexToolsWeb/master/version.xml";
            string changeLog = "";
            string siteURL = "";
            Version v = null;

            try
            {
                using (XmlTextReader reader = new XmlTextReader(xmlURL))
                {
                    reader.MoveToContent();
                    string elementName = "";

                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "FFXIVTexTools2")
                    {
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                elementName = reader.Name;
                            }
                            else
                            {
                                if (reader.NodeType == XmlNodeType.Text && reader.HasValue)
                                {
                                    switch (elementName)
                                    {
                                        case "version":
                                            v = new Version(reader.Value);
                                            break;
                                        case "url":
                                            siteURL = reader.Value;
                                            break;
                                        case "log":
                                            changeLog = reader.Value;
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }

                var ver = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion;
                var curVersion = new Version(ver);

                if (curVersion.CompareTo(v) < 0)
                {

                    Update up = new Update();

                    up.Message = "Version: " + v.ToString().Substring(0, 5) + "\n\nChange Log:\n" + changeLog + "\n\nPlease visit the website to download the update.";
                    up.Show();
                }
            }
            catch (Exception ex)
            {
               FlexibleMessageBox.Show("There was an issue checking for updates. \n" + ex.Message, "Updater Error " + Info.appVersion, MessageBoxButtons.OK,MessageBoxIcon.None);
            }
        }

        /// <summary>
        /// Creates files that will contain modded information
        /// </summary>
        private void MakeModContainers()
        {
            foreach (var datName in Info.ModDatDict)
            {
                var datPath = string.Format(Info.datDir, datName.Key, datName.Value);

                if (!File.Exists(datPath))
                {
                    CreateDat.MakeDat();
                    CreateDat.ChangeDatAmounts();
                }
            }

            if (Properties.Settings.Default.Modlist_Directory.Equals(""))
            {
                string md = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/TexTools/TexTools.modlist";
                Properties.Settings.Default.Modlist_Directory = md;
                Properties.Settings.Default.Save();
            }

            if (!File.Exists(Properties.Settings.Default.Modlist_Directory))
            {
                CreateDat.CreateModList();
            }
        }

        /// <summary>
        /// Creates 
        /// Creates a list of items from the EXD files 
        /// </summary>
        public void FillTree()
        {
            var exdDict = ExdReader.GetEXDData();
            if (exdDict == null)
            {
                Properties.Settings.Default.FFXIV_Directory = "";
                Properties.Settings.Default.Language = "en";
                Properties.Settings.Default.Save();
                System.Windows.Application.Current.Shutdown();
            }
            else
            {
                foreach (var cat in Info.MainCategoryList)
                {

                    TreeNode cm = new TreeNode
                    {
                        Name = cat,
                        _subNode = exdDict[cat]
                    };
                    if (Category == null)
                    {
                        Category = new ObservableCollection<CategoryViewModel>();
                    }

                    var cvm = new CategoryViewModel(cm);
                    Category.Add(cvm);
                }

                oCategory = Category;
            }
        }


        public void OpenID(ItemData item, string race, string category, string part, string variant)
        {
            if(ModelVM != null)
            {
                ModelVM.Dispose();
            }

            TextureVM.UpdateTextureFromID(item, race, category, part, variant);
            ModelVM.UpdateModel(item, category);
            ModelVM.ModelTabEnabled = true;
        }



        /// <summary>
        /// Data bound to the treeview item source
        /// </summary>
        public ObservableCollection<CategoryViewModel> Category
        {
            get { return _categories; }
            set
            {
                _categories = value;
                NotifyPropertyChanged("Category");
            }
        }

        /// <summary>
        /// Data bound to the search textbox text field
        /// </summary>
        public string SearchText
        {
            get { return searchText; }
            set
            {
                searchText = value;
                NotifyPropertyChanged("SearchText");
                SearchTextChanged();
            }
        }

        /// <summary>
        /// Filters the item list when input is detected in the search textbox
        /// </summary>
        private void SearchTextChanged()
        {
            if (SearchText.Length > 2)
            {
                Dictionary<string, TreeNode> catDict = new Dictionary<string, TreeNode>();

                foreach (var c in oCategory)
                {
                    catDict.Add(c.Name, new TreeNode() { Name = c.Name });
                    Dictionary<string, TreeNode> subCatDict = new Dictionary<string, TreeNode>();

                    foreach (var ch in c.Children)
                    {
                        foreach (var ch1 in ch.Children)
                        {
                            if (ch1.Children.Count > 0)
                            {
                                Dictionary<string, TreeNode> subSubCatDict = new Dictionary<string, TreeNode>();

                                foreach (var ch2 in ch1.Children)
                                {
                                    if (ch2.Name.ToLower().Contains(searchText.ToLower()))
                                    {
                                        var itemNode = new TreeNode() { Name = ch2.Name, ItemData = ch2.ItemData };

                                        if (subSubCatDict.ContainsKey(ch1.Name))
                                        {
                                            subSubCatDict[ch1.Name]._subNode.Add(itemNode);
                                        }
                                        else
                                        {
                                            subSubCatDict.Add(ch1.Name, new TreeNode { Name = ch1.Name });
                                            subSubCatDict[ch1.Name]._subNode.Add(itemNode);
                                        }
                                    }
                                }

                                if (subSubCatDict.Values.Count > 0)
                                {
                                    if (!subCatDict.ContainsKey(ch.Name))
                                    {
                                        subCatDict.Add(ch.Name, new TreeNode() { Name = ch.Name });
                                    }

                                    foreach (var s in subSubCatDict.Values)
                                    {
                                        subCatDict[ch.Name]._subNode.Add(s);
                                    }
                                }
                            }
                            else
                            {
                                if (ch1.Name.ToLower().Contains(searchText.ToLower()))
                                {
                                    var itemNode = new TreeNode() { Name = ch1.Name, ItemData = ch1.ItemData };
                                    if (subCatDict.ContainsKey(ch.Name))
                                    {
                                        subCatDict[ch.Name]._subNode.Add(itemNode);
                                    }
                                    else
                                    {
                                        subCatDict.Add(ch.Name, new TreeNode() { Name = ch.Name });
                                        subCatDict[ch.Name]._subNode.Add(itemNode);
                                    }
                                }
                            }
                        }
                    }

                    if(subCatDict.Values.Count > 0)
                    {
                        foreach(var s in subCatDict.Values)
                        {
                            catDict[c.Name]._subNode.Add(s);
                        }
                    }
                }

                Category = new ObservableCollection<CategoryViewModel>();

                foreach (var c in catDict.Values)
                {
                    if(c.SubNode.Count > 0)
                    {
                        var cvm = new CategoryViewModel(c);
                        Category.Add(cvm);
                        cvm.ExpandAll();
                    }

                }
            }
            else
            {
                Category = oCategory;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }


        private void RevertAllMods()
        {
            if (!Helper.IsIndexLocked(true))
            {
                JsonEntry modEntry = null;
                string line;

                try
                {
                    using (StreamReader sr = new StreamReader(Properties.Settings.Default.Modlist_Directory))
                    {
                        while ((line = sr.ReadLine()) != null)
                        {
                            modEntry = JsonConvert.DeserializeObject<JsonEntry>(line);
                            Helper.UpdateIndex(modEntry.originalOffset, modEntry.fullPath, modEntry.datFile);
                            Helper.UpdateIndex2(modEntry.originalOffset, modEntry.fullPath, modEntry.datFile);
                        }
                    }
                }
                catch (Exception ex)
                {
                    FlexibleMessageBox.Show("Error Accessing .modlist File \n" + ex.Message, "MainViewModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }


        private bool CheckIndexValues()
        {

            bool problem = false;

            foreach (var indexFile in Info.ModIndexDict)
            {
                var indexPath = string.Format(Info.indexDir, indexFile.Key);
                var index2Path = string.Format(Info.index2Dir, indexFile.Key);

                try
                {
                    using (BinaryReader br = new BinaryReader(File.OpenRead(indexPath)))
                    {
                        br.BaseStream.Seek(1036, SeekOrigin.Begin);
                        int numOfFiles = br.ReadInt32();

                        br.BaseStream.Seek(2048, SeekOrigin.Begin);
                        for (int i = 0; i < numOfFiles; br.ReadBytes(4), i += 16)
                        {
                            br.ReadBytes(8);
                            int offset = br.ReadInt32();

                            int datNum = (offset & 0x000f) / 2;

                            if (indexFile.Key.Equals(Strings.ItemsDat))
                            {
                                if (datNum == 4)
                                {
                                    problem = true;
                                    break;
                                }
                            }
                            else if (indexFile.Key.Equals(Strings.UIDat))
                            {
                                if (datNum == 1)
                                {
                                    problem = true;
                                    break;
                                }
                            }
                            else if (offset == 0)
                            {
                                problem = true;
                                break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    FlexibleMessageBox.Show("Error checking index values for backup. \n" + e.Message, "MainViewModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                try
                {
                    using (BinaryReader br = new BinaryReader(File.OpenRead(index2Path)))
                    {
                        br.BaseStream.Seek(1036, SeekOrigin.Begin);
                        int numOfFiles = br.ReadInt32();

                        br.BaseStream.Seek(2048, SeekOrigin.Begin);
                        for (int i = 0; i < numOfFiles; i += 8)
                        {
                            br.ReadBytes(4);
                            int offset = br.ReadInt32();
                            int datNum = (offset & 0x000f) / 2;

                            if (indexFile.Key.Equals(Strings.ItemsDat))
                            {
                                if (datNum == 4)
                                {
                                    problem = true;
                                    break;
                                }
                            }
                            else if (indexFile.Key.Equals(Strings.UIDat))
                            {
                                if (datNum == 1)
                                {
                                    problem = true;
                                    break;
                                }
                            }
                            else if (offset == 0)
                            {
                                problem = true;
                                break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    FlexibleMessageBox.Show("Error checking index2 values for backup. \n" + e.Message, "MainViewModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);

                }
            }

            return problem;
        }

        /// <summary>
        /// Gets the ModListModel from ModList window and selects the item
        /// **Currently not in use as it does not function correctly with Virtualization**
        /// </summary>
        /// <param name="mlm"></param>
        //public void GoToItem(ModListModel mlm)
        //{
        //    goTo = mlm;

        //    var entry = mlm.Entry;

        //    var vm = textureTreeView.DataContext as TreeViewModel;

        //    int index = 0;
        //    foreach (string key in Info.IDSlot.Keys)
        //    {
        //        if (key.Equals(entry.category))
        //        {
        //            break;
        //        }
        //        index++;
        //    }

        //    var category = vm.Category[index];
        //    category.IsExpanded = true;

        //    ItemViewModel c = null;

        //    foreach (var child in category.Children)
        //    {
        //        c = child as ItemViewModel;
        //        if (c.ItemName.Equals(entry.name))
        //        {
        //            c.IsSelected = true;
        //            break;
        //        }
        //    }

        //    TreeViewItem tvi = textureTreeView.ItemContainerGenerator.ContainerFromItem(c) as TreeViewItem;
        //}
    }
}
