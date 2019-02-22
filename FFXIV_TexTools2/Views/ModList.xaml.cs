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
using FFXIV_TexTools2.ViewModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace FFXIV_TexTools2.Views
{
    /// <summary>
    /// Interaction logic for ModList.xaml
    /// </summary>
    public partial class ModList : Window
    {
        ModelViewModel mvm;
        TextureViewModel tvm;

        public ModList(ModelViewModel mvm, TextureViewModel tvm)
        {
            InitializeComponent();

            this.mvm = mvm;
            this.tvm = tvm;

            ModListViewModel vm = new ModListViewModel();
            modListTreeView.DataContext = vm;
        }

        private void ModListTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selected = modListTreeView.SelectedItem as ModListTVViewModel;

            if(selected!=null&&selected.Parent != null)
            {
                ListViewModel vm = new ListViewModel(selected.Name);

                listBox.DataContext = vm;
            }
        }

        private void ModToggleButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = listBox.SelectedItems;

            foreach(ModListModel s in selected)
            {
                int offset = 0;

                if (s.Active == Brushes.Transparent)
                {
                    offset = s.Entry.originalOffset;
                    s.Active = Brushes.Gray;
                    s.ActiveOpacity = 0.5f;
                    s.ActiveBorder = Brushes.Red;
                    modToggleButton.Content = "Enable";
                }
                else
                {
                    offset = s.Entry.modOffset;
                    s.Active = Brushes.Transparent;
                    s.ActiveOpacity = 1;
                    s.ActiveBorder = Brushes.Green;
                    modToggleButton.Content = "Disable";
                }

                Helper.UpdateIndex(offset, s.Entry.fullPath, s.Entry.datFile);
                Helper.UpdateIndex2(offset, s.Entry.fullPath, s.Entry.datFile);
            }
        }

        private void ListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if(listBox.SelectedItems.Count < 2)
            {
                if (listBox.SelectedItem is ModListModel selected)
                {
                    if (selected.Active == Brushes.Transparent)
                    {
                        modToggleButton.Content = "Disable";
                    }
                    else
                    {
                        modToggleButton.Content = "Enable";
                    }

                    //goToButton.IsEnabled = true;
                    modToggleButton.IsEnabled = true;
                    deleteButton.IsEnabled = true;
                }
                else
                {
                    //goToButton.IsEnabled = false;
                    modToggleButton.IsEnabled = false;
                    deleteButton.IsEnabled = false;
                }
            }
            else
            {
                modToggleButton.Content = "Enable/Disable";
                modToggleButton.IsEnabled = true;
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = listBox.SelectedItems;

            List<ModListModel> removeList = new List<ModListModel>();

            var mlm = (ObservableCollection<ModListModel>)listBox.ItemsSource;

            foreach (ModListModel s in selected)
            {
                int offset = 0;

                if (s.Active == Brushes.Transparent)
                {
                    offset = s.Entry.originalOffset;

                    Helper.UpdateIndex(offset, s.Entry.fullPath, s.Entry.datFile);
                    Helper.UpdateIndex2(offset, s.Entry.fullPath, s.Entry.datFile);
                }

                removeList.Add(s);
            }

            string[] lines = File.ReadAllLines(Properties.Settings.Default.Modlist_Directory);

            foreach(var r in removeList)
            {
                for(int i = 0; i < lines.Length; i++)
                {
                    var modEntry = JsonConvert.DeserializeObject<JsonEntry>(lines[i]);

                    if (r.Entry.fullPath.Equals(modEntry.fullPath))
                    {
                        JsonEntry replaceEntry = new JsonEntry()
                        {
                            category = String.Empty,
                            name = String.Empty,
                            fullPath = String.Empty,
                            originalOffset = 0,
                            modOffset = modEntry.modOffset,
                            modSize = modEntry.modSize,
                            datFile = modEntry.datFile
                        };

                        lines[i] = JsonConvert.SerializeObject(replaceEntry);
                    }
                }

                mlm.Remove(r);
            }

            File.WriteAllLines(Properties.Settings.Default.Modlist_Directory, lines);

            if(mlm.Count < 1)
            {
                ModListViewModel vm = new ModListViewModel();
                modListTreeView.DataContext = vm;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (tvm != null)
            {
                tvm.ReloadTexture();
            }
            if (mvm != null)
            {
                mvm.ReloadModel();
            }
        }
    }
}
