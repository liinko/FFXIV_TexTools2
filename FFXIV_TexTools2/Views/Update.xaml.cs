using System.Windows;
using System.Windows.Documents;

namespace FFXIV_TexTools2.Views
{
    /// <summary>
    /// Interaction logic for Update.xaml
    /// </summary>
    public partial class Update : Window
    {
        public Update()
        {
            InitializeComponent();
        }

        public string Message
        {
            set { changeLogTextBox.Document.Blocks.Add(new Paragraph(new Run(value))); }
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void visitWebsiteButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://ffxivtextools.dualwield.net");
        }
    }
}
