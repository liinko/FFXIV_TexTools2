using FFXIV_TexTools2.Helpers;
using FFXIV_TexTools2.Material;
using FFXIV_TexTools2.Model;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace FFXIV_TexTools2.Views
{
    /// <summary>
    /// Interaction logic for IMCInfo.xaml
    /// </summary>
    public partial class IMCInfo : Window
    {

        Dictionary<int, List<IMCData>> imcDataDictionary = new Dictionary<int, List<IMCData>>();
        string type;

        public IMCInfo(string selectedCategory, ItemData item, bool isSecondary)
        {
            InitializeComponent();

            int stride;
            type = Helper.GetCategoryType(selectedCategory);

            var imcData = IMC.GetIMCData(selectedCategory, item, false);
            List<string> variants = new List<string>();

            using (BinaryReader br = new BinaryReader(new MemoryStream(imcData.Item2)))
            {
                var variantCount = br.ReadUInt16();

                totalVariants.Content = variantCount;

                    var unk = br.ReadUInt16();

                imcUnk.Content = unk;

                if (type.Equals("weapon") || type.Equals("food") || type.Equals("monster"))
                {
                    stride = 6;
                }
                else
                {
                    stride = 30;
                }

                for(int i = 0; i < variantCount + 1; i++)
                {
                    imcDataDictionary.Add(i, new List<IMCData>());

                    if (stride == 6)
                    {
                        IMCData id = new IMCData();
                        id.Offset = br.BaseStream.Position;
                        id.Material = br.ReadUInt16();
                        id.Mask = br.ReadUInt16();
                        id.Effect = br.ReadUInt16();

                        imcDataDictionary[i].Add(id);
                    }
                    else
                    {
                        for(int j = 0; j < 5; j++)
                        {
                            IMCData id = new IMCData();
                            id.Offset = br.BaseStream.Position;
                            id.Material = br.ReadUInt16();
                            id.Mask = br.ReadUInt16();
                            id.Effect = br.ReadUInt16();

                            imcDataDictionary[i].Add(id);
                        }
                    }

                    if(i == 0)
                    {
                        variants.Add("Default");
                    }
                    else
                    {
                        variants.Add(i.ToString());
                    }
                }

                VariantComboBox.ItemsSource = variants;
            }

            if (VariantComboBox.HasItems)
            {
                VariantComboBox.SelectedIndex = 0;
            }
        }

        private void VariantComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = VariantComboBox.SelectedItem.ToString();
            int variant = 0;

            if (!selected.Equals("Default"))
            {
                variant = int.Parse(selected);
            }

            var slotCount = imcDataDictionary[variant].Count;

            if(slotCount == 5)
            {
                Slot1.Content = "Head";
                Offset1.Content = imcDataDictionary[variant][0].Offset;
                Mat1.Content = imcDataDictionary[variant][0].Material;
                Mask1.Content = imcDataDictionary[variant][0].Mask;
                Effect1.Content = imcDataDictionary[variant][0].Effect;

                Slot2.Content = "Body";
                Offset2.Content = imcDataDictionary[variant][1].Offset;
                Mat2.Content = imcDataDictionary[variant][1].Material;
                Mask2.Content = imcDataDictionary[variant][1].Mask;
                Effect2.Content = imcDataDictionary[variant][1].Effect;

                Slot3.Content = "Hands";
                Offset3.Content = imcDataDictionary[variant][2].Offset;
                Mat3.Content = imcDataDictionary[variant][2].Material;
                Mask3.Content = imcDataDictionary[variant][2].Mask;
                Effect3.Content = imcDataDictionary[variant][2].Effect;

                Slot4.Content = "Legs";
                Offset4.Content = imcDataDictionary[variant][3].Offset;
                Mat4.Content = imcDataDictionary[variant][3].Material;
                Mask4.Content = imcDataDictionary[variant][3].Mask;
                Effect4.Content = imcDataDictionary[variant][3].Effect;

                Slot5.Content = "Feet";
                Offset5.Content = imcDataDictionary[variant][4].Offset;
                Mat5.Content = imcDataDictionary[variant][4].Material;
                Mask5.Content = imcDataDictionary[variant][4].Mask;
                Effect5.Content = imcDataDictionary[variant][4].Effect;

            }
            else
            {
                Slot1.Content = type;
                Offset1.Content = imcDataDictionary[variant][0].Offset;
                Mat1.Content = imcDataDictionary[variant][0].Material;
                Mask1.Content = imcDataDictionary[variant][0].Mask;
                Effect1.Content = imcDataDictionary[variant][0].Effect;
            }
        }


        public class IMCData
        {
            public long Offset;
            public int Material;
            public int Mask;
            public int Effect;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

            var selected = VariantComboBox.SelectedItem.ToString();
            int variant = 0;

            if (!selected.Equals("Default"))
            {
                variant = int.Parse(selected);
            }

            var tot = int.Parse(totalVariants.Content.ToString());
            totalVariants.Content = tot.ToString("X");

            var unk = int.Parse(imcUnk.Content.ToString());
            imcUnk.Content = unk.ToString("X");

            var slotCount = imcDataDictionary[variant].Count;

            if(slotCount == 5)
            {
                var offset1 = int.Parse(Offset1.Content.ToString());
                Offset1.Content = offset1.ToString("X");
                var mat1 = int.Parse(Mat1.Content.ToString());
                Mat1.Content = mat1.ToString("X");
                var mask1 = int.Parse(Mask1.Content.ToString());
                Mask1.Content = mask1.ToString("X");
                var effect1 = int.Parse(Effect1.Content.ToString());
                Effect1.Content = effect1.ToString("X");

                var offset2 = int.Parse(Offset2.Content.ToString());
                Offset2.Content = offset2.ToString("X");
                var mat2 = int.Parse(Mat2.Content.ToString());
                Mat2.Content = mat2.ToString("X");
                var mask2 = int.Parse(Mask2.Content.ToString());
                Mask2.Content = mask2.ToString("X");
                var effect2 = int.Parse(Effect2.Content.ToString());
                Effect2.Content = effect2.ToString("X");

                var offset3 = int.Parse(Offset3.Content.ToString());
                Offset3.Content = offset3.ToString("X");
                var mat3 = int.Parse(Mat3.Content.ToString());
                Mat3.Content = mat3.ToString("X");
                var mask3 = int.Parse(Mask3.Content.ToString());
                Mask3.Content = mask3.ToString("X");
                var effect3 = int.Parse(Effect3.Content.ToString());
                Effect3.Content = effect3.ToString("X");

                var offset4 = int.Parse(Offset4.Content.ToString());
                Offset4.Content = offset4.ToString("X");
                var mat4 = int.Parse(Mat4.Content.ToString());
                Mat4.Content = mat4.ToString("X");
                var mask4 = int.Parse(Mask4.Content.ToString());
                Mask4.Content = mask4.ToString("X");
                var effect4 = int.Parse(Effect4.Content.ToString());
                Effect4.Content = effect4.ToString("X");

                var offset5 = int.Parse(Offset5.Content.ToString());
                Offset5.Content = offset5.ToString("X");
                var mat5 = int.Parse(Mat5.Content.ToString());
                Mat5.Content = mat5.ToString("X");
                var mask5 = int.Parse(Mask5.Content.ToString());
                Mask5.Content = mask5.ToString("X");
                var effect5 = int.Parse(Effect5.Content.ToString());
                Effect5.Content = effect5.ToString("X");
            }
            else
            {
                var offset1 = int.Parse(Offset1.Content.ToString());
                Offset1.Content = offset1.ToString("X");
                var mat1 = int.Parse(Mat1.Content.ToString());
                Mat1.Content = mat1.ToString("X");
                var mask1 = int.Parse(Mask1.Content.ToString());
                Mask1.Content = mask1.ToString("X");
                var effect1 = int.Parse(Effect1.Content.ToString());
                Effect1.Content = effect1.ToString("X");
            }



        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var selected = VariantComboBox.SelectedItem.ToString();
            int variant = 0;

            if (!selected.Equals("Default"))
            {
                variant = int.Parse(selected);
            }

            var tot = int.Parse(totalVariants.Content.ToString(), System.Globalization.NumberStyles.HexNumber);

            totalVariants.Content = tot;

            var unk = int.Parse(imcUnk.Content.ToString(), System.Globalization.NumberStyles.HexNumber);

            imcUnk.Content = unk;


            var slotCount = imcDataDictionary[variant].Count;

            if (slotCount == 5)
            {
                var offset1 = int.Parse(Offset1.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
                Offset1.Content = offset1;
                var mat1 = int.Parse(Mat1.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
                Mat1.Content = mat1;
                var mask1 = int.Parse(Mask1.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
                Mask1.Content = mask1;
                var effect1 = int.Parse(Effect1.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
                Effect1.Content = effect1;

                var offset2 = int.Parse(Offset2.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
                Offset2.Content = offset2;
                var mat2 = int.Parse(Mat2.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
                Mat2.Content = mat2;
                var mask2 = int.Parse(Mask2.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
                Mask2.Content = mask2;
                var effect2 = int.Parse(Effect2.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
                Effect2.Content = effect2;

                var offset3 = int.Parse(Offset3.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
                Offset3.Content = offset3;
                var mat3 = int.Parse(Mat3.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
                Mat3.Content = mat3;
                var mask3 = int.Parse(Mask3.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
                Mask3.Content = mask3;
                var effect3 = int.Parse(Effect3.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
                Effect3.Content = effect3;

                var offset4 = int.Parse(Offset4.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
                Offset4.Content = offset4;
                var mat4 = int.Parse(Mat4.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
                Mat4.Content = mat4;
                var mask4 = int.Parse(Mask4.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
                Mask4.Content = mask4;
                var effect4 = int.Parse(Effect4.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
                Effect4.Content = effect4;

                var offset5 = int.Parse(Offset5.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
                Offset5.Content = offset5;
                var mat5 = int.Parse(Mat5.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
                Mat5.Content = mat5;
                var mask5 = int.Parse(Mask5.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
                Mask5.Content = mask5;
                var effect5 = int.Parse(Effect5.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
                Effect5.Content = effect5;
            }
            else
            {
                var offset1 = int.Parse(Offset1.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
                Offset1.Content = offset1;
                var mat1 = int.Parse(Mat1.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
                Mat1.Content = mat1;
                var mask1 = int.Parse(Mask1.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
                Mask1.Content = mask1;
                var effect1 = int.Parse(Effect1.Content.ToString(), System.Globalization.NumberStyles.HexNumber);
                Effect1.Content = effect1;
            }

        }
    }
}
