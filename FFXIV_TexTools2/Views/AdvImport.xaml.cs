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
using FFXIV_TexTools2.Material.ModelMaterial;

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
        ModelData modelData;
        ModelViewModel mvm;
        ItemData item;
        List<string> AttributeNiceNames;
        private int attrSum = 0;
        public AdvImport(ModelViewModel mvm, string savePath, string category, ItemData item, string modelName, string selectedMesh, string internalPath, ModelData modelData)
        {
            this.category = category;
            this.itemName = item.ItemName;
            this.modelName = modelName;
            this.selectedMesh = selectedMesh;
            this.internalPath = internalPath;
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

                var partCount = modelData.LoD[0].MeshList[i].MeshPartList.Count;

                for (int j = 0; j < partCount; j++)
                {
                    var partattr = modelData.LoD[0].MeshList[i].MeshPartList[j].Attributes;
                }
            }

            FixCheckbox.IsEnabled = false;
            DisableCheckbox.IsEnabled = false;

            MeshCountLabel.Content = "Meshes: " + modelData.LoD[0].MeshCount;
            ExtraMeshDataLabel.Content = string.Format("Extra Mesh Data: {0}", modelData.ExtraData.totalExtraCounts);

            foreach (var bone in modelData.Bones)
            {
                BoneList.Items.Add(bone);
            }

            foreach(var material in modelData.Materials)
            {
                Materials_List.Items.Add(material);
                MeshMaterialComboBox.Items.Add(material);
            }

            RebuildAttributesDictionary();

            MeshComboBox.ItemsSource = meshCounts;
            MeshComboBox.SelectedIndex = 0;

            CreateXMLButton.IsEnabled = false;

            if (!File.Exists(Properties.Settings.Default.Save_Directory + "/" + category + "/" + itemName + "/3D/" + modelName + "_Settings.xml"))
            {
                if (modelData.ExtraData.totalExtraCounts != null)
                {
                    //CreateXMLButton.IsEnabled = true;
                }
            }
        }

        private void RebuildAttributesDictionary()
        {
            AttributeNiceNames = new List<string>();

            var a = 1;
            foreach (var attr in modelData.Attributes)
            {
                var hasNum = attr.Any(c => char.IsDigit(c));

                if (hasNum)
                {
                    var attrNum = attr.Substring(attr.Length - 1);
                    var attrName = attr.Substring(0, attr.Length - 1);

                    AttributeNiceNames.Add(attr + " - " + Info.AttributeDict[attrName] + " " + attrNum);
                }
                else
                {
                    if (attr.Count(x => x == '_') > 1)
                    {
                        var mAtr = attr.Substring(0, attr.LastIndexOf("_"));
                        var atrNum = attr.Substring(attr.LastIndexOf("_") + 1, 1);
                        if (Info.AttributeDict.ContainsKey(mAtr))
                        {
                            AttributeNiceNames.Add(attr + " - " + Info.AttributeDict[mAtr] + atrNum);
                        }
                        else
                        {
                            AttributeNiceNames.Add(attr);
                        }
                    }
                    else
                    {
                        if (Info.AttributeDict.ContainsKey(attr))
                        {
                            AttributeNiceNames.Add(attr + " - " + Info.AttributeDict[attr]);
                        }
                        else
                        {
                            AttributeNiceNames.Add(attr);
                        }
                    }
                }
            }

            AttributesList.ItemsSource = null;
            NewAttrComobBox.ItemsSource = null;
            AttributesList.ItemsSource = AttributeNiceNames;
            NewAttrComobBox.ItemsSource = AttributeNiceNames;
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

            if (e == null || e.AddedItems.Count > 0)
            {
                if (!meshString.Equals(Strings.All))
                {
                    int meshNum = int.Parse(meshString);
                    var partNum = int.Parse(PartComboBox.SelectedItem.ToString());
                    int partAttr = modelData.LoD[0].MeshList[meshNum].MeshPartList[partNum].Attributes;
                    attrSum = partAttr;

                    List<string> FriendlyText = new List<string>();
                    for (int i = 0; i < modelData.Attributes.Count; i++)
                    {
                        int value = 1 << i;
                        if((partAttr & value) > 0)
                        {
                            FriendlyText.Add(AttributeNiceNames[i]);
                        }

                    }

                    MeshPartAttributesList.IsEnabled = true;
                    MeshPartAttributesList.ItemsSource = FriendlyText;
                } else
                {
                    MeshPartAttributesList.ItemsSource = null;
                    MeshPartAttributesList.IsEnabled = false;
                }

                NewAttrComobBox.IsEnabled = true;
            } else
            {
                MeshPartAttributesList.ItemsSource = null;
                MeshPartAttributesList.IsEnabled = false;
            }
        }

        private void NewAttrComobBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var selected = NewAttrComobBox.SelectedItem.ToString();
                var meshString = MeshComboBox.SelectedItem.ToString();
                var partNum = int.Parse(PartComboBox.SelectedItem.ToString());
                var meshNum = int.Parse(meshString);
                string atrName = selected.Split(' ')[0];

                bool addition = true;
                if(MeshPartAttributesList.Items.Contains(selected))
                {
                    addition = false;
                }
                
                if (selected.Equals("None"))
                {
                    attrSum = 0;
                }
                else
                {
                    int value = 0;
                    for(int i = 0; i < modelData.Attributes.Count; i++)
                    {
                        if(modelData.Attributes[i] == atrName)
                        {
                            value = 1 << i;
                            break;
                        }
                    }

                    if (addition)
                    {
                        attrSum += value;
                    }
                    else
                    {
                        attrSum -= value;
                    }
                }

                modelData.LoD[0].MeshList[meshNum].MeshPartList[partNum].Attributes = attrSum;

                NewAttrComobBox.SelectedIndex = -1;

                MeshPartAttributesList.ItemsSource = null;
                PartComboBox_SelectionChanged(null, null);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MeshAddPartButton_Click(object sender, RoutedEventArgs e)
        {
            var meshString = MeshComboBox.SelectedItem.ToString();
            var meshNum = int.Parse(meshString);

            MeshPart newPart = new MeshPart();
            modelData.LoD[0].MeshList[meshNum].MeshPartList.Add(newPart);

            MeshPartCount.Content = modelData.LoD[0].MeshList[meshNum].MeshPartList.Count;
            List<int> parts = (List<int>) PartComboBox.ItemsSource;
            parts.Add(modelData.LoD[0].MeshList[meshNum].MeshPartList.Count - 1);
            PartComboBox.ItemsSource = null;
            PartComboBox.ItemsSource = parts;
            PartComboBox.SelectedIndex = 0;
        }

        private void MaterialsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MaterialAdditionText.Text = (string) Materials_List.SelectedItem;
        }

        private void AttributeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AttributeAdditionText.Text = (string)AttributesList.SelectedItem;
        }

        private void MeshMaterialComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var meshString = MeshComboBox.SelectedItem.ToString();
                var meshNum = int.Parse(meshString);
                modelData.LoD[0].MeshList[meshNum].MaterialId = MeshMaterialComboBox.SelectedIndex;
            } catch
            {

            }
        }

        private void AddMaterialButton_Click(object sender, RoutedEventArgs e)
        {
            var items = Materials_List.Items;

            int removeAtIndex = -1;
            for(int i = 0; i < items.Count; i++)
            {
                var item = items[i];

                // Found one to remove.
                if(items[i].ToString() == MaterialAdditionText.Text)
                {
                    removeAtIndex = i;
                    break;
                }
            }

            // Reset our mesh selection since the UI will have to be refreshed anyways.
            MeshComboBox.SelectedIndex = 0;

            // Addition mode, easy.
            if(removeAtIndex == -1)
            {
                modelData.Materials.Add(MaterialAdditionText.Text);
            } else
            {
                modelData.Materials.RemoveAt(removeAtIndex);


                // Adjust the material indexes to correct for the removed material.
                for(int l = 0; l < modelData.LoD.Count; l++)
                {
                    for(int m = 0; m < modelData.LoD[l].MeshCount; m++)
                    {
                        if(modelData.LoD[l].MeshList[m].MaterialId < removeAtIndex)
                        {
                            // No-Op, we're still fine.
                        } else if (modelData.LoD[l].MeshList[m].MaterialId == removeAtIndex)
                        {
                            // Reset the material for the mesh.
                            modelData.LoD[l].MeshList[m].MaterialId = 0;
                        } else
                        {
                            // Adjust the index number if we need to be shuffled down.
                            modelData.LoD[l].MeshList[m].MaterialId = modelData.LoD[l].MeshList[m].MaterialId - 1;
                        }
                    }
                }
            }

            // Refresh the material listing.
            Materials_List.Items.Clear();
            MeshMaterialComboBox.Items.Clear();
            foreach (var material in modelData.Materials)
            {
                Materials_List.Items.Add(material);
                MeshMaterialComboBox.Items.Add(material);
            }

        }

        private void AddAttributeButton_Click(object sender, RoutedEventArgs e)
        {
            var items = AttributesList.Items;

            int removeAtIndex = -1;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];

                // Found one to remove.
                if (items[i].ToString() == AttributeAdditionText.Text)
                {
                    removeAtIndex = i;
                    break;
                }
            }

            // Reset our mesh selection since the UI will have to be refreshed anyways.
            MeshComboBox.SelectedIndex = 0;

            // Addition mode, easy.
            if (removeAtIndex == -1)
            {
                modelData.Attributes.Add(AttributeAdditionText.Text);
            }
            else
            {
                modelData.Attributes.RemoveAt(removeAtIndex);
                
                int atrValue = 1 << removeAtIndex;

                // Adjust the attribute indexes to correct for the removed attribute.
                for (int l = 0; l < modelData.LoD.Count; l++)
                {
                    for (int m = 0; m < modelData.LoD[l].MeshCount; m++)
                    {
                        for (int p = 0; p < modelData.LoD[l].MeshList[m].MeshPartList.Count; p++)
                        {
                            if (modelData.LoD[l].MeshList[m].MeshPartList[p].Attributes < atrValue)
                            {
                                // No-Op, we're still fine.
                            }
                            else if (modelData.LoD[l].MeshList[m].MeshPartList[p].Attributes == atrValue)
                            {
                                // Reset the attributes
                                modelData.LoD[l].MeshList[m].MeshPartList[p].Attributes = 0;
                            }
                            else
                            {
                                // Gotta break the bit flag and downshift everything above the attribute number.
                                int original = modelData.LoD[l].MeshList[m].MeshPartList[p].Attributes;
                                int lowEnd = original % atrValue;
                                int highEnd = original - lowEnd;
                                highEnd = highEnd >> 1;

                                // Get rid of the extra bit if we had it flagged before.
                                int remainder = highEnd % atrValue;
                                highEnd -= remainder;

                                int result = highEnd + lowEnd;
                                modelData.LoD[l].MeshList[m].MeshPartList[p].Attributes = result;

                            }
                        }
                    }
                }
            }

            // Refresh the attributes listing.
            RebuildAttributesDictionary();
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
                }
                else
                {
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
            }
            else
            {
                selectedItem = -1;
            }


            if (selectedItem != -1)
            {
                var meshString = MeshComboBox.SelectedItem.ToString();
                var meshNum = int.Parse(meshString);

                PartComboBox.IsEnabled = true;
                MeshMaterialComboBox.IsEnabled = true;
                MeshAddPartButton.IsEnabled = true;
                NewAttrComobBox.IsEnabled = true;
                MeshMaterialComboBox.SelectedIndex = modelData.LoD[0].MeshList[selectedItem].MaterialId;


                var partcCount = modelData.LoD[0].MeshList[meshNum].MeshPartList.Count;

                List<int> partList = new List<int>();
                for (int i = 0; i < partcCount; i++)
                {
                    partList.Add(i);
                }

                PartComboBox.ItemsSource = partList;

                MeshPartCount.Content = partcCount;
                PartComboBox.SelectedIndex = 0;
            }
            else
            {
                MeshPartCount.Content = "";
                MeshMaterialComboBox.SelectedIndex = -1;
                PartComboBox.ItemsSource = null;
                NewAttrComobBox.IsEnabled = false;
                PartComboBox.IsEnabled = false;
                MeshMaterialComboBox.IsEnabled = false;
                MeshAddPartButton.IsEnabled = false;
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

            ImportModel.ImportDAE(category, itemName, modelName, selectedMesh, internalPath, modelData, importDict);
            mvm.UpdateModel(item, category);
            Close();
        }


    }
}
