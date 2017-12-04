﻿// FFXIV TexTools
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
using FFXIV_TexTools2.Resources;
using FFXIV_TexTools2.ViewModel;
using FFXIV_TexTools2.Views;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace FFXIV_TexTools2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainViewModel mViewModel;

        public MainWindow()
        {
            InitializeComponent();
            mViewModel = new MainViewModel();
            DataContext = mViewModel;

            DXVerStatus.Content = "DX Version: " + Properties.Settings.Default.DX_Ver.Substring(2);

            //HavokInterop.InitializeSTA();
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

                if ((CategoryViewModel)textureTreeView.SelectedItem != null)
                {
                    var itemSelected = (CategoryViewModel)textureTreeView.SelectedItem;
                    ((CategoryViewModel)textureTreeView.SelectedItem).IsSelected = false;
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

                if ((CategoryViewModel)textureTreeView.SelectedItem != null)
                {
                    var itemSelected = (CategoryViewModel)textureTreeView.SelectedItem;
                    ((CategoryViewModel)textureTreeView.SelectedItem).IsSelected = false;
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
            try
            {
                RevertAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show("[Main] Error Accessing .modlist File \n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void RevertAll()
        {
            JsonEntry modEntry = null;
            string line;

            using (StreamReader sr = new StreamReader(Info.modListDir))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    modEntry = JsonConvert.DeserializeObject<JsonEntry>(line);
                    if (modEntry.originalOffset != 0)
                    {
                        Helper.UpdateIndex(modEntry.originalOffset, modEntry.fullPath, modEntry.datFile);
                        Helper.UpdateIndex2(modEntry.originalOffset, modEntry.fullPath, modEntry.datFile);
                    }
                }
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
                            Helper.UpdateIndex(modEntry.modOffset, modEntry.fullPath, modEntry.datFile);
                            Helper.UpdateIndex2(modEntry.modOffset, modEntry.fullPath, modEntry.datFile);
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

        GridLength RCWidth;
        double RCMinWidth;
        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {

            if (MainSplitter.IsEnabled == true)
            {
                IMGCollapse1.RenderTransformOrigin = new Point(0.5, 0.5);
                IMGCollapse2.RenderTransformOrigin = new Point(0.5, 0.5);
                ScaleTransform flipTrans = new ScaleTransform();
                flipTrans.ScaleX = -1;
                IMGCollapse1.RenderTransform = flipTrans;
                IMGCollapse2.RenderTransform = flipTrans;
                RCMinWidth = RightColumn.MinWidth;
                RCWidth = RightColumn.Width;
                RightColumn.MinWidth = 8;
                RightColumn.MaxWidth = 8;
                MainSplitter.IsEnabled = false;
            }
            else
            {
                IMGCollapse1.RenderTransformOrigin = new Point(0.5, 0.5);
                IMGCollapse2.RenderTransformOrigin = new Point(0.5, 0.5);
                ScaleTransform flipTrans = new ScaleTransform();
                flipTrans.ScaleX = 1;
                IMGCollapse1.RenderTransform = flipTrans;
                IMGCollapse2.RenderTransform = flipTrans;
                RightColumn.MaxWidth = 100000;
                RightColumn.MinWidth = RCMinWidth;
                RightColumn.Width = RCWidth;
                MainSplitter.IsEnabled = true;
            }
        }

 
        private void textureTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var item = e.NewValue as CategoryViewModel;
            CategoryViewModel topLevel = null;
            if(item!= null)
            {
                Save_All_DDS.IsEnabled = true;
                var itemParent = item.Parent;

                if (item.Children != null)
                {
                    if (item.IsExpanded != true)
                    {
                        item.IsExpanded = true;
                        item.IsSelected = false;
                    }
                    else
                    {
                        item.IsExpanded = false;
                        item.IsSelected = false;
                    }
                }

                while (itemParent != null)
                {
                    topLevel = itemParent;
                    itemParent = itemParent.Parent;
                }

                if (item.ItemData != null)
                {
                    if (!topLevel.Name.Equals("UI"))
                    {
                        mViewModel.TextureVM.UpdateTexture(item.ItemData, item.Parent.Name);

                        if (item.Name.Equals(Strings.Face_Paint) || item.Name.Equals(Strings.Equipment_Decals))
                        {
                            tabControl.SelectedIndex = 0;
                            if (mViewModel.ModelVM != null)
                            {
                                mViewModel.ModelVM.ModelTabEnabled = false;
                            }
                        }
                        else
                        {
                            mViewModel.ModelVM.UpdateModel(item.ItemData, item.Parent.Name);
                            mViewModel.ModelVM.ModelTabEnabled = true;
                        }
                    }
                    else
                    {
                        tabControl.SelectedIndex = 0;
                        mViewModel.TextureVM.UpdateTexture(item.ItemData, "UI");
                        mViewModel.ModelVM.ModelTabEnabled = false;
                    }

                }

            }
            else
            {
                Save_All_DDS.IsEnabled = false;
            }
        }
        private void NewBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) { 
                    DragMove();
            }
        }

        private void NewBar_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Normal)
            {
                WindowState = System.Windows.WindowState.Maximized;
            }
            else
            {
                WindowState = System.Windows.WindowState.Normal;
            }
        }
        private void MaximizeButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Normal)
            {
                WindowState = System.Windows.WindowState.Maximized;
            }
            else
            {
                WindowState = System.Windows.WindowState.Normal;
            }
        }
        private void MinimizeButton_MouseDown(object sender, MouseButtonEventArgs e)
        { WindowState = System.Windows.WindowState.Minimized; }
        private void CloseButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            App.Current.Shutdown();
        }
            private void Save_All_DDS_Click(object sender, RoutedEventArgs e)
        {
            mViewModel.TextureVM.SaveAllDDS();
        }

        private void Menu_StartOver_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Starting over will:\n\n" +
                "Restore index files to their original state.\n" +
                "Delete all mods and create new .dat files.\n" +
                "Delete all .modlist file entries.\n\n" +
                "Do you want to start over?", "Start Over", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
            {

                RevertAll();

                string[] indexFiles = new string[]
                {
                    Directory.GetCurrentDirectory() + "/Index_Backups/040000.win32.index",
                    Directory.GetCurrentDirectory() + "/Index_Backups/040000.win32.index2",
                    Directory.GetCurrentDirectory() + "/Index_Backups/060000.win32.index",
                    Directory.GetCurrentDirectory() + "/Index_Backups/060000.win32.index2"
                };

                foreach(var i in indexFiles)
                {
                    if (File.Exists(i))
                    {
                        File.Copy(i, Properties.Settings.Default.FFXIV_Directory + "/" + Path.GetFileName(i), true);
                    }
                }

                foreach (var datName in Info.ModDatDict)
                {
                    var datPath = string.Format(Info.datDir, datName.Key, datName.Value);

                    if (File.Exists(datPath))
                    {
                        File.Delete(datPath);
                    }
                }

                File.Delete(Info.modListDir);

                MakeModContainers();
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

            if (!File.Exists(Info.modListDir))
            {
                CreateDat.CreateModList();
            }
        }


        private void SplitCollapse_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
 }

        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (searchBox.Text == "")
            {
                // Create an ImageBrush.
                ImageBrush textImageBrush = new ImageBrush();
                textImageBrush.ImageSource =
                    new BitmapImage(
                        new Uri("pack://application:,,,/Resources/search.png")
                    );
                textImageBrush.AlignmentX = AlignmentX.Left;
                // Use the brush to paint the button's background.
                searchBox.Background = textImageBrush;

            }
            else
            {

                searchBox.Background = null;
            }
       
    }

   
}
