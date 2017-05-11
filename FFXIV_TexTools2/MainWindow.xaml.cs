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
using FFXIV_TexTools2.Material;
using FFXIV_TexTools2.Model;
using FFXIV_TexTools2.Resources;
using FFXIV_TexTools2.Shader;
using FFXIV_TexTools2.ViewModel;
using FolderSelect;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FFXIV_TexTools2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<Items> itemList;
        Dictionary<string, Items> mountsDict, minionsDict;
        MTRLInfo mtrlInfo;
        string selectedParent, imcVersion, fullPath;
        Items selectedItem;
        BitmapSource noAlphaBitmap, alphaBitmap;
        ColorChannels CC = new ColorChannels();
        List<Category> categoryList;
        TexInfo texInfo;

        public MainWindow()
        {
            InitializeComponent();

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

            CultureInfo ci = new CultureInfo(Properties.Settings.Default.Language);
            CultureInfo.DefaultThreadCurrentCulture = ci;
            CultureInfo.DefaultThreadCurrentUICulture = ci;

            foreach(Button b in modelButtonGrid.Children)
            {
                b.IsEnabled = false;
            }

            if (Properties.Settings.Default.Language.Equals("en"))
            {
                Menu_English.IsChecked = true;
                Menu_English.IsEnabled = false;
            }
            else if (Properties.Settings.Default.Language.Equals("ja"))
            {
                Menu_Japanese.IsChecked = true;
                Menu_Japanese.IsEnabled = false;
            }
            else if (Properties.Settings.Default.Language.Equals("fr"))
            {
                Menu_French.IsChecked = true;
                Menu_French.IsEnabled = false;
            }
            else if (Properties.Settings.Default.Language.Equals("de"))
            {
                Menu_German.IsChecked = true;
                Menu_German.IsEnabled = false;
            }

            if (Properties.Settings.Default.DX_Ver.Equals("DX11"))
            {
                Menu_DX11.IsChecked = true;
                Menu_DX11.IsEnabled = false;
            }
            else if (Properties.Settings.Default.DX_Ver.Equals("DX9"))
            {
                Menu_DX9.IsChecked = true;
                Menu_DX9.IsEnabled = false;
            }

            importButton.IsEnabled = false;
            revertButton.IsEnabled = false;

            raceComboBox.SelectionChanged += new SelectionChangedEventHandler(RaceComboBox_SelectionChanged);
            mapComboBox.SelectionChanged += new SelectionChangedEventHandler(MapComboBox_SelectionChanged);
            partComboBox.SelectionChanged += new SelectionChangedEventHandler(PartComboBox_SelectionChanged);
            typeComboBox.SelectionChanged += new SelectionChangedEventHandler(TypeComboBox_SelectionChanged);

            searchTextBox.Text = Strings.SearchBox;
            searchTextBox.Foreground = System.Windows.Media.Brushes.Gray;

            FillTree();
        }

        private void FillTree()
        {
            BackgroundWorker fillTreeView = new BackgroundWorker();
            fillTreeView.WorkerReportsProgress = true;
            fillTreeView.WorkerSupportsCancellation = true;
            fillTreeView.DoWork += new DoWorkEventHandler(FillTreeView_Work);
            fillTreeView.ProgressChanged += new ProgressChangedEventHandler(FillTreeView_ProgressChanged);
            fillTreeView.RunWorkerCompleted += new RunWorkerCompletedEventHandler(FillTreeView_RunWorkerCompleted);
            fillTreeView.RunWorkerAsync();
        }

        private void FillTreeView_Work(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            if (!File.Exists(Info.modDatDir))
            {
                CreateDat.MakeDat();
                CreateDat.CreateModList();
            }

            var itemDicts = ExdReader.MakeItemsList();
            itemList = itemDicts.Values.ToList();

            mountsDict = ExdReader.MakeMountsList();
            itemList.AddRange(mountsDict.Values);

            minionsDict = ExdReader.MakeMinionsList();
            itemList.AddRange(minionsDict.Values);

            itemList.Add(new Items(Strings.Body, "25"));
            itemList.Add(new Items(Strings.Face, "25"));
            itemList.Add(new Items(Strings.Hair, "25"));
            itemList.Add(new Items(Strings.Tail, "25"));
            itemList.Add(new Items(Strings.Face_Paint, "25"));
            itemList.Add(new Items(Strings.Equipment_Decals, "25"));

            itemList.Add(new Items(Strings.Eos, "22"));
            itemList.Add(new Items(Strings.Selene, "22"));
            itemList.Add(new Items(Strings.Carbuncle, "22"));
            itemList.Add(new Items(Strings.Ifrit_Egi, "22"));
            itemList.Add(new Items(Strings.Titan_Egi, "22"));
            itemList.Add(new Items(Strings.Garuda_Egi, "22"));
            itemList.Add(new Items(Strings.Ramuh_Egi, "22"));
            itemList.Add(new Items(Strings.Rook_Autoturret, "22"));
            itemList.Add(new Items(Strings.Bishop_Autoturret, "22"));
        }

        private void FillTreeView_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void FillTreeView_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if(e.Error != null)
            {
                MessageBox.Show(Strings.Error_FileNotFound + "\n\n" + e.Error.Message, Strings.Error_DirectoryError);
            }
            else
            {
                categoryList = new List<Category>();

                foreach(string slot in Info.IDSlot.Keys)
                {
                    categoryList.Add(new Category(slot)); 
                }

                TreeViewModel viewModel = new TreeViewModel(categoryList.ToArray(), itemList);
                textureTreeView.DataContext = viewModel;
            }
        }

        private void TextureTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if(textureTreeView.SelectedItem is ItemViewModel)
            {
                var item = textureTreeView.SelectedItem as ItemViewModel;
                var parent = item.Parent as CategoryViewModel;

                selectedParent = parent.CategoryName;
                selectedItem = item.Item;

                BackgroundWorker selectedWorker = new BackgroundWorker();
                selectedWorker.WorkerReportsProgress = true;
                selectedWorker.DoWork += new DoWorkEventHandler(SelectedWoker_Work);
                selectedWorker.ProgressChanged += new ProgressChangedEventHandler(SelectedWorker_ProgressChanged);
                selectedWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(SelectedWorker_RunWorkerCompleted);
                selectedWorker.RunWorkerAsync();
            }
        }

        private void SelectedWoker_Work(object sender, DoWorkEventArgs e)
        {
            imcVersion = IMC.GetVersion(selectedParent, selectedItem, false);

            e.Result = MTRL.GetMTRLRaces(selectedItem, selectedParent, imcVersion);
        }

        private void SelectedWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void SelectedWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if(e.Result != null)
            {
                ComboView cView = new ComboView(e.Result as List<ComboBoxInfo>);
                raceComboBox.DataContext = cView;
                raceComboBox.SelectedIndex = 0;

                if ((e.Result as List<ComboBoxInfo>).Count > 1)
                {
                    raceComboBox.IsEnabled = true;
                }
                else
                {
                    raceComboBox.IsEnabled = false;
                }

            }
        }

        private void RaceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboInfo = (ComboBoxInfo)raceComboBox.SelectedItem;

            if (e.AddedItems.Count > 0)
            {
                BackgroundWorker raceWorker = new BackgroundWorker();
                raceWorker.WorkerReportsProgress = true;
                raceWorker.DoWork += new DoWorkEventHandler(RaceWoker_Work);
                raceWorker.ProgressChanged += new ProgressChangedEventHandler(RaceWorker_ProgressChanged);
                raceWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(RaceWorker_RunWorkerCompleted);
                raceWorker.RunWorkerAsync(comboInfo);
            }

            e.Handled = true;
        }


        private void RaceWoker_Work(object sender, DoWorkEventArgs e)
        {
            var comboInfo = (ComboBoxInfo)e.Argument;

            e.Result = MTRL.GetMTRLParts(comboInfo, selectedItem, imcVersion, selectedParent);
        }

        private void RaceWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void RaceWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if(e.Result != null)
            {
                ComboView cView = new ComboView(e.Result as List<ComboBoxInfo>);
                partComboBox.DataContext = cView;
                partComboBox.SelectedIndex = 0;

                if ((e.Result as List<ComboBoxInfo>).Count > 1)
                {
                    partComboBox.IsEnabled = true;
                }
                else
                {
                    partComboBox.IsEnabled = false;

                }
            }
        }

        private void PartComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxInfo[] cbi = new ComboBoxInfo[2];
            cbi[0] = partComboBox.SelectedItem as ComboBoxInfo;
            cbi[1] = raceComboBox.SelectedItem as ComboBoxInfo;

            if (e.AddedItems.Count > 0)
            {
                BackgroundWorker partsWorker = new BackgroundWorker();
                partsWorker.WorkerReportsProgress = true;
                partsWorker.DoWork += new DoWorkEventHandler(PartsWoker_Work);
                partsWorker.ProgressChanged += new ProgressChangedEventHandler(PartsWorker_ProgressChanged);
                partsWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(PartsdWorker_RunWorkerCompleted);
                partsWorker.RunWorkerAsync(cbi);
            }

            e.Handled = true;
        }

        private void PartsWoker_Work(object sender, DoWorkEventArgs e)
        {
            var comboInfo = (ComboBoxInfo[])e.Argument;

            e.Result = MTRL.GetMTRLOffset(comboInfo[1], selectedParent, selectedItem, comboInfo[0].Name, imcVersion, "");
        }

        private void PartsWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void PartsdWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if(e.Result != null)
            {

                var info = e.Result as Tuple<MTRLInfo, List<ComboBoxInfo>>;
                mtrlInfo = info.Item1;
                ComboView cView = new ComboView(info.Item2);

                if (selectedItem.itemName.Equals(Strings.Face) || selectedItem.itemName.Equals(Strings.Hair))
                {
                    typeComboBox.DataContext = cView;
                    typeComboBox.SelectedIndex = 0;
                    typeComboBox.IsEnabled = true;
                }
                else if (selectedParent.Equals(Strings.Mounts))
                {
                    if ((mountsDict[selectedItem.itemName]).itemID.Equals("1") || (mountsDict[selectedItem.itemName]).itemID.Equals("2") ||
                        (mountsDict[selectedItem.itemName]).itemID.Equals("1011") || (mountsDict[selectedItem.itemName]).itemID.Equals("1022"))
                    {
                        typeComboBox.DataContext = cView;
                        typeComboBox.SelectedIndex = 0;
                        typeComboBox.IsEnabled = true;
                    }
                    else
                    {
                        typeComboBox.DataContext = null;
                        typeComboBox.IsEnabled = false;
                        mapComboBox.DataContext = cView;
                        mapComboBox.SelectedIndex = 0;
                    }
                }
                else
                {
                    typeComboBox.DataContext = null;
                    typeComboBox.IsEnabled = false;
                    mapComboBox.DataContext = cView;
                    mapComboBox.SelectedIndex = 0;
                }
            }
        }

        private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboInfo = typeComboBox.SelectedItem as ComboBoxInfo;

            if (e.AddedItems.Count > 0)
            {

                var info = MTRL.GetTexFromType(selectedItem, raceComboBox.SelectedItem as ComboBoxInfo, ((ComboBoxInfo)partComboBox.SelectedItem).Name, comboInfo.Name, imcVersion, selectedParent);
                mtrlInfo = info.Item1;

                ComboView cView = new ComboView(info.Item2);
                mapComboBox.DataContext = cView;

                mapComboBox.SelectedIndex = 0;
            }

            e.Handled = true;
        }

        private void MapComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var comboInfo = mapComboBox.SelectedItem as ComboBoxInfo;
                Paragraph paragraph = new Paragraph();
                paragraph.TextAlignment = TextAlignment.Center;
                Bitmap colorBmp = null;
                int offset = 0;

                if (comboInfo.Name.Equals(Strings.Normal))
                {
                    fullPath = mtrlInfo.NormalPath;
                    offset = mtrlInfo.NormalOffset;      
                    paragraph.Inlines.Add(new Run(fullPath));
                }
                else if (comboInfo.Name.Equals(Strings.Specular))
                {
                    fullPath = mtrlInfo.SpecularPath;
                    offset = mtrlInfo.SpecularOffset;
                    paragraph.Inlines.Add(new Run(fullPath));
                }
                else if (comboInfo.Name.Equals(Strings.Diffuse))
                {
                    fullPath = mtrlInfo.DiffusePath;
                    offset = mtrlInfo.DiffuseOffset;
                    paragraph.Inlines.Add(new Run(fullPath));
                }
                else if (comboInfo.Name.Equals(Strings.Mask) || comboInfo.Name.Equals(Strings.Skin))
                {
                    if(selectedItem.itemName.Equals(Strings.Face_Paint) || selectedItem.itemName.Equals(Strings.Equipment_Decals))
                    {
                        string part;
                        if (selectedItem.itemName.Equals(Strings.Equipment_Decals))
                        {
                            part = ((ComboBoxInfo)partComboBox.SelectedItem).Name.PadLeft(3, '0');
                        }
                        else
                        {
                            part = ((ComboBoxInfo)partComboBox.SelectedItem).Name;
                        }

                        fullPath = mtrlInfo.MaskPath;
                        offset = MTRL.GetDecalOffset(selectedItem.itemName, ((ComboBoxInfo)partComboBox.SelectedItem).Name);
                        paragraph.Inlines.Add(new Run(String.Format(fullPath, part)));
                    }
                    else
                    {
                        fullPath = mtrlInfo.MaskPath;
                        offset = mtrlInfo.MaskOffset;
                        paragraph.Inlines.Add(new Run(fullPath));
                    }
                }
                else if (comboInfo.Name.Equals(Strings.ColorSet))
                {
                    colorBmp = TEX.TextureToBitmap(mtrlInfo.ColorData, 9312, 4, 16);
                }

                if (File.Exists(Properties.Settings.Default.Save_Directory + "/" + selectedParent + "/" + selectedItem.itemName + "/" +  Path.GetFileNameWithoutExtension(fullPath) + ".dds"))
                {
                    importButton.IsEnabled = true;
                }
                else
                {
                    importButton.IsEnabled = false;
                }

                fullPathLabel.Document = new FlowDocument(paragraph);

                if (offset == 0)
                {
                    texTypeLabel.Content = "A16B16G16R16F";
                    texDimensionLabel.Content = "(4 x 16)";

                    alphaBitmap = Imaging.CreateBitmapSourceFromHBitmap(colorBmp.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                    var removeAlphaBitmap = SetAlpha(colorBmp, 255);

                    noAlphaBitmap = Imaging.CreateBitmapSourceFromHBitmap(removeAlphaBitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                    RenderOptions.SetBitmapScalingMode(texImage, BitmapScalingMode.NearestNeighbor);
                }
                else
                {
                    texInfo = TEX.GetTex(offset);

                    texTypeLabel.Content = texInfo.TypeString;
                    texDimensionLabel.Content = "(" + texInfo.Width + " x " + texInfo.Height + ")";

                    alphaBitmap = Imaging.CreateBitmapSourceFromHBitmap(texInfo.BMP.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                    var removeAlphaBitmap = SetAlpha(texInfo.BMP, 255);

                    noAlphaBitmap = Imaging.CreateBitmapSourceFromHBitmap(removeAlphaBitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                }

                zoomBox.CenterContent();
                zoomBox.FitToBounds();

                redCheckBox.IsChecked = true;
                greenCheckBox.IsChecked = true;
                blueCheckBox.IsChecked = true;
                alphaCheckBox.IsChecked = false;

                try
                {
                    CC.Channel = new System.Windows.Media.Media3D.Point4D(1.0f, 1.0f, 1.0f, 0.0f);

                    texImage.Effect = CC;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                }

                texImage.Source = noAlphaBitmap;
                texImage.UpdateLayout();
            }
            e.Handled = true;
        }

        public Bitmap SetAlpha(Bitmap bmp, byte alpha)
        {
            var data = bmp.LockBits(
                new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadWrite,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            var line = data.Scan0;
            var eof = line + data.Height * data.Stride;
            while (line != eof)
            {
                var pixelAlpha = line + 3;
                var eol = pixelAlpha + data.Width * 4;
                while (pixelAlpha != eol)
                {
                    Marshal.WriteByte(pixelAlpha, alpha);
                    pixelAlpha += 4;
                }
                line += data.Stride;
            }
            bmp.UnlockBits(data);

            return bmp;
        }

        private void SavePNGButton_Click(object sender, RoutedEventArgs e)
        {
            savePNGButton.DataContext = new SaveViewModel(selectedItem.itemName, selectedParent, ((ComboBoxInfo)mapComboBox.SelectedItem).Name, texImage.Source as BitmapSource, null, fullPath);
        }

        private void SaveDDSButton_Click(object sender, RoutedEventArgs e)
        {
            saveDDSButton.DataContext = new SaveViewModel(selectedItem.itemName, selectedParent, ((ComboBoxInfo)mapComboBox.SelectedItem).Name, null, texInfo, fullPath);
            importButton.IsEnabled = true;
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            importButton.DataContext = new ImportViewModel(texInfo, selectedParent, selectedItem.itemName, ((ComboBoxInfo)mapComboBox.SelectedItem).Name, fullPath);

        }

        private void RevertButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ExportObjButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Import3DButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Revert3DButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Menu_Search_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Menu_ModList_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Menu_Importer_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Menu_Directories_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Menu_RevertAll_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Menu_ReapplyAll_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Menu_DX9_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DX_Ver = "DX9";
            Properties.Settings.Default.Save();

            Menu_DX9.IsEnabled = false;
            Menu_DX11.IsEnabled = true;
            Menu_DX11.IsChecked = false;

            var itemSelected = (ItemViewModel)textureTreeView.SelectedItem;
            ((ItemViewModel)textureTreeView.SelectedItem).IsSelected = false;
            itemSelected.IsSelected = true;
        }

        private void Menu_DX11_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DX_Ver = "DX11";
            Properties.Settings.Default.Save();

            Menu_DX11.IsEnabled = false;
            Menu_DX9.IsEnabled = true;
            Menu_DX9.IsChecked = false;

            var itemSelected = (ItemViewModel)textureTreeView.SelectedItem;
            ((ItemViewModel)textureTreeView.SelectedItem).IsSelected = false;
            itemSelected.IsSelected = true;
        }

        private void Menu_ProblemCheck_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Menu_BugReport_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Menu_About_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Menu_English_Click(object sender, RoutedEventArgs e)
        {
            if(MessageBox.Show("Changing language requires the application to restart. \nRestart now?", "Language Change", MessageBoxButton.OKCancel, MessageBoxImage.Information) == MessageBoxResult.OK)
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

        private void RaceComboBox3D_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;

            float r = redCheckBox.IsChecked == true ? 1.0f : 0.0f;
            float g = greenCheckBox.IsChecked == true ? 1.0f : 0.0f;
            float b = blueCheckBox.IsChecked == true ? 1.0f : 0.0f;
            float a = alphaCheckBox.IsChecked == true ? 1.0f : 0.0f;

            BitmapSource img;

            if (alphaCheckBox.IsChecked == true)
            {
                img = alphaBitmap;

            }
            else
            {
                img = noAlphaBitmap;
            }

            ColorChannels CC = new ColorChannels();

            CC.Channel = new System.Windows.Media.Media3D.Point4D(r, g, b, a);

            texImage.Effect = CC;

            texImage.Source = img;

            texImage.UpdateLayout();
        }

        private void MapComboBox_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

        }

        class CategoryComparer : IEqualityComparer<Category>
        {
            public bool Equals(Category x, Category y)
            {
                return StringComparer.InvariantCultureIgnoreCase.Equals(x.CategoryName, y.CategoryName);
            }

            public int GetHashCode(Category obj)
            {
                return StringComparer.InvariantCultureIgnoreCase.GetHashCode(obj.CategoryName);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            if (!textBox.Text.Equals(Strings.SearchBox))
            {
                if (textBox.Text.Length > 2)
                {
                    IEnumerable<Items> matchingItems = from item in itemList where item.itemName.ToLower().Contains(textBox.Text.ToLower()) orderby item.itemName select item;
                    HashSet<Category> categories = new HashSet<Category>(new CategoryComparer());

                    foreach (Items item in matchingItems)
                    {
                        var key = Info.IDSlot.FirstOrDefault(x => x.Value == item.itemSlot).Key;
                        categories.Add(new Category(key));
                    }

                    TreeViewModel vm = new TreeViewModel(categories.ToArray(), matchingItems.ToList());
                    textureTreeView.DataContext = vm;

                    foreach(CategoryViewModel tvi in textureTreeView.Items)
                    {
                        tvi.IsExpanded = true;
                    }
                }
                else
                {
                    TreeViewModel viewModel = new TreeViewModel(categoryList.ToArray(), itemList);
                    textureTreeView.DataContext = viewModel;
                }
            }
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (searchTextBox.Text.Equals(Strings.SearchBox))
            {
                searchTextBox.Clear();
                searchTextBox.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (searchTextBox.Text.Length == 0)
            {
                searchTextBox.Text = Strings.SearchBox;
                searchTextBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

    }
}
