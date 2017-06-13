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
using FFXIV_TexTools2.Views;
using FolderSelect;
using HelixToolkit.Wpf.SharpDX;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
        BitmapSource newDiffuse = null;
        Bitmap noAlphaNormal = null;
        ColorChannels CC = new ColorChannels();
        List<Category> categoryList;
        TexInfo texInfo;
        ModListModel goTo = null;
        List<Mesh> meshList;
        string modelName;
        bool loaded3D = false;

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
                if (!b.Content.Equals("Export OBJ + Materials"))
                {
                    b.IsEnabled = false;
                }
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
                DXVerStatus.Content = DXVerStatus.Content + "11";
            }
            else if (Properties.Settings.Default.DX_Ver.Equals("DX9"))
            {
                Menu_DX9.IsChecked = true;
                Menu_DX9.IsEnabled = false;
                DXVerStatus.Content = DXVerStatus.Content + "9";
            }

            importButton.IsEnabled = false;
            revertButton.IsEnabled = false;

            raceComboBox.SelectionChanged += new SelectionChangedEventHandler(RaceComboBox_SelectionChanged);
            mapComboBox.SelectionChanged += new SelectionChangedEventHandler(MapComboBox_SelectionChanged);
            partComboBox.SelectionChanged += new SelectionChangedEventHandler(PartComboBox_SelectionChanged);
            typeComboBox.SelectionChanged += new SelectionChangedEventHandler(TypeComboBox_SelectionChanged);
            raceComboBox3D.SelectionChanged += new SelectionChangedEventHandler(RaceComboBox3D_SelectionChanged);
            bodyComboBox3D.SelectionChanged += new SelectionChangedEventHandler(BodyComboBox3D_SelectionChanged);

            searchTextBox.Text = Strings.SearchBox;
            searchTextBox.Foreground = System.Windows.Media.Brushes.Gray;

            FillTree();
        }

        private void FillTree()
        {
            BackgroundWorker fillTreeView = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
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
            tabControl.SelectedIndex = 0;
            loaded3D = false;
            if(textureTreeView.SelectedItem is ItemViewModel)
            {
                savePNGButton.IsEnabled = true;
                saveDDSButton.IsEnabled = true;
                mapComboBox.IsEnabled = true;

                var item = textureTreeView.SelectedItem as ItemViewModel;
                var parent = item.Parent as CategoryViewModel;

                selectedParent = parent.CategoryName;
                selectedItem = item.Item;

                if(selectedItem.itemName.Equals(Strings.Face_Paint) || selectedItem.itemName.Equals(Strings.Equipment_Decals) || selectedParent.Equals(Strings.Pets))
                {
                    _3DTab.IsEnabled = false;
                }
                else
                {
                    _3DTab.IsEnabled = true;
                }

                BackgroundWorker selectedWorker = new BackgroundWorker()
                {
                    WorkerReportsProgress = true
                };
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

                if(goTo != null)
                {
                    foreach (var race in raceComboBox.Items)
                    {
                        if (((ComboBoxInfo)race).Name.Equals(goTo.Race))
                        {
                            raceComboBox.SelectedItem = race;
                            break;
                        }
                    }
                }
                else
                {
                    raceComboBox.SelectedIndex = 0;
                }


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
                BackgroundWorker raceWorker = new BackgroundWorker()
                {
                    WorkerReportsProgress = true
                };
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

                if(goTo != null)
                {
                    foreach (var part in partComboBox.Items)
                    {
                        if (((ComboBoxInfo)part).Name.Equals(goTo.Part))
                        {
                            partComboBox.SelectedItem = part;
                            break;
                        }
                    }
                }
                else
                {
                    partComboBox.SelectedIndex = 0;
                }

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
                BackgroundWorker partsWorker = new BackgroundWorker()
                {
                    WorkerReportsProgress = true
                };
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

                    if(goTo != null)
                    {
                        foreach (var type in typeComboBox.Items)
                        {
                            if (((ComboBoxInfo)type).Name.Equals(goTo.Type))
                            {
                                typeComboBox.SelectedItem = type;
                                break;
                            }
                        }
                    }
                    else
                    {
                    typeComboBox.SelectedIndex = 0;
                    }
                    typeComboBox.IsEnabled = true;
                }
                else if (selectedParent.Equals(Strings.Mounts))
                {
                    if ((mountsDict[selectedItem.itemName]).itemID.Equals("1") || (mountsDict[selectedItem.itemName]).itemID.Equals("2") ||
                        (mountsDict[selectedItem.itemName]).itemID.Equals("1011") || (mountsDict[selectedItem.itemName]).itemID.Equals("1022"))
                    {
                        typeComboBox.DataContext = cView;
                        if (goTo != null)
                        {
                            foreach (var type in typeComboBox.Items)
                            {
                                if (((ComboBoxInfo)type).Name.Equals(goTo.Type))
                                {
                                    typeComboBox.SelectedItem = type;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            typeComboBox.SelectedIndex = 0;
                        }
                        typeComboBox.IsEnabled = true;
                    }
                    else
                    {
                        typeComboBox.DataContext = null;
                        typeComboBox.IsEnabled = false;
                        mapComboBox.DataContext = cView;
                        if (goTo != null)
                        {
                            foreach (var map in mapComboBox.Items)
                            {
                                if (((ComboBoxInfo)map).Name.Equals(goTo.Map))
                                {
                                    mapComboBox.SelectedItem = map;
                                    goTo = null;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            mapComboBox.SelectedIndex = 0;
                        }
                    }
                }
                else
                {
                    typeComboBox.DataContext = null;
                    typeComboBox.IsEnabled = false;
                    mapComboBox.DataContext = cView;
                    if (goTo != null)
                    {
                        foreach (var map in mapComboBox.Items)
                        {
                            if (((ComboBoxInfo)map).Name.Equals(goTo.Map))
                            {
                                mapComboBox.SelectedItem = map;
                                goTo = null;
                                break;
                            }
                        }
                    }
                    else
                    {
                        mapComboBox.SelectedIndex = 0;
                    }
                }
            }
        }

        private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboInfo = typeComboBox.SelectedItem as ComboBoxInfo;
            string type;

            if (e.AddedItems.Count > 0)
            {
                if (selectedParent.Equals(Strings.Mounts))
                {
                    if ((mountsDict[selectedItem.itemName]).itemID.Equals("1") || (mountsDict[selectedItem.itemName]).itemID.Equals("2") ||
                        (mountsDict[selectedItem.itemName]).itemID.Equals("1011") || (mountsDict[selectedItem.itemName]).itemID.Equals("1022"))
                    {
                        type = Info.slotAbr[comboInfo.Name];
                    }
                    else
                    {
                        type = comboInfo.Name;
                    }
                }
                else
                {
                    type = comboInfo.Name;
                }


                var info = MTRL.GetTexFromType(selectedItem, raceComboBox.SelectedItem as ComboBoxInfo, ((ComboBoxInfo)partComboBox.SelectedItem).Name, type, imcVersion, selectedParent);
                mtrlInfo = info.Item1;

                ComboView cView = new ComboView(info.Item2);
                mapComboBox.DataContext = cView;

                if(goTo != null)
                {
                    foreach (var map in mapComboBox.Items)
                    {
                        if (((ComboBoxInfo)map).Name.Equals(goTo.Map))
                        {
                            mapComboBox.SelectedItem = map;
                            goTo = null;
                            break;
                        }
                    }
                }
                else
                {
                    mapComboBox.SelectedIndex = 0;
                }
            }

            e.Handled = true;
        }

        private void MapComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var comboInfo = mapComboBox.SelectedItem as ComboBoxInfo;
                Paragraph paragraph = new Paragraph()
                {
                    TextAlignment = TextAlignment.Center
                };
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

                        fullPath = String.Format(mtrlInfo.MaskPath, part);
                        offset = MTRL.GetDecalOffset(selectedItem.itemName, ((ComboBoxInfo)partComboBox.SelectedItem).Name);
                        paragraph.Inlines.Add(new Run(fullPath));
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
                    fullPath = mtrlInfo.MTRLPath;
                    paragraph.Inlines.Add(new Run(fullPath));
                }

                string dxPath = Path.GetFileNameWithoutExtension(fullPath);

                if (File.Exists(Properties.Settings.Default.Save_Directory + "/" + selectedParent + "/" + selectedItem.itemName + "/" + dxPath  + ".dds"))
                {
                    importButton.IsEnabled = true;
                }
                else
                {
                    importButton.IsEnabled = false;
                }

                string line;
                JsonEntry modEntry = null;
                bool inModList = false;
                try
                {
                    using (StreamReader sr = new StreamReader(Info.modListDir))
                    {
                        while ((line = sr.ReadLine()) != null)
                        {
                            modEntry = JsonConvert.DeserializeObject<JsonEntry>(line);
                            if (modEntry.fullPath.Equals(fullPath))
                            {
                                inModList = true;
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("[Main] Error Accessing .modlist File \n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                if (inModList)
                {
                    var currOffset = Helper.GetOffset(FFCRC.GetHash(modEntry.fullPath.Substring(0, modEntry.fullPath.LastIndexOf("/"))), FFCRC.GetHash(Path.GetFileName(modEntry.fullPath)));

                    if(currOffset == modEntry.modOffset)
                    {
                        revertButton.Content = "Disable";
                    }
                    else if(currOffset == modEntry.originalOffset)
                    {
                        revertButton.Content = "Enable";
                    }
                    else
                    {
                        revertButton.Content = "Error";
                    }

                    revertButton.IsEnabled = true;
                }
                else
                {
                    revertButton.IsEnabled = false;
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
                    Debug.WriteLine(ex.StackTrace);
                }

                texImage.Source = noAlphaBitmap;
                texImage.UpdateLayout();
            }
            e.Handled = true;
        }

        public Bitmap SetAlpha(Bitmap bmp, byte alpha)
        {
            var data = bmp.LockBits(
                new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
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
            savePNGButton.DataContext = new SaveViewModel(selectedItem.itemName, selectedParent, ((ComboBoxInfo)mapComboBox.SelectedItem).Name, texImage.Source as BitmapSource, fullPath);
        }

        private void SaveDDSButton_Click(object sender, RoutedEventArgs e)
        {
            if((mapComboBox.SelectedItem as ComboBoxInfo).Name.Equals(Strings.ColorSet))
            {
                saveDDSButton.DataContext = new SaveViewModel(selectedItem.itemName, selectedParent, ((ComboBoxInfo)mapComboBox.SelectedItem).Name, mtrlInfo);

            }
            else
            {
                saveDDSButton.DataContext = new SaveViewModel(selectedItem.itemName, selectedParent, ((ComboBoxInfo)mapComboBox.SelectedItem).Name, texInfo, fullPath);
            }
            
            importButton.IsEnabled = true;
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            if ((mapComboBox.SelectedItem as ComboBoxInfo).Name.Equals(Strings.ColorSet))
            {
                importButton.DataContext = new ImportViewModel(mtrlInfo, selectedParent, selectedItem.itemName);
                revertButton.IsEnabled = true;

            }
            else
            {
                importButton.DataContext = new ImportViewModel(texInfo, selectedParent, selectedItem.itemName, ((ComboBoxInfo)mapComboBox.SelectedItem).Name, fullPath);
                revertButton.IsEnabled = true;
            }

            //var itemSelected = (ItemViewModel)textureTreeView.SelectedItem;
            //((ItemViewModel)textureTreeView.SelectedItem).IsSelected = false;
            //itemSelected.IsSelected = true;
        }

        private void RevertButton_Click(object sender, RoutedEventArgs e)
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
                        if (modEntry.fullPath.Equals(fullPath))
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("[Main] Error Accessing .modlist File \n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (modEntry != null)
            {
                if (revertButton.Content.Equals("Enable"))
                {
                    Helper.UpdateIndex(modEntry.modOffset, fullPath);
                    Helper.UpdateIndex2(modEntry.modOffset, fullPath);
                    revertButton.Content = "Disable";
                }
                else if (revertButton.Content.Equals("Disable"))
                {
                    Helper.UpdateIndex(modEntry.originalOffset, fullPath);
                    Helper.UpdateIndex2(modEntry.originalOffset, fullPath);
                    revertButton.Content = "Enable";
                }
                else
                {
                    //error
                }
            }

            //var itemSelected = (ItemViewModel)textureTreeView.SelectedItem;
            //((ItemViewModel)textureTreeView.SelectedItem).IsSelected = false;
            //itemSelected.IsSelected = true;

        }

        private void ExportObjButton_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(Properties.Settings.Default.Save_Directory + "/" + selectedParent + "/" + selectedItem.itemName + "/3D");
            File.WriteAllLines(Properties.Settings.Default.Save_Directory + "/" + selectedParent + "/" + selectedItem.itemName + "/3D/" + modelName + "_mesh_" + bodyComboBox3D.SelectedItem + ".obj", meshList[(int)bodyComboBox3D.SelectedItem].Obj);

            var dir = Properties.Settings.Default.Save_Directory + "/" + selectedParent + "/" + selectedItem.itemName + "/3D/" + modelName + "_mesh_" + bodyComboBox3D.SelectedItem + "_Diffuse.png";

            using (var fileStream = new FileStream(dir, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(newDiffuse));
                encoder.Save(fileStream);
            }

            dir = Properties.Settings.Default.Save_Directory + "/" + selectedParent + "/" + selectedItem.itemName + "/3D/" + modelName + "_mesh_" + bodyComboBox3D.SelectedItem + "_Normal.png";

            noAlphaNormal.Save(dir, ImageFormat.Png);
        }

        private void Import3DButton_Click(object sender, RoutedEventArgs e)
        {
            //Not yet implemented
        }

        private void Revert3DButton_Click(object sender, RoutedEventArgs e)
        {
            //Not yet implemented
        }

        private void Menu_ModList_Click(object sender, RoutedEventArgs e)
        {
            ModList ml = new ModList()
            {
                Owner = this
            };
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
                        Helper.UpdateIndex(modEntry.modOffset, modEntry.fullPath);
                        Helper.UpdateIndex2(modEntry.modOffset, modEntry.fullPath);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("[Main] Error Accessing .modlist File \n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void Menu_DX9_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DX_Ver = "DX9";
            Properties.Settings.Default.Save();

            Menu_DX9.IsEnabled = false;
            Menu_DX11.IsEnabled = true;
            Menu_DX11.IsChecked = false;

            DXVerStatus.Content = DXVerStatus.Content + "9";

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

            DXVerStatus.Content = DXVerStatus.Content + "11";

            var itemSelected = (ItemViewModel)textureTreeView.SelectedItem;
            ((ItemViewModel)textureTreeView.SelectedItem).IsSelected = false;
            itemSelected.IsSelected = true;
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
            a.Show();
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

        /// <summary>
        /// Gets the ModListModel from ModList window and selects the item
        /// **Currently not in use as it does not function correctly with Virtualization**
        /// </summary>
        /// <param name="mlm"></param>
        public void GoToItem(ModListModel mlm)
        {
            goTo = mlm;

            var entry = mlm.Entry;

            var vm = textureTreeView.DataContext as TreeViewModel;

            int index = 0;
            foreach(string key in Info.IDSlot.Keys)
            {
                if (key.Equals(entry.category))
                {
                    break;
                }
                index++;
            }

            var category = vm.Category[index];
            category.IsExpanded = true;

            ItemViewModel c = null;

            foreach(var child in category.Children)
            {
                c = child as ItemViewModel;
                if (c.ItemName.Equals(entry.name))
                {
                    c.IsSelected = true;
                    break;
                }
            }

            TreeViewItem tvi = textureTreeView.ItemContainerGenerator.ContainerFromItem(c) as TreeViewItem;
        }

        private void RaceComboBox3D_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bodyComboBox3D.Items.Clear();
            if (e.AddedItems.Count > 0)
            {
                var comboInfo = (ComboBoxInfo)raceComboBox3D.SelectedItem;

                MDL mdl = new MDL(selectedItem, Info.raceID[comboInfo.Name], selectedParent);
                meshList = mdl.GetMeshList();
                var numMesh = mdl.GetNumMeshes() / 3;
                modelName = mdl.GetModelName();

                for(int i = 0; i < numMesh; i++)
                {
                    bodyComboBox3D.Items.Add(i);
                }

                bodyComboBox3D.SelectedIndex = 0;

            }
            e.Handled = true;
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

            ColorChannels CC = new ColorChannels()
            {
                Channel = new System.Windows.Media.Media3D.Point4D(r, g, b, a)
            };
            texImage.Effect = CC;

            texImage.Source = img;

            texImage.UpdateLayout();
        }

        private void MapComboBox_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var control = sender as TabControl;
            var selected = control.SelectedItem as TabItem;

            if (selected.Header.Equals("3D Model") && !loaded3D)
            {
                string type = Helper.GetItemType(selectedParent);
                List<ComboBoxInfo> cbiInfo = new List<ComboBoxInfo>();
                Dictionary<int, string> racesDict = new Dictionary<int, string>();
                string MDLFolder, MDLFile = "";

                List<string> races = new List<string>();
                if(type.Equals("weapon") || type.Equals("food"))
                {
                    MDLFolder = "";
                    cbiInfo.Add(new ComboBoxInfo(Strings.All, Strings.All));
                }
                else if (type.Equals("accessory"))
                {
                    MDLFolder = "chara/accessory/a" + selectedItem.itemID + "/model";
                    MDLFile = "c{0}a" + selectedItem.itemID + "_" + Info.slotAbr[selectedParent] + ".mdl";
                }
                else if(type.Equals("character"))
                {
                    if (selectedItem.itemName.Equals(Strings.Body))
                    {
                        MDLFolder = "chara/human/c{0}/obj/body/b0001/model";
                    }
                    else if (selectedItem.itemName.Equals(Strings.Face))
                    {
                        MDLFolder = "chara/human/c{0}/obj/face/f0001/model";
                    }
                    else if (selectedItem.itemName.Equals(Strings.Hair))
                    {
                        MDLFolder = "chara/human/c{0}/obj/hair/h0001/model";
                    }
                    else if (selectedItem.itemName.Equals(Strings.Tail))
                    {
                        MDLFolder = "chara/human/c{0}/obj/tail/t0001/model";
                    }
                    else
                    {
                        MDLFolder = null;
                    }
                }
                else if (type.Equals("monster"))
                {
                    MDLFolder = "";
                    cbiInfo.Add(new ComboBoxInfo(Strings.All, Strings.All));
                }
                else
                {
                    MDLFolder = "chara/equipment/e" + selectedItem.itemID + "/model";
                    MDLFile = "c{0}e" + selectedItem.itemID + "_" + Info.slotAbr[selectedParent] + ".mdl";
                }

                var fileOffsetDict = Helper.GetAllFilesInFolder(FFCRC.GetHash(MDLFolder));
                int fileHash;

                if (!type.Equals("weapon") && !type.Equals("monster"))
                {
                    foreach (string raceID in Info.IDRace.Keys)
                    {
                        if (type.Equals("character"))
                        {
                            string fol = String.Format(MDLFolder, raceID);
                            racesDict.Add(FFCRC.GetHash(fol), raceID);
                        }
                        else
                        {
                            string mdl = String.Format(MDLFile, raceID);
                            fileHash = FFCRC.GetHash(mdl);

                            if (fileOffsetDict.Keys.Contains(fileHash))
                            {

                                cbiInfo.Add(new ComboBoxInfo(Info.IDRace[raceID], raceID));
                            }
                        }
                    }
                    if (type.Equals("character"))
                    {
                        cbiInfo = Helper.FolderExistsListRace(racesDict).ToList();
                    }
                }

                ComboView cView = new ComboView(cbiInfo);
                raceComboBox3D.DataContext = cView;
                raceComboBox3D.SelectedIndex = 0;

                loaded3D = true;
            }
            e.Handled = true;
        }

        private void BodyComboBox3D_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (e.AddedItems.Count > 0)
            {
                var comboInfo = (int)bodyComboBox3D.SelectedItem;

                BitmapSource colorBMP = null;

                BitmapSource displace = null;
                MTRLInfo mInfo = MTRL3D();




                if (mInfo.ColorData != null)
                {
                    var colorBmp = TEX.TextureToBitmap(mInfo.ColorData, 9312, 4, 16);
                    colorBMP = Imaging.CreateBitmapSourceFromHBitmap(colorBmp.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                }


                TexInfo specularTI = null;
                TexInfo maskTI = null;
                TexInfo normalTI = null;

                if (mInfo.NormalOffset != 0)
                {
                    normalTI = TEX.GetTex(mInfo.NormalOffset);
                }

                if (mInfo.SpecularOffset != 0)
                {
                    specularTI = TEX.GetTex(mInfo.SpecularOffset);
                }

                if (mInfo.MaskOffset != 0)
                {
                    maskTI = TEX.GetTex(mInfo.MaskOffset);
                    displace = TexHelper.MakeDisplaceMap(maskTI);
                }

                if (mInfo.DiffuseOffset != 0)
                {
                    var diffTI = TEX.GetTex(mInfo.DiffuseOffset);

                    newDiffuse = Imaging.CreateBitmapSourceFromHBitmap(diffTI.BMP.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                }
                else
                {
                    newDiffuse = TexHelper.MakeDiffuseMap(normalTI.BMP, colorBMP, maskTI);
                }

                MeshGeometry3D mg3d = new MeshGeometry3D();

                Element3DCollection modelGeometry = new Element3DCollection();

                var gm = new MeshGeometryModel3D();

                mg3d.Normals = meshList[comboInfo].NormalList;
                mg3d.Positions = meshList[comboInfo].VertexList;
                mg3d.TextureCoordinates = meshList[comboInfo].CoordList;


                gm.Geometry = mg3d;

                noAlphaNormal = SetNormal(normalTI.BMP, 255);

                var MDLViewModel = new MDLViewModel(meshList[comboInfo], newDiffuse, Imaging.CreateBitmapSourceFromHBitmap(noAlphaNormal.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()), gm, displace);

                viewport3DX.DataContext = MDLViewModel;
            }
            e.Handled = true;
        }

        public MTRLInfo MTRL3D()
        {

            MTRLInfo mInfo;

            if (typeComboBox.HasItems)
            {
                string type;

                if (selectedParent.Equals(Strings.Mounts))
                {
                    if ((mountsDict[selectedItem.itemName]).itemID.Equals("1") || (mountsDict[selectedItem.itemName]).itemID.Equals("2") ||
                        (mountsDict[selectedItem.itemName]).itemID.Equals("1011") || (mountsDict[selectedItem.itemName]).itemID.Equals("1022"))
                    {
                        type = Info.slotAbr[((ComboBoxInfo)typeComboBox.SelectedItem).Name];
                    }
                    else
                    {
                        type = ((ComboBoxInfo)typeComboBox.SelectedItem).Name;
                    }
                }
                else
                {
                    type = ((ComboBoxInfo)typeComboBox.SelectedItem).Name;
                }

                var info = MTRL.GetTexFromType(selectedItem, raceComboBox3D.SelectedItem as ComboBoxInfo, ((ComboBoxInfo)partComboBox.SelectedItem).Name, type, imcVersion, selectedParent);
                mInfo = info.Item1;

            }
            else
            {
                var info = MTRL.GetMTRLOffset(((ComboBoxInfo)raceComboBox3D.SelectedItem), selectedParent, selectedItem, ((ComboBoxInfo)partComboBox.SelectedItem).Name, imcVersion, "");

                if(info != null)
                {
                    mInfo = info.Item1;
                }
                else
                {
                    var combo = new ComboBoxInfo("Female", "0201");
                    info = MTRL.GetMTRLOffset(combo, selectedParent, selectedItem, ((ComboBoxInfo)partComboBox.SelectedItem).Name, imcVersion, "");

                    if (info != null)
                    {
                        mInfo = info.Item1;
                    }
                    else
                    {
                        combo = new ComboBoxInfo("Male", "0101");
                        info = MTRL.GetMTRLOffset(combo, selectedParent, selectedItem, ((ComboBoxInfo)partComboBox.SelectedItem).Name, imcVersion, "");
                        mInfo = info.Item1;
                    }
                }
            }

            return mInfo;
        }

        public Bitmap SetNormal(Bitmap bmp, byte alpha)
        {
            var data = bmp.LockBits(
                new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadWrite,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            var line = data.Scan0;
            var eof = line + data.Height * data.Stride;
            while (line != eof)
            {
                var pixelAlpha = line + 3;
                var pixelBlue = line + 2;
                var eol = pixelAlpha + data.Width * 4;
                while (pixelAlpha != eol)
                {
                    Marshal.WriteByte(pixelAlpha, 255);
                    Marshal.WriteByte(pixelBlue, 255);
                    pixelAlpha += 4;
                }
                line += data.Stride;
            }
            bmp.UnlockBits(data);

            return bmp;
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
