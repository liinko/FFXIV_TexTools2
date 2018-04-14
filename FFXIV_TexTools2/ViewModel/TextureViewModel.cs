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
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using PixelFormat = System.Drawing.Imaging.PixelFormat;


namespace FFXIV_TexTools2.ViewModel
{
    public class TextureViewModel : INotifyPropertyChanged
    {
        ItemData selectedItem;
        BitmapSource alphaBitmap, noAlphaBitmap, imageSource;
        TEXData texData;
        MTRLData mtrlData;
        ColorChannels imageEffect;

        string activeToggle = "Enable/Disable";
        string translucencyToggle = "Translucency OFF";
        string selectedCategory, imcVersion, fullPath, textureType, textureDimensions, fullPathString, VFXVersion;

        int raceIndex, mapIndex, typeIndex, partIndex, currMap;

        bool redChecked = true, greenChecked = true, blueChecked = true;
        bool alphaChecked, raceEnabled, mapEnabled, typeEnabled, partEnabled, importEnabled, activeEnabled, saveEnabled, channelsEnabled, openEnabled, translucencyEnabled;


        private ObservableCollection<ComboBoxInfo> raceComboInfo = new ObservableCollection<ComboBoxInfo>();
        private ObservableCollection<ComboBoxInfo> mapComboInfo = new ObservableCollection<ComboBoxInfo>();
        private ObservableCollection<ComboBoxInfo> partComboInfo = new ObservableCollection<ComboBoxInfo>();
        private ObservableCollection<ComboBoxInfo> typeComboInfo = new ObservableCollection<ComboBoxInfo>();

        private ComboBoxInfo selectedRace;
        private ComboBoxInfo selectedPart;
        private ComboBoxInfo selectedType;
        private ComboBoxInfo selectedMap;

        public int RaceIndex { get { return raceIndex; } set { raceIndex = value; NotifyPropertyChanged("RaceIndex"); } }
        public int MapIndex { get { return mapIndex; } set { mapIndex = value; NotifyPropertyChanged("MapIndex"); } }
        public int TypeIndex { get { return typeIndex; } set { typeIndex = value; NotifyPropertyChanged("TypeIndex"); } }
        public int PartIndex { get { return partIndex; } set { partIndex = value; NotifyPropertyChanged("PartIndex"); } }

        public string TextureType { get { return textureType; } set { textureType = value; NotifyPropertyChanged("TextureType"); } }
        public string TextureDimensions { get { return textureDimensions; } set { textureDimensions = value; NotifyPropertyChanged("TextureDimensions"); } }
        public string FullPathString { get { return fullPathString; } set { fullPathString = value; NotifyPropertyChanged("FullPathString"); } }
        public ColorChannels ImageEffect { get { return imageEffect; } set { imageEffect = value; NotifyPropertyChanged("ImageEffect"); } }
        public BitmapSource ImageSource { get { return imageSource; } set { imageSource = value; NotifyPropertyChanged("ImageSource"); } }

        public bool ChannelsEnabled { get { return channelsEnabled; } set { channelsEnabled = value; NotifyPropertyChanged("ChannelsEnabled"); } }
        public bool RedChecked { get { return redChecked; } set { redChecked = value; NotifyPropertyChanged("RedChecked"); SetColorChannelFilter(imageEffect); } }
        public bool GreenChecked { get { return greenChecked; } set { greenChecked = value; NotifyPropertyChanged("GreenChecked"); SetColorChannelFilter(imageEffect); } }
        public bool BlueChecked { get { return blueChecked; } set { blueChecked = value; NotifyPropertyChanged("BlueChecked"); SetColorChannelFilter(imageEffect); } }
        public bool AlphaChecked { get { return alphaChecked; } set { alphaChecked = value; NotifyPropertyChanged("AlphaChecked"); SetColorChannelFilter(imageEffect); } }

        public bool RaceEnabled { get { return raceEnabled; } set { raceEnabled = value;NotifyPropertyChanged("RaceEnabled"); } }
        public bool MapEnabled { get { return mapEnabled; } set { mapEnabled = value; NotifyPropertyChanged("MapEnabled"); } }
        public bool TypeEnabled { get { return typeEnabled; } set { typeEnabled = value; NotifyPropertyChanged("TypeEnabled"); } }
        public bool PartEnabled { get { return partEnabled; } set { partEnabled = value; NotifyPropertyChanged("PartEnabled"); } }
        public bool OpenEnabled { get { return openEnabled; } set { openEnabled = value; NotifyPropertyChanged("OpenEnabled"); } }
        public bool TranslucencyEnabled { get { return translucencyEnabled; } set { translucencyEnabled = value; NotifyPropertyChanged("TranslucencyEnabled"); } }

        public string ActiveToggle { get { return activeToggle; } set { activeToggle = value; NotifyPropertyChanged("ActiveToggle"); } }
        public string TranslucencyToggle { get { return translucencyToggle; } set { translucencyToggle = value; NotifyPropertyChanged("TranslucencyToggle"); } }
        public bool ImportEnabled { get { return importEnabled; } set { importEnabled = value; NotifyPropertyChanged("ImportEnabled"); } }
        public bool ActiveEnabled { get { return activeEnabled; } set { activeEnabled = value; NotifyPropertyChanged("ActiveEnabled"); } }
        public bool SaveEnabled { get { return saveEnabled; } set { saveEnabled = value; NotifyPropertyChanged("SaveEnabled"); } }


