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
using FFXIV_TexTools2.Material;
using FFXIV_TexTools2.Model;
using FFXIV_TexTools2.Resources;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Brushes = System.Windows.Media.Brushes;

namespace FFXIV_TexTools2.ViewModel
{
    public class ListViewModel : INotifyPropertyChanged
    {
        ObservableCollection<ModListModel> mlmList = new ObservableCollection<ModListModel>();
        bool _isSelected;

        public ListViewModel(string selectedItem)
        {
            try
            {
                foreach (string line in File.ReadAllLines(Properties.Settings.Default.Modlist_Directory))
                {
                    JsonEntry entry = JsonConvert.DeserializeObject<JsonEntry>(line);

                    if (entry.name.Equals(selectedItem))
                    {
                        mlmList.Add(ParseEntry(entry));
                    }
                }
            }
            catch (Exception e)
            {
                FlexibleMessageBox.Show("Error Accessing .modlist File \n" + e.Message, "ListViewModel Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Debug.WriteLine(e.StackTrace);
            }


        }

        public ObservableCollection<ModListModel> ModList
        {
            get { return mlmList; }
        }

        public ModListModel ParseEntry(JsonEntry entry)
        {
            ModListModel mlm = new ModListModel();
            string race, map, part, type;

            if (entry.fullPath.Contains("mt_"))
            {
                Debug.WriteLine(entry.fullPath);
            }

            if (entry.fullPath.Contains("weapon") || entry.fullPath.Contains("accessory") || entry.fullPath.Contains("decal") || entry.fullPath.Contains("vfx") || entry.fullPath.Contains("ui/"))
            {
                race = Strings.All;
            }
            else if (entry.fullPath.Contains("monster") || entry.fullPath.Contains("demihuman"))
            {
                race = Strings.Monster;
            }
            else
            {
                race = entry.fullPath.Substring(entry.fullPath.LastIndexOf('/'));
                if(entry.fullPath.Contains("mt_") && entry.fullPath.Contains("_acc_"))
                {
                    race = race.Substring(race.IndexOf("_") + 2, 4);
                }
                else if ((entry.fullPath.Contains("_fac_") || entry.fullPath.Contains("_etc_") || entry.fullPath.Contains("_acc_")) && Properties.Settings.Default.DX_Ver.Equals(Strings.DX11))
                {
                    race = race.Substring(race.LastIndexOf("--c") + 3, 4);
                }
                else if (entry.fullPath.Contains("_fac_") || entry.fullPath.Contains("_etc_") || entry.fullPath.Contains("_acc_"))
                {
                    race = race.Substring(race.LastIndexOf("/c") + 2, 4);

                }
                else if (entry.fullPath.Contains("_c_"))
                {
                    race = race.Substring(race.IndexOf("_c") + 2, 4);
                }
                else
                {
                    if (entry.fullPath.Contains(".mdl") && entry.fullPath.Contains("_fac"))
                    {
                        race = race.Substring(race.IndexOf('c') + 1, 4);
                    }
                    else
                    {
                        race = race.Substring(race.LastIndexOf('c') + 1, 4);
                    }
                }

                if (entry.fullPath.Contains("mt_"))
                {
                    Debug.WriteLine(race + "\n");
                }

                race = Info.IDRace[race];
            }

            mlm.Race = race;


            if (entry.fullPath.Contains("_d."))
            {
                map = Strings.Diffuse;
            }
            else if (entry.fullPath.Contains("_n."))
            {
                map = Strings.Normal;
            }
            else if (entry.fullPath.Contains("_s."))
            {
                map = Strings.Specular;
            }
            else if (entry.fullPath.Contains("material"))
            {
                map = Strings.ColorSet;
            }
            else if (entry.fullPath.Contains("model"))
            {
                map = "3D";
            }
            else if (entry.fullPath.Contains("ui/"))
            {
                map = "UI";
            }
            else
            {
                map = Strings.Mask;
            }

            mlm.Map = map;


            if (entry.fullPath.Contains("_b_"))
            {
                part = "b";
            }
            else if (entry.fullPath.Contains("_c_"))
            {
                part = "c";
            }
            else if (entry.fullPath.Contains("_d_"))
            {
                part = "d";
            }
            else if (entry.fullPath.Contains("decal"))
            {
                part = entry.fullPath.Substring(entry.fullPath.LastIndexOf('_') + 1, entry.fullPath.LastIndexOf('.') - (entry.fullPath.LastIndexOf('_') + 1));
            }
            else
            {
                part = "a";
            }

            mlm.Part = part;


            if (entry.fullPath.Contains("_iri_"))
            {
                type = "Iris";
            }
            else if (entry.fullPath.Contains("_etc_"))
            {
                type = "Etc.";
            }
            else if (entry.fullPath.Contains("_fac_"))
            {
                type = "Face";
            }
            else if (entry.fullPath.Contains("_hir_"))
            {
                type = "Hair";
            }
            else if (entry.fullPath.Contains("_acc_"))
            {
                type = "Accessory";
            }
            else if (entry.fullPath.Contains("demihuman"))
            {
                type = entry.fullPath.Substring(entry.fullPath.LastIndexOf('_') - 3, 3);
                type = (Info.slotAbr).FirstOrDefault(x => x.Value == type).Key;
            }
            else
            {
                type = "-";
            }

            mlm.Type = type;

            if (entry.fullPath.Contains("material"))
            {
                var info = MTRL.GetMTRLInfo(entry.modOffset, false);

                using (var bitmap = TEX.ColorSetToBitmap(info.ColorData))
                    mlm.BMP = TexHelper.CreateBitmapSource(bitmap);
                mlm.BMP.Freeze();
            }
            else if (entry.fullPath.Contains("model"))
            {
                mlm.BMP = new BitmapImage(new Uri("pack://application:,,,/FFXIV TexTools 2;component/Resources/3DModel.png"));
            }
            else
            {
                TEXData texData;

                if (entry.fullPath.Contains("vfx"))
                {
                    texData = TEX.GetVFX(entry.modOffset, entry.datFile);
                }
                else
                {
                    if (entry.fullPath.Contains("icon"))
                    {
                        texData = TEX.GetTex(entry.modOffset, entry.datFile);
                    }
                    else
                    {
                        texData = TEX.GetTex(entry.modOffset, entry.datFile);
                    }
                }

                var scale = 1;

                if (texData.Width >= 4096 || texData.Height >= 4096)
                {
                    scale = 16;
                }
                else if (texData.Width >= 2048 || texData.Height >= 2048)
                {
                    scale = 8;
                }
                else if (texData.Width >= 1024 || texData.Height >= 1024)
                {
                    scale = 4;
                }

                var nWidth = texData.Width / scale;
                var nHeight = texData.Height / scale;

                var resizedImage = TexHelper.CreateResizedImage(texData.BMPSouceNoAlpha, nWidth, nHeight);
                mlm.BMP = (BitmapSource)resizedImage;
                mlm.BMP.Freeze();
            }

            var offset = Helper.GetDataOffset(FFCRC.GetHash(entry.fullPath.Substring(0, entry.fullPath.LastIndexOf("/"))), FFCRC.GetHash(Path.GetFileName(entry.fullPath)), entry.datFile);

            if(offset == entry.modOffset)
            {
                mlm.ActiveBorder = Brushes.Green;
                mlm.Active = Brushes.Transparent;
                mlm.ActiveOpacity = 1;
            }
            else if(offset == entry.originalOffset)
            {
                mlm.ActiveBorder = Brushes.Red;
                mlm.Active = Brushes.Gray;
                mlm.ActiveOpacity = 0.5f;
            }
            else
            {
                mlm.ActiveBorder = Brushes.Red;
                mlm.Active = Brushes.Red;
                mlm.ActiveOpacity = 1;
            }

            mlm.Entry = entry;

            return mlm;
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    this.OnPropertyChanged("IsSelected");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }
}
