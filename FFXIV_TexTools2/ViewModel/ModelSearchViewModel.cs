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
using FFXIV_TexTools2.Resources;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;

namespace FFXIV_TexTools2.ViewModel
{
    public class ModelSearchViewModel : INotifyPropertyChanged
    {
        string searchText, partBodyHeader, mtrlPath, progressLabel;
        ComboBoxInfo selectedType;
        SearchItems selectedItem;
        ObservableCollection<SearchItems> resultList = new ObservableCollection<SearchItems>();
        int typeIndex, progressValue;
        bool openEnabled;

        MainViewModel parentVM;

        private ObservableCollection<ComboBoxInfo> typeComboInfo = new ObservableCollection<ComboBoxInfo>();

        public string ModelSearchText { get { return searchText; } set { searchText = value; } }
        public string PartBodyHeader { get { return partBodyHeader; } set { partBodyHeader = value; } }
        public string MTRLPath { get { return mtrlPath; } set { mtrlPath = value; NotifyPropertyChanged("MTRLPath"); } }
        public string ProgressLabel { get { return progressLabel; } set { progressLabel = value; NotifyPropertyChanged("ProgressLabel"); } }
        public int TypeIndex { get { return typeIndex; } set { typeIndex = value; } }
        public int ProgressValue { get { return progressValue; } set { progressValue = value; NotifyPropertyChanged("ProgressValue"); } }
        public ObservableCollection<SearchItems> ResultList { get { return resultList; } set { resultList = value; NotifyPropertyChanged("ResultList"); } }
        public bool OpenEnabled { get { return openEnabled; } set { openEnabled = value; NotifyPropertyChanged("OpenEnabled"); } }

        Dictionary<string, string> equipSlotDict = new Dictionary<string, string>()
        {
            {"met", Strings.Head },
            {"glv", Strings.Hands },
            {"dwn", Strings.Legs },
            {"sho", Strings.Feet },
            {"top", Strings.Body }
        };

        Dictionary<string, string> accSlotDict = new Dictionary<string, string>()
        {
            {"ear", Strings.Ears },
            {"nek", Strings.Neck },
            {"rir", Strings.Rings },
            {"wrs", Strings.Wrists }
        };

        public ModelSearchViewModel(MainViewModel parent)
        {
            parentVM = parent;
            typeComboInfo.Add(new ComboBoxInfo() { Name = "Equipment", ID = "Equipment" });
            typeComboInfo.Add(new ComboBoxInfo() { Name = "Weapon", ID = "Weapon" });
            typeComboInfo.Add(new ComboBoxInfo() { Name = "Accessory", ID = "Accessory" });
            typeComboInfo.Add(new ComboBoxInfo() { Name = "Monster", ID = "Monster" });
            typeComboInfo.Add(new ComboBoxInfo() { Name = "DemiHuman", ID = "DemiHuman" });


            TypeIndex = 0;
        }


        /// <summary>
        /// Command for the Search Button
        /// </summary>
        public ICommand ModelSearchCommand
        {
            get { return new RelayCommand(ModelSearch); }
        }

        /// <summary>
        /// Command for the Open Button
        /// </summary>
        public ICommand OpenCommand
        {
            get { return new RelayCommand(Open); }
        }

        /// <summary>
        /// The ItemSource binding of the combobox which is to contain the list of available races for the selected item
        /// </summary>
        public ObservableCollection<ComboBoxInfo> TypeComboBox
        {
            get { return typeComboInfo; }
            set
            {
                if (typeComboInfo != null)
                {
                    typeComboInfo = value;
                    NotifyPropertyChanged("TypeComboBox");
                }
            }
        }

        /// <summary>
        /// The SelectedItem binding of the combobox containing available races
        /// </summary>
        public ComboBoxInfo SelectedType
        {
            get { return selectedType; }
            set
            {
                if (value.Name != null)
                {
                    selectedType = value;
                }

            }
        }

        public SearchItems SelectedItem
        {
            get { return selectedItem; }
            set
            {
                if(value != null)
                {
                    selectedItem = value;
                    NotifyPropertyChanged("SelectedItem");
                    SelectedInfo();
                }
            }
        }

