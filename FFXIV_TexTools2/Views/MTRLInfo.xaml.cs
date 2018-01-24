using FFXIV_TexTools2.Helpers;
using FFXIV_TexTools2.Material;
using FFXIV_TexTools2.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FFXIV_TexTools2.Views
{
    /// <summary>
    /// Interaction logic for MTRLInfo.xaml
    /// </summary>
    public partial class MTRLInfo : Window
    {

        List<Struct1> struct1 = new List<Struct1>();
        List<Struct2> struct2 = new List<Struct2>();
        Params par = new Params();


        public MTRLInfo(string MTRLPath)
        {
            InitializeComponent();

            var MTRLData = MTRL.GetRawMTRL(MTRLPath);

            using (BinaryReader br = new BinaryReader(new MemoryStream(MTRLData)))
            {
                br.BaseStream.Seek(4, SeekOrigin.Begin);

                FileSizeOffset.Content = br.BaseStream.Position;
                var fileSize = br.ReadUInt16();
                FileSizeData.Content = fileSize;

                ColorSetOffset.Content = br.BaseStream.Position;
                var colorDataSize = br.ReadUInt16();
                ColorSetData.Content = colorDataSize;

                MaterialSizeOffset.Content = br.BaseStream.Position;
                var textureNameSize = br.ReadUInt16();
                MaterialSizeData.Content = textureNameSize;

                PathSizeOffset.Content = br.BaseStream.Position;
                var pathSize = br.ReadUInt16();
                PathSizeData.Content = pathSize;

                TextureCountOffset.Content = br.BaseStream.Position;
                byte numOfTextures = br.ReadByte();
                TextureCountData.Content = numOfTextures;

                MapCountOffset.Content = br.BaseStream.Position;
                byte numOfMaps = br.ReadByte();
                MapCountData.Content = numOfMaps;

                ColorSetCountOffset.Content = br.BaseStream.Position;
                byte numOfColorSets = br.ReadByte();
                ColorSetCountData.Content = numOfColorSets;

                UnknownOffset.Content = br.BaseStream.Position;
                byte unknown = br.ReadByte();
                UnknownData.Content = unknown;

                int headerEnd = 16 + ((numOfTextures + numOfMaps + numOfColorSets) * 4);

                int[] texPathOffsets = new int[numOfTextures + 1];

                for (int i = 0; i < numOfTextures + 1; i++)
                {
                    texPathOffsets[i] = br.ReadUInt16();
                    br.ReadBytes(2);
                }

                br.ReadBytes((numOfMaps - 1) * 4);

                string pathStrings = "";

                for (int i = 0; i < numOfTextures; i++)
                {
                    br.BaseStream.Seek(headerEnd + texPathOffsets[i], SeekOrigin.Begin);

                    string fullPath = Encoding.ASCII.GetString(br.ReadBytes(texPathOffsets[i + 1] - texPathOffsets[i])).Replace("\0", "");

                    string fileName = fullPath.Substring(fullPath.LastIndexOf("/") + 1);

                    //if (Properties.Settings.Default.DX_Ver.Equals(Strings.DX11))
                    //{
                    //    if (textureNameSize > 50)
                    //    {
                    //        fileName = fileName.Insert(0, "--");
                    //    }
                    //}

                    pathStrings += fileName + "\n";
                }

                var pos = br.BaseStream.Position;

                var remaining = numOfMaps + numOfColorSets + 1;

                for(int i = 0; i < remaining; i++)
                {
                    List<byte> bList = new List<byte>();
                    byte b = 0;
                    while((b = br.ReadByte()) != 0)
                    {
                        bList.Add(b);
                    }

                    string fullPath = Encoding.ASCII.GetString(bList.ToArray()).Replace("\0", "");

                    string fileName = fullPath.Substring(fullPath.LastIndexOf("/") + 1);

                    pathStrings += fileName + "\n";
                }

                StringsTextBlock.Text = pathStrings;

                br.BaseStream.Seek((16 + (numOfTextures * 4) + (numOfMaps * 4) + (numOfColorSets * 4) + textureNameSize + 4), SeekOrigin.Begin);

                if (numOfColorSets > 0 && colorDataSize > 0)
                {
                    br.ReadBytes(colorDataSize);
                }


                DataSizeOffset.Content = br.BaseStream.Position;
                int dataSize = br.ReadUInt16();
                DataSizeData.Content = dataSize;

                Struct1Offset.Content = br.BaseStream.Position;
                int struct1Size = br.ReadUInt16();
                Struct1Data.Content = struct1Size;

                Struct2Offset.Content = br.BaseStream.Position;
                int struct2Size = br.ReadUInt16();
                Struct2Data.Content = struct2Size;

                ParamOffset.Content = br.BaseStream.Position;
                ParamData.Content = br.ReadUInt16();

                Unk1Offset.Content = br.BaseStream.Position;
                Unk1Data.Content = br.ReadUInt16();

                Unk2Offset.Content = br.BaseStream.Position;
                Unk2Data.Content = br.ReadUInt16();

                List<string> structList = new List<string>();

                for (int i = 0; i < struct1Size; i++)
                {
                    structList.Add("struct1 part " + (i + 1));
                }


                for (int i = 0; i < struct2Size; i++)
                {
                    structList.Add("struct2 part " + (i + 1));
                }

                structList.Add("parameters");

                DataComboBox.ItemsSource = structList;



                for (int i = 0; i < struct1Size; i++)
                {
                    Struct1 st1 = new Struct1();
                    st1.IDOffset = br.BaseStream.Position;
                    st1.ID = br.ReadUInt32();

                    st1.Unknown1Offset = br.BaseStream.Position;
                    st1.Unknown1 = br.ReadUInt32();

                    struct1.Add(st1);
                }

                for (int i = 0; i < struct2Size; i++)
                {
                    Struct2 st2 = new Struct2();
                    st2.IDOffset = br.BaseStream.Position;
                    st2.ID = br.ReadUInt32();

                    st2.offsetOffset = br.BaseStream.Position;
                    st2.Offset = br.ReadUInt16();

                    st2.sizeOffset = br.BaseStream.Position;
                    st2.Size = br.ReadUInt16();

                    struct2.Add(st2);
                }

                par.IDOffset = br.BaseStream.Position;
                par.ID = br.ReadUInt32();

                par.Unknown1Offset = br.BaseStream.Position;
                par.Unknown1 = br.ReadUInt16();

                par.Unknown2Offset = br.BaseStream.Position;
                par.Unknown2 = br.ReadUInt16();

                par.TextureIndexOffset = br.BaseStream.Position;
                par.TextureIndex = br.ReadUInt32();

                string data = "";

                for (int i = 0; i < dataSize; i++)
                {
                    int b = br.ReadByte();

                    data += b.ToString("X").PadLeft(2, '0') + " ";
                }

                DataTextBlock.Text = data;
            }

            if (DataComboBox.HasItems)
            {
                DataComboBox.SelectedIndex = 0;
            }

        }

        public class Struct1
        {
            public long IDOffset;
            public uint ID;

            public long Unknown1Offset;
            public uint Unknown1;
        }

        public class Struct2
        {
            public long IDOffset;
            public uint ID;

            public long offsetOffset;
            public uint Offset;

            public long sizeOffset;
            public uint Size;
        }

        public class Params
        {
            public long IDOffset;
            public uint ID;

            public long Unknown1Offset;
            public uint Unknown1;

            public long Unknown2Offset;
            public uint Unknown2;

            public long TextureIndexOffset;
            public uint TextureIndex;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            int fsd = int.Parse(FileSizeData.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
            FileSizeData.Content = fsd;
            int fso = int.Parse(FileSizeOffset.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
            FileSizeOffset.Content = fso;

            int csd = int.Parse(ColorSetData.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
            ColorSetData.Content = csd;
            int cso = int.Parse(ColorSetOffset.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
            ColorSetOffset.Content = cso;

            int msd = int.Parse(MaterialSizeData.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
            MaterialSizeData.Content = msd;
            int mso = int.Parse(MaterialSizeOffset.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
            MaterialSizeOffset.Content = mso;

            int psd = int.Parse(PathSizeData.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
            PathSizeData.Content = psd;
            int pso = int.Parse(PathSizeOffset.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
            PathSizeOffset.Content = pso;

            int tcd = int.Parse(TextureCountData.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
            TextureCountData.Content = tcd;
            int tco = int.Parse(TextureCountOffset.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
            TextureCountOffset.Content = tco;

            int mcd = int.Parse(MapCountData.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
            MapCountData.Content = mcd;
            int mco = int.Parse(MapCountOffset.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
            MapCountOffset.Content = mco;

            int cscd = int.Parse(ColorSetCountData.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
            ColorSetCountData.Content = cscd;
            int csco = int.Parse(ColorSetCountOffset.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
            ColorSetCountOffset.Content = csco;

            int ukd = int.Parse(UnknownData.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
            UnknownData.Content = ukd;
            int uko = int.Parse(UnknownOffset.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
            UnknownOffset.Content = uko;


            int dsd = int.Parse(DataSizeData.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
            DataSizeData.Content = dsd;
            int dso = int.Parse(DataSizeOffset.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
            DataSizeOffset.Content = dso;

            int s1d = int.Parse(Struct1Data.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
            Struct1Data.Content = s1d;
            int s1o = int.Parse(Struct1Offset.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
            Struct1Offset.Content = s1o;

            int s2d = int.Parse(Struct2Data.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
            Struct2Data.Content = s2d;
            int s2o = int.Parse(Struct2Offset.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
            Struct2Offset.Content = s2o;

            int pd = int.Parse(ParamData.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
            ParamData.Content = pd;
            int po = int.Parse(ParamOffset.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
            ParamOffset.Content = po;

            int uk1d = int.Parse(Unk1Data.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
            Unk1Data.Content = uk1d;
            int uk1o = int.Parse(Unk1Offset.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
            Unk1Offset.Content = uk1o;

            int uk2d = int.Parse(Unk2Data.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
            Unk2Data.Content = uk2d;
            int uk2o = int.Parse(Unk2Offset.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
            Unk2Offset.Content = uk2o;

            DataComboBox.SelectedIndex = 0;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            int fsd = int.Parse(FileSizeData.Content.ToString());
            FileSizeData.Content = fsd.ToString("X");
            int fso = int.Parse(FileSizeOffset.Content.ToString());
            FileSizeOffset.Content = fso.ToString("X");

            int csd = int.Parse(ColorSetData.Content.ToString());
            ColorSetData.Content = csd.ToString("X");
            int cso = int.Parse(ColorSetOffset.Content.ToString());
            ColorSetOffset.Content = cso.ToString("X");

            int msd = int.Parse(MaterialSizeData.Content.ToString());
            MaterialSizeData.Content = msd.ToString("X");
            int mso = int.Parse(MaterialSizeOffset.Content.ToString());
            MaterialSizeOffset.Content = mso.ToString("X");

            int psd = int.Parse(PathSizeData.Content.ToString());
            PathSizeData.Content = psd.ToString("X");
            int pso = int.Parse(PathSizeOffset.Content.ToString());
            PathSizeOffset.Content = pso.ToString("X");

            int tcd = int.Parse(TextureCountData.Content.ToString());
            TextureCountData.Content = tcd.ToString("X");
            int tco = int.Parse(TextureCountOffset.Content.ToString());
            TextureCountOffset.Content = tco.ToString("X");

            int mcd = int.Parse(MapCountData.Content.ToString());
            MapCountData.Content = mcd.ToString("X");
            int mco = int.Parse(MapCountOffset.Content.ToString());
            MapCountOffset.Content = mco.ToString("X");

            int cscd = int.Parse(ColorSetCountData.Content.ToString());
            ColorSetCountData.Content = cscd.ToString("X");
            int csco = int.Parse(ColorSetCountOffset.Content.ToString());
            ColorSetCountOffset.Content = csco.ToString("X");

            int ukd = int.Parse(UnknownData.Content.ToString());
            UnknownData.Content = ukd.ToString("X");
            int uko = int.Parse(UnknownOffset.Content.ToString());
            UnknownOffset.Content = uko.ToString("X");


            int dsd = int.Parse(DataSizeData.Content.ToString());
            DataSizeData.Content = dsd.ToString("X");
            int dso = int.Parse(DataSizeOffset.Content.ToString());
            DataSizeOffset.Content = dso.ToString("X");

            int s1d = int.Parse(Struct1Data.Content.ToString());
            Struct1Data.Content = s1d.ToString("X");
            int s1o = int.Parse(Struct1Offset.Content.ToString());
            Struct1Offset.Content = s1o.ToString("X");

            int s2d = int.Parse(Struct2Data.Content.ToString());
            Struct2Data.Content = s2d.ToString("X");
            int s2o = int.Parse(Struct2Offset.Content.ToString());
            Struct2Offset.Content = s2o.ToString("X");

            int pd = int.Parse(ParamData.Content.ToString());
            ParamData.Content = pd.ToString("X");
            int po = int.Parse(ParamOffset.Content.ToString());
            ParamOffset.Content = po.ToString("X");

            int uk1d = int.Parse(Unk1Data.Content.ToString());
            Unk1Data.Content = uk1d.ToString("X");
            int uk1o = int.Parse(Unk1Offset.Content.ToString());
            Unk1Offset.Content = uk1o.ToString("X");

            int uk2d = int.Parse(Unk2Data.Content.ToString());
            Unk2Data.Content = uk2d.ToString("X");
            int uk2o = int.Parse(Unk2Offset.Content.ToString());
            Unk2Offset.Content = uk2o.ToString("X");

            DataComboBox.SelectedIndex = 0;
        }

        private void DataComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = DataComboBox.SelectedItem.ToString();

            if (selected.Contains("struct1"))
            {
                var part = int.Parse(selected.Substring(selected.Length - 1)) - 1;

                var d = struct1[part];


                Data1Offset.Content = d.IDOffset;
                Data1Name.Content = "ID";
                Data1Data.Content = d.ID;

                Data2Offset.Content = d.Unknown1Offset;
                Data2Name.Content = "Unknown1";
                Data2Data.Content = d.Unknown1;

                Data3Offset.Content = "";
                Data3Name.Content = "";
                Data3Data.Content = "";

                Data4Offset.Content = "";
                Data4Name.Content = "";
                Data4Data.Content = "";

                if (HexCheck.IsChecked == true)
                {
                    Data1Offset.Content = d.IDOffset.ToString("X");
                    Data1Data.Content = d.ID.ToString("X");

                    Data2Offset.Content = d.Unknown1Offset.ToString("X");
                    Data2Data.Content = d.Unknown1.ToString("X");
                }


            }
            else if(selected.Contains("struct2"))
            {
                var part = int.Parse(selected.Substring(selected.Length - 1)) - 1;

                var d = struct2[part];

                Data1Offset.Content = d.IDOffset;
                Data1Name.Content = "ID";
                Data1Data.Content = d.ID;

                Data2Offset.Content = d.offsetOffset;
                Data2Name.Content = "Offset";
                Data2Data.Content = d.Offset;

                Data3Offset.Content = d.sizeOffset;
                Data3Name.Content = "Size";
                Data3Data.Content = d.Size;

                Data4Offset.Content = "";
                Data4Name.Content = "";
                Data4Data.Content = "";

                if (HexCheck.IsChecked == true)
                {
                    Data1Offset.Content = d.IDOffset.ToString("X");
                    Data1Data.Content = d.ID.ToString("X");

                    Data2Offset.Content = d.offsetOffset.ToString("X");
                    Data2Data.Content = d.Offset.ToString("X");

                    Data3Offset.Content = d.sizeOffset.ToString("X");
                    Data3Data.Content = d.Size.ToString("X");
                }

            }
            else
            {

                Data1Offset.Content = par.IDOffset;
                Data1Name.Content = "ID";
                Data1Data.Content = par.ID;

                Data2Offset.Content = par.Unknown1Offset;
                Data2Name.Content = "Unknown1";
                Data2Data.Content = par.Unknown1;

                Data3Offset.Content = par.Unknown2Offset;
                Data3Name.Content = "Unknown2";
                Data3Data.Content = par.Unknown2;

                Data4Offset.Content = par.TextureIndexOffset;
                Data4Name.Content = "Texture Index";
                Data4Data.Content = par.TextureIndex;

                if (HexCheck.IsChecked == true)
                {
                    Data1Offset.Content = par.IDOffset.ToString("X");
                    Data1Data.Content = par.ID.ToString("X");

                    Data2Offset.Content = par.Unknown1Offset.ToString("X");
                    Data2Data.Content = par.Unknown1.ToString("X");

                    Data3Offset.Content = par.Unknown2Offset.ToString("X");
                    Data3Data.Content = par.Unknown2.ToString("X");

                    Data4Offset.Content = par.TextureIndexOffset.ToString("X");
                    Data4Data.Content = par.TextureIndex.ToString("X");

                }
            }
        }
    }
}
