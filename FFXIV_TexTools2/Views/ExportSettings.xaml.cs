using System;
using System.Collections.Generic;
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
using FFXIV_TexTools2.Helpers;

namespace FFXIV_TexTools2.Views
{
    /// <summary>
    /// Interaction logic for ExportSettings.xaml
    /// </summary>
    public partial class ExportSettings : Window
    {
        public ExportSettings()
        {
            InitializeComponent();

            DAETargetComboBox.ItemsSource = Info.DAEPluginTargets;

            foreach( var item in DAETargetComboBox.Items )
            {
                if(Properties.Settings.Default.DAE_Plugin_Target == item.ToString())
                {
                    DAETargetComboBox.SelectedItem = item;
                    break;
                }
            }
            
        }

        private void DAETargetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Properties.Settings.Default.DAE_Plugin_Target = DAETargetComboBox.SelectedItem.ToString();
            Properties.Settings.Default.Save();
        }
    }
}
