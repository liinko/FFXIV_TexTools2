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

using FolderSelect;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace FFXIV_TexTools2.Views
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class DirectoriesView : Window
    {
        public DirectoriesView()
        {
            InitializeComponent();
            DataContext = Properties.Settings.Default;

            var savePath = Path.GetFullPath(Properties.Settings.Default.Save_Directory);
            saveDir.Text = savePath;

            var ffxivPath = Path.GetFullPath(Properties.Settings.Default.FFXIV_Directory);
            FFXIVDir.Text = ffxivPath;
        }

        private void FFXIVDirButton_Click(object sender, RoutedEventArgs e)
        {
            FolderSelectDialog folderSelect = new FolderSelectDialog()
            {
                Title = "Select ffxiv folder"
            };
            var result = folderSelect.ShowDialog();
            if (result)
            {
                Properties.Settings.Default.FFXIV_Directory = folderSelect.FileName;
                Properties.Settings.Default.Save();

                FFXIVDir.Text = folderSelect.FileName;
            }
        }

        private void SaveDirButton_Click(object sender, RoutedEventArgs e)
        {
            FolderSelectDialog folderSelect = new FolderSelectDialog()
            {
                Title = "Select save folder"
            };
            var result = folderSelect.ShowDialog();
            if (result)
            {
                Properties.Settings.Default.Save_Directory = folderSelect.FileName;
                Properties.Settings.Default.Save();

                saveDir.Text = folderSelect.FileName;
            }
        }
    }
}