        public void Open(object obj)
        {
            ItemData itemData = new ItemData()
            {
                ItemName = searchText,
                ItemCategory = SelectedItem.SlotID,
                PrimaryModelID = searchText.PadLeft(4, '0'),
                PrimaryModelBody = SelectedItem.Body.PadLeft(4, '0'),
                PrimaryModelVariant = SelectedItem.Variant.PadLeft(4, '0'),
                PrimaryMTRLFolder = MTRLPath.Substring(0, MTRLPath.LastIndexOf("v") + 1)
            };

            string category = SelectedItem.Slot;
            if (SelectedType.Name.Equals(Strings.Weapon))
            {
                category = Strings.Main_Hand;
            }


            parentVM.OpenID(itemData, SelectedItem.RaceID, category, SelectedItem.Part, SelectedItem.Variant.PadLeft(4, '0'));
        }

        public void SelectedInfo()
        {
            string folder = Strings.EquipMtrlFolder;
            string file = Strings.EquipMtrlFile;

            if (SelectedType.Name.Equals(Strings.Weapon))
            {
                folder = Strings.WeapMtrlFolder;
                file = Strings.WeapMtrlFile;
            }
            else if (SelectedType.Name.Equals(Strings.Accessory))
            {
                folder = Strings.AccMtrlFolder;
                file = Strings.AccMtrlFile;
            }
            else if (SelectedType.Name.Equals(Strings.Monster))
            {
                folder = Strings.MonsterMtrlFolder;
                file = Strings.MonsterMtrlFile;
            }
            else if (SelectedType.Name.Equals("DemiHuman"))
            {
                folder = Strings.DemiMtrlFolder;
                file = Strings.DemiMtrlFile;
            }

            string MTRLFolder = "";
            string MTRLFile = "";
            if (SelectedType.Name.Equals(Strings.Equipment) || SelectedType.Name.Equals(Strings.Accessory))
            {
                MTRLFolder = string.Format(folder, searchText.PadLeft(4, '0')) + SelectedItem.Variant.PadLeft(4, '0');
                if (SelectedType.Name.Equals(Strings.Accessory))
                {
                    MTRLFile = string.Format(file, searchText.PadLeft(4, '0'), SelectedItem.SlotAbr, SelectedItem.Part);
                }
                else
                {
                    MTRLFile = string.Format(file, SelectedItem.RaceID, searchText.PadLeft(4, '0'), SelectedItem.SlotAbr, SelectedItem.Part);
                }
            }
            else
            {
                MTRLFolder = string.Format(folder, searchText.PadLeft(4, '0'), SelectedItem.Body.PadLeft(4, '0')) + SelectedItem.Variant.PadLeft(4, '0');
                MTRLFile = string.Format(file, searchText.PadLeft(4, '0'), SelectedItem.Body.PadLeft(4, '0'), SelectedItem.Part);
            }

            MTRLPath = MTRLFolder + "/" + MTRLFile;

            OpenEnabled = true;
        }

        public void ModelSearch(object obj)
        {
            resultList.Clear();
            if(searchText != null && searchText.Length > 0)
            {
                BackgroundWorker bw = new BackgroundWorker();
                bw.WorkerReportsProgress = true;
                bw.DoWork += new DoWorkEventHandler(Bw_DoWork);
                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Bw_RunWorkerCompleted);
                bw.ProgressChanged += new ProgressChangedEventHandler(Bw_ProgressChanged);
                bw.RunWorkerAsync();           
            }
        }