        /// <summary>
        /// View Model for TextureView
        /// </summary>
        /// <param name="item">The currently selected item</param>
        /// <param name="category">The category of the selected item</param>
        public void UpdateTexture(ItemData item, string category)
        {
            selectedItem = item;
            selectedCategory = category;

            if (!category.Equals("UI"))
            {
                var imcData = IMC.GetVersion(selectedCategory, selectedItem, false, false);

                imcVersion = imcData.Item1;
                VFXVersion = imcData.Item2;

                RaceComboBox = MTRL.GetMTRLRaces(selectedItem, selectedCategory, imcVersion);
            }
            else
            {
                RaceComboBox = new ObservableCollection<ComboBoxInfo>() { new ComboBoxInfo() { Name = "-", ID = "-", IsNum = false } };
            }


            if (RaceComboBox.Count > 1)
            {
                RaceEnabled = true;
            }
            else
            {
                RaceEnabled = false;
            }

            RaceIndex = 0;
        }

        public TextureViewModel()
        {

        }

        /// <summary>
        /// View Model for TextureView from ID
        /// </summary>
        /// <param name="item">The currently selected item</param>
        /// <param name="category">The category of the selected item</param>
        public void UpdateTextureFromID(ItemData item, string raceID, string category, string part, string variant)
        {
            selectedItem = item;
            selectedCategory = category;
            imcVersion = variant;
            VFXVersion = "0000";

            RaceComboBox = MTRL.GetMTRLRaces(selectedItem, selectedCategory, imcVersion);

            if (RaceComboBox.Count > 1)
            {
                RaceEnabled = true;
            }
            else
            {
                RaceEnabled = false;
            }

            RaceIndex = 0;

            //var info = MTRL.GetMTRLData(item, raceID, category, part, variant, "", "", "0000");
            //MapComboBox = info.Item2;
            //MapIndex = 0;
        }

        /// <summary>
        /// Command for the Export DDS Button
        /// </summary>
        public ICommand SaveDDSCommand
        {
            get{ return new RelayCommand(SaveDDS); }
        }

        /// <summary>
        /// Command for the Export PNG Button
        /// </summary>
        public ICommand SavePNGCommand
        {
            get{ return new RelayCommand(SavePNG); }
        }

        /// <summary>
        /// Command for the Import Button
        /// </summary>
        public ICommand ImportCommand
        {
            get{ return new RelayCommand(Import); }
        }

        /// <summary>
        /// Command for Enable/Disable button
        /// </summary>
        public ICommand ActiveCommand
        {
            get { return new RelayCommand(Revert); }
        }

        /// <summary>
        /// Command for Enable/Disable button
        /// </summary>
        public ICommand TranslucencyCommand
        {
            get { return new RelayCommand(Translucency); }
        }

        /// <summary>
        /// Command for Open Folder button
        /// </summary>
        public ICommand OpenFolderCommand
        {
            get { return new RelayCommand(OpenFolder); }
        }

        /// <summary>
        /// Runs the SaveDDS method from SaveTex 
        /// </summary>
        /// <param name="obj"></param>
        private void Translucency(object obj)
        {
            var newOffset = 0;
            if (TranslucencyToggle.Contains("OFF"))
            {
                newOffset = ChangeMTRL.TranslucencyToggle(mtrlData, selectedCategory, selectedItem.ItemName, true);
                TranslucencyToggle = "Translucency ON";
                mtrlData.ShaderNum = 0x1D;
            }
            else if (TranslucencyToggle.Contains("ON"))
            {
                newOffset = ChangeMTRL.TranslucencyToggle(mtrlData, selectedCategory, selectedItem.ItemName, false);
                TranslucencyToggle = "Translucency OFF";
                mtrlData.ShaderNum = 0x0D;
            }

            if (newOffset != 0)
            {
                mtrlData.MTRLOffset = newOffset;
            }
        }

        /// <summary>
        /// Runs the SaveDDS method from SaveTex 
        /// </summary>
        /// <param name="obj"></param>
        private void SaveDDS(object obj)
        {
            SaveTex.SaveDDS(selectedCategory, selectedItem.ItemName, fullPath, SelectedMap.Name, mtrlData, texData, selectedItem.ItemCategory);
            ImportEnabled = true;
            OpenEnabled = true;
        }

        /// <summary>
        /// Runs the SavePNG method from SaveTex
        /// </summary>
        /// <param name="obj"></param>
        public void SavePNG(object obj)
        {
            SaveTex.SaveImage(selectedCategory, selectedItem.ItemName, fullPath, ImageSource, texData, SelectedMap.Name, selectedItem.ItemCategory);
            OpenEnabled = true;
        }

        /// <summary>
        /// Runs the OpenFolder method 
        /// </summary>
        /// <param name="obj"></param>
        public void OpenFolder(object obj)
        {
            string savePath = Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItem.ItemName;

            if (selectedCategory.Equals("UI"))
            {
                savePath = Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItem.ItemCategory;
            }

            Process.Start(savePath);
        }

