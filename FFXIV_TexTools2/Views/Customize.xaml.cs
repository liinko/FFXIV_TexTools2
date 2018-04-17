using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using FFXIV_TexTools2.Resources;

namespace FFXIV_TexTools2.Views
{
    /// <summary>
    /// Interaction logic for Customize.xaml
    /// </summary>
    public partial class Customize : Window
    {
        private Color skinColor, hairColor, eyeColor, etcColor;

        public Customize()
        {
            InitializeComponent();

            List<string> baseRace = new List<string>
            {
                {Strings.Hyur_M },
                {Strings.Hyur_H },
                {Strings.AuRa_Raen },
                {Strings.AuRa_Xaela }
            };

            skinColor = (Color)ColorConverter.ConvertFromString(Properties.Settings.Default.Skin_Color);
            hairColor = (Color)ColorConverter.ConvertFromString(Properties.Settings.Default.Hair_Color);
            eyeColor = (Color)ColorConverter.ConvertFromString(Properties.Settings.Default.Iris_Color);
            etcColor = (Color)ColorConverter.ConvertFromString(Properties.Settings.Default.Etc_Color);

            RaceComboBox.ItemsSource = baseRace;
            RaceComboBox.SelectedIndex = baseRace.IndexOf(Properties.Settings.Default.Default_Race);

            SkinR.Text = skinColor.R.ToString();
            SkinG.Text = skinColor.G.ToString();
            SkinB.Text = skinColor.B.ToString();

            HairR.Text = hairColor.R.ToString();
            HairG.Text = hairColor.G.ToString();
            HairB.Text = hairColor.B.ToString();

            EyeR.Text = eyeColor.R.ToString();
            EyeG.Text = eyeColor.G.ToString();
            EyeB.Text = eyeColor.B.ToString();

            EtcR.Text = etcColor.R.ToString();
            EtcG.Text = etcColor.G.ToString();
            EtcB.Text = etcColor.B.ToString();

            SkinColor.Fill = new SolidColorBrush(skinColor);
            HairColor.Fill = new SolidColorBrush(hairColor);
            EyeColor.Fill = new SolidColorBrush(eyeColor);
            EtcColor.Fill = new SolidColorBrush(etcColor);

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SkinR_TextChanged(object sender, TextChangedEventArgs e)
        {
            var s = sender as TextBox;

            try
            {
                var num = int.Parse(s.Text);
                if (num < 0 || num > 255)
                {
                    s.Text = "";
                }
                else
                {
                    skinColor = Color.FromArgb(255, (byte) num, skinColor.G, skinColor.B);
                    SkinColor.Fill = new SolidColorBrush(skinColor);
                }
            }
            catch
            {
                s.Text = "";
            }
        }

        private void SkinG_TextChanged(object sender, TextChangedEventArgs e)
        {
            var s = sender as TextBox;

            try
            {
                var num = int.Parse(s.Text);
                if (num < 0 || num > 255)
                {
                    s.Text = "";
                }
                else
                {
                    skinColor = Color.FromArgb(255, skinColor.R, (byte) num, skinColor.B);
                    SkinColor.Fill = new SolidColorBrush(skinColor);
                }
            }
            catch
            {
                s.Text = "";
            }
        }

        private void SkinB_TextChanged(object sender, TextChangedEventArgs e)
        {
            var s = sender as TextBox;

            try
            {
                var num = int.Parse(s.Text);
                if (num < 0 || num > 255)
                {
                    s.Text = "";
                }
                else
                {
                    skinColor = Color.FromArgb(255, skinColor.R, skinColor.G, (byte) num);
                    SkinColor.Fill = new SolidColorBrush(skinColor);
                }
            }
            catch
            {
                s.Text = "";
            }
        }

        private void HairR_TextChanged(object sender, TextChangedEventArgs e)
        {
            var s = sender as TextBox;

            try
            {
                var num = int.Parse(s.Text);
                if (num < 0 || num > 255)
                {
                    s.Text = "";
                }
                else
                {
                    hairColor = Color.FromArgb(255, (byte)num, hairColor.G, hairColor.B);
                    HairColor.Fill = new SolidColorBrush(hairColor);
                }
            }
            catch
            {
                s.Text = "";
            }
        }

        private void HairG_TextChanged(object sender, TextChangedEventArgs e)
        {
            var s = sender as TextBox;

            try
            {
                var num = int.Parse(s.Text);
                if (num < 0 || num > 255)
                {
                    s.Text = "";
                }
                else
                {
                    hairColor = Color.FromArgb(255, hairColor.R, (byte)num, hairColor.B);
                    HairColor.Fill = new SolidColorBrush(hairColor);
                }
            }
            catch
            {
                s.Text = "";
            }
        }

        private void HairB_TextChanged(object sender, TextChangedEventArgs e)
        {
            var s = sender as TextBox;

            try
            {
                var num = int.Parse(s.Text);
                if (num < 0 || num > 255)
                {
                    s.Text = "";
                }
                else
                {
                    hairColor = Color.FromArgb(255, hairColor.R, hairColor.G, (byte)num);
                    HairColor.Fill = new SolidColorBrush(hairColor);
                }
            }
            catch
            {
                s.Text = "";
            }
        }

        private void EyeR_TextChanged(object sender, TextChangedEventArgs e)
        {
            var s = sender as TextBox;

            try
            {
                var num = int.Parse(s.Text);
                if (num < 0 || num > 255)
                {
                    s.Text = "";
                }
                else
                {
                    eyeColor = Color.FromArgb(255, (byte)num, eyeColor.G, eyeColor.B);
                    EyeColor.Fill = new SolidColorBrush(eyeColor);
                }
            }
            catch
            {
                s.Text = "";
            }
        }

        private void EyeG_TextChanged(object sender, TextChangedEventArgs e)
        {
            var s = sender as TextBox;

            try
            {
                var num = int.Parse(s.Text);
                if (num < 0 || num > 255)
                {
                    s.Text = "";
                }
                else
                {
                    eyeColor = Color.FromArgb(255, eyeColor.R, (byte)num, eyeColor.B);
                    EyeColor.Fill = new SolidColorBrush(eyeColor);
                }
            }
            catch
            {
                s.Text = "";
            }
        }

        private void EyeB_TextChanged(object sender, TextChangedEventArgs e)
        {
            var s = sender as TextBox;

            try
            {
                var num = int.Parse(s.Text);
                if (num < 0 || num > 255)
                {
                    s.Text = "";
                }
                else
                {
                    eyeColor = Color.FromArgb(255, eyeColor.R, eyeColor.G, (byte)num);
                    EyeColor.Fill = new SolidColorBrush(eyeColor);
                }
            }
            catch
            {
                s.Text = "";
            }
        }

        private void EtcR_TextChanged(object sender, TextChangedEventArgs e)
        {
            var s = sender as TextBox;

            try
            {
                var num = int.Parse(s.Text);
                if (num < 0 || num > 255)
                {
                    s.Text = "";
                }
                else
                {
                    etcColor = Color.FromArgb(255, (byte)num, etcColor.G, etcColor.B);
                    EtcColor.Fill = new SolidColorBrush(etcColor);
                }
            }
            catch
            {
                s.Text = "";
            }
        }

        private void EtcG_TextChanged(object sender, TextChangedEventArgs e)
        {
            var s = sender as TextBox;

            try
            {
                var num = int.Parse(s.Text);
                if (num < 0 || num > 255)
                {
                    s.Text = "";
                }
                else
                {
                    etcColor = Color.FromArgb(255, etcColor.R, (byte)num, etcColor.B);
                    EtcColor.Fill = new SolidColorBrush(etcColor);
                }
            }
            catch
            {
                s.Text = "";
            }
        }

        private void EtcB_TextChanged(object sender, TextChangedEventArgs e)
        {
            var s = sender as TextBox;

            try
            {
                var num = int.Parse(s.Text);
                if (num < 0 || num > 255)
                {
                    s.Text = "";
                }
                else
                {
                    etcColor = Color.FromArgb(255, etcColor.R, etcColor.G, (byte)num);
                    EtcColor.Fill = new SolidColorBrush(etcColor);
                }
            }
            catch
            {
                s.Text = "";
            }
        }

        private void SetButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Default_Race = RaceComboBox.Text;
            Properties.Settings.Default.Skin_Color = skinColor.ToString();
            Properties.Settings.Default.Hair_Color = hairColor.ToString();
            Properties.Settings.Default.Iris_Color = eyeColor.ToString();
            Properties.Settings.Default.Etc_Color = etcColor.ToString();

            Properties.Settings.Default.Save();

            Close();
        }
    }
}
