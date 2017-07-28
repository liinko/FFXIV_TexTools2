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
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Xml;

namespace FFXIV_TexTools2.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        string searchText;
        TextureViewModel TVM;
        ModelViewModel MVM;
        List<ItemData> itemList;
        ObservableCollection<ItemData> oItemList;
        ObservableCollection<CategoryViewModel> _categories;
        List<string> categoryList = new List<string>();
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

        public bool ModlistOnEnabled { get { return modListOnEnabled; } set { modListOnEnabled = value; NotifyPropertyChanged("ModlistOnEnabled"); } }
        public bool ModlistOnChecked { get { return modListOnChecked; } set { modListOnChecked = value; NotifyPropertyChanged("ModlistOnChecked"); } }
        public bool ModlistOffEnabled { get { return modListOffEnabled; } set { modListOffEnabled = value; NotifyPropertyChanged("ModlistOffEnabled"); } }
        public bool ModlistOffChecked { get { return modListOffChecked; } set { modListOffChecked = value; NotifyPropertyChanged("ModlistOffChecked"); } }


        public TextureViewModel TextureVM { get { return TVM; } set { TVM = value; NotifyPropertyChanged("TextureVM"); } }
        public ModelViewModel ModelVM { get { return MVM; } set { MVM = value; NotifyPropertyChanged("ModelVM"); } }


        /// <summary>
        /// View Model Constructor for Main Window
        /// </summary>
        public MainViewModel()
        {
            SetDirectories();

            CultureInfo ci = new CultureInfo(Properties.Settings.Default.Language);
            CultureInfo.DefaultThreadCurrentCulture = ci;
            CultureInfo.DefaultThreadCurrentUICulture = ci;

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

                    if (Properties.Settings.Default.Mod_List == 0)
                    {
                        RevertAllMods();
                    }

                    try
                    {
                        File.Copy(Properties.Settings.Default.FFXIV_Directory + "/040000.win32.index", Directory.GetCurrentDirectory() + "/Index_Backups/040000.win32.index", true);
                        File.Copy(Properties.Settings.Default.FFXIV_Directory + "/040000.win32.index2", Directory.GetCurrentDirectory() + "/Index_Backups/040000.win32.index2", true);
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

                        if (Properties.Settings.Default.Mod_List == 0)
                        {
                            RevertAllMods();
                        }

                        try
                        {
                            File.Copy(Properties.Settings.Default.FFXIV_Directory + "/040000.win32.index", Directory.GetCurrentDirectory() + "/Index_Backups/040000.win32.index", true);
                            File.Copy(Properties.Settings.Default.FFXIV_Directory + "/040000.win32.index2", Directory.GetCurrentDirectory() + "/Index_Backups/040000.win32.index2", true);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("There was an issue creating backup files. \n" + e.Message, "Backup Error", MessageBoxButton.OK, MessageBoxImage.None);
                        }
                    }
                }
            }


            if(Properties.Settings.Default.Mod_List == 0)
            {
                ModlistOnEnabled = false;
                ModlistOnChecked = true;

                ModlistOffEnabled = true;
                ModlistOffChecked = false;
            }
            else
            {
                ModlistOnEnabled = true;
                ModlistOnChecked = false;

                ModlistOffEnabled = false;
                ModlistOffChecked = true;

            }

            MakeModContainers();
            FillTree();
            SetEventHandler();
        }

        /// <summary>
        /// Command for the ModList Menu
        /// </summary>
        public ICommand ModListOnCommand
        {
            get { return new RelayCommand(ModlistOn); }
        }

        /// <summary>
        /// Command for the ModList Menu
        /// </summary>
        public ICommand ModListOffCommand
        {
            get { return new RelayCommand(ModlistOff); }
        }

        private void ModlistOff(object obj)
        {
            Properties.Settings.Default.Mod_List = 1;
            Properties.Settings.Default.Save();

            ModlistOnEnabled = true;
            ModlistOnChecked = false;

            ModlistOffEnabled = false;
            ModlistOffChecked = true;
        }

        private void ModlistOn(object obj)
        {

            Properties.Settings.Default.Mod_List = 0;
            Properties.Settings.Default.Save();

            ModlistOnEnabled = false;
            ModlistOnChecked = true;

            ModlistOffEnabled = true;
            ModlistOffChecked = false;
        }

        /// <summary>
        /// Asks for game directory and sets default save directory
        /// </summary>
        private void SetDirectories()
        {
            if (Properties.Settings.Default.FFXIV_Directory.Equals(""))
            {
                Properties.Settings.Default.Save_Directory = Directory.GetCurrentDirectory() + "/Saved";
                FolderSelectDialog folderSelect = new FolderSelectDialog()
                {
                    Title = "Select sqpack/ffxiv Folder"
                };
                folderSelect.ShowDialog();
                Properties.Settings.Default.FFXIV_Directory = folderSelect.FileName;
                Properties.Settings.Default.Save();
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
            if (!File.Exists(Info.modDatDir))
            {
                CreateDat.MakeDat();
                CreateDat.ChangeDatAmounts();
            }

            if (!File.Exists(Info.modListDir) && Properties.Settings.Default.Mod_List == 0)
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
            itemList = ExdReader.MakeItemsList();

            itemList.AddRange(ExdReader.MakeMountsList());

            itemList.AddRange(ExdReader.MakeMinionsList());

            var characterCategory = "25";

            ItemData item = new ItemData(){ ItemName = Strings.Body, ItemCategory = characterCategory };
            itemList.Add(item);

            item = new ItemData(){ ItemName = Strings.Face, ItemCategory = characterCategory };
            itemList.Add(item);

            item = new ItemData() { ItemName = Strings.Hair, ItemCategory = characterCategory };
            itemList.Add(item);

            item = new ItemData() { ItemName = Strings.Tail, ItemCategory = characterCategory };
            itemList.Add(item);

            item = new ItemData() { ItemName = Strings.Face_Paint, ItemCategory = characterCategory };
            itemList.Add(item);

            item = new ItemData() { ItemName = Strings.Equipment_Decals, ItemCategory = characterCategory };
            itemList.Add(item);

            var petCategory = "22";

            item = new ItemData(){ ItemName = Strings.Eos, ItemCategory = petCategory };
            itemList.Add(item);

            item = new ItemData() { ItemName = Strings.Selene, ItemCategory = petCategory };
            itemList.Add(item);

            item = new ItemData() { ItemName = Strings.Carbuncle, ItemCategory = petCategory };
            itemList.Add(item);

            item = new ItemData() { ItemName = Strings.Ifrit_Egi, ItemCategory = petCategory };
            itemList.Add(item);

            item = new ItemData() { ItemName = Strings.Titan_Egi, ItemCategory = petCategory };
            itemList.Add(item);

            item = new ItemData() { ItemName = Strings.Garuda_Egi, ItemCategory = petCategory };
            itemList.Add(item);

            item = new ItemData() { ItemName = Strings.Ramuh_Egi, ItemCategory = petCategory };
            itemList.Add(item);

            item = new ItemData() { ItemName = Strings.Rook_Autoturret, ItemCategory = petCategory };
            itemList.Add(item);

            item = new ItemData() { ItemName = Strings.Bishop_Autoturret, ItemCategory = petCategory };
            itemList.Add(item);

            oItemList = new ObservableCollection<ItemData>(itemList);

            foreach (string slot in Info.IDSlot.Keys)
            {
                if (!slot.Equals(Strings.Waist) && !slot.Equals(Strings.Soul_Crystal))
                {
                    categoryList.Add(slot);
                }
            }

            Category = new ObservableCollection<CategoryViewModel>((from category in categoryList select new CategoryViewModel(category, oItemList)).ToList());
        }

        /// <summary>
        /// Sets the event handler for each item in order to detect item selection on treeview
        /// </summary>
        private void SetEventHandler()
        {
            foreach (var category in Category)
            {
                foreach (var item in category.Children)
                {
                    item.PropertyChanged += new PropertyChangedEventHandler(TreeView_SelectionChanged);
                }
            }
        }

        /// <summary>
        /// Sets the View Model for the Texture and Model tab when an item is selected from the treeview
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeView_SelectionChanged(object sender, PropertyChangedEventArgs e)
        {
            var itemVM = sender as ItemViewModel;

            if (itemVM.IsSelected)
            {
                //dispose of the data from teh previously selected item
                if(ModelVM != null)
                {
                    ModelVM.Dispose();
                }

                TextureVM = new TextureViewModel(itemVM.Item, ((CategoryViewModel)itemVM.Parent).CategoryName);

                if(itemVM.ItemName.Equals(Strings.Face_Paint) || itemVM.ItemName.Equals(Strings.Equipment_Decals))
                {
                    if(ModelVM != null)
                    {
                        ModelVM.ModelTabEnabled = false;
                    }
                }
                else
                {
                    ModelVM = new ModelViewModel(itemVM.Item, ((CategoryViewModel)itemVM.Parent).CategoryName);
                    ModelVM.ModelTabEnabled = true;
                }
            }
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
                IEnumerable<ItemData> matchingItems = from item in itemList where item.ItemName.ToLower().Contains(SearchText.ToLower()) orderby item.ItemName select item;
                HashSet<string> categories = new HashSet<string>();

                foreach (ItemData item in matchingItems)
                {
                    var key = Info.IDSlot.FirstOrDefault(x => x.Value == item.ItemCategory).Key;
                    categories.Add(key);
                }

                Category = new ObservableCollection<CategoryViewModel>((from category in categories select new CategoryViewModel(category, new ObservableCollection<ItemData>(matchingItems))).ToList());

                foreach (var c in Category)
                {
                    c.IsExpanded = true;
                }
            }
            else
            {
                Category = new ObservableCollection<CategoryViewModel>((from category in categoryList select new CategoryViewModel(category, oItemList)).ToList());
            }

            SetEventHandler();
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
                        Helper.UpdateIndex(modEntry.originalOffset, modEntry.fullPath);
                        Helper.UpdateIndex2(modEntry.originalOffset, modEntry.fullPath);
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
