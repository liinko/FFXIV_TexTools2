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

using FFXIV_TexTools2.Custom.Interop;
using FFXIV_TexTools2.Helpers;
using FFXIV_TexTools2.ViewModel;
using FFXIV_TexTools2.Views;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace FFXIV_TexTools2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainViewModel mViewModel = new MainViewModel();
            this.DataContext = mViewModel;

            DXVerStatus.Content = "DX Version: " + Properties.Settings.Default.DX_Ver.Substring(2);

            //HavokInterop.InitializeSTA();

            //searchTextBox.Text = Strings.SearchBox;
            //searchTextBox.Foreground = System.Windows.Media.Brushes.Gray;
        }

        private void Menu_DX9_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Properties.Settings.Default.DX_Ver = "DX9";
                Properties.Settings.Default.Save();

                Menu_DX9.IsEnabled = false;
                Menu_DX11.IsEnabled = true;
                Menu_DX11.IsChecked = false;

                DXVerStatus.Content = "DX Version: 9";

                if((ItemViewModel)textureTreeView.SelectedItem != null)
                {
                    var itemSelected = (ItemViewModel)textureTreeView.SelectedItem;
                    ((ItemViewModel)textureTreeView.SelectedItem).IsSelected = false;
                    itemSelected.IsSelected = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("[Main] DX Switch Error \n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Menu_DX11_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Properties.Settings.Default.DX_Ver = "DX11";
                Properties.Settings.Default.Save();

                Menu_DX11.IsEnabled = false;
                Menu_DX9.IsEnabled = true;
                Menu_DX9.IsChecked = false;

                DXVerStatus.Content = "DX Version: 11";

                if ((ItemViewModel)textureTreeView.SelectedItem != null)
                {
                    var itemSelected = (ItemViewModel)textureTreeView.SelectedItem;
                    ((ItemViewModel)textureTreeView.SelectedItem).IsSelected = false;
                    itemSelected.IsSelected = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("[Main] DX Switch Error \n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void Menu_ProblemCheck_Click(object sender, RoutedEventArgs e)
        {
            if (Helper.CheckIndex())
            {
                if (MessageBox.Show("The index file does not have access to the modded dat file. \nFix now?", "Found an Issue", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                {
                    Helper.FixIndex();
                }
            }
            else
            {
                MessageBox.Show("No issues were found \nIf you are still experiencing issues, please submit a bug report.", "No Issues Found", MessageBoxButton.OK, MessageBoxImage.None);
            }
        }

        private void Menu_BugReport_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://ffxivtextools.dualwield.net/bug_report.html");
        }

        private void Menu_About_Click(object sender, RoutedEventArgs e)
        {
            About a = new About();
            a.Owner = GetWindow(this);
            a.Show();
        }

        private void Menu_English_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Changing language requires the application to restart. \nRestart now?", "Language Change", MessageBoxButton.OKCancel, MessageBoxImage.Information) == MessageBoxResult.OK)
            {
                Properties.Settings.Default.Language = "en";
                Properties.Settings.Default.Save();

                System.Windows.Forms.Application.Restart();
                Application.Current.Shutdown();
            }
            else
            {
                Menu_English.IsChecked = false;
            }
        }

        private void Menu_Japanese_Click(object sender, RoutedEventArgs e)
        {


            if (MessageBox.Show("Changing language requires the application to restart. \nRestart now?", "Language Change", MessageBoxButton.OKCancel, MessageBoxImage.Information) == MessageBoxResult.OK)
            {
                Properties.Settings.Default.Language = "ja";
                Properties.Settings.Default.Save();

                System.Windows.Forms.Application.Restart();
                Application.Current.Shutdown();
            }
            else
            {
                Menu_Japanese.IsChecked = false;
            }
        }

        private void Menu_French_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Changing language requires the application to restart. \nRestart now?", "Language Change", MessageBoxButton.OKCancel, MessageBoxImage.Information) == MessageBoxResult.OK)
            {
                Properties.Settings.Default.Language = "fr";
                Properties.Settings.Default.Save();

                System.Windows.Forms.Application.Restart();
                Application.Current.Shutdown();
            }
            else
            {
                Menu_French.IsChecked = false;
            }
        }

        private void Menu_German_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Changing language requires the application to restart. \nRestart now?", "Language Change", MessageBoxButton.OKCancel, MessageBoxImage.Information) == MessageBoxResult.OK)
            {
                Properties.Settings.Default.Language = "de";
                Properties.Settings.Default.Save();

                System.Windows.Forms.Application.Restart();
                Application.Current.Shutdown();
            }
            else
            {
                Menu_German.IsChecked = false;
            }
        }

        private void Menu_ModList_Click(object sender, RoutedEventArgs e)
        {
            ModList ml = new ModList();
            ml.Show();
        }

        private void Menu_Importer_Click(object sender, RoutedEventArgs e)
        {
            //Not yet implemented
        }

        private void Menu_Directories_Click(object sender, RoutedEventArgs e)
        {
            DirectoriesView dv = new DirectoriesView();
            dv.Show();
        }

        private void Menu_RevertAll_Click(object sender, RoutedEventArgs e)
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
                        if(modEntry.originalOffset != 0)
                        {
                            Helper.UpdateIndex(modEntry.originalOffset, modEntry.fullPath);
                            Helper.UpdateIndex2(modEntry.originalOffset, modEntry.fullPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("[Main] Error Accessing .modlist File \n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void Menu_ReapplyAll_Click(object sender, RoutedEventArgs e)
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
                        if(modEntry.originalOffset != 0)
                        {
                            Helper.UpdateIndex(modEntry.modOffset, modEntry.fullPath);
                            Helper.UpdateIndex2(modEntry.modOffset, modEntry.fullPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("[Main] Error Accessing .modlist File \n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            App.Current.Shutdown();
        }
    }
}
