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
using FFXIV_TexTools2.IO;
using FFXIV_TexTools2.Material;
using FFXIV_TexTools2.Model;
using FFXIV_TexTools2.Resources;
using FFXIV_TexTools2.Views;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace FFXIV_TexTools2.ViewModel
{
    public class ModelViewModel : INotifyPropertyChanged
    {
        ItemData selectedItem;
        List<ModelMeshData> meshList;
        List<string> materialStrings;
        List<MDLTEXData> meshData;
        List<ComboBoxInfo> cbi = new List<ComboBoxInfo>();
        Composite3DViewModel CVM;
        ModelData modelData;

        int raceIndex, meshIndex, bodyIndex, partIndex;
        bool raceEnabled, meshEnabled, bodyEnabled, partEnabled, modelRendering, secondModelRendering, thirdModelRendering, is3DLoaded, disposing, modelTabEnabled;
        bool import3dEnabled, activeEnabled, openEnabled, newCat, advImport3dEnabled;
        string selectedCategory, modelName, fullPath, prevCat, reflectionContent;
        string activeToggle = FFXIV_TexTools2.Resources.Strings.Enable+"/"+FFXIV_TexTools2.Resources.Strings.Disable;
        string statusText = "";

        private ObservableCollection<ComboBoxInfo> raceComboInfo = new ObservableCollection<ComboBoxInfo>();
        private ObservableCollection<ComboBoxInfo> meshComboInfo = new ObservableCollection<ComboBoxInfo>();
        private ObservableCollection<ComboBoxInfo> partComboInfo = new ObservableCollection<ComboBoxInfo>();
        private ObservableCollection<ComboBoxInfo> bodyComboInfo = new ObservableCollection<ComboBoxInfo>();

        private ComboBoxInfo selectedRace;
        private ComboBoxInfo selectedMesh;
        private ComboBoxInfo selectedPart;
        private ComboBoxInfo selectedBody;

        private float StatusTextDuration = 3.0f;
        private System.Timers.Timer StatusTextTimer;

        public int RaceIndex { get { return raceIndex; } set { raceIndex = value; NotifyPropertyChanged("RaceIndex"); } }
        public int MeshIndex { get { return meshIndex; } set { meshIndex = value; NotifyPropertyChanged("MeshIndex"); } }
        public int BodyIndex { get { return bodyIndex; } set { bodyIndex = value; NotifyPropertyChanged("BodyIndex"); } }
        public int PartIndex { get { return partIndex; } set { partIndex = value; NotifyPropertyChanged("PartIndex"); } }

        public string ReflectionContent { get { return reflectionContent; } set { reflectionContent = value; NotifyPropertyChanged("ReflectionContent"); } }
        public string ActiveToggle { get { return activeToggle; } set { activeToggle = value; NotifyPropertyChanged("ActiveToggle"); } }
        public string StatusText { get { return statusText; } set { statusText = value; NotifyPropertyChanged("StatusText"); } }

        public bool RaceEnabled { get { return raceEnabled; } set { raceEnabled = value; NotifyPropertyChanged("RaceEnabled"); } }
        public bool MeshEnabled { get { return meshEnabled; } set { meshEnabled = value; NotifyPropertyChanged("MeshEnabled"); } }
        public bool BodyEnabled { get { return bodyEnabled; } set { bodyEnabled = value; NotifyPropertyChanged("BodyEnabled"); } }
        public bool PartEnabled { get { return partEnabled; } set { partEnabled = value; NotifyPropertyChanged("PartEnabled"); } }

        public bool ModelTabEnabled { get { return modelTabEnabled; } set { modelTabEnabled = value; NotifyPropertyChanged("ModelTabEnabled"); } }
        public bool Import3DEnabled { get { return import3dEnabled; } set { import3dEnabled = value; NotifyPropertyChanged("Import3DEnabled"); } }
        public bool AdvImport3DEnabled { get { return advImport3dEnabled; } set { advImport3dEnabled = value; NotifyPropertyChanged("AdvImport3DEnabled"); } }
        public bool ActiveEnabled { get { return activeEnabled; } set { activeEnabled = value; NotifyPropertyChanged("ActiveEnabled"); } }
        public bool Open3DEnabled { get { return openEnabled; } set { openEnabled = value; NotifyPropertyChanged("Open3DEnabled"); } }

        public bool ModelRendering { get { return modelRendering; } set { modelRendering = value; NotifyPropertyChanged("ModelRendering"); } }
        public bool SecondModelRendering { get { return secondModelRendering; } set { secondModelRendering = value; NotifyPropertyChanged("SecondModelRendering"); } }
        public bool ThirdModelRendering { get { return thirdModelRendering; } set { thirdModelRendering = value; NotifyPropertyChanged("ThirdModelRendering"); } }

        public Composite3DViewModel CompositeVM { get { return CVM; } set { CVM = value; NotifyPropertyChanged("CompositeVM"); } }

        public ModelViewModel()
        {
            CompositeVM = new Composite3DViewModel();
        }

        public void ReloadModel()
        {
            UpdateModel(selectedItem, selectedCategory);
        }

        // Shows the given status text for the standard duration.
        private void ShowStatusText(string text, bool error = false)
        {
            if(error)
            {
                text = "Error: " + text;
            }

            StatusText = text;

            if(StatusTextTimer != null)
            {
                StatusTextTimer.Stop();
                StatusTextTimer.Dispose();
            }


            StatusTextTimer = new System.Timers.Timer(StatusTextDuration * 1000);
            // Hook up the Elapsed event for the timer. 
            StatusTextTimer.AutoReset = false;
            StatusTextTimer.Enabled = true;
            StatusTextTimer.Elapsed += new System.Timers.ElapsedEventHandler(ClearStatusText);

        }

        private void ClearStatusText(object Sender, EventArgs e)
        {
            if (StatusTextTimer != null)
            {
                StatusTextTimer.Stop();
                StatusTextTimer.Dispose();
            }

            StatusText = "";
        }


        public void UpdateModel(ItemData item, string category)
        {
            if(item == null || category == null)
            {
                return;
            }

            CompositeVM.Dispose();
            disposing = true;
            cbi.Clear();
            bool itemChanged = false;

            if(selectedItem != null && !item.ItemName.Equals(selectedItem.ItemName))
            {
                itemChanged = true;
            }

            prevCat = selectedCategory;
            selectedItem = item;
            selectedCategory = category;

            if (!selectedCategory.Equals(prevCat))
            {
                newCat = true;
            }

            if(RaceComboBox.Count > 0 && !itemChanged)
            {
                if(BodyComboBox.Count > 0)
                {
                    if(PartComboBox.Count > 0)
                    {
                        PartComboBoxChanged();
                    } else
                    {
                        BodyComboBoxChanged();
                    }
                } else
                {
                    RaceComboBoxChanged();
                }
                ShowStatusText("Model Updated Successfully");
            }
            else
            {
                ClearStatusText(null, null);
                try
                {
                    string categoryType = Helper.GetCategoryType(selectedCategory);

                    string MDLFolder = "";
                    string MDLFile = "";

                    if (categoryType.Equals("weapon") || categoryType.Equals("food"))
                    {
                        MDLFolder = "";
                        cbi.Add(new ComboBoxInfo() { Name = Strings.All, ID = Strings.All, IsNum = false });
                    }
                    else if (categoryType.Equals("accessory"))
                    {
                        MDLFolder = string.Format(Strings.AccMDLFolder, selectedItem.PrimaryModelID);
                        MDLFile = string.Format(Strings.AccMDLFile, "{0}", selectedItem.PrimaryModelID, Info.slotAbr[selectedCategory]);
                    }
                    else if (categoryType.Equals("character"))
                    {
                        if (selectedItem.ItemName.Equals(Strings.Body))
                        {
                            MDLFolder = Strings.BodyMDLFolder;
                        }
                        else if (selectedItem.ItemName.Equals(Strings.Face))
                        {
                            MDLFolder = Strings.FaceMDLFolder;
                        }
                        else if (selectedItem.ItemName.Equals(Strings.Hair))
                        {
                            MDLFolder = Strings.HairMDLFolder;
                        }
                        else if (selectedItem.ItemName.Equals(Strings.Tail))
                        {
                            MDLFolder = Strings.TailMDLFolder;
                        }
                    }
                    else if (categoryType.Equals("monster"))
                    {
                        cbi.Add(new ComboBoxInfo() { Name = Strings.All, ID = Strings.All, IsNum = false });
                    }
                    else
                    {
                        MDLFolder = string.Format(Strings.EquipMDLFolder, selectedItem.PrimaryModelID);
                        MDLFile = string.Format(Strings.EquipMDLFile, "{0}", selectedItem.PrimaryModelID, Info.slotAbr[selectedCategory]);
                    }

                    var fileHashList = Helper.GetAllFilesInFolder(FFCRC.GetHash(MDLFolder), Strings.ItemsDat);

                    if (!categoryType.Equals("weapon") && !categoryType.Equals("monster"))
                    {
                        foreach (string raceID in Info.IDRace.Keys)
                        {
                            if (categoryType.Equals("character"))
                            {
                                for (int i = 0; i < 3; i++)
                                {
                                    var mdlFolder = String.Format(MDLFolder, raceID, i.ToString().PadLeft(4, '0'));
                                    if (selectedItem.ItemName.Equals(Strings.Face) && (raceID.Equals("0301") || raceID.Equals("0304") || raceID.Equals("0401") || raceID.Equals("0404")))
                                    {
                                        mdlFolder = String.Format(MDLFolder, raceID, "01" + i.ToString().PadLeft(2, '0'));

                                    }


                                    if (Helper.FolderExists(FFCRC.GetHash(mdlFolder), Strings.ItemsDat))
                                    {
                                        cbi.Add(new ComboBoxInfo() { Name = Info.IDRace[raceID], ID = raceID, IsNum = false });
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                var mdlFile = String.Format(MDLFile, raceID);
                                var fileHash = FFCRC.GetHash(mdlFile);

                                if (fileHashList.Contains(fileHash))
                                {
                                    cbi.Add(new ComboBoxInfo() { Name = Info.IDRace[raceID], ID = raceID, IsNum = false });
                                }
                            }
                        }
                    }

                    RaceComboBox = new ObservableCollection<ComboBoxInfo>(cbi);
                    RaceIndex = 0;

                    if (cbi.Count <= 1)
                    {
                        RaceEnabled = false;
                    }
                    else
                    {
                        RaceEnabled = true;
                    }
                }
                catch (Exception ex)
                {
                    FlexibleMessageBox.Show("Model Error \n" + ex.Message, "ModelViewModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Debug.WriteLine(ex.StackTrace);
                }
            }


        }

        /// <summary>
        /// Command for the Transparency button
        /// </summary>
        public ICommand TransparencyCommand
        {
            get { return new RelayCommand(SetTransparency); }
        }

        /// <summary>
        /// Sets the transparency of the model
        /// </summary>
        /// <param name="obj"></param>
        private void SetTransparency(object obj)
        {
            CompositeVM.Transparency();
        }

        /// <summary>
        /// Command for the Update TEX button
        /// </summary>
        public ICommand UpdateCommand
        {
            get { return new RelayCommand(UpdateTEX); }
        }

        /// <summary>
        /// updates the models textures
        /// </summary>
        /// <param name="obj"></param>
        private void UpdateTEX(object obj)
        {
            is3DLoaded = false;
            MeshComboBoxChanged();
        }

        /// <summary>
        /// Command for the Reflection button
        /// </summary>
        public ICommand ReflectionCommand
        {
            get { return new RelayCommand(SetReflection); }
        }


        /// <summary>
        /// Sets the reflectivity(Specular Shininess) of the model
        /// </summary>
        /// <param name="obj"></param>
        private void SetReflection(object obj)
        {
            CompositeVM.Reflections(selectedItem.ItemName);

            ReflectionContent = FFXIV_TexTools2.Resources.Strings.Reflection+" " + string.Format("{0:0.##}", CompositeVM.CurrentSS);
        }

        /// <summary>
        /// Command for the Lighting button
        /// </summary>
        public ICommand LightingCommand
        {
            get { return new RelayCommand(SetLighting); }
        }

        /// <summary>
        /// Sets the lighting of the scene
        /// </summary>
        /// <param name="obj"></param>
        private void SetLighting(object obj)
        {
            CompositeVM.Lighting();

        }

        /// <summary>
        /// Command for the Export OBJ button
        /// </summary>
        public ICommand ExportOBJCommand
        {
            get { return new RelayCommand(ExportOBJ); }
        }

        /// <summary>
        /// Command for Open Folder button
        /// </summary>
        public ICommand OpenFolderCommand
        {
            get { return new RelayCommand(OpenFolder); }
        }

        /// <summary>
        /// Runs the OpenFolder method 
        /// </summary>
        /// <param name="obj"></param>
        public void OpenFolder(object obj)
        {
            string savePath = Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItem.ItemName + "/3D";

            try
            {
                Process.Start(savePath);
            }
            catch (Exception ex)
            {
                FlexibleMessageBox.Show("Error opening the folder "+ selectedItem.ItemName +"/3D \n"+ ex.Message, "ModelViewModel Error "+ Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Saves the model and created textures as an OBJ file
        /// </summary>
        /// <param name="obj"></param>
        private void ExportOBJ(object obj)
        {
            SaveModel.Save(selectedCategory, modelName, SelectedMesh.ID, selectedItem.ItemName, meshData, meshList);

            try
            {
                var result = SaveModel.SaveCollada(selectedCategory, modelName, selectedItem.ItemName, meshData, meshList, modelData);
                if (result)
                {
                    Import3DEnabled = true;
                    AdvImport3DEnabled = true;
                }
            }
            catch (Exception ex)
            {
                FlexibleMessageBox.Show("Error saving .dae File \n" + ex.Message, "ModelViewModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Debug.WriteLine(ex.StackTrace);
            }

            Open3DEnabled = true;
        }

        /// <summary>
        /// Command for the Import OBJ button
        /// </summary>
        public ICommand ImportOBJCommand
        {
            get { return new RelayCommand(ImportOBJ); }
        }

        /// <summary>
        /// Command for the Import OBJ button
        /// </summary>
        public ICommand AdvImportOBJCommand
        {
            get { return new RelayCommand(AdvImport); }
        }

        /// <summary>
        /// Imports the model
        /// </summary>
        /// <param name="obj"></param>
        private void ImportOBJ(object obj)
        {
            if (!Helper.IsIndexLocked(true))
            {
                ImportModel.ImportDAE(selectedCategory, selectedItem.ItemName, modelName, SelectedMesh.ID, fullPath, modelData, null);
                UpdateModel(selectedItem, selectedCategory);
            }
        }

        /// <summary>
        /// Imports the model
        /// </summary>
        /// <param name="obj"></param>
        private void AdvImport(object obj)
        {
            if (!Helper.IsIndexLocked(true))
            {
                var savePath = Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItem.ItemName + "/3D/" + modelName + ".DAE";
                AdvImport advImport = new AdvImport(this, savePath, selectedCategory, selectedItem, modelName, SelectedMesh.ID, fullPath, modelData);
                advImport.Owner = App.Current.MainWindow;
                advImport.Show();
            }
        }

        /// <summary>
        /// Command for the Revert OBJ button
        /// </summary>
        public ICommand RevertOBJCommand
        {
            get { return new RelayCommand(RevertOBJ); }
        }

        /// <summary>
        /// Reverts the model
        /// </summary>
        /// <param name="obj"></param>
        private void RevertOBJ(object obj)
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
                    FlexibleMessageBox.Show("Error Accessing .modlist File \n" + ex.Message, "ModelViewModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                if (modEntry != null)
                {
                    int offset = 0;

                    if (ActiveToggle.Equals(FFXIV_TexTools2.Resources.Strings.Enable))
                    {
                        offset = modEntry.modOffset;
                        Helper.UpdateIndex(offset, fullPath, Strings.ItemsDat);
                        Helper.UpdateIndex2(offset, fullPath, Strings.ItemsDat);
                        ActiveToggle = FFXIV_TexTools2.Resources.Strings.Disable;
                    }
                    else if (ActiveToggle.Equals(FFXIV_TexTools2.Resources.Strings.Disable))
                    {
                        offset = modEntry.originalOffset;
                        Helper.UpdateIndex(offset, fullPath, Strings.ItemsDat);
                        Helper.UpdateIndex2(offset, fullPath, Strings.ItemsDat);
                        ActiveToggle = FFXIV_TexTools2.Resources.Strings.Enable;
                    }
                }

                UpdateModel(selectedItem, selectedCategory);
            }
        }


        /// <summary>
        /// Disposes of the view model data
        /// </summary>
        public void Dispose()
        {
            if (selectedItem != null)
            {
                selectedItem = null;
                meshList = null;
                materialStrings = null;
                meshData = null;
            }

            if (CompositeVM != null)
            {
                CompositeVM.Dispose();
            }
        }

        /// <summary>
        /// Item source for combo box containing available races for the model
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
        /// Selected item for race combo box
        /// </summary>
        public ComboBoxInfo SelectedRace
        {
            get { return selectedRace; }
            set
            {
                if (value != null)
                {
                    selectedRace = value;
                    NotifyPropertyChanged("SelectedRace");
                    RaceComboBoxChanged();
                }
            }
        }

        /// <summary>
        /// Sets the data for the body combo box
        /// </summary>
        private void RaceComboBoxChanged()
        {
            try
            {
                is3DLoaded = false;

                if (CompositeVM != null && !disposing)
                {
                    disposing = true;
                    CompositeVM.Dispose();
                }

                List<ComboBoxInfo> cbi = new List<ComboBoxInfo>();
                string categoryType = Helper.GetCategoryType(selectedCategory);
                string MDLFolder = "";

                if (categoryType.Equals("weapon"))
                {
                    cbi.Add(new ComboBoxInfo() { Name = selectedItem.PrimaryModelBody, ID = selectedItem.PrimaryModelBody, IsNum = false });
                }
                else if (categoryType.Equals("food"))
                {
                    cbi.Add(new ComboBoxInfo() { Name = selectedItem.PrimaryModelBody, ID = selectedItem.PrimaryModelBody, IsNum = false });
                }
                else if (categoryType.Equals("accessory"))
                {
                    cbi.Add(new ComboBoxInfo() { Name = "-", ID = "-", IsNum = false });
                }
                else if (categoryType.Equals("character"))
                {
                    if (selectedItem.ItemName.Equals(Strings.Body))
                    {
                        MDLFolder = string.Format(Strings.BodyMDLFolder, SelectedRace.ID, "{0}");
                    }
                    else if (selectedItem.ItemName.Equals(Strings.Face))
                    {
                        MDLFolder = string.Format(Strings.FaceMDLFolder, SelectedRace.ID, "{0}");
                    }
                    else if (selectedItem.ItemName.Equals(Strings.Hair))
                    {
                        MDLFolder = string.Format(Strings.HairMDLFolder, SelectedRace.ID, "{0}");
                    }
                    else if (selectedItem.ItemName.Equals(Strings.Tail))
                    {
                        MDLFolder = string.Format(Strings.TailMDLFolder, SelectedRace.ID, "{0}");
                    }
                }
                else if (categoryType.Equals("monster"))
                {
                    cbi.Add(new ComboBoxInfo() { Name = selectedItem.PrimaryModelBody, ID = selectedItem.PrimaryModelBody, IsNum = false });
                }
                else
                {
                    cbi.Add(new ComboBoxInfo() { Name = "-", ID = "-", IsNum = false });
                }


                if (categoryType.Equals("character"))
                {
                    for (int i = 0; i < 250; i++)
                    {
                        string folder = String.Format(MDLFolder, i.ToString().PadLeft(4, '0'));

                        if (Helper.FolderExists(FFCRC.GetHash(folder), Strings.ItemsDat))
                        {
                            cbi.Add(new ComboBoxInfo() { Name = i.ToString(), ID = i.ToString(), IsNum = true });

                            if (selectedItem.ItemName.Equals(Strings.Body))
                            {
                                //break;
                            }
                        }
                    }
                }

                BodyComboBox = new ObservableCollection<ComboBoxInfo>(cbi);
                BodyIndex = 0;

                if (cbi.Count <= 1)
                {
                    BodyEnabled = false;
                }
                else
                {
                    BodyEnabled = true;
                }
            } catch (Exception e)
            {
                ResetBadCompositeState();
            }
        }

        /// <summary>
        /// Item source for the body combo box
        /// </summary>
        public ObservableCollection<ComboBoxInfo> BodyComboBox
        {
            get { return bodyComboInfo; }
            set { bodyComboInfo = value; NotifyPropertyChanged("BodyComboBox"); }
        }

        /// <summary>
        /// Selected item for the body combo box
        /// </summary>
        public ComboBoxInfo SelectedBody
        {
            get { return selectedBody; }
            set
            {
                if (value != null)
                {
                    selectedBody = value;
                    NotifyPropertyChanged("SelectedBody");
                    BodyComboBoxChanged();
                }
            }
        }

        /// <summary>
        /// Sets the data for the part combo box
        /// </summary>
        private void BodyComboBoxChanged()
        {
            try
            {
                ClearStatusText(null, null);

                is3DLoaded = false;
                bool isDemiHuman = false;

                if (CompositeVM != null && !disposing)
                {
                    disposing = true;
                    CompositeVM.Dispose();
                }

                if (selectedItem.PrimaryMTRLFolder != null && selectedItem.PrimaryMTRLFolder.Contains("demihuman"))
                {
                    isDemiHuman = true;
                }

                List<ComboBoxInfo> cbi = new List<ComboBoxInfo>();
                string type = Helper.GetCategoryType(selectedCategory);

                string MDLFolder = "";
                string MDLFile = "";
                string[] abrParts = null;

                if (type.Equals("character"))
                {
                    if (selectedItem.ItemName.Equals(Strings.Body))
                    {
                        MDLFolder = string.Format(Strings.BodyMDLFolder, SelectedRace.ID, SelectedBody.ID.PadLeft(4, '0'));
                        MDLFile = string.Format(Strings.BodyMDLFile, SelectedRace.ID, SelectedBody.ID.PadLeft(4, '0'), "{0}");

                        abrParts = new string[5] { "met", "glv", "dwn", "sho", "top" };
                    }
                    else if (selectedItem.ItemName.Equals(Strings.Face))
                    {
                        MDLFolder = string.Format(Strings.FaceMDLFolder, SelectedRace.ID, SelectedBody.ID.PadLeft(4, '0'));
                        MDLFile = string.Format(Strings.FaceMDLFile, SelectedRace.ID, SelectedBody.ID.PadLeft(4, '0'), "{0}");

                        abrParts = new string[3] { "fac", "iri", "etc" };
                    }
                    else if (selectedItem.ItemName.Equals(Strings.Hair))
                    {
                        MDLFolder = string.Format(Strings.HairMDLFolder, SelectedRace.ID, SelectedBody.ID.PadLeft(4, '0'));
                        MDLFile = string.Format(Strings.HairMDLFile, SelectedRace.ID, SelectedBody.ID.PadLeft(4, '0'), "{0}");

                        abrParts = new string[2] { "hir", "acc" };
                    }
                    else if (selectedItem.ItemName.Equals(Strings.Tail))
                    {
                        MDLFolder = string.Format(Strings.TailMDLFolder, SelectedRace.ID, SelectedBody.ID.PadLeft(4, '0'));
                        MDLFile = string.Format(Strings.TailMDLFile, SelectedRace.ID, SelectedBody.ID.PadLeft(4, '0'), "{0}");

                        abrParts = new string[1] { "til" };
                    }

                    var fileHashList = Helper.GetAllFilesInFolder(FFCRC.GetHash(MDLFolder), Strings.ItemsDat);

                    foreach (string abrPart in abrParts)
                    {
                        var file = String.Format(MDLFile, abrPart);

                        if (fileHashList.Contains(FFCRC.GetHash(file)))
                        {
                            if (selectedItem.ItemName.Equals(Strings.Body))
                            {
                                cbi.Add(new ComboBoxInfo() { Name = Info.slotAbr.FirstOrDefault(x => x.Value == abrPart).Key, ID = abrPart, IsNum = false });
                            }
                            else if (selectedItem.ItemName.Equals(Strings.Face))
                            {
                                cbi.Add(new ComboBoxInfo() { Name = Info.FaceTypes.FirstOrDefault(x => x.Value == abrPart).Key, ID = abrPart, IsNum = false });
                            }
                            else if (selectedItem.ItemName.Equals(Strings.Hair))
                            {
                                cbi.Add(new ComboBoxInfo() { Name = Info.HairTypes.FirstOrDefault(x => x.Value == abrPart).Key, ID = abrPart, IsNum = false });
                            }
                            else if (selectedItem.ItemName.Equals(Strings.Tail))
                            {
                                cbi.Add(new ComboBoxInfo() { Name = Strings.Tail, ID = abrPart, IsNum = false });
                            }
                        }
                    }
                }
                else if (isDemiHuman)
                {
                    MDLFolder = string.Format(Strings.DemiMDLFolder, selectedItem.PrimaryModelID, selectedItem.PrimaryModelBody);
                    MDLFile = string.Format(Strings.DemiMDLFile, selectedItem.PrimaryModelID, selectedItem.PrimaryModelBody, "{0}");

                    abrParts = new string[5] { "met", "glv", "dwn", "sho", "top" };

                    var fileHashList = Helper.GetAllFilesInFolder(FFCRC.GetHash(MDLFolder), Strings.ItemsDat);

                    foreach (string abrPart in abrParts)
                    {
                        var file = String.Format(MDLFile, abrPart);

                        if (fileHashList.Contains(FFCRC.GetHash(file)))
                        {
                            cbi.Add(new ComboBoxInfo() { Name = Info.slotAbr.FirstOrDefault(x => x.Value == abrPart).Key, ID = abrPart, IsNum = false });
                        }
                    }
                }
                else if (type.Equals("weapon"))
                {
                    if (selectedItem.SecondaryModelID != null)
                    {
                        cbi.Add(new ComboBoxInfo() { Name = "Primary", ID = "Primary", IsNum = false });
                        cbi.Add(new ComboBoxInfo() { Name = "Secondary", ID = "Secondary", IsNum = false });

                    }
                    else
                    {
                        cbi.Add(new ComboBoxInfo() { Name = "Primary", ID = "Primary", IsNum = false });

                    }
                }
                else if (type.Equals("monster"))
                {
                    if (selectedCategory.Equals(Strings.Pets))
                    {
                        cbi.AddRange(MTRL.GetMTRLParts(selectedItem, SelectedRace.ID, "", selectedCategory));
                    }
                    else
                    {
                        cbi.Add(new ComboBoxInfo() { Name = "1", ID = "1", IsNum = false });
                    }
                }
                else if (selectedItem.PrimaryModelID.Equals("9900"))
                {
                    MDLFolder = string.Format(Strings.EquipMDLFolder, selectedItem.PrimaryModelID);
                    MDLFile = string.Format(Strings.EquipMDLFile, SelectedRace.ID, selectedItem.PrimaryModelID, "{0}");

                    abrParts = new string[5] { "met", "glv", "dwn", "sho", "top" };

                    var fileHashList = Helper.GetAllFilesInFolder(FFCRC.GetHash(MDLFolder), Strings.ItemsDat);

                    foreach (string abrPart in abrParts)
                    {
                        var file = String.Format(MDLFile, abrPart);

                        if (fileHashList.Contains(FFCRC.GetHash(file)))
                        {
                            cbi.Add(new ComboBoxInfo() { Name = Info.slotAbr.FirstOrDefault(x => x.Value == abrPart).Key, ID = abrPart, IsNum = false });
                        }
                    }
                }
                else if (selectedCategory.Equals(Strings.Rings))
                {
                    cbi.Add(new ComboBoxInfo() { Name = Strings.Right, ID = Strings.Right, IsNum = false });
                    cbi.Add(new ComboBoxInfo() { Name = Strings.Left, ID = Strings.Left, IsNum = false });
                }
                else
                {
                    cbi.Add(new ComboBoxInfo() { Name = "-", ID = "-", IsNum = false });
                }

                PartComboBox = new ObservableCollection<ComboBoxInfo>(cbi);
                PartIndex = 0;

                if (cbi.Count <= 1)
                {
                    PartEnabled = false;
                }
                else
                {
                    PartEnabled = true;
                }
            } catch (Exception e)
            {
                ResetBadCompositeState();
            }
        }

        /// <summary>
        /// Item source of the part combo box
        /// </summary>
        public ObservableCollection<ComboBoxInfo> PartComboBox
        {
            get { return partComboInfo; }
            set { partComboInfo = value; NotifyPropertyChanged("PartComboBox"); }
        }

        /// <summary>
        /// selected item of the part combo box
        /// </summary>
        public ComboBoxInfo SelectedPart
        {
            get { return selectedPart; }
            set
            {
                if (value != null)
                {
                    selectedPart = value;
                    NotifyPropertyChanged("SelectedPart");
                    PartComboBoxChanged();
                }
            }
        }

        /// <summary>
        /// Sets the data for the mesh combo box
        /// </summary>
        private void PartComboBoxChanged()
        {
            try
            {
                ClearStatusText(null, null);
                
                is3DLoaded = false;

                if (CompositeVM != null && !disposing)
                {
                    disposing = true;
                    CompositeVM.Dispose();
                }


                List<ComboBoxInfo> cbi = new List<ComboBoxInfo>();

                MDL mdl = new MDL(selectedItem, selectedCategory, Info.raceID[SelectedRace.Name], SelectedBody.ID, SelectedPart.ID);
                meshList = mdl.GetMeshList();
                modelName = mdl.GetModelName();
                materialStrings = mdl.GetMaterialStrings();
                fullPath = mdl.GetInternalPath();
                modelData = mdl.GetModelData();

                cbi.Add(new ComboBoxInfo() { Name = Strings.All, ID = Strings.All, IsNum = false });

                if (meshList.Count > 1)
                {
                    for (int i = 0; i < meshList.Count; i++)
                    {
                        cbi.Add(new ComboBoxInfo() { Name = i.ToString(), ID = i.ToString(), IsNum = true });
                    }
                }

                MeshComboBox = new ObservableCollection<ComboBoxInfo>(cbi);
                MeshIndex = 0;

                if (cbi.Count > 1)
                {
                    MeshEnabled = true;
                }
                else
                {
                    MeshEnabled = false;
                }

                try
                {

                }
                catch (Exception ex)
                {
                    FlexibleMessageBox.Show("part 3D Error \n" + ex.Message, "ModelViewModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Debug.WriteLine(ex.StackTrace);
                    ResetBadCompositeState();
                }
            } catch(Exception e)
            {
                ResetBadCompositeState();
            }
        }

        /// <summary>
        /// Item source for the mesh combo box
        /// </summary>
        public ObservableCollection<ComboBoxInfo> MeshComboBox
        {
            get { return meshComboInfo; }
            set { meshComboInfo = value; NotifyPropertyChanged("MeshComboBox"); }
        }

        /// <summary>
        /// Selected item for the mesh combo box
        /// </summary>
        public ComboBoxInfo SelectedMesh
        {
            get { return selectedMesh; }
            set
            {
                if (value != null)
                {
                    selectedMesh = value;
                    NotifyPropertyChanged("SelectedMesh");
                    MeshComboBoxChanged();
                }
            }
        }

        /// <summary>
        /// Gets the model data and sets the display viewmodel
        /// </summary>
        private void MeshComboBoxChanged()
        {
            try
            {
                if (!is3DLoaded)
                {
                    disposing = false;

                    meshData = new List<MDLTEXData>();

                    for (int i = 0; i < meshList.Count; i++)
                    {
                        BitmapSource specularBMP = null;
                        BitmapSource diffuseBMP = null;
                        BitmapSource normalBMP = null;
                        BitmapSource alphaBMP = null;
                        BitmapSource maskBMP = null;
                        BitmapSource emissiveBMP = null;

                        TEXData specularData = null;
                        TEXData diffuseData = null;
                        TEXData normalData = null;
                        TEXData maskData = null;

                        bool isBody = false;
                        bool isFace = false;
                        MTRLData mtrlData;
                        try
                        {
                            mtrlData = MTRL3D(i);
                        } catch(Exception e)
                        {
                            continue;
                        }

                        if (selectedCategory.Equals(Strings.Character))
                        {
                            if (selectedItem.ItemName.Equals(Strings.Tail) || selectedItem.ItemName.Equals(Strings.Hair))
                            {
                                if (mtrlData.MaskPath != null)
                                {
                                    normalData = TEX.GetTex(mtrlData.NormalOffset, Strings.ItemsDat);
                                    maskData = TEX.GetTex(mtrlData.MaskOffset, Strings.ItemsDat);

                                    var maps = TexHelper.MakeModelTextureMaps(normalData, null, maskData, null, mtrlData);

                                    diffuseBMP = maps[0];
                                    specularBMP = maps[1];
                                    normalBMP = maps[2];
                                    alphaBMP = maps[3];
                                    emissiveBMP = maps[4];
                                }
                                else
                                {
                                    normalData = TEX.GetTex(mtrlData.NormalOffset, Strings.ItemsDat);
                                    specularData = TEX.GetTex(mtrlData.SpecularOffset, Strings.ItemsDat);

                                    if (mtrlData.DiffusePath != null)
                                    {
                                        diffuseData = TEX.GetTex(mtrlData.DiffuseOffset, Strings.ItemsDat);
                                    }

                                    var maps = TexHelper.MakeCharacterMaps(normalData, diffuseData, null, specularData, selectedItem.ItemName, mtrlData.MTRLPath);

                                    diffuseBMP = maps[0];
                                    specularBMP = maps[1];
                                    normalBMP = maps[2];
                                    alphaBMP = maps[3];
                                }
                            }

                            if (selectedItem.ItemName.Equals(Strings.Body))
                            {
                                normalData = TEX.GetTex(mtrlData.NormalOffset, Strings.ItemsDat);
                                specularData = TEX.GetTex(mtrlData.SpecularOffset, Strings.ItemsDat);
                                diffuseData = TEX.GetTex(mtrlData.DiffuseOffset, Strings.ItemsDat);

                                isBody = true;

                                var maps = TexHelper.MakeCharacterMaps(normalData, diffuseData, null, specularData, selectedItem.ItemName, mtrlData.MTRLPath);

                                diffuseBMP = maps[0];
                                specularBMP = maps[1];
                                normalBMP = maps[2];
                                alphaBMP = maps[3];
                            }

                            if (selectedItem.ItemName.Equals(Strings.Face))
                            {
                                normalData = TEX.GetTex(mtrlData.NormalOffset, Strings.ItemsDat);

                                
                                if (mtrlData.MTRLPath.Contains("_fac_"))
                                {
                                    specularData = TEX.GetTex(mtrlData.SpecularOffset, Strings.ItemsDat);
                                    diffuseData = TEX.GetTex(mtrlData.DiffuseOffset, Strings.ItemsDat);

                                    var maps = TexHelper.MakeCharacterMaps(normalData, diffuseData, null, specularData, selectedItem.ItemName, mtrlData.MTRLPath);

                                    diffuseBMP = maps[0];
                                    specularBMP = maps[1];
                                    normalBMP = maps[2];
                                    alphaBMP = maps[3];
                                    isFace = true;
                                }
                                else
                                {
                                    if (mtrlData.ColorData != null)
                                    {
                                        specularData = TEX.GetTex(mtrlData.SpecularOffset, Strings.ItemsDat);
                                        var maps = TexHelper.MakeModelTextureMaps(normalData, null, null, specularData, mtrlData);

                                        diffuseBMP = maps[0];
                                        specularBMP = maps[1];
                                        normalBMP = maps[2];
                                        alphaBMP = maps[3];
                                        emissiveBMP = maps[4];
                                    }
                                    else
                                    {
                                        specularData = TEX.GetTex(mtrlData.SpecularOffset, Strings.ItemsDat);
                                        var maps = TexHelper.MakeCharacterMaps(normalData, diffuseData, null, specularData, selectedItem.ItemName, mtrlData.MTRLPath);

                                        diffuseBMP = maps[0];
                                        specularBMP = maps[1];
                                        normalBMP = maps[2];
                                        alphaBMP = maps[3];
                                    }
                                }
                            }
                        }
                        else
                        {

                            if (mtrlData.NormalOffset != 0)
                            {
                                normalData = TEX.GetTex(mtrlData.NormalOffset, Strings.ItemsDat);
                            }

                            if (mtrlData.MaskOffset != 0)
                            {
                                maskData = TEX.GetTex(mtrlData.MaskOffset, Strings.ItemsDat);
                                //maskBMP = TexHelper.CreateBitmapSource(maskData.BMP);
                            }

                            if (mtrlData.DiffuseOffset != 0)
                            {
                                diffuseData = TEX.GetTex(mtrlData.DiffuseOffset, Strings.ItemsDat);
                                specularData = TEX.GetTex(mtrlData.SpecularOffset, Strings.ItemsDat);
                                if (mtrlData.DiffusePath.Contains("human") && !mtrlData.DiffusePath.Contains("demi"))
                                {
                                    isBody = true;

                                    var maps = TexHelper.MakeCharacterMaps(normalData, diffuseData, null, specularData, selectedItem.ItemName, mtrlData.MTRLPath);

                                    diffuseBMP = maps[0];
                                    specularBMP = maps[1];
                                    normalBMP = maps[2];
                                    alphaBMP = maps[3];
                                }
                            }

                            if (!isBody && mtrlData.SpecularOffset != 0)
                            {
                                specularData = TEX.GetTex(mtrlData.SpecularOffset, Strings.ItemsDat);

                                specularBMP = specularData.BMPSouceAlpha;
                            }

                            if (!isBody && specularData == null)
                            {
                                var maps = TexHelper.MakeModelTextureMaps(normalData, diffuseData, maskData, null, mtrlData);
                                diffuseBMP = maps[0];
                                specularBMP = maps[1];
                                normalBMP = maps[2];
                                alphaBMP = maps[3];
                                emissiveBMP = maps[4];
                            }
                            else if (!isBody && specularData != null)
                            {
                                var maps = TexHelper.MakeModelTextureMaps(normalData, diffuseData, null, specularData, mtrlData);
                                diffuseBMP = maps[0];
                                specularBMP = maps[1];
                                normalBMP = maps[2];
                                alphaBMP = maps[3];
                                emissiveBMP = maps[4];
                            }
                        }

                        specularBMP?.Freeze();
                        diffuseBMP?.Freeze();
                        normalBMP?.Freeze();
                        alphaBMP?.Freeze();
                        emissiveBMP?.Freeze();

                        var mData = new MDLTEXData()
                        {
                            Specular = specularBMP,
                            Diffuse = diffuseBMP,
                            Normal = normalBMP,
                            Alpha = alphaBMP,
                            Emissive = emissiveBMP,

                            IsBody = isBody,
                            IsFace = isFace,

                            Mesh = meshList[i]
                        };

                        meshData.Add(mData);
                    }


                    // Preserve Camera settings before reset
                    var lookDir = CompositeVM.Camera.LookDirection;
                    var upDir = CompositeVM.Camera.UpDirection;
                    var pos = CompositeVM.Camera.Position;


                    // Reset 3d View for loading new model
                    CompositeVM = new Composite3DViewModel();
                    CompositeVM.UpdateModel(meshData, selectedItem);

                    // Re-apply original camera settings 
                    // Applies when camera is not in the default position 
                    // and when within the same item category
                    if (lookDir.Z != -5 && !newCat)
                    {
                        CompositeVM.Camera.LookDirection = lookDir;
                        CompositeVM.Camera.UpDirection = upDir;
                        CompositeVM.Camera.Position = pos;
                    }

                    is3DLoaded = true;
                    newCat = false;

                    if (File.Exists(Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItem.ItemName + "/3D/" + modelName + ".DAE"))
                    {
                        Import3DEnabled = true;
                        AdvImport3DEnabled = true;
                    }
                    else
                    {
                        Import3DEnabled = false;
                        AdvImport3DEnabled = false;
                    }

                    if (Directory.Exists(Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItem.ItemName + "/3D/"))
                    {
                        Open3DEnabled = true;
                    }
                    else
                    {
                        openEnabled = false;
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
                        FlexibleMessageBox.Show("Error Accessing .modlist File \n" + ex.Message, "ModelViewModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    if (inModList)
                    {
                        var currOffset = Helper.GetDataOffset(FFCRC.GetHash(modEntry.fullPath.Substring(0, modEntry.fullPath.LastIndexOf("/"))), FFCRC.GetHash(Path.GetFileName(modEntry.fullPath)), Strings.ItemsDat);

                        if (currOffset == modEntry.modOffset)
                        {
                            ActiveToggle = FFXIV_TexTools2.Resources.Strings.Disable;
                        }
                        else if (currOffset == modEntry.originalOffset)
                        {
                            ActiveToggle = FFXIV_TexTools2.Resources.Strings.Enable;
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
                        ActiveToggle = FFXIV_TexTools2.Resources.Strings.Enable+"/"+FFXIV_TexTools2.Resources.Strings.Disable;
                    }

                    ReflectionContent = FFXIV_TexTools2.Resources.Strings.Reflection+" " + string.Format("{0:0.##}", CompositeVM.CurrentSS);

                }
                else
                {
                    CompositeVM.Rendering(SelectedMesh.Name);
                }
            } catch(Exception e)
            {
                ResetBadCompositeState();
            }
        }

        /// <summary>
        /// Gets the MTRL data of the given mesh
        /// </summary>
        /// <param name="mesh">The mesh to obtain the data from</param>
        /// <returns>The MTRLData of the given mesh</returns>
        public MTRLData MTRL3D(int mesh)
        {
            var mNum = meshList[mesh].MaterialNum;
            var typeChar = materialStrings[mNum][4].ToString() + materialStrings[mNum][9].ToString();
            var race = SelectedRace.ID;
            var modelID = selectedItem.PrimaryModelID;
            var body = "";
            var mtrlFolder = "";
            string slotAbr = null;

            if (typeChar.Contains("c"))
            {
                race = materialStrings[mNum].Substring(materialStrings[mNum].IndexOf("c") + 1, 4);
            }

            var part = materialStrings[mNum].Substring(materialStrings[mNum].LastIndexOf("_") + 1, 1);
            string itemVersion;
            if (selectedCategory.Equals("DemiHuman"))
            {
                itemVersion = IMC.GetVersion(selectedPart.Name, selectedItem, false, true).Item1;
            }
            else
            {
                itemVersion = IMC.GetVersion(selectedCategory, selectedItem, false, false).Item1;
            }

            var itemType = Helper.GetCategoryType(selectedCategory);

            var MTRLFile = materialStrings[mNum].Substring(1);

            switch (typeChar)
            {
                //equipment
                case "ce":
                    modelID = materialStrings[mNum].Substring(materialStrings[mNum].IndexOf("e") + 1, 4);
                    break;
                //accessory
                case "ca":
                    modelID = materialStrings[mNum].Substring(materialStrings[mNum].IndexOf("a") + 1, 4);
                    break;
                //weapon
                case "wb":
                    modelID = materialStrings[mNum].Substring(materialStrings[mNum].IndexOf("w") + 1, 4);
                    body = materialStrings[mNum].Substring(materialStrings[mNum].IndexOf("b") + 1, 4);
                    break;
                //body
                case "cb":

                    modelID = materialStrings[mNum].Substring(materialStrings[mNum].IndexOf("b") + 1, 4);

                    var gender = 0;
                    if (int.Parse(race.Substring(0, 2)) % 2 == 0)
                    {
                        gender = 1;
                    }

                    if (!race.Equals("0901") && !race.Equals("1001") && !race.Equals("1101"))
                    {
                        if (Properties.Settings.Default.Default_Race.Equals(Strings.Hyur_M))
                        {
                            if (gender == 0)
                            {
                                race = "0101";
                            }
                            else
                            {
                                race = "0201";
                            }
                        }
                        else if (Properties.Settings.Default.Default_Race.Equals(Strings.Hyur_H))
                        {
                            if (gender == 0)
                            {
                                race = "0301";
                            }
                            else
                            {
                                race = "0401";
                            }
                        }
                        else if (Properties.Settings.Default.Default_Race.Equals(Strings.AuRa_Raen))
                        {
                            if (gender == 0)
                            {
                                race = "1301";
                            }
                            else
                            {
                                race = "1401";
                            }
                        }
                        else if (Properties.Settings.Default.Default_Race.Equals(Strings.AuRa_Xaela))
                        {
                            if (gender == 0)
                            {
                                race = "1301";
                            }
                            else
                            {
                                race = "1401";
                            }

                            modelID = "0101";
                        }
                    }


                    mtrlFolder = string.Format(Strings.BodyMtrlFolder, race, modelID);
                    MTRLFile = string.Format(Strings.BodyMtrlFile, race, modelID);
                    break;
                //face
                case "cf":
                    modelID = materialStrings[mNum].Substring(materialStrings[mNum].IndexOf("f") + 1, 4);
                    slotAbr = materialStrings[mNum].Substring(materialStrings[mNum].LastIndexOf("_") - 3, 3);
                    slotAbr = Info.FaceTypes.FirstOrDefault(x => x.Value == slotAbr).Key;
                    break;
                //hair
                case "ch":
                    modelID = materialStrings[mNum].Substring(materialStrings[mNum].IndexOf("h") + 1, 4);
                    slotAbr = materialStrings[mNum].Substring(materialStrings[mNum].LastIndexOf("_") - 3, 3);
                    slotAbr = Info.HairTypes.FirstOrDefault(x => x.Value == slotAbr).Key;
                    break;
                //tail
                case "ct":
                    modelID = materialStrings[mNum].Substring(10, 4);
                    mtrlFolder = string.Format(Strings.TailMtrlFolder, race, modelID);
                    break;
                //monster
                case "mb":
                    modelID = materialStrings[mNum].Substring(materialStrings[mNum].IndexOf("m") + 1, 4);
                    body = materialStrings[mNum].Substring(materialStrings[mNum].IndexOf("b") + 1, 4);
                    break;
                //demihuman
                case "de":
                    race = materialStrings[mNum].Substring(materialStrings[mNum].IndexOf("d") + 1, 4);
                    modelID = materialStrings[mNum].Substring(materialStrings[mNum].IndexOf("e") + 1, 4);
                    slotAbr = Info.slotAbr[SelectedPart.Name];
                    break;
            }

            if(typeChar.Equals("ch") || typeChar.Equals("cf") || typeChar.Equals("de"))
            {
                var info = MTRL.GetMTRLDatafromType(selectedItem, race, modelID, slotAbr, itemVersion, selectedCategory, part);
                return info.Item1;
            }
            else if (typeChar.Equals("cb") || typeChar.Equals("ct"))
            {
                var info = MTRL.GetMTRLInfo(Helper.GetDataOffset(FFCRC.GetHash(mtrlFolder), FFCRC.GetHash(MTRLFile), Strings.ItemsDat), true);
                info.MTRLPath = mtrlFolder + "/" + MTRLFile;
                return info;
            }
            else
            {
                var info = MTRL.GetMTRLData(selectedItem, race, selectedCategory, part, itemVersion, body, modelID, "0000");
                return info.Item1;
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void ResetBadCompositeState()
        {
            if(CompositeVM != null && !CompositeVM.IsDead())
            {
                CompositeVM.Dispose();
            }

            CompositeVM = new Composite3DViewModel();
            FlexibleMessageBox.Show("The 3D Model Viewer was unable to display the item.", "Model Viewer Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
