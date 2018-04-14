using FFXIV_TexTools2.IO;
using FFXIV_TexTools2.Model;
using FFXIV_TexTools2.Resources;
using FFXIV_TexTools2.ViewModel;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using FFXIV_TexTools2.Helpers;

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
        Dictionary<int, string> attributeDictionary = new Dictionary<int, string>();
        Dictionary<string, int> attrNameDict = new Dictionary<string, int>();
        private int attrSum = 0;
        public AdvImport(ModelViewModel mvm, string savePath, string category, ItemData item, string modelName, string selectedMesh, string internalPath, List<string> boneStrings, List<string> atrStrings, ModelData modelData)
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

            for (int i = 0; i < modelData.LoD[0].MeshCount; i++)
            {
                meshCounts.Add(i.ToString());
                importDict.Add(i.ToString(), new ImportSettings());
                importDict[i.ToString()].PartDictionary = new Dictionary<int, int>();

                var partCount = modelData.LoD[0].MeshList[i].MeshPartList.Count;

                for (int j = 0; j < partCount; j++)
                {
                    var partattr = modelData.LoD[0].MeshList[i].MeshPartList[j].Attributes;
                    importDict[i.ToString()].PartDictionary.Add(j, partattr);
                }
            }

            FixCheckbox.IsEnabled = false;
            DisableCheckbox.IsEnabled = false;

            MeshCountLabel.Content = "Mesh Count: " + modelData.LoD[0].MeshCount;


            attributeDictionary.Add(0, "none");
            attrNameDict.Add("None", 0);
            var a = 1;
            foreach (var attr in atrStrings)
            {
                var hasNum = attr.Any(c => char.IsDigit(c));

                if (hasNum)
                {
                    var attrNum = attr.Substring(attr.Length - 1);
                    var attrName = attr.Substring(0, attr.Length - 1);

                    attrNameDict.Add(Info.AttributeDict[attrName] + " " + attrNum, a);
                }
                else
                {
                    if (attr.Count(x => x == '_') > 1)
                    {
                        var mAtr = attr.Substring(0, attr.LastIndexOf("_"));
                        var atrNum = attr.Substring(attr.LastIndexOf("_") + 1, 1);
                        if (Info.AttributeDict.ContainsKey(mAtr))
                        {
                            attrNameDict.Add(Info.AttributeDict[mAtr] + atrNum, a);
                        }
                        else
                        {
                            attrNameDict.Add(attr, a);
                        }
                    }
                    else
                    {
                        attrNameDict.Add(Info.AttributeDict[attr], a);
                    }
                }
                attributeDictionary.Add(a, attr);
                a *= 2;
            }

            NewAttrComobBox.ItemsSource = attrNameDict.Keys;

            MeshComboBox.ItemsSource = meshCounts;
            MeshComboBox.SelectedIndex = 0;

            CreateXMLButton.IsEnabled = false;

            if (!File.Exists(Properties.Settings.Default.Save_Directory + "/" + category + "/" + itemName + "/3D/" + modelName + "_Settings.xml"))
            {
                if (modelData.ExtraData.totalExtraCounts != null)
                {
                    CreateXMLButton.IsEnabled = true;
                }
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
            CreateXMLButton.IsEnabled = false;
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

        private void PartComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var meshString = MeshComboBox.SelectedItem.ToString();

            if (e.AddedItems.Count > 0)
            {
                if (!meshString.Equals(Strings.All))
                {
                    var partNum = int.Parse(PartComboBox.SelectedItem.ToString());

                    var partAttr = importDict[meshString].PartDictionary[partNum];


                    attrSum = partAttr;

                    try
                    {
                        var attr = attributeDictionary[partAttr];
                        var hasNum = attr.Any(c => char.IsDigit(c));
                        var attrNum = " ]";

                        if (hasNum)
                        {
                            attrNum = " " + attr.Substring(attr.Length - 1) + " ]";
                            attr = attr.Substring(0, attr.Length - 1);
                        }

                        if (attr.Count(x => x == '_') > 1)
                        {
                            attrNum = " " + attr.Substring(attr.LastIndexOf("_") + 1, 1) + " ]";
                            attr = attr.Substring(0, attr.LastIndexOf("_"));
                        }

                        OrigAttrText.Text = "[ " +Info.AttributeDict[attr] + attrNum;
                    }
                    catch
                    {
                        OrigAttrText.Text = "";
                        foreach (var atrd in attributeDictionary)
                        {
                            if (atrd.Key != 0)
                            {
                                var r = (atrd.Key & partAttr);
                                if (r == atrd.Key)
                                {
                                    var attr = attributeDictionary[r];
                                    var hasNum = attr.Any(c => char.IsDigit(c));
                                    var attrNum = " ]";

                                    if (hasNum)
                                    {
                                        attrNum = " " + attr.Substring(attr.Length - 1) + " ]";
                                        attr = attr.Substring(0, attr.Length - 1);
                                    }
                                    else if (attr.Count(x => x == '_') > 1)
                                    {
                                        if (!attr.Contains("_cn_"))
                                        {
                                            attrNum = attr.Substring(attr.LastIndexOf("_") + 1, 1) + " ]";
                                            attr = attr.Substring(0, attr.LastIndexOf("_"));
                                        }
                                    }



                                    OrigAttrText.Text += "[ " + Info.AttributeDict[attr] + attrNum;
                                }
                            }
                        }
                    }
                }

                NewAttrComobBox.IsEnabled = true;
            }
        }

        private void NewAttrComobBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var selected = NewAttrComobBox.SelectedItem.ToString();

                if (selected.Equals("None"))
                {
                    attrSum = 0;
                    OrigAttrText.Text = "[ None ]";
                }
                else
                {
                    if (OrigAttrText.Text.Equals("[ None ]"))
                    {
                        OrigAttrText.Text = "";
                    }

                    var newText = "[ " + selected + " ]";

                    if (OrigAttrText.Text.Contains(newText))
                    {
                        var repText = OrigAttrText.Text.Replace(newText, "");
                        OrigAttrText.Text = repText;
                        attrSum -= attrNameDict[selected];

                        if (attrSum == 0)
                        {
                            OrigAttrText.Text = "[ None ]";
                        }
                    }
                    else
                    {
                        OrigAttrText.Text += newText;
                        attrSum += attrNameDict[selected];
                    }
                }

                var meshString = MeshComboBox.SelectedItem.ToString();
                var partNum = int.Parse(PartComboBox.SelectedItem.ToString());
                importDict[meshString].PartDictionary[partNum] = attrSum;

                NewAttrComobBox.SelectedIndex = -1;
            }
        }

        private void DisableCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            importDict[MeshComboBox.SelectedItem.ToString()].Disable = false;
        }

        private void MeshComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var selectedItem = -1;
            if (!MeshComboBox.SelectedItem.ToString().Equals(Strings.All))
            {
                selectedItem = int.Parse(MeshComboBox.SelectedItem.ToString());

                if (modelData.ExtraData.totalExtraCounts != null &&
                    modelData.ExtraData.totalExtraCounts.ContainsKey(selectedItem))
                {
                    DisableCheckbox.IsEnabled = true;
                    FixCheckbox.IsEnabled = true;

                    AttemptFixText.Text =
                        "Use this option if mesh appears to have holes, when another item is equipped.\n* It is recommended to try this option before disabling";
                    DisableHidingText.Text =
                        "Use this option to disable the mesh from hiding when another item is equipped.";
                }
                else
                {
                    AttemptFixText.Text = "This option is not available for this mesh.";
                    DisableHidingText.Text = "This option is not available for this mesh.";
                    DisableCheckbox.IsEnabled = false;
                    FixCheckbox.IsEnabled = false;
                }

                DisableCheckbox.IsChecked = importDict[MeshComboBox.SelectedItem.ToString()].Disable;
                FixCheckbox.IsChecked = importDict[MeshComboBox.SelectedItem.ToString()].Fix;
            }
            else if(modelData.ExtraData.totalExtraCounts != null)
            {
                selectedItem = -1;
                DisableCheckbox.IsEnabled = true;
                FixCheckbox.IsEnabled = true;

                AttemptFixText.Text =
                    "Use this option if mesh appears to have holes, when another item is equipped.\n* It is recommended to try this option before disabling";
                DisableHidingText.Text =
                    "Use this option to disable the mesh from hiding when another item is equipped.";
            }
            else
            {
                AttemptFixText.Text = "This option is not available for this mesh.";
                DisableHidingText.Text = "This option is not available for this mesh.";
                selectedItem = -1;
            }


            if (selectedItem != -1)
            {
                PartComboBox.IsEnabled = true;
                NewAttrComobBox.IsEnabled = true;

                var partcCount = importDict[MeshComboBox.SelectedItem.ToString()].PartDictionary.Count;

                List<int> partList = new List<int>();
                for (int i = 0; i < partcCount; i++)
                {
                    partList.Add(i);
                }

                PartComboBox.ItemsSource = partList;

                PartComboBox.SelectedIndex = 0;
            }
            else
            {
                PartComboBox.ItemsSource = null;
                NewAttrComobBox.IsEnabled = false;
                PartComboBox.IsEnabled = false;
            }


        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var dir = Properties.Settings.Default.Save_Directory + "/" + category + "/" + itemName + "/3D/" + modelName + "_Settings.xml";

            if (modelData.ExtraData.totalExtraCounts != null)
            {
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

                    if (mesh != null)
                    {
                        var fix = mesh.SelectSingleNode("Fix");
                        fix.InnerText = impSett.Fix.ToString().ToLower();

                        var disable = mesh.SelectSingleNode("DisableHide");
                        disable.InnerText = impSett.Disable.ToString().ToLower();
                    }
                }

                doc.Save(dir);
            }

            ImportModel.ImportDAE(category, itemName, modelName, selectedMesh, internalPath, boneStrings, modelData, importDict);
            mvm.UpdateModel(item, category);
            Close();
        }


    }
}
