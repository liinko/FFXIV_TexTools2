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
using System.Windows;
using System.Windows.Media;

namespace FFXIV_TexTools2.Views
{
    /// <summary>
    /// Interaction logic for ModList.xaml
    /// </summary>
    public partial class ModList : Window
    {
        public ModList()
        {
            InitializeComponent();

            ModListViewModel vm = new ModListViewModel();
            modListTreeView.DataContext = vm;
        }

        private void ModListTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selected = modListTreeView.SelectedItem as ModListTVViewModel;

            if(selected.Parent != null)
            {
                ListViewModel vm = new ListViewModel(selected.Name);

                listBox.DataContext = vm;
            }
        }

        private void ModToggleButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = listBox.SelectedItem as ModListModel;
            int offset = 0;

            if (modToggleButton.Content.Equals("Disable"))
            {
                offset = selected.Entry.originalOffset;
                selected.Active = Brushes.Gray;
                selected.ActiveOpacity = 0.5f;
                selected.ActiveBorder = Brushes.Red;

            }
            else if (modToggleButton.Content.Equals("Enable"))
            {
                offset = selected.Entry.modOffset;
                selected.Active = Brushes.Transparent;
                selected.ActiveOpacity = 1;
                selected.ActiveBorder = Brushes.Green;

            }

            Helper.UpdateIndex(offset, selected.Entry.fullPath, selected.Entry.datFile);
            Helper.UpdateIndex2(offset, selected.Entry.fullPath, selected.Entry.datFile);

        }

        private void ListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
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
            }
            else
            {
                //goToButton.IsEnabled = false;
                modToggleButton.IsEnabled = false;
            }

        }

        private void GoToButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = listBox.SelectedItem as ModListModel;

            //((MainWindow)Owner).GoToItem(selected);
        }
    }
}