        public void SaveAllDDS()
        {
            foreach(var m in MapComboBox)
            {
                int offset = 0;

                if (m.Name.Equals(Strings.Normal))
                {
                    fullPath = mtrlData.NormalPath;
                    offset = mtrlData.NormalOffset;
                }
                else if (m.Name.Equals(Strings.Specular))
                {
                    fullPath = mtrlData.SpecularPath;
                    offset = mtrlData.SpecularOffset;
                }
                else if (m.Name.Equals(Strings.Diffuse))
                {
                    fullPath = mtrlData.DiffusePath;
                    offset = mtrlData.DiffuseOffset;
                }
                else if (m.Name.Equals(Strings.Mask) || m.Name.Equals(Strings.Skin))
                {
                    if (selectedItem.ItemName.Equals(Strings.Face_Paint) || selectedItem.ItemName.Equals(Strings.Equipment_Decals))
                    {
                        string part;
                        if (selectedItem.ItemName.Equals(Strings.Equipment_Decals))
                        {
                            if (!SelectedPart.Name.Contains("stigma"))
                            {
                                part = selectedPart.Name.PadLeft(3, '0');
                            }
                            else
                            {
                                part = SelectedPart.Name;
                            }
                        }
                        else
                        {
                            part = selectedPart.Name;
                        }

                        fullPath = String.Format(mtrlData.MaskPath, part);
                        offset = MTRL.GetDecalOffset(selectedItem.ItemName, selectedPart.Name);
                    }
                    else
                    {
                        fullPath = mtrlData.MaskPath;
                        offset = mtrlData.MaskOffset;
                    }
                }
                else if (m.Name.Equals(Strings.ColorSet))
                {
                    fullPath = mtrlData.MTRLPath;
                }
                else if (m.Name.Contains("Icon"))
                {
                    if (m.Name.Contains("HQ"))
                    {
                        fullPath = mtrlData.UIHQPath;
                        offset = mtrlData.UIHQOffset;
                    }
                    else
                    {
                        fullPath = mtrlData.UIPath;
                        offset = mtrlData.UIOffset;
                    }
                }
                else if (selectedItem.ItemCategory.Equals(Strings.Maps))
                {
                    if (selectedMap.Name.Contains("HighRes Map"))
                    {
                        fullPath = string.Format(mtrlData.UIPath, "_m");
                        offset = mtrlData.UIOffset = int.Parse(selectedMap.ID);
                    }
                    else if (selectedMap.Name.Contains("LowRes Map"))
                    {
                        fullPath = string.Format(mtrlData.UIPath, "_s");
                        offset = mtrlData.UIOffset = int.Parse(selectedMap.ID);
                    }
                    else if (selectedMap.Name.Contains("PoI"))
                    {
                        fullPath = string.Format(mtrlData.UIPath, "d");
                        offset = mtrlData.UIOffset = int.Parse(selectedMap.ID);
                    }
                    else if (selectedMap.Name.Contains("HighRes Mask"))
                    {
                        fullPath = string.Format(mtrlData.UIPath, "m_m");
                        offset = mtrlData.UIOffset = int.Parse(selectedMap.ID);
                    }
                    else if (selectedMap.Name.Contains("LowRes Mask"))
                    {
                        fullPath = string.Format(mtrlData.UIPath, "m_s");
                        offset = mtrlData.UIOffset = int.Parse(selectedMap.ID);
                    }
                }
                else if (selectedItem.ItemCategory.Equals("HUD"))
                {
                    fullPath = mtrlData.UIPath;
                    offset = mtrlData.UIOffset;
                }
                else
                {
                    fullPath = SelectedMap.ID;
                    var VFXFolder = fullPath.Substring(0, fullPath.LastIndexOf("/"));
                    var VFXFile = fullPath.Substring(fullPath.LastIndexOf("/") + 1);

                    offset = Helper.GetDataOffset(FFCRC.GetHash(VFXFolder), FFCRC.GetHash(VFXFile), Strings.ItemsDat);
                }

                if(offset != 0)
                {
                    if (m.ID.Contains("vfx"))
                    {
                        texData = TEX.GetVFX(offset, Strings.ItemsDat);
                    }
                    else
                    {
                        if (selectedCategory.Equals("UI"))
                        {
                            texData = TEX.GetTex(offset, Strings.UIDat);
                        }
                        else
                        {
                            texData = TEX.GetTex(offset, Strings.ItemsDat);
                        }
                    }
                }


                SaveTex.SaveDDS(selectedCategory, selectedItem.ItemName, fullPath, m.Name, mtrlData, texData, selectedItem.ItemCategory);
            }
        }


