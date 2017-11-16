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
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Xml;

namespace FFXIV_TexTools2.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        string searchText, modelText, variantText;
        TextureViewModel TVM = new TextureViewModel();
        ModelViewModel MVM = new ModelViewModel();
        List<ItemData> itemList;
        ObservableCollection<ItemData> oItemList;
        ObservableCollection<CategoryViewModel> _categories;
        ObservableCollection<CategoryViewModel> oCategory;
        List<string> categoryList = new List<string>();
        Dictionary<string, ObservableCollection<ItemData>> UIDict;
        bool modListOnEnabled, modListOnChecked, modListOffEnabled, modListOffChecked;

        public bool IsEnglish { get { return Properties.Settings.Default.Language.Equals("en"); } }
        public bool IsJapanese { get { return Properties.Settings.Default.Language.Equals("ja"); } }
        public bool IsGerman { get { return Properties.Settings.Default.Language.Equals("de"); } }
        public bool IsFrench { get { return Properties.Settings.Default.Language.Equals("fr"); } }

        public bool EnglishEnabled { get { return !Properties.Settings.Default.Language.Equals("en"); } } 
        public bool JapaneseEnabled { get { return !Properties.Settings.Default.Language.Equals("ja"); } } 
        public bool GermanEnabled { get { return !Properties.Settings.Default.Language.Equals("de"); } }
        public bool FrenchEnabled { get { return !Properties.Settings.Default.Language.Equals("fr"); } }

        public bool IsDX9 { get { return Properties.Settings.Default.DX_Ver.Equals("DX9"); } }
        public bool IsDX11 { get { return Properties.Settings.Default.DX_Ver.Equals("DX11"); } }

        public bool DX9Enabled { get { return !Properties.Settings.Default.DX_Ver.Equals("DX9"); } }
        public bool DX11Enabled { get { return !Properties.Settings.Default.DX_Ver.Equals("DX11"); } }

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
            CultureInfo.DefaultThreadCurrentCulture = ci;
            CultureInfo.DefaultThreadCurrentUICulture = ci;

            if (!Properties.Settings.Default.FFXIV_Directory.Contains("ffxiv"))
            {
                SetDirectories();
            }

            CheckForModList();

            var gameDir = Properties.Settings.Default.FFXIV_Directory.Substring(0, Properties.Settings.Default.FFXIV_Directory.LastIndexOf("sqpack"));
            var versionFile = File.ReadAllLines(gameDir + "/ffxivgame.ver");
            var ffxivVersion = new Version(versionFile[0].Substring(0, versionFile[0].LastIndexOf(".")));

            if (!Directory.Exists(Directory.GetCurrentDirectory() + "/Index_Backups"))
            {
                Properties.Settings.Default.FFXIV_Ver = ffxivVersion.ToString();
                Properties.Settings.Default.Save();

                var backupMessage = "No index file backups were detected. \nWould you like to create a backup now? \n\nWarning:\nIn order to create a clean backup, all active modifications will be set to disabled, they will have to be re-enabled manually.";

                if (MessageBox.Show(backupMessage, "Create Backup", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                {
                    Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/Index_Backups");

                    RevertAllMods();

                    try
                    {
                        File.Copy(Properties.Settings.Default.FFXIV_Directory + "/040000.win32.index", Directory.GetCurrentDirectory() + "/Index_Backups/040000.win32.index", true);
                        File.Copy(Properties.Settings.Default.FFXIV_Directory + "/040000.win32.index2", Directory.GetCurrentDirectory() + "/Index_Backups/040000.win32.index2", true);
                        File.Copy(Properties.Settings.Default.FFXIV_Directory + "/060000.win32.index", Directory.GetCurrentDirectory() + "/Index_Backups/060000.win32.index", true);
                        File.Copy(Properties.Settings.Default.FFXIV_Directory + "/060000.win32.index2", Directory.GetCurrentDirectory() + "/Index_Backups/060000.win32.index2", true);
                    }
                    catch(Exception e)
                    {
                        MessageBox.Show("There was an issue creating backup files. \n" + e.Message, "Backup Error", MessageBoxButton.OK, MessageBoxImage.None);
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
                    if (MessageBox.Show(backupMessage, "Create Backup", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                    {
                        Properties.Settings.Default.FFXIV_Ver = ffxivVersion.ToString();
                        Properties.Settings.Default.Save();

                        RevertAllMods();

                        try
                        {
                            File.Copy(Properties.Settings.Default.FFXIV_Directory + "/040000.win32.index", Directory.GetCurrentDirectory() + "/Index_Backups/040000.win32.index", true);
                            File.Copy(Properties.Settings.Default.FFXIV_Directory + "/040000.win32.index2", Directory.GetCurrentDirectory() + "/Index_Backups/040000.win32.index2", true);
                            File.Copy(Properties.Settings.Default.FFXIV_Directory + "/060000.win32.index", Directory.GetCurrentDirectory() + "/Index_Backups/060000.win32.index", true);
                            File.Copy(Properties.Settings.Default.FFXIV_Directory + "/060000.win32.index2", Directory.GetCurrentDirectory() + "/Index_Backups/060000.win32.index2", true);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("There was an issue creating backup files. \n" + e.Message, "Backup Error", MessageBoxButton.OK, MessageBoxImage.None);
                        }
                    }
                }
            }

            CheckVersion();
            MakeModContainers();
            FillTree();
        }

        /// <summary>
        /// Command for the ModList Menu
        /// </summary>
        public ICommand IDSearchCommand
        {
            get { return new RelayCommand(IDSearch); }
        }

        private void CheckForModList()
        {
            var oldModListDir = Properties.Settings.Default.FFXIV_Directory + "/040000.modlist";

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
                    File.WriteAllLines(Info.modListDir, lineList);
                }

                File.Delete(oldModListDir);

                MessageBox.Show("TexTools discovered an old modlist in the ffxiv directory. \n\n" +
                    "A new modlist (TexTools.modlist) with the same data has been created and is located in the TexTools folder. ", "ModList Change.", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }


        private void IDSearch(object obj)
        {
            ModelSearch modelSearch = new ModelSearch(this);
            modelSearch.Show();
        }


        /// <summary>
        /// Asks for game directory and sets default save directory
        /// </summary>
        private void SetDirectories()
        {
            string[] commonInstallDirectories = new string[]
            {
                "C:/Program Files (x86)/SquareEnix/FINAL FANTASY XIV - A Realm Reborn/game/sqpack/ffxiv",
                "C:/Program Files/SquareEnix/FINAL FANTASY XIV - A Realm Reborn/game/sqpack/ffxiv",
                "C:/Program Files/Steam/SteamApps/common/FINAL FANTASY XIV - A Realm Reborn/game/sqpack/ffxiv",
                "C:/Program Files (x86)/Steam/SteamApps/common/FINAL FANTASY XIV - A Realm Reborn/game/sqpack/ffxiv"
            };

            if (Properties.Settings.Default.FFXIV_Directory.Equals(""))
            {
                Properties.Settings.Default.Save_Directory = Directory.GetCurrentDirectory() + "/Saved";

                var installDirectory = "";
                foreach (var i in commonInstallDirectories)
                {
                    if (Directory.Exists(i))
                    {
                        if (MessageBox.Show("FFXIV install directory found at \n\n" + i + "\n\nUse this directory? ", "Install Directory Found", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
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
                    if (MessageBox.Show("Please locate the following directory. \n\n .../FINAL FANTASY XIV - A Realm Reborn/game/sqpack/ffxiv", "Install Directory Not Found", MessageBoxButton.OK, MessageBoxImage.Question) == MessageBoxResult.OK)
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

                Version curVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

                if (curVersion.CompareTo(v) < 0)
                {

                    Update up = new Update();

                    up.Message = "Version: " + v.ToString().Substring(0, 5) + "\n\nChange Log:\n" + changeLog + "\n\nPlease visit the website to download the update.";
                    up.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was an issue checking for updates. \n" + ex.Message, "Updater Error", MessageBoxButton.OK, MessageBoxImage.None);
            }
        }

        /// <summary>
        /// Creates files that will contain modded information
        /// </summary>
        private void MakeModContainers()
        {
            foreach(var datName in Info.ModDatDict)
            {
                var datPath = string.Format(Info.datDir, datName.Key, datName.Value);

                if (!File.Exists(datPath))
                {
                    CreateDat.MakeDat();
                    CreateDat.ChangeDatAmounts();
                }
            }

            if (!File.Exists(Info.modListDir))
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

            foreach(var cat in Info.MainCategoryList)
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
            JsonEntry modEntry = null;
            string line;

            try
            {
                using (StreamReader sr = new StreamReader(Info.modListDir))
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
                MessageBox.Show("[Main] Error Accessing .modlist File \n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

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
