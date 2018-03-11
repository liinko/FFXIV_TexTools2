using FFXIV_TexTools2.IO;
using FFXIV_TexTools2.Model;
using FFXIV_TexTools2.Resources;
using FFXIV_TexTools2.ViewModel;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Xml;

namespace FFXIV_TexTools2.Views
{
    /// <summary>
    /// Interaction logic for AdvImport.xaml
    /// </summary>
    public partial class AdvImport : Window
    {
        string path = "";

        Dictionary<string, ImportSettings> importDict = new Dictionary<string, ImportSettings>();

        string category, itemName, modelName, selectedMesh, internalPath;
        List<string> boneStrings;
        ModelData modelData;
        ModelViewModel mvm;
        ItemData item;

        public AdvImport(ModelViewModel mvm, string savePath, string category, ItemData item, string modelName, string selectedMesh, string internalPath, List<string> boneStrings, ModelData modelData)
        {
            this.category = category;
            this.itemName = item.ItemName;
            this.modelName = modelName;
            this.selectedMesh = selectedMesh;
            this.internalPath = internalPath;
            this.boneStrings = boneStrings;
            this.modelData = modelData;
            this.mvm = mvm;
            this.item = item;

            InitializeComponent();

            path = Path.GetFullPath(savePath);

            ImportDir.Text = path;

            List<string> meshCounts = new List<string>();

            meshCounts.Add(Strings.All);
            importDict.Add(Strings.All, new ImportSettings());
            importDict[Strings.All].path = path;

            foreach (var e in modelData.ExtraData.totalExtraCounts)
            {
                meshCounts.Add(e.Key.ToString());

                importDict.Add(e.Key.ToString(), new ImportSettings());
            }

            MeshComboBox.ItemsSource = meshCounts;
            MeshComboBox.SelectedIndex = 0;
            MeshCountLabel.Content = "Mesh Count: " + modelData.ExtraData.totalExtraCounts.Count;

            if(File.Exists(Properties.Settings.Default.Save_Directory + "/" + category + "/" + itemName + "/3D/" + modelName + "_Settings.xml"))
            {
                CreateXMLButton.IsEnabled = false;
            }
            else
            {
                CreateXMLButton.IsEnabled = true;
            }
        }

        private void AdvImportButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = path;
            openFileDialog.Filter = "Collada files (*.dae)|*.dae";
            openFileDialog.Title = "Select a Model File to import...";
            if(openFileDialog.ShowDialog() == true)
            {
                ImportDir.Text = openFileDialog.FileName;
                importDict[Strings.All].path = openFileDialog.FileName;
            }
        }

        private void FixCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            DisableCheckbox.IsChecked = false;
            importDict[MeshComboBox.SelectedItem.ToString()].Fix = true;
        }

        private void DisableCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            FixCheckbox.IsChecked = false;
            importDict[MeshComboBox.SelectedItem.ToString()].Disable = true;

        }

        private void FixCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            importDict[MeshComboBox.SelectedItem.ToString()].Fix = false;

        }

        private void CreateXMLButton_Click(object sender, RoutedEventArgs e)
        {
            MakeXML();
        }

        private void MakeXML()
        {
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings()
            {
                Indent = true,
            };

            using (XmlWriter xmlWriter = XmlWriter.Create(Properties.Settings.Default.Save_Directory + "/" + category + "/" + itemName + "/3D/" + modelName + "_Settings.xml", xmlWriterSettings))
            {
                xmlWriter.WriteStartDocument();

                xmlWriter.WriteComment("Note: Both Fix and DisableHide should not be True, if they are, TexTools will choose Hide over Fix");

                //<TexTools_Import>
                xmlWriter.WriteStartElement("TexTools_Import");

                //<Model>
                xmlWriter.WriteStartElement("Model");
                xmlWriter.WriteAttributeString("name", modelName);

                //<Mesh>
                xmlWriter.WriteStartElement("Mesh");
                xmlWriter.WriteAttributeString("name", "ALL");

                //<Fix>
                xmlWriter.WriteStartElement("Fix");
                xmlWriter.WriteString("false");
                xmlWriter.WriteEndElement();
                //</Fix>

                //<Hide>
                xmlWriter.WriteStartElement("DisableHide");
                xmlWriter.WriteString("false");
                xmlWriter.WriteEndElement();
                //</Hide>

                xmlWriter.WriteEndElement();
                //</Mesh>

                foreach (var m in modelData.ExtraData.totalExtraCounts)
                {

                    var isBody = modelData.LoD[0].MeshList[m.Key].IsBody;
                    //<Mesh>
                    xmlWriter.WriteStartElement("Mesh");
                    xmlWriter.WriteAttributeString("name", m.Key.ToString());
                    if (isBody)
                    {
                        xmlWriter.WriteAttributeString("type", "Body Mesh");
                    }

                    //<Fix>
                    xmlWriter.WriteStartElement("Fix");
                    xmlWriter.WriteString("false");
                    xmlWriter.WriteEndElement();
                    //</Fix>

                    //<Hide>
                    xmlWriter.WriteStartElement("DisableHide");
                    xmlWriter.WriteString("false");
                    xmlWriter.WriteEndElement();
                    //</Hide>

                    xmlWriter.WriteEndElement();
                    //</Mesh>
                }

                xmlWriter.WriteEndElement();
                //</Model>


                xmlWriter.WriteEndElement();
                //</TexTools_Import>

                xmlWriter.WriteEndDocument();

                xmlWriter.Flush();
            }
        }

        private void DisableCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            importDict[MeshComboBox.SelectedItem.ToString()].Disable = false;
        }

        private void MeshComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            DisableCheckbox.IsChecked = importDict[MeshComboBox.SelectedItem.ToString()].Disable;
            FixCheckbox.IsChecked = importDict[MeshComboBox.SelectedItem.ToString()].Fix;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var dir = Properties.Settings.Default.Save_Directory + "/" + category + "/" + itemName + "/3D/" + modelName + "_Settings.xml";

            if (!File.Exists(dir))
            {
                MakeXML();
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(dir);

            foreach (var imp in importDict)
            {
                XmlNode mesh = doc.SelectSingleNode("//Mesh[@name='" + imp.Key + "']");

                var impSett = imp.Value;

                if(mesh != null)
                {
                    var fix = mesh.SelectSingleNode("Fix");
                    fix.InnerText = impSett.Fix.ToString().ToLower();

                    var disable = mesh.SelectSingleNode("DisableHide");
                    disable.InnerText = impSett.Disable.ToString().ToLower();
                }
            }

            doc.Save(dir);

            ImportModel.ImportDAE(category, itemName, modelName, selectedMesh, internalPath, boneStrings, modelData, importDict);
            mvm.UpdateModel(item, category);
            Close();
        }


    }
}