        /// <summary>
        /// Runs the Import method from ImportTex, then updates the displayed image to reflect the changes
        /// </summary>
        /// <param name="obj"></param>
        public void Import(object obj)
        {
            if (!Helper.IsIndexLocked(true))
            {
                if (SelectedMap.Name.Equals(Strings.ColorSet))
                {
                    var newData = ImportTex.ImportColor(mtrlData, selectedCategory, selectedItem.ItemName);
                    if (newData.Item1 != 0)
                    {
                        UpdateImage(newData.Item1, true);
                        ActiveToggle = "Disable";
                        ActiveEnabled = true;
                        mtrlData.ColorData = newData.Item2;
                    }
                }
                else if (SelectedMap.ID.Contains("vfx"))
                {
                    int newOffset = ImportTex.ImportVFX(texData, selectedCategory, selectedItem.ItemName, SelectedMap.ID);
                    if (newOffset != 0)
                    {
                        UpdateImage(newOffset, false);
                        ActiveToggle = "Disable";
                        ActiveEnabled = true;
                    }
                }
                else
                {
                    int newOffset = ImportTex.ImportTexture(texData, selectedCategory, selectedItem.ItemCategory, selectedItem.ItemName, fullPath);
                    if (newOffset != 0)
                    {
                        UpdateImage(newOffset, false);
                        ActiveToggle = "Disable";
                        ActiveEnabled = true;

                        if (selectedMap.Name.Equals(Strings.Normal))
                        {
                            mtrlData.NormalOffset = newOffset;
                        }
                        else if (selectedMap.Name.Equals(Strings.Diffuse))
                        {
                            mtrlData.DiffuseOffset = newOffset;
                        }
                        else if (selectedMap.Name.Equals(Strings.Specular))
                        {
                            mtrlData.SpecularOffset = newOffset;
                        }
                        else if (selectedMap.Name.Equals(Strings.Mask))
                        {
                            mtrlData.MaskOffset = newOffset;
                        }
                        else if (selectedCategory.Equals("UI"))
                        {
                            if (SelectedMap.Name.Contains("HQ"))
                            {
                                mtrlData.UIHQOffset = newOffset;
                            }
                            else
                            {
                                mtrlData.UIOffset = newOffset;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Enables or Disables the modification on the current item's texture map
        /// </summary>
        /// <param name="obj"></param>
        private void Revert(object obj)
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
                            if (modEntry.fullPath.Equals(fullPath))
                            {
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    FlexibleMessageBox.Show("Error Accessing .modlist File \n" + ex.Message, "TextureViewModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                if (modEntry != null)
                {
                    int offset = 0;
                    string toggle = "";

                    if (ActiveToggle.Equals("Enable"))
                    {
                        offset = modEntry.modOffset;
                        toggle = "Disable";
                    }
                    else if (ActiveToggle.Equals("Disable"))
                    {
                        offset = modEntry.originalOffset;
                        toggle = "Enable";
                    }

                    if(offset != 0)
                    {
                        ActiveToggle = toggle;
                        Helper.UpdateIndex(offset, fullPath, modEntry.datFile);
                        Helper.UpdateIndex2(offset, fullPath, modEntry.datFile);

                        if (selectedMap.Name.Equals(Strings.Normal))
                        {
                            mtrlData.NormalOffset = offset;
                        }
                        else if (selectedMap.Name.Equals(Strings.Diffuse))
                        {
                            mtrlData.DiffuseOffset = offset;
                        }
                        else if (selectedMap.Name.Equals(Strings.Specular))
                        {
                            mtrlData.SpecularOffset = offset;
                        }
                        else if (selectedMap.Name.Equals(Strings.Mask))
                        {
                            mtrlData.MaskOffset = offset;
                        }

                        if (SelectedMap.Name.Equals(Strings.ColorSet))
                        {
                            UpdateImage(offset, true);
                        }
                        else
                        {
                            UpdateImage(offset, false);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The ItemSource binding of the combobox which is to contain the list of available races for the selected item
        /// </summary>
        public ObservableCollection<ComboBoxInfo> RaceComboBox
        {
            get { return raceComboInfo; }
            set
            {
                if (raceComboInfo != null)
                {
                    raceComboInfo = value;
                    NotifyPropertyChanged("RaceComboBox");
                }
            }
        }

        /// <summary>
        /// The SelectedItem binding of the combobox containing available races
        /// </summary>
        public ComboBoxInfo SelectedRace {
            get { return selectedRace; }
            set
            {
                if(value != null)
                {
                    selectedRace = value;
                    NotifyPropertyChanged("SelectedRace");
                    RaceComboBoxChanged();
                }

            }
        }

        /// <summary>
        /// Gets the available parts for the selected item given a specified race
        /// </summary>
        private void RaceComboBoxChanged()
        {
            if(selectedCategory != "UI")
            {
                PartComboBox = MTRL.GetMTRLParts(selectedItem, SelectedRace.ID, imcVersion, selectedCategory);
            }
            else
            {
                PartComboBox = new ObservableCollection<ComboBoxInfo>() { new ComboBoxInfo() { Name = "-", ID = "-", IsNum = false } };

            }

            if (PartComboBox.Count > 1)
            {
                PartEnabled = true;
            }
            else
            {
                PartEnabled = false;
            }

            PartIndex = 0;
        }

        /// <summary>
        /// The ItemSource binding of the combobox which is to contain the list of available parts for the selected item
        /// </summary>
        public ObservableCollection<ComboBoxInfo> PartComboBox
        {
            get { return partComboInfo; }
            set { partComboInfo = value; NotifyPropertyChanged("PartComboBox"); }
        }

        /// <summary>
        /// The SelectedItem binding of the combobox containing available parts
        /// </summary>
        public ComboBoxInfo SelectedPart
        {
            get { return selectedPart; }
            set
            {
                if(value != null)
                {
                    selectedPart = value;
                    NotifyPropertyChanged("SelectedPart");
                    PartComboBoxChanged();
                }
            }
        }

        /// <summary>
        /// Gets the available types or maps for the currently selcted item given a specified race and part
        /// Hair, Face, and Mounts that are demihuman, contain types, all others do not.
        /// </summary>
        private void PartComboBoxChanged()
        {
            if (selectedCategory != "UI")
            {
                var body = "";
                var modelID = selectedItem.PrimaryModelID;
                var imc = imcVersion;
                var vfx = VFXVersion;
                var part = selectedPart.Name;

                if(selectedItem.PrimaryMTRLFolder != null)
                {
                    if (selectedItem.PrimaryMTRLFolder.Contains("weapon"))
                    {
                        var imcData = IMC.GetVersion(selectedCategory, selectedItem, false, false);

                        imc = imcData.Item1;
                        vfx = imcData.Item2;

                        modelID = selectedItem.PrimaryModelID;
                        body = selectedItem.PrimaryModelBody;
                        part = selectedPart.Name;
                    }
                }


                if (SelectedPart.Name.Equals("s"))
                {
                    var imcData = IMC.GetVersion(selectedCategory, selectedItem, true, false);

                    imc = imcData.Item1;
                    vfx = imcData.Item2;

                    modelID = selectedItem.SecondaryModelID;
                    body = selectedItem.SecondaryModelBody;
                    part = "a";

                    if(body == null)
                    {
                        body = "0001";
                    }
                }

                //if (selectedCategory.Equals(Strings.Pets))
                //{
                //    body = "a";
                //}
                var info = MTRL.GetMTRLData(selectedItem, selectedRace.ID, selectedCategory, part, imc, body, modelID, vfx);
                mtrlData = info.Item1;

                if (selectedItem.ItemName.Equals(Strings.Face) || selectedItem.ItemName.Equals(Strings.Hair))
                {
                    TypeComboBox = info.Item2;
                    TypeIndex = 0;
                }
                else if (selectedCategory.Equals(Strings.Mounts) || selectedCategory.Equals(Strings.Monster) || selectedCategory.Equals(Strings.DemiHuman))
                {
                    bool isDemiHuman = selectedItem.PrimaryMTRLFolder.Contains("demihuman");

                    if (isDemiHuman)
                    {
                        TypeComboBox = info.Item2;
                        TypeIndex = 0;
                    }
                    else
                    {
                        MapComboBox = info.Item2;
                        if(MapComboBox.Count < (currMap + 1))
                        {
                            MapIndex = 0;
                        }
                        else
                        {
                            MapIndex = currMap;
                        }
                        TypeComboBox.Clear();
                    }
                }
                else
                {
                    MapComboBox = info.Item2;
                    if (MapComboBox.Count < (currMap + 1))
                    {
                        MapIndex = 0;
                    }
                    else
                    {
                        MapIndex = currMap;
                    }
                    TypeComboBox.Clear();
                }
            }
            else
            {
                var info = MTRL.GetUIData(selectedItem);
                mtrlData = info.Item1;
                MapComboBox = info.Item2;
                if (MapComboBox.Count < (currMap + 1))
                {
                    MapIndex = 0;
                }
                else
                {
                    MapIndex = currMap;
                }
                TypeComboBox.Clear();
            }


            if (TypeComboBox.Count > 1)
            {
                TypeEnabled = true;
            }
            else
            {
                TypeEnabled = false;
            }

            if (MapComboBox.Count > 0)
            {
                MapEnabled = true;
            }
            else
            {
                MapEnabled = false;
            }
        }

        /// <summary>
        /// The ItemSource binding of the combobox which is to contain the list of available types for the selected item
        /// </summary>
        public ObservableCollection<ComboBoxInfo> TypeComboBox
        {
            get { return typeComboInfo; }
            set { typeComboInfo = value; NotifyPropertyChanged("TypeComboBox"); }
        }

        /// <summary>
        /// The SelectedItem binding of the combobox containing available types
        /// </summary>
        public ComboBoxInfo SelectedType
        {
            get { return selectedType; }
            set
            {
                if(value != null)
                {
                    selectedType = value;
                    NotifyPropertyChanged("SelectedType");
                    TypeComboBoxChanged();
                    TypeIndex = 0;
                }
            }
        }

        /// <summary>
        /// Gets the available maps for the currently selected item given a specified race, part, and type
        /// </summary>
        private void TypeComboBoxChanged()
        {
            string type;
            string part = "a";

            if (selectedCategory.Equals(Strings.Mounts) || selectedCategory.Equals(Strings.Monster) || selectedCategory.Equals(Strings.DemiHuman))
            {
                bool isDemiHuman = selectedItem.PrimaryMTRLFolder.Contains("demihuman");

                if (isDemiHuman)
                {
                    type = Info.slotAbr[selectedPart.Name];
                    part = selectedType.Name;
                }
                else
                {
                    type = selectedType.Name;
                }
            }
            else
            {
                type = selectedType.Name;
            }


            var info = MTRL.GetMTRLDatafromType(selectedItem, selectedRace.ID, selectedPart.Name, type, imcVersion, selectedCategory, part);
            mtrlData = info.Item1;

            MapComboBox = info.Item2;

            if (MapComboBox.Count > 0)
            {
                MapEnabled = true;
            }
            else
            {
                MapEnabled = false;
            }

            if (MapComboBox.Count < (currMap + 1))
            {
                MapIndex = currMap;
            }
            else
            {
                MapIndex = 0;
            }

        }

        /// <summary>
        /// The ItemSource binding of the combobox which is to contain the list of available texture maps for the selected item
        /// </summary>
        public ObservableCollection<ComboBoxInfo> MapComboBox
        {
            get { return mapComboInfo; }
            set { mapComboInfo = value; NotifyPropertyChanged("MapComboBox"); }
        }

        /// <summary>
        /// The SelectedItem binding of the combobox containing available maps
        /// </summary>
        public ComboBoxInfo SelectedMap
        {
            get { return selectedMap; }
            set
            {
                if(value != null)
                {
                    selectedMap = value;
                    NotifyPropertyChanged("SelectedMap");
                    MapComboBoxChanged();
                    currMap = mapIndex;
                }
            }
        }

        /// <summary>
        /// Gets the texture data and displays it for the currently selected item given a specified race, part, type(if applicable), and map
        /// </summary>
        private void MapComboBoxChanged()
        {
            Bitmap colorBmp = null;
            int offset = 0;
            bool isVFX = false;
            bool isUI = false;

            if (selectedMap.Name.Equals(Strings.Normal))
            {
                fullPath = mtrlData.NormalPath;
                offset = mtrlData.NormalOffset;
                FullPathString = fullPath;
            }
            else if (selectedMap.Name.Equals(Strings.Specular))
            {
                fullPath = mtrlData.SpecularPath;
                offset = mtrlData.SpecularOffset;
                FullPathString = fullPath;
            }
            else if (selectedMap.Name.Equals(Strings.Diffuse))
            {
                fullPath = mtrlData.DiffusePath;
                offset = mtrlData.DiffuseOffset;
                FullPathString = fullPath;
            }
            else if (selectedMap.Name.Equals(Strings.Mask) || selectedMap.Name.Equals(Strings.Skin))
            {
                if (selectedItem.ItemName.Equals(Strings.Face_Paint) || selectedItem.ItemName.Equals(Strings.Equipment_Decals))
                {
                    string part;
                    if (selectedItem.ItemName.Equals(Strings.Equipment_Decals))
                    {
                        if (!SelectedPart.Name.Contains("stigma"))
                        {
                            part = selectedPart.Name.PadLeft(3, '0');
                        }
                        else
                        {
                            part = SelectedPart.Name;
                        }
                    }
                    else
                    {
                        part = selectedPart.Name;
                    }

                    fullPath = String.Format(mtrlData.MaskPath, part);
                    offset = MTRL.GetDecalOffset(selectedItem.ItemName, selectedPart.Name);
                }
                else
                {
                    fullPath = mtrlData.MaskPath;
                    offset = mtrlData.MaskOffset;
                }
                FullPathString = fullPath;
            }
            else if (selectedMap.Name.Equals(Strings.ColorSet))
            {
                colorBmp = TEX.ColorSetToBitmap(mtrlData.ColorData);
                fullPath = mtrlData.MTRLPath;
                FullPathString = fullPath;
            }
            else if (SelectedMap.Name.Contains("Icon"))
            {
                if (SelectedMap.Name.Contains("HQ"))
                {
                    fullPath = mtrlData.UIHQPath;
                    offset = mtrlData.UIHQOffset;
                }
                else
                {
                    fullPath = mtrlData.UIPath;
                    offset = mtrlData.UIOffset;
                }
                FullPathString = fullPath;
                isUI = true;
            }
            else if (selectedItem.ItemCategory.Equals(Strings.Maps))
            {
                if(selectedMap.Name.Contains("HighRes Map"))
                {
                    fullPath = string.Format(mtrlData.UIPath, "_m");
                    offset = mtrlData.UIOffset = int.Parse(selectedMap.ID);
                }
                else if (selectedMap.Name.Contains("LowRes Map"))
                {
                    fullPath = string.Format(mtrlData.UIPath, "_s");
                    offset = mtrlData.UIOffset = int.Parse(selectedMap.ID);
                }
                else if (selectedMap.Name.Contains("PoI"))
                {
                    fullPath = string.Format(mtrlData.UIPath, "d");
                    offset = mtrlData.UIOffset = int.Parse(selectedMap.ID);
                }
                else if (selectedMap.Name.Contains("HighRes Mask"))
                {
                    fullPath = string.Format(mtrlData.UIPath, "m_m");
                    offset = mtrlData.UIOffset = int.Parse(selectedMap.ID);
                }
                else if (selectedMap.Name.Contains("LowRes Mask"))
                {
                    fullPath = string.Format(mtrlData.UIPath, "m_s");
                    offset = mtrlData.UIOffset = int.Parse(selectedMap.ID);
                }
                FullPathString = fullPath;
                isUI = true;
            }
            else if (selectedItem.ItemCategory.Equals("HUD") || selectedItem.ItemCategory.Equals("LoadingImage"))
            {
                fullPath = mtrlData.UIPath;
                offset = mtrlData.UIOffset;
                FullPathString = fullPath;

                isUI = true;
            }
            else
            {
                fullPath = SelectedMap.ID;
                var VFXFolder = fullPath.Substring(0, fullPath.LastIndexOf("/"));
                var VFXFile = fullPath.Substring(fullPath.LastIndexOf("/") + 1);

                offset = Helper.GetDataOffset(FFCRC.GetHash(VFXFolder), FFCRC.GetHash(VFXFile), Strings.ItemsDat);

                FullPathString = fullPath;

                isVFX = true;
            }

            string line;
            JsonEntry modEntry = null;
            bool inModList = false;
            try
            {
                using (StreamReader sr = new StreamReader(Properties.Settings.Default.Modlist_Directory))
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
                FlexibleMessageBox.Show("Error Accessing .modlist File \n" + ex.Message, "TextureViewModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (inModList)
            {
                var currOffset = Helper.GetDataOffset(FFCRC.GetHash(modEntry.fullPath.Substring(0, modEntry.fullPath.LastIndexOf("/"))), FFCRC.GetHash(Path.GetFileName(modEntry.fullPath)), modEntry.datFile);

                if (currOffset == modEntry.modOffset)
                {
                    ActiveToggle = "Disable";
                }
                else if (currOffset == modEntry.originalOffset)
                {
                    ActiveToggle = "Enable";
                }
                else
                {
                    ActiveToggle = "Error";
                }

                ActiveEnabled = true;
            }
            else
            {
                ActiveEnabled = false;
                ActiveToggle = "Enable/Disable";
            }

            if (offset == 0 && colorBmp != null)
            {
                TextureType = "Type: 16.16.16.16f ABGR\nMipMaps: None";

                TextureDimensions = "(4 x 16)";

                try
                {
                    alphaBitmap = Imaging.CreateBitmapSourceFromHBitmap(colorBmp.GetHbitmap(), IntPtr.Zero,
                        Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    alphaBitmap.Freeze();
                }
                catch (Exception e)
                {
                    FlexibleMessageBox.Show("alphaBitmap Error\n\nItem: " + selectedItem.ItemName + "\nMap: " + SelectedMap.Name + "\n\n" + e.Message,
                        "TextureViewModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                var removeAlphaBitmap = SetAlpha(colorBmp, 255);

                try
                {
                    noAlphaBitmap = Imaging.CreateBitmapSourceFromHBitmap(removeAlphaBitmap.GetHbitmap(), IntPtr.Zero,
                        Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    noAlphaBitmap.Freeze();
                }
                catch (Exception e)
                {
                    FlexibleMessageBox.Show("noAlphaBitmap Error\n\nItem: " + selectedItem.ItemName + "\nMap: " + SelectedMap.Name + "\n\n" + e.Message,
                        "TextureViewModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }


                colorBmp.Dispose();
                removeAlphaBitmap.Dispose();
            }
            else if (offset == 0 && colorBmp == null)
            {
                alphaBitmap = noAlphaBitmap =
                    new BitmapImage(
                        new Uri("pack://application:,,,/FFXIV TexTools 2;component/Resources/textureDNE.png"));
            }
            else
            {
                if (!isVFX)
                {
                    if (!isUI)
                    {
                        texData = TEX.GetTex(offset, Strings.ItemsDat);
                    }
                    else
                    {
                        texData = TEX.GetTex(offset, Strings.UIDat);
                    }
                }
                else
                {
                    texData = TEX.GetVFX(offset, Strings.ItemsDat);
                }

                string mipMaps = "Yes (" + texData.MipCount + ")";
                if(texData.MipCount < 1)
                {
                    mipMaps = "None";
                }
                TextureType = "Type: " + texData.TypeString + "\nMipMaps: " + mipMaps;
                
                TextureDimensions = "(" + texData.Width + " x " + texData.Height + ")";

                var scale = 1;

                if (texData.Width >= 4096 || texData.Height >= 4096)
                {
                    scale = 4;
                }
                else if (texData.Width >= 2048 || texData.Height >= 2048)
                {
                    scale = 2;
                }

                var nWidth = texData.Width / scale;
                var nHeight = texData.Height / scale;

                var resizedAlphaImage = TexHelper.CreateResizedImage(texData.BMPSouceAlpha, nWidth, nHeight);

                alphaBitmap = (BitmapSource)resizedAlphaImage;
                alphaBitmap.Freeze();

                var resizedNoAlphaImage = TexHelper.CreateResizedImage(texData.BMPSouceNoAlpha, nWidth, nHeight);

                noAlphaBitmap = (BitmapSource)resizedNoAlphaImage;
                noAlphaBitmap.Freeze();
            }

            try
            {
                ImageEffect = new ColorChannels()
                {
                    Channel = new System.Windows.Media.Media3D.Point4D(1.0f, 1.0f, 1.0f, 0.0f)
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.StackTrace);
            }

            SetColorChannelFilter(imageEffect);

            ChannelsEnabled = true;

            SaveEnabled = true;

            string dxPath = Path.GetFileNameWithoutExtension(fullPath);

            string savePath = Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItem.ItemName + "/" + dxPath + ".dds";
            if (selectedCategory.Equals("UI"))
            {
                savePath = Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItem.ItemCategory + "/" + dxPath + ".dds";
            }

            if (File.Exists(savePath))
            {
                ImportEnabled = true;
            }
            else
            {
                ImportEnabled= false;
            }

            string folderPath = Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItem.ItemName;
            if (selectedCategory.Equals("UI"))
            {
                folderPath = Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItem.ItemCategory;
            }

            if (Directory.Exists(folderPath))
            {
                OpenEnabled = true;
            }
            else
            {
                OpenEnabled = false;
            }

            var shaderNum = mtrlData.ShaderNum;

            if (shaderNum == 0x0D)
            {
                TranslucencyToggle = "Translucency OFF";
                TranslucencyEnabled = true;
            }
            else if (shaderNum == 0x1D)
            {
                TranslucencyToggle = "Translucency ON";
                TranslucencyEnabled = true;
            }
            else
            {
                TranslucencyToggle = "Not Supported";
                TranslucencyEnabled = false;
            }

        }

        /// <summary>
        /// Sets the alpha channel a bitmap.
        /// </summary>
        /// <param name="bmp">The bitmap to modify.</param>
        /// <param name="alpha">The alpha value to use.</param>
        /// <returns>A bitmap with the new alpha value.</returns>
        public Bitmap SetAlpha(Bitmap bmp, byte alpha)
        {
            var data = bmp.LockBits(
                new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb);

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


        /// <summary>
        /// Updates the displayed image 
        /// </summary>
        /// <remarks>
        /// This is called when an image has either been imported or enabled/disabled
        /// </remarks>
        /// <param name="offset">The new offset for the image.</param>
        /// <param name="isColor">Is the image being udpated a color set.</param>
        public void UpdateImage(int offset, bool isColor)
        {
            try
            {
                if (isColor)
                {
                    var colorBMP = MTRL.GetColorBitmap(offset);

                    TextureType = "Type: 16.16.16.16f ABGR\nMipMaps: 0";
                    TextureDimensions = "(4 x 16)";

                    try
                    {
                        alphaBitmap = Imaging.CreateBitmapSourceFromHBitmap(colorBMP.Item1.GetHbitmap(), IntPtr.Zero,
                            Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        alphaBitmap.Freeze();
                    }
                    catch (Exception e)
                    {
                        FlexibleMessageBox.Show("alphaBitmap Update Error\n\nItem: " + selectedItem.ItemName + "\nMap: " + SelectedMap.Name + "\n\n" + e.Message,
                            "TextureViewModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }


                    var removeAlphaBitmap = SetAlpha(colorBMP.Item1, 255);

                    try
                    {
                        noAlphaBitmap = Imaging.CreateBitmapSourceFromHBitmap(removeAlphaBitmap.GetHbitmap(),
                            IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        noAlphaBitmap.Freeze();
                    }
                    catch (Exception e)
                    {
                        FlexibleMessageBox.Show("noAlphaBitmap Update Error\n\nItem: " + selectedItem.ItemName + "\nMap: " + SelectedMap.Name + "\n\n" + e.Message,
                            "TextureViewModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }


                    mtrlData.ColorData = colorBMP.Item2;

                    colorBMP.Item1.Dispose();
                    removeAlphaBitmap.Dispose();

                    mtrlData.ShaderNum = colorBMP.Item3;

                    if (colorBMP.Item3 == 0x0D)
                    {
                        TranslucencyToggle = "Translucency OFF";
                    }
                    else if (colorBMP.Item3 == 0x1D)
                    {
                        TranslucencyToggle = "Translucency ON";
                    }
                }
                else
                {
                    if (SelectedMap.ID.Contains("vfx"))
                    {
                        texData = TEX.GetVFX(offset, Strings.ItemsDat);
                    }
                    else
                    {
                        if (selectedCategory.Equals("UI"))
                        {
                            texData = TEX.GetTex(offset, Strings.UIDat);
                        }
                        else
                        {
                            texData = TEX.GetTex(offset, Strings.ItemsDat);
                        }
                    }

                    string mipMaps = "Yes (" + texData.MipCount + ")";
                    if (texData.MipCount < 1)
                    {
                        mipMaps = "None";
                    }
                    TextureType = "Type: " + texData.TypeString + "\nMipMaps: " + mipMaps;
                    TextureDimensions = "(" + texData.Width + " x " + texData.Height + ")";


                    var scale = 1;

                    if (texData.Width >= 4096 || texData.Height >= 4096)
                    {
                        scale = 4;
                    }
                    else if (texData.Width >= 2048 || texData.Height >= 2048)
                    {
                        scale = 2;
                    }

                    var nWidth = texData.Width / scale;
                    var nHeight = texData.Height / scale;

                    var resizedAlphaImage = TexHelper.CreateResizedImage(texData.BMPSouceAlpha, nWidth, nHeight);

                    alphaBitmap = (BitmapSource)resizedAlphaImage;
                    alphaBitmap.Freeze();

                    var resizedNoAlphaImage = TexHelper.CreateResizedImage(texData.BMPSouceNoAlpha, nWidth, nHeight);

                    noAlphaBitmap = (BitmapSource)resizedNoAlphaImage;
                    noAlphaBitmap.Freeze();
                }

                if (AlphaChecked)
                {
                    ImageSource = alphaBitmap;
                }
                else
                {
                    ImageSource = noAlphaBitmap;
                }
            }
            catch(Exception e)
            {
                    FlexibleMessageBox.Show("There was an error updating the image.\n" + e.Message, "TextureViewModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }


        /// <summary>
        /// Sets the color channel filter on currently displayed image based on selected color checkboxes.
        /// </summary>
        private void SetColorChannelFilter(ColorChannels CC)
        {
            float r = RedChecked == true ? 1.0f : 0.0f;
            float g = GreenChecked == true ? 1.0f : 0.0f;
            float b = BlueChecked == true ? 1.0f : 0.0f;
            float a = AlphaChecked == true ? 1.0f : 0.0f;

            BitmapSource img;

            if (AlphaChecked == true)
            {
                img = alphaBitmap;
            }
            else
            {
                img = noAlphaBitmap;
            }

            CC.Channel = new System.Windows.Media.Media3D.Point4D(r, g, b, a);

            ImageEffect = CC;

            ImageSource = img;

        }

        public event PropertyChangedEventHandler PropertyChanged;
  

        protected void NotifyPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
