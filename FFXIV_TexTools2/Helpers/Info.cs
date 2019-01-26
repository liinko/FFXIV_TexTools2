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

using FFXIV_TexTools2.Resources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace FFXIV_TexTools2.Helpers
{
    public static class Info
    {
        public static string appVersion = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion;

        public static string EXDDatNum = "0";
        public static string ModPackVersion = "1.0";

        public static string indexDir = Properties.Settings.Default.FFXIV_Directory + "/{0}.win32.index";
        public static string index2Dir = Properties.Settings.Default.FFXIV_Directory + "/{0}.win32.index2";
        public static string datDir = Properties.Settings.Default.FFXIV_Directory + "/{0}.win32.dat{1}";
        public static int modelMultiplier = 10;

        public static string modDatDir = Properties.Settings.Default.FFXIV_Directory + "/{0}.win32.dat{1}";

        public static bool otherClientSupport = true;

        public static Dictionary<string, string> ModDatDict = new Dictionary<string, string>
        {
            {Strings.ItemsDat, "4" },
            {Strings.UIDat, "1" }
        };

        public static Dictionary<string, int> ModIndexDict = new Dictionary<string, int>
        {
            {Strings.ItemsDat, 5 },
            {Strings.UIDat, 2 }
        };

        public static List<string> DAEPluginTargets = new List<string>
        {
            Strings.OpenCollada,
            Strings.AutodeskCollada
        };

        public static List<string> GearCategories = new List<string>
        {
            Strings.Main_Hand,
            Strings.Off_Hand,
            Strings.Head,
            Strings.Body,
            Strings.Hands,
            Strings.Waist,
            Strings.Legs,
            Strings.Feet,
            Strings.Ears,
            Strings.Neck,
            Strings.Wrists,
            Strings.Rings,
            Strings.Two_Handed,
            Strings.Main_Off,
            Strings.Head_Body,
            Strings.Body_Hands_Legs_Feet,
            Strings.Soul_Crystal,
            Strings.Legs_Feet,
            Strings.All,
            Strings.Body_Hands_Legs,
            Strings.Body_Legs_Feet
        };

        public static ObservableCollection<string> SubCategoryList = new ObservableCollection<string>
        {
            Strings.Items, Strings.Maps, Strings.Actions, "HUD", Strings.Status
        };

        public static ObservableCollection<string> MainCategoryList = new ObservableCollection<string>
        {
            Strings.Gear, Strings.Character, Strings.Companions, "UI"
        };

        public static ObservableCollection<string> CharCategoryList = new ObservableCollection<string>
        {
            Strings.Hair, Strings.Face, Strings.Body, Strings.Tail, Strings.Face_Paint, Strings.Equipment_Decals
        };

        public static Dictionary<string, string> raceID = new Dictionary<string, string>
        {
            {Strings.Hyur_M + " " + Strings.Male, "0101"},
            {Strings.Hyur_M + " " + Strings.Male + " NPC", "0104"},
            {Strings.Hyur_M + " " + Strings.Female, "0201"},
            {Strings.Hyur_M + " " + Strings.Female + " NPC", "0204"},
            {Strings.Hyur_H + " " + Strings.Male, "0301"},
            {Strings.Hyur_H + " " + Strings.Male + " NPC", "0304"},
            {Strings.Hyur_H + " " + Strings.Female, "0401"},
            {Strings.Hyur_H + " " + Strings.Female + " NPC", "0404"},
            {Strings.Elezen + " " + Strings.Male, "0501"},
            {Strings.Elezen + " " + Strings.Male + " NPC", "0504"},
            {Strings.Elezen + " " + Strings.Female, "0601"},
            {Strings.Elezen + " " + Strings.Female + " NPC", "0604"},
            {Strings.Miqote + " " + Strings.Male, "0701"},
            {Strings.Miqote + " " + Strings.Male + " NPC", "0704"},
            {Strings.Miqote + " " + Strings.Female, "0801"},
            {Strings.Miqote + " " + Strings.Female + " NPC", "0804"},
            {Strings.Roegadyn + " " + Strings.Male, "0901"},
            {Strings.Roegadyn + " " + Strings.Male + " NPC", "0904"},
            {Strings.Roegadyn + " " + Strings.Female, "1001"},
            {Strings.Roegadyn + " " + Strings.Female + " NPC", "1004"},
            {Strings.Lalafell + " " + Strings.Male, "1101"},
            {Strings.Lalafell + " " + Strings.Male + " NPC", "1104"},
            {Strings.Lalafell + " " + Strings.Female, "1201"},
            {Strings.Lalafell + " " + Strings.Female + " NPC", "1204"},
            {Strings.Au_Ra + " " + Strings.Male, "1301"},
            {Strings.Au_Ra + " " + Strings.Male + " NPC", "1304"},
            {Strings.Au_Ra + " " + Strings.Female, "1401"},
            {Strings.Au_Ra + " " + Strings.Female + " NPC", "1404"},
            {"NPC " + Strings.Male,  "9104"},
            {"NPC " + Strings.Female, "9204"},
            {Strings.All, Strings.All}
        };

        public static List<string> baseRace = new List<string>
        {
            {Strings.Hyur_M },
            {Strings.Hyur_H },
            {Strings.AuRa_Raen },
            {Strings.AuRa_Xaela }
        };


        public static Dictionary<string, string> IDRace = new Dictionary<string, string>
        {
            {"0101", Strings.Hyur_M + " " + Strings.Male},
            {"0104", Strings.Hyur_M + " " + Strings.Male + " NPC"},
            {"0201", Strings.Hyur_M + " " + Strings.Female},
            {"0204", Strings.Hyur_M + " " + Strings.Female + " NPC"},
            {"0301", Strings.Hyur_H + " " + Strings.Male},
            {"0304", Strings.Hyur_H + " " + Strings.Male + " NPC"},
            {"0401", Strings.Hyur_H + " " + Strings.Female},
            {"0404", Strings.Hyur_H + " " + Strings.Female + " NPC"},
            {"0501", Strings.Elezen + " " + Strings.Male},
            {"0504", Strings.Elezen + " " + Strings.Male + " NPC"},
            {"0601", Strings.Elezen + " " + Strings.Female},
            {"0604", Strings.Elezen + " " + Strings.Female + " NPC"},
            {"0701", Strings.Miqote + " " + Strings.Male},
            {"0704", Strings.Miqote + " " + Strings.Male + " NPC"},
            {"0801", Strings.Miqote + " " + Strings.Female},
            {"0804", Strings.Miqote + " " + Strings.Female + " NPC"},
            {"0901", Strings.Roegadyn + " " + Strings.Male},
            {"0904", Strings.Roegadyn + " " + Strings.Male + " NPC"},
            {"1001", Strings.Roegadyn + " " + Strings.Female},
            {"1004", Strings.Roegadyn + " " + Strings.Female + " NPC"},
            {"1101", Strings.Lalafell + " " + Strings.Male},
            {"1104", Strings.Lalafell + " " + Strings.Male + " NPC"},
            {"1201", Strings.Lalafell + " " + Strings.Female},
            {"1204", Strings.Lalafell + " " + Strings.Female + " NPC"},
            {"1301", Strings.Au_Ra + " " + Strings.Male},
            {"1304", Strings.Au_Ra + " " + Strings.Male + " NPC"},
            {"1401", Strings.Au_Ra + " " + Strings.Female},
            {"1404", Strings.Au_Ra + " " + Strings.Female + " NPC"},
            {"9104", "NPC " + Strings.Male},
            {"9204", "NPC " + Strings.Female}
        };


        public static Dictionary<string, string> slotAbr = new Dictionary<string, string>
        {
            {Strings.Head, "met"},
            {Strings.Hands, "glv"},
            {Strings.Legs, "dwn"},
            {Strings.Feet, "sho"},
            {Strings.Body, "top"},
            {Strings.Ears, "ear"},
            {Strings.Neck, "nek"},
            {Strings.Rings, "rir"},
            {Strings.RingsLeft, "ril"},
            {Strings.Wrists, "wrs"},
            {Strings.Head_Body, "top"},
            {Strings.Body_Hands, "top"},
            {Strings.Body_Hands_Legs, "top"},
            {Strings.Body_Legs_Feet, "top"},
            {Strings.Body_Hands_Legs_Feet, "top"},
            {Strings.Legs_Feet, "dwn"},
            {Strings.All, "top"}
        };

        public static Dictionary<string, int> slotID = new Dictionary<string, int>
        {
            {Strings.Main_Hand, 0},
            {Strings.Off_Hand, 0},
            {Strings.Two_Handed, 0},
            {Strings.Main_Off, 0},
            {Strings.Head, 0},
            {Strings.Body, 1},
            {Strings.Hands, 2},
            {Strings.Legs, 3},
            {Strings.Feet, 4},
            {Strings.Ears, 0},
            {Strings.Neck, 1},
            {Strings.Wrists, 2},
            {Strings.Rings, 3},
            {Strings.Head_Body, 1},
            {Strings.Body_Hands, 1},
            {Strings.Body_Hands_Legs, 1},
            {Strings.Body_Legs_Feet, 1},
            {Strings.Body_Hands_Legs_Feet, 1},
            {Strings.Legs_Feet, 3},
            {Strings.All, 1},
            {Strings.Food, 0},
            {Strings.Mounts, 0},
            {Strings.DemiHuman, 0},
            {Strings.Minions, 0},
            {Strings.Monster, 0},
            {Strings.Pets, 0}
        };

        public static Dictionary<string, string> petID = new Dictionary<string, string>
        {
            {Strings.Eos, "7001"},
            {Strings.Selene, "7001"},
            {Strings.Carbuncle, "7002"},
            {Strings.Ifrit_Egi, "7003"},
            {Strings.Titan_Egi, "7004"},
            {Strings.Garuda_Egi, "7005"},
            {Strings.Ramuh_Egi, "7006"},
            {Strings.Rook_Autoturret, "7101"},
            {Strings.Bishop_Autoturret, "7101"},
            {Strings.Sephirot_Egi, "7007" },
            {Strings.Bahamut_Egi, "7102" },
            {Strings.Placeholder_Egi, "7103" }
        };

        public static Dictionary<string, string> petIDKO = new Dictionary<string, string>
        {
            {"요정 에오스", "7001"},
            {"요정 셀레네", "7001"},
            {"카벙클", "7002"},
            {"이프리트 에기", "7003"},
            {"타이탄 에기", "7004"},
            {"가루다 에기", "7005"},
            {Strings.Ramuh_Egi, "7006"},
            {"자동포탑 룩", "7101"},
            {"자동포탑 비숍", "7101"},
            {Strings.Sephirot_Egi, "7007" },
            {Strings.Bahamut_Egi, "7102" },
            {Strings.Placeholder_Egi, "7103" }
        };

        public static Dictionary<string, string> petIDCHS = new Dictionary<string, string>
        {
            {"朝日小仙女", "7001"},
            {"夕月小仙女", "7001"},
            {"石兽", "7002"},
            {"伊弗利特之灵", "7003"},
            {"泰坦之灵", "7004"},
            {"迦楼罗之灵", "7005"},
            {Strings.Ramuh_Egi, "7006"},
            {"车式浮空炮塔", "7101"},
            {"象式浮空炮塔", "7101"},
            {Strings.Sephirot_Egi, "7007" },
            {Strings.Bahamut_Egi, "7102" },
            {Strings.Placeholder_Egi, "7103" }
        };

        public static Dictionary<string, string> FaceTypes = new Dictionary<string, string>
        {
            {Strings.Face, "fac"},
            {Strings.Iris, "iri"},
            {Strings.Etc, "etc"},
            {Strings.Accessory, "acc"}

        };

        public static Dictionary<string, string> HairTypes = new Dictionary<string, string>
        {
            {Strings.Accessory, "acc"},
            {Strings.Hair, "hir"},

        };

        public static Dictionary<string, string> IDSlot = new Dictionary<string, string>
        {
            {Strings.Character, "25"},
            {Strings.Main_Hand, "1"},
            {Strings.Off_Hand, "2"},
            {Strings.Head, "3"},
            {Strings.Body, "4"},
            {Strings.Hands, "5"},
            {Strings.Waist, "6"},
            {Strings.Legs, "7"},
            {Strings.Feet, "8"},
            {Strings.Ears, "9"},
            {Strings.Neck, "10"},
            {Strings.Wrists, "11"},
            {Strings.Rings, "12"},
            {Strings.Two_Handed, "13"},
            {Strings.Main_Off, "14"},
            {Strings.Head_Body, "15"},
            {Strings.Body_Hands_Legs_Feet, "16"},
            {Strings.Soul_Crystal, "17"},
            {Strings.Legs_Feet, "18"},
            {Strings.All, "19"},
            {Strings.Body_Hands_Legs, "20"},
            {Strings.Body_Legs_Feet, "21"},
            {Strings.Pets, "22"},
            {Strings.Mounts, "23"},
            {Strings.Minions, "24"},
            {Strings.Monster, "26"},
            {Strings.DemiHuman, "27"},
            {Strings.Food, "0"}
        };

        public static Dictionary<string, string> IDSlotName = new Dictionary<string, string>
        {
            {"25", Strings.Character },
            {"1", Strings.Main_Hand },
            {"2", Strings.Off_Hand },
            {"3", Strings.Head },
            {"4", Strings.Body },
            {"5", Strings.Hands },
            {"6", Strings.Waist },
            {"7", Strings.Legs },
            {"8", Strings.Feet },
            {"9", Strings.Ears },
            {"10", Strings.Neck },
            {"11", Strings.Wrists },
            {"12", Strings.Rings },
            {"13", Strings.Two_Handed },
            {"14", Strings.Main_Off },
            {"15", Strings.Head_Body },
            {"16", Strings.Body_Hands_Legs_Feet },
            {"17", Strings.Soul_Crystal },
            {"18", Strings.Legs_Feet },
            {"19", Strings.All },
            {"20", Strings.Body_Hands_Legs },
            {"21", Strings.Body_Legs_Feet },
            {"22", Strings.Pets },
            {"23", Strings.Mounts },
            {"24", Strings.Minions },
            {"26", Strings.Monster },
            {"27", Strings.DemiHuman },
            {"0", Strings.Food }
        };

        public static Dictionary<string, string> AttributeDict = new Dictionary<string, string>
        {
            {"none", "None" },
            {"atr_arm", "Arm"},
            {"atr_arrow", "Arrow"},
            {"atr_attach", "Attachment"},
            {"atr_hair", "Hair"},
            {"atr_hig", "Facial Hair"},
            {"atr_hij", "Lower Arm"},
            {"atr_hiz", "Upper Leg"},
            {"atr_hrn", "Horns"},
            {"atr_inr", "Neck"},
            {"atr_kam", "Hair"},
            {"atr_kao", "Face"},
            {"atr_kod", "Waist"},
            {"atr_leg", "Leg"},
            {"atr_lod", "LoD"},
            {"atr_lpd", "Feet Pads"},
            {"atr_mim", "Ear"},
            {"atr_nek", "Neck"},
            {"atr_sne", "Lower Leg"},
            {"atr_sta", "STA"},
            {"atr_tlh", "Tail Hide"},
            {"atr_tls", "Tail Show"},
            {"atr_top", "Top"},
            {"atr_ude", "Upper Arm"},
            {"atr_bv", "Body Part "},
            {"atr_dv", "Leg Part "},
            {"atr_mv", "Head Part "},
            {"atr_gv", "Hand Part "},
            {"atr_sv", "Feet Part "},
            {"atr_tv", "Top Part "},
            {"atr_fv", "Face Part "},
            {"atr_hv", "Hair Part "},
            {"atr_nv", "Neck Part "},
            {"atr_parts", "Part "},
            {"atr_rv", "RV Part "},
            {"atr_wv", "WV Part "},
            {"atr_ev", "EV Part "},
            {"atr_cn_ankle", "CN Ankle"},
            {"atr_cn_neck", "CN Neck"},
            {"atr_cn_waist", "CN Waist"},
            {"atr_cn_wrist", "CN Wrist"}
        };


        public static Dictionary<int, int> DDSType = new Dictionary<int, int>
        {
            //DXT1
            {13344, 827611204 },
            {827611204, 13344 },

            //DXT3
            {13360, 861165636 },
            {861165636, 13360 },

            //DXT5
            {13361, 894720068 },
            {894720068, 13361 },

            //ARGB 16F
            {9312, 113 },
            {113, 9312 },

            //Uncompressed RGBA
            {5200, 0 },
            {0, 5200 }

        };

        public static Dictionary<int, string> TextureTypes = new Dictionary<int, string>
        {
            {4400, "8   L" },
            {4401, "8   A" },
            {4440, "Unknown" },
            {5184, "4.4.4.4 RGB" },
            {5185, "1.5.5.5 ARGB" },
            {5200, "8.8.8.8 ARGB" },
            {5201, "X.8.8.8 XRGB" },
            {8528, "32f R" },
            {8784, "16.16f  GR" },
            {8800, "G32R32F" },
            {9312, "16.16.16.16f ABGR" },
            {9328, "32.32.32.32f ABGR" },
            {13344, "DXT1   RGB" },
            {13360, "DXT3   ARGB" },
            {13361, "DXT5   ARGB" },
            {16704, "D16" }
        };
    }
}