        private void Bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressValue = e.ProgressPercentage;
        }

        private void Bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var list = e.Result as List<SearchItems>;

            ResultList = new ObservableCollection<SearchItems>(list);
        }

        private void Bw_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            List<SearchItems> workList = new List<SearchItems>();

            string[] eqSlots = new string[] { "met", "glv", "dwn", "sho", "top",  };
            string[] acSlots = new string[] { "ear", "nek", "rir", "wrs" };
            string[] parts = new string[] { "a", "b", "c", "d" };
            List<int> variantList = new List<int>();
            List<int> bodyList = new List<int>();
            Dictionary<int, List<string>> slotList = new Dictionary<int, List<string>>();

            string folder = Strings.EquipMtrlFolder;
            string file = Strings.EquipMtrlFile;

            if (SelectedType.Name.Equals(Strings.Weapon))
            {
                folder = Strings.WeapMtrlFolder;
                file = Strings.WeapMtrlFile;
            }
            else if (SelectedType.Name.Equals(Strings.Accessory))
            {
                folder = Strings.AccMtrlFolder;
                file = Strings.AccMtrlFile;
            }
            else if (SelectedType.Name.Equals(Strings.Monster))
            {
                folder = Strings.MonsterMtrlFolder;
                file = Strings.MonsterMtrlFile;
            }
            else if (SelectedType.Name.Equals("DemiHuman"))
            {
                folder = Strings.DemiMtrlFolder;
                file = Strings.DemiMtrlFile;
            }


            if (SelectedType.Name.Equals(Strings.Equipment) || SelectedType.Name.Equals(Strings.Accessory))
            {
                for (int i = 0; i <= 200; i++)
                {
                    var folderCheck = string.Format(folder, searchText.PadLeft(4, '0')) + i.ToString().PadLeft(4, '0');

                    if (Helper.FolderExists(FFCRC.GetHash(folderCheck), Strings.ItemsDat))
                    {
                        variantList.Add(i);
                    }
                    worker.ReportProgress(i / 2);
                    ProgressLabel = "Variant: " + i;
                }
                int var = 0;

                if(variantList.Count > 0)
                {
                    foreach (int v in variantList)
                    {
                        slotList.Add(v, new List<string>());
                        var eFolder = string.Format(folder, searchText.PadLeft(4, '0')) + v.ToString().PadLeft(4, '0');

                        var files = Helper.GetAllFilesInFolder(FFCRC.GetHash(eFolder), Strings.ItemsDat);

                        if (SelectedType.Name.Equals(Strings.Accessory))
                        {
                            foreach (var s in acSlots)
                            {
                                foreach (var p in parts)
                                {
                                    var aFile = string.Format(file, searchText.PadLeft(4, '0'), s, p);

                                    var fileHash = FFCRC.GetHash(aFile);
                                    if (files.Contains(fileHash))
                                    {
                                        workList.Add(new SearchItems() { Race = Info.IDRace["0101"], RaceID = "0101", Slot = accSlotDict[s], SlotAbr = s, SlotID = Info.IDSlot[accSlotDict[s]], Body = "-", Variant = v.ToString(), Part = p });
                                    }

                                    ProgressLabel = "Slot: " + s + "Part: " + p;
                                }
                            }
                        }
                        else
                        {
                            foreach (var r in Info.IDRace.Keys)
                            {
                                foreach (var s in eqSlots)
                                {
                                    foreach (var p in parts)
                                    {
                                        var eFile = string.Format(file, r, searchText.PadLeft(4, '0'), s, p);

                                        var fileHash = FFCRC.GetHash(eFile);
                                        if (files.Contains(fileHash))
                                        {
                                            if (!slotList[v].Contains(s))
                                            {
                                                slotList[v].Add(s);
                                                workList.Add(new SearchItems() { Race = Info.IDRace[r], RaceID = r, Slot = equipSlotDict[s], SlotAbr = s, SlotID = Info.IDSlot[equipSlotDict[s]], Body = "-", Variant = v.ToString(), Part = p });
                                            }
                                        }
                                        ProgressLabel = "Race: " + r + " Slot: " + s + "Part: " + p;
                                    }
                                }
                            }
                        }
                        int prog = (int)(((double)var / Info.IDRace.Count) * 100f);
                        worker.ReportProgress(prog);
                        var++;
                    }
                }
                else
                {
                    folder = Strings.EquipMDLFolder;
                    file = Strings.EquipMDLFile;

                    if (SelectedType.Name.Equals(Strings.Accessory))
                    {
                        folder = Strings.AccMDLFolder;
                        file = Strings.AccMDLFile;
                    }

                    var folderCheck = string.Format(folder, searchText.PadLeft(4, '0'));

                    if (Helper.FolderExists(FFCRC.GetHash(folderCheck), Strings.ItemsDat))
                    {
                        variantList.Add(1);
                    }

                    slotList.Add(1, new List<string>());

                    var files = Helper.GetAllFilesInFolder(FFCRC.GetHash(folderCheck), Strings.ItemsDat);

                    if (SelectedType.Name.Equals(Strings.Accessory))
                    {
                        foreach (var s in acSlots)
                        {
                            var aFile = string.Format(file, "0101", searchText.PadLeft(4, '0'), s);

                            var fileHash = FFCRC.GetHash(aFile);
                            if (files.Contains(fileHash))
                            {
                                workList.Add(new SearchItems() { Race = Info.IDRace["0101"], RaceID = "0101", Slot = accSlotDict[s], SlotAbr = s, SlotID = Info.IDSlot[accSlotDict[s]], Body = "-", Variant = "1", Part = "a" });
                            }

                            ProgressLabel = "Slot: " + s;
                        }
                    }
                    else
                    {
                        foreach (var r in Info.IDRace.Keys)
                        {
                            foreach (var s in eqSlots)
                            {
                                var eFile = string.Format(file, r, searchText.PadLeft(4, '0'), s);

                                var fileHash = FFCRC.GetHash(eFile);
                                if (files.Contains(fileHash))
                                {
                                    workList.Add(new SearchItems() { Race = Info.IDRace[r], RaceID = r, Slot = equipSlotDict[s], SlotAbr = s, SlotID = Info.IDSlot[equipSlotDict[s]], Body = "-", Variant = "1", Part = "a" });
                                }
                                ProgressLabel = "Race: " + r + " Slot: " + s ;
                            }
                        }
                    }

                }


                ProgressLabel = "Found: " + workList.Count;
            }
            else
            {
                if (!SelectedType.Name.Equals("DemiHuman"))
                {
                    string slotName = Strings.Main_Hand;

                    if (SelectedType.Name.Equals(Strings.Monster))
                    {
                        slotName = Strings.Mounts;
                    }



                    for (int i = 0; i <= 50; i++)
                    {
                        var folderCheck = string.Format(folder, searchText.PadLeft(4, '0'), i.ToString().PadLeft(4, '0')) + "0001";

                        if (Helper.FolderExists(FFCRC.GetHash(folderCheck), Strings.ItemsDat))
                        {
                            bodyList.Add(i);
                        }
                        ProgressLabel = "Body: " + i;
                        worker.ReportProgress(i * 2);
                    }

                    for (int i = 0; i < bodyList.Count; i++)
                    {
                        for (int j = 0; j <= 20; j++)
                        {
                            var wmFolder = string.Format(folder, searchText.PadLeft(4, '0'), bodyList[i].ToString().PadLeft(4, '0')) + j.ToString().PadLeft(4, '0');

                            var files = Helper.GetAllFilesInFolder(FFCRC.GetHash(wmFolder), Strings.ItemsDat);

                            foreach (var p in parts)
                            {
                                var wmFile = string.Format(file, searchText.PadLeft(4, '0'), bodyList[i].ToString().PadLeft(4, '0'), p);

                                var fileHash = FFCRC.GetHash(wmFile);
                                if (files.Contains(fileHash))
                                {
                                    workList.Add(new SearchItems() { Race = "-", Slot = slotName, SlotID = Info.IDSlot[slotName], Body = bodyList[i].ToString(), Variant = j.ToString(), Part = p });
                                }

                                ProgressLabel = "Body: " + bodyList[i] + " Variant: " + j + " Part: " + p;
                            }
                        }

                        int prog = (int)(((double)(i + 1) / bodyList.Count) * 100f);
                        worker.ReportProgress(prog);
                    }

                    ProgressLabel = "Found: " + workList.Count;
                }
                else
                {
                    string slotName = "DemiHuman";

                    for (int i = 0; i <= 100; i++)
                    {
                        var folderCheck = string.Format(folder, searchText.PadLeft(4, '0'), i.ToString().PadLeft(4, '0')) + "0001";

                        if (Helper.FolderExists(FFCRC.GetHash(folderCheck), Strings.ItemsDat))
                        {
                            bodyList.Add(i);
                        }
                        ProgressLabel = "Equipment: " + i;
                        worker.ReportProgress(i * 2);
                    }

                    for (int i = 0; i < bodyList.Count; i++)
                    {
                        for (int j = 0; j <= 20; j++)
                        {
                            var wmFolder = string.Format(folder, searchText.PadLeft(4, '0'), bodyList[i].ToString().PadLeft(4, '0')) + j.ToString().PadLeft(4, '0');

                            var files = Helper.GetAllFilesInFolder(FFCRC.GetHash(wmFolder), Strings.ItemsDat);

                            foreach (var eq in eqSlots)
                            {
                                var wmFile = string.Format(file, searchText.PadLeft(4, '0'), bodyList[i].ToString().PadLeft(4, '0'), eq);

                                var fileHash = FFCRC.GetHash(wmFile);
                                if (files.Contains(fileHash))
                                {
                                    workList.Add(new SearchItems() { Race = "-", Slot = equipSlotDict[eq], SlotID = Info.IDSlot[equipSlotDict[eq]], Body = bodyList[i].ToString(), Variant = j.ToString(), Part = eq });
                                }

                                ProgressLabel = "Body: " + bodyList[i] + " Variant: " + j;
                            }
                        }

                        int prog = (int)(((double)(i + 1) / bodyList.Count) * 100f);
                        worker.ReportProgress(prog);
                    }

                    ProgressLabel = "Found: " + workList.Count;
                }

            }
            e.Result = workList;
        }

        public class SearchItems
        {
            public string Race { get; set; }
            public string RaceID { get; set; }
            public string Slot { get; set; }
            public string SlotAbr { get; set; }
            public string SlotID { get; set; }
            public string Body { get; set; }
            public string Variant { get; set; }
            public string Part { get; set; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
