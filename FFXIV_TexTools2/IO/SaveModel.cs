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

using FFXIV_TexTools2.FileTypes.ModelContainers;
using FFXIV_TexTools2.Helpers;
using FFXIV_TexTools2.Material;
using FFXIV_TexTools2.Model;
using FFXIV_TexTools2.Resources;
using HelixToolkit.Wpf.SharpDX.Core;
using Newtonsoft.Json;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml;

namespace FFXIV_TexTools2.IO
{
    public class SaveModel
    {

        static Dictionary<string, JsonSkeleton> fullSkel = new Dictionary<string, JsonSkeleton>();
        static Dictionary<int, JsonSkeleton> fullSkelnum = new Dictionary<int, JsonSkeleton>();


        /// <summary>
        /// Saves the model as an OBJ file with its associated textures
        /// </summary>
        /// <param name="selectedCategory">The category of the item</param>
        /// <param name="modelName">The internal file name of the items model</param>
        /// <param name="selectedMesh">The currently selected mesh</param>
        /// <param name="selectedItemName">the currently selected items name</param>
        /// <param name="meshData">The mesh data for the selected items model</param>
        /// <param name="meshList">The list of mesh data for the selected items model</param>
        public static void Save(string selectedCategory, string modelName, string selectedMesh, string selectedItemName, List<MDLTEXData> meshData, List<ModelMeshData> meshList)
        {
            fullSkel.Clear();
            fullSkelnum.Clear();

            Directory.CreateDirectory(Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItemName + "/3D/");

            if (!selectedMesh.Equals(Strings.All))
            {
                int meshNum = int.Parse(selectedMesh);

                File.WriteAllLines(Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItemName + "/3D/" + modelName + "_" + meshNum + ".obj", meshList[meshNum].OBJFileData);

                var saveDir = Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItemName + "/3D/" + modelName + "_" + meshNum + "_Diffuse.bmp";

                using (var fileStream = new FileStream(saveDir, FileMode.Create))
                {
                    BitmapEncoder encoder = new BmpBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(meshData[meshNum].Diffuse));
                    encoder.Save(fileStream);
                }

                saveDir = Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItemName + "/3D/" + modelName + "_" + meshNum + "_Normal.bmp";

                using (var fileStream = new FileStream(saveDir, FileMode.Create))
                {
                    BitmapEncoder encoder = new BmpBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(meshData[meshNum].Normal));
                    encoder.Save(fileStream);
                }


                if (meshData[meshNum].Specular != null)
                {
                    saveDir = Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItemName + "/3D/" + modelName + "_" + meshNum + "_Specular.bmp";

                    using (var fileStream = new FileStream(saveDir, FileMode.Create))
                    {
                        BitmapEncoder encoder = new BmpBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(meshData[meshNum].Specular));
                        encoder.Save(fileStream);
                    }
                }


                if (meshData[meshNum].Alpha != null)
                {
                    saveDir = Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItemName + "/3D/" + modelName + "_" + meshNum + "_Alpha.bmp";

                    using (var fileStream = new FileStream(saveDir, FileMode.Create))
                    {
                        BitmapEncoder encoder = new BmpBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(meshData[meshNum].Alpha));
                        encoder.Save(fileStream);
                    }
                }

                if (meshData[meshNum].Emissive != null)
                {
                    saveDir = Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItemName + "/3D/" + modelName + "_" + meshNum + "_Emissive.bmp";

                    using (var fileStream = new FileStream(saveDir, FileMode.Create))
                    {
                        BitmapEncoder encoder = new BmpBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(meshData[meshNum].Emissive));
                        encoder.Save(fileStream);
                    }
                }
            }
            else
            {
                for (int i = 0; i < meshList.Count; i++)
                {

                    File.WriteAllLines(Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItemName + "/3D/" + modelName + "_" + i + ".obj", meshList[i].OBJFileData);

                    var saveDir = Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItemName + "/3D/" + modelName + "_" + i + "_Diffuse.bmp";

                    using (var fileStream = new FileStream(saveDir, FileMode.Create))
                    {
                        BitmapEncoder encoder = new BmpBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(meshData[i].Diffuse));
                        encoder.Save(fileStream);
                    }

                    saveDir = Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItemName + "/3D/" + modelName + "_" + i + "_Normal.bmp";

                    using (var fileStream = new FileStream(saveDir, FileMode.Create))
                    {
                        BitmapEncoder encoder = new BmpBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(meshData[i].Normal));
                        encoder.Save(fileStream);
                    }


                    if (meshData[i].Specular != null)
                    {
                        saveDir = Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItemName + "/3D/" + modelName + "_" + i + "_Specular.bmp";

                        using (var fileStream = new FileStream(saveDir, FileMode.Create))
                        {
                            BitmapEncoder encoder = new BmpBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(meshData[i].Specular));
                            encoder.Save(fileStream);
                        }
                    }

                    if (meshData[i].Alpha != null)
                    {
                        saveDir = Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItemName + "/3D/" + modelName + "_" + i + "_Alpha.bmp";

                        using (var fileStream = new FileStream(saveDir, FileMode.Create))
                        {
                            BitmapEncoder encoder = new BmpBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(meshData[i].Alpha));
                            encoder.Save(fileStream);
                        }
                    }

                    if (meshData[i].Emissive != null)
                    {
                        saveDir = Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItemName + "/3D/" + modelName + "_" + i + "_Emissive.bmp";

                        using (var fileStream = new FileStream(saveDir, FileMode.Create))
                        {
                            BitmapEncoder encoder = new BmpBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(meshData[i].Emissive));
                            encoder.Save(fileStream);
                        }
                    }

                }
            }

        }


        public static bool SaveCollada(string selectedCategory, string modelName, string selectedItemName, List<MDLTEXData> meshData, List<ModelMeshData> meshList)
        {
            string skelName = modelName.Substring(0, 5);

            if (!File.Exists(Directory.GetCurrentDirectory() + "/Skeletons/" + skelName + ".skel"))
            {
                skelName = "c0101";
            }

            string[] skeleton1 = File.ReadAllLines(Directory.GetCurrentDirectory() + "/Skeletons/" + skelName + ".skel");

            Dictionary<string, JsonSkeleton> skelDict = new Dictionary<string, JsonSkeleton>();

            foreach (var b in skeleton1)
            {
                var j = JsonConvert.DeserializeObject<JsonSkeleton>(b);

                fullSkel.Add(j.BoneName, j);
                fullSkelnum.Add(j.BoneNumber, j);
            }


            bool runAsset = false;

            foreach (var s in meshList[0].BoneStrings)
            {
                if (fullSkel.ContainsKey(s))
                {
                    var skel = fullSkel[s];

                    if (skel.BoneParent == -1)
                    {
                        skelDict.Add(skel.BoneName, skel);
                    }

                    while (skel.BoneParent != -1)
                    {
                        if (!skelDict.ContainsKey(skel.BoneName))
                        {
                            skelDict.Add(skel.BoneName, skel);
                        }
                        skel = fullSkelnum[skel.BoneParent];

                        if (skel.BoneParent == -1 && !skelDict.ContainsKey(skel.BoneName))
                        {
                            skelDict.Add(skel.BoneName, skel);
                        }
                    }
                }
                else
                {
                    runAsset = true;
                    break;
                }
            }

            var hasAssetcc = File.Exists(Directory.GetCurrentDirectory() + "/AssetCc2.exe");

            if (runAsset && hasAssetcc)
            {
                var sklbName = modelName.Substring(0, 5);

                if (selectedCategory.Equals(Strings.Head))
                {
                    sklbName = modelName.Substring(5, 5);
                }

                var skelLoc = Directory.GetCurrentDirectory() + "\\Skeletons\\";

                if (!File.Exists(skelLoc + sklbName + ".xml"))
                {
                    GetSkeleton(modelName, selectedCategory);

                    var proc = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = Directory.GetCurrentDirectory() + "/AssetCc2.exe",
                            Arguments = "-s \"" + skelLoc + "\\" + sklbName + ".sklb\" \"" + skelLoc + "\\" + sklbName + ".xml\"",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    proc.Start();
                    proc.WaitForExit();
                }
                skelDict.Clear();
                skelDict = ParseSkeleton(skelLoc + sklbName + ".xml", meshList);
            }
            else if(runAsset && !hasAssetcc)
            {
                MessageBox.Show("[SaveModel] No skeleton found for item. No .dae file will be saved. \n\nPlace AssetCc2(Not provided) in root folder to create skeleton.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }


            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings()
            {
                Indent = true,
            };

            using(XmlWriter xmlWriter = XmlWriter.Create(Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItemName + "/3D/" + modelName + ".DAE", xmlWriterSettings))
            {
                xmlWriter.WriteStartDocument();

                //<COLLADA>
                xmlWriter.WriteStartElement("COLLADA", "http://www.collada.org/2005/11/COLLADASchema");
                xmlWriter.WriteAttributeString("xmlns", "http://www.collada.org/2005/11/COLLADASchema");
                xmlWriter.WriteAttributeString("version", "1.4.1");
                xmlWriter.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");

                //Assets
                XMLassets(xmlWriter);

                ////Cameras
                //XMLcameras(xmlWriter);

                ////Lights
                //XMLlights(xmlWriter);

                //Images
                XMLimages(xmlWriter, modelName, meshList.Count, meshData);

                //effects
                XMLeffects(xmlWriter, modelName, meshList.Count, meshData);

                //Materials
                XMLmaterials(xmlWriter, modelName, meshList.Count);


                //Geometries
                XMLgeometries(xmlWriter, modelName, meshList);

                //Controllers
                XMLcontrollers(xmlWriter, modelName, meshList, skelDict);

                //Scenes
                XMLscenes(xmlWriter, modelName, meshList, skelDict);

                xmlWriter.WriteEndElement();
                //</COLLADA>

                xmlWriter.WriteEndDocument();

                xmlWriter.Flush();
                fullSkel.Clear();
                fullSkelnum.Clear();
            }

            return true;
        }

        private static void GetSkeleton(string modelName, string category)
        {
            var skelFolder = "";
            var skelFile = "";
            if (modelName[0].Equals('w'))
            {
                skelFolder = string.Format(Strings.WeapSkelFolder, modelName.Substring(1, 4), "0001");
                skelFile = string.Format(Strings.WeapSkelFile, modelName.Substring(1, 4), "0001");
            }
            else if (modelName[0].Equals('m'))
            {
                skelFolder = string.Format(Strings.MonsterSkelFolder, modelName.Substring(1, 4), "0001");
                skelFile = string.Format(Strings.MonsterSkelFile, modelName.Substring(1, 4), "0001");
            }
            else if (modelName[0].Equals('d'))
            {
                skelFolder = string.Format(Strings.DemiSkelFolder, modelName.Substring(1, 4), "0001");
                skelFile = string.Format(Strings.DemiSkelFile, modelName.Substring(1, 4), "0001");
            }
            else if (category.Equals(Strings.Head))
            {
                skelFolder = string.Format(Strings.MetSkelFolder, modelName.Substring(1, 4), modelName.Substring(6, 4));
                skelFile = string.Format(Strings.MetSkelFIle, modelName.Substring(1, 4), modelName.Substring(6, 4));
            }

            if(Helper.FileExists(FFCRC.GetHash(skelFile), FFCRC.GetHash(skelFolder), Strings.ItemsDat))
            {
                var offset = Helper.GetDataOffset(FFCRC.GetHash(skelFolder), FFCRC.GetHash(skelFile), Strings.ItemsDat);

                int datNum = ((offset / 8) & 0x000f) / 2;

                var sklbData = Helper.GetType2DecompressedData(offset, datNum, Strings.ItemsDat);
                byte[] havokData = null;

                using (BinaryReader br = new BinaryReader(new MemoryStream(sklbData)))
                {
                    br.BaseStream.Seek(0, SeekOrigin.Begin);

                    var magic = br.ReadInt32();
                    var format = br.ReadInt32();

                    br.ReadBytes(2);

                    if (magic != 0x736B6C62)
                    {
                        Debug.WriteLine("\nNot an SKLB file\n");
                        throw new FormatException();
                    }

                    int dataOffset = 0;
                    if (format == 0x31323030)
                    {
                        dataOffset = br.ReadInt16();
                    }
                    else if (format == 0x31333030)
                    {
                        br.ReadBytes(2);
                        dataOffset = br.ReadInt16();
                    }
                    else
                    {
                        Debug.WriteLine("\nUnknown Format" + format + "\n");
                        throw new FormatException();
                    }

                    br.BaseStream.Seek(dataOffset, SeekOrigin.Begin);

                    havokData = br.ReadBytes(sklbData.Length - dataOffset);

                    var mName = modelName.Substring(0, 5);

                    if (category.Equals(Strings.Head))
                    {
                        mName = modelName.Substring(5, 5);
                    }

                    File.WriteAllBytes(Directory.GetCurrentDirectory() + "/Skeletons/" + mName  + ".sklb", havokData);
                }
            }
            else
            {
                Debug.WriteLine("Could not find skeleton file: " + skelFolder + "/" + skelFile);
            }

        }

        private static Dictionary<string, JsonSkeleton> ParseSkeleton(string skelLoc, List<ModelMeshData> meshList)
        {
            JsonSkeleton jskel = new JsonSkeleton();
            List<string> jsonBones = new List<string>();

            List<int> parentIndices = new List<int>();
            List<string> boneNames = new List<string>();
            List<Matrix> matrix = new List<Matrix>();
            string referencepose = "";
            int boneCount = 0;

            using (XmlReader reader = XmlReader.Create(skelLoc))
            {
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        if (reader.Name.Equals("hkparam"))
                        {
                            var name = reader["name"];

                            if (name.Equals("parentIndices"))
                            {
                                parentIndices.AddRange((int[])reader.ReadElementContentAs(typeof(int[]), null));
                            }

                            if (name.Equals("bones"))
                            {
                                boneCount = int.Parse(reader["numelements"]);

                                while (reader.Read())
                                {
                                    if (reader.IsStartElement())
                                    {
                                        if (reader.Name.Equals("hkparam"))
                                        {
                                            name = reader["name"];
                                            if (name.Equals("name"))
                                            {
                                                boneNames.Add(reader.ReadElementContentAsString());
                                            }
                                            if (name.Equals("referencePose"))
                                            {
                                                break;
                                            }
                                        }
                                    }
                                }

                                referencepose = reader.ReadElementContentAsString();

                                string pattern = @"\(([^\(\)]+)\)";

                                var matches = Regex.Matches(referencepose, pattern);

                                for(int i = 0; i < matches.Count; i+=3)
                                {
                                    var t = matches[i].Groups[1].Value.Split(' ');
                                    var translation = new Vector3(float.Parse(t[0]), float.Parse(t[1]), float.Parse(t[2]));

                                    var r = matches[i + 1].Groups[1].Value.Split(' ');
                                    var rotation = new Vector4(float.Parse(r[0]), float.Parse(r[1]), float.Parse(r[2]), float.Parse(r[3]));

                                    var s = matches[i + 2].Groups[1].Value.Split(' ');
                                    var scale = new Vector3(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));

                                    var tMatrix = Matrix.Scaling(scale) * Matrix.RotationQuaternion(new Quaternion(rotation)) * Matrix.Translation(translation);

                                    matrix.Add(tMatrix);

                                }

                                break;
                            }
                        }
                    }
                }
            }

            var ReferencePose = new Matrix[boneCount];
            for (var target = 0; target < boneCount; ++target)
            {
                var current = target;
                ReferencePose[target] = Matrix.Identity;
                while (current >= 0)
                {
                    ReferencePose[target] = ReferencePose[target] * matrix[current];

                    current = parentIndices[current];
                }
            }

            for (int i = 0; i < boneCount; i++)
            {
                jskel.BoneNumber = i;
                jskel.BoneName = boneNames[i];
                jskel.BoneParent = parentIndices[i];

                List<float> poseMatrix = new List<float>();

                var rpl = matrix[i].ToArray();

                foreach (var f in rpl)
                {
                    if (f > 0.999 && f < 1)
                    {
                        poseMatrix.Add(1f);
                    }
                    else if (f > -0.001 && f < 0)
                    {
                        poseMatrix.Add(0f);
                    }
                    else if (f < 0.001 && f > 0)
                    {
                        poseMatrix.Add(0f);
                    }
                    else
                    {
                        poseMatrix.Add(f);
                    }
                }

                jskel.PoseMatrix = poseMatrix.ToArray();

                var InversePose = ReferencePose.Select(_ => Matrix.Invert(_)).ToArray();

                List<float> iposeMatrix = new List<float>();

                var ipl = InversePose[i].ToArray();

                foreach (var f in ipl)
                {
                    if (f > 0.999 && f < 1)
                    {
                        iposeMatrix.Add(1f);
                    }
                    else if (f > -0.001 && f < 0)
                    {
                        iposeMatrix.Add(0f);
                    }
                    else if (f < 0.001 && f > 0)
                    {
                        iposeMatrix.Add(0f);
                    }
                    else
                    {
                        iposeMatrix.Add(f);
                    }
                }

                jskel.InversePoseMatrix = iposeMatrix.ToArray();

                jsonBones.Add(JsonConvert.SerializeObject(jskel));
            }

            File.WriteAllLines(Path.ChangeExtension(skelLoc, ".skel"), jsonBones.ToArray());


            fullSkel.Clear();
            fullSkelnum.Clear();

            Dictionary<string, JsonSkeleton> skelDict = new Dictionary<string, JsonSkeleton>();

            var skeleton1 = File.ReadAllLines(Directory.GetCurrentDirectory() + "/Skeletons/" + Path.GetFileNameWithoutExtension(skelLoc) + ".skel");

            foreach (var b in skeleton1)
            {
                var j = JsonConvert.DeserializeObject<JsonSkeleton>(b);

                fullSkel.Add(j.BoneName, j);
                fullSkelnum.Add(j.BoneNumber, j);
            }

            foreach (var s in meshList[0].BoneStrings)
            {
                var skel = fullSkel[s];

                if (skel.BoneParent == -1)
                {
                    skelDict.Add(skel.BoneName, skel);
                }

                while (skel.BoneParent != -1)
                {
                    if (!skelDict.ContainsKey(skel.BoneName))
                    {
                        skelDict.Add(skel.BoneName, skel);
                    }
                    skel = fullSkelnum[skel.BoneParent];

                    if (skel.BoneParent == -1 && !skelDict.ContainsKey(skel.BoneName))
                    {
                        skelDict.Add(skel.BoneName, skel);
                    }
                }
            }

            File.Delete(Path.ChangeExtension(skelLoc, ".sklb"));
            File.Delete(Path.ChangeExtension(skelLoc, ".xml"));

            return skelDict;
        }

        private static void XMLassets(XmlWriter xmlWriter)
        {
            //<asset>
            xmlWriter.WriteStartElement("asset");

            //<contributor>
            xmlWriter.WriteStartElement("contributor");
            //<authoring_tool>
            xmlWriter.WriteStartElement("authoring_tool");
            xmlWriter.WriteString("FFXIV TexTools2");
            xmlWriter.WriteEndElement();
            //</authoring_tool>
            xmlWriter.WriteEndElement();
            //</contributor>

            //<created>
            xmlWriter.WriteStartElement("created");
            xmlWriter.WriteString(DateTime.Now.ToLongDateString());
            xmlWriter.WriteEndElement();
            //</created>

            //<unit>
            xmlWriter.WriteStartElement("unit");
            xmlWriter.WriteAttributeString("name", "inch");
            xmlWriter.WriteAttributeString("meter", "0.0254");
            xmlWriter.WriteEndElement();
            //</unit>

            //<up_axis>
            xmlWriter.WriteStartElement("up_axis");
            xmlWriter.WriteString("Y_UP");
            xmlWriter.WriteEndElement();
            //</up_axis>

            xmlWriter.WriteEndElement();
            //</asset>
        }

        private static void XMLcameras(XmlWriter xmlWriter)
        {
            //<library_cameras>
            xmlWriter.WriteStartElement("library_cameras");

            //<camera>
            xmlWriter.WriteStartElement("camera");
            xmlWriter.WriteAttributeString("id", "Camera-camera");
            xmlWriter.WriteAttributeString("name", "Camera");

            //<optics>
            xmlWriter.WriteStartElement("optics");
            //<technique_common>
            xmlWriter.WriteStartElement("technique_common");
            //<perspective>
            xmlWriter.WriteStartElement("perspective");

            //<xfov>
            xmlWriter.WriteStartElement("xfov");
            xmlWriter.WriteAttributeString("sid", "xfov");
            xmlWriter.WriteString("50");
            xmlWriter.WriteEndElement();
            //</xfov>

            //<aspect_ratio>
            xmlWriter.WriteStartElement("aspect_ratio");
            xmlWriter.WriteString("1.777778");
            xmlWriter.WriteEndElement();
            //</aspect_ratio>

            //<znear>
            xmlWriter.WriteStartElement("znear");
            xmlWriter.WriteAttributeString("sid", "znear");
            xmlWriter.WriteString("0.1");
            xmlWriter.WriteEndElement();
            //</znear>

            //<zfar>
            xmlWriter.WriteStartElement("zfar");
            xmlWriter.WriteAttributeString("sid", "zfar");
            xmlWriter.WriteString("100");
            xmlWriter.WriteEndElement();
            //</zfar>

            xmlWriter.WriteEndElement();
            //</perspective>

            xmlWriter.WriteEndElement();
            //</techniqe_common>

            xmlWriter.WriteEndElement();
            //</optics>

            xmlWriter.WriteEndElement();
            //</camera>

            xmlWriter.WriteEndElement();
            //</library_cameras>
        }

        private static void XMLlights(XmlWriter xmlWriter)
        {
            //<library_lights>
            xmlWriter.WriteStartElement("library_lights");
            //<light>
            xmlWriter.WriteStartElement("light");
            xmlWriter.WriteAttributeString("id", "Lamp-light");
            xmlWriter.WriteAttributeString("name", "Lamp");
            //<technique_common>
            xmlWriter.WriteStartElement("technique_common");
            //<point>
            xmlWriter.WriteStartElement("point");
            //<color>
            xmlWriter.WriteStartElement("color");
            xmlWriter.WriteAttributeString("sid", "color");
            xmlWriter.WriteString("1 1 1");
            xmlWriter.WriteEndElement();
            //</color>

            //<constant_attenuation>
            xmlWriter.WriteStartElement("constant_attenuation");
            xmlWriter.WriteString("1");
            xmlWriter.WriteEndElement();
            //</constant_attenuation>

            //<linear_attenuation>
            xmlWriter.WriteStartElement("linear_attenuation");
            xmlWriter.WriteString("0");
            xmlWriter.WriteEndElement();
            //</linear_attenuation>

            //<quadratic_attenuation>
            xmlWriter.WriteStartElement("quadratic_attenuation");
            xmlWriter.WriteString("0.00111109");
            xmlWriter.WriteEndElement();
            //</quadratic_attenuation>

            xmlWriter.WriteEndElement();
            //</point>
            xmlWriter.WriteEndElement();
            //</technique_common>

            xmlWriter.WriteEndElement();
            //</light>
            xmlWriter.WriteEndElement();
            //</library_lights>
        }

        private static void XMLimages(XmlWriter xmlWriter, string modelName, int meshCount, List<MDLTEXData> meshData)
        {
            //<library_images>
            xmlWriter.WriteStartElement("library_images");

            for(int i = 0; i < meshCount; i++)
            {
                //<image>
                xmlWriter.WriteStartElement("image");
                xmlWriter.WriteAttributeString("id", modelName + "_" + i + "_Diffuse_bmp");
                xmlWriter.WriteAttributeString("name", modelName + "_" + i + "_Diffuse_bmp");
                //<init_from>
                xmlWriter.WriteStartElement("init_from");
                xmlWriter.WriteString(modelName + "_" + i + "_Diffuse.bmp");
                xmlWriter.WriteEndElement();
                //</init_from>
                xmlWriter.WriteEndElement();
                //</image>
                //<image>
                xmlWriter.WriteStartElement("image");
                xmlWriter.WriteAttributeString("id", modelName + "_" + i + "_Normal_bmp");
                xmlWriter.WriteAttributeString("name", modelName + "_" + i + "_Normal_bmp");
                //<init_from>
                xmlWriter.WriteStartElement("init_from");
                xmlWriter.WriteString(modelName + "_" + i + "_Normal.bmp");
                xmlWriter.WriteEndElement();
                //</init_from>
                xmlWriter.WriteEndElement();
                //</image>

                if (!meshData[i].IsBody)
                {
                    //<image>
                    xmlWriter.WriteStartElement("image");
                    xmlWriter.WriteAttributeString("id", modelName + "_" + i + "_Specular_bmp");
                    xmlWriter.WriteAttributeString("name", modelName + "_" + i + "_Specular_bmp");
                    //<init_from>
                    xmlWriter.WriteStartElement("init_from");
                    xmlWriter.WriteString(modelName + "_" + i + "_Specular.bmp");
                    xmlWriter.WriteEndElement();
                    //</init_from>
                    xmlWriter.WriteEndElement();
                    //</image>
                    //<image>
                    xmlWriter.WriteStartElement("image");
                    xmlWriter.WriteAttributeString("id", modelName + "_" + i + "_Alpha_bmp");
                    xmlWriter.WriteAttributeString("name", modelName + "_" + i + "_Alpha_bmp");
                    //<init_from>
                    xmlWriter.WriteStartElement("init_from");
                    xmlWriter.WriteString(modelName + "_" + i + "_Alpha.bmp");
                    xmlWriter.WriteEndElement();
                    //</init_from>
                    xmlWriter.WriteEndElement();
                    //</image>
                }
            }

            xmlWriter.WriteEndElement();
            //</library_images>
        }

        private static void XMLeffects(XmlWriter xmlWriter, string modelName, int meshCount, List<MDLTEXData> meshData)
        {
            //<library_effects>
            xmlWriter.WriteStartElement("library_effects");

            for (int i = 0; i < meshCount; i++)
            {
                //<effect>
                xmlWriter.WriteStartElement("effect");
                xmlWriter.WriteAttributeString("id", modelName + "_" + i);
                //<profile_COMMON>
                xmlWriter.WriteStartElement("profile_COMMON");
                //<newparam>
                xmlWriter.WriteStartElement("newparam");
                xmlWriter.WriteAttributeString("sid", modelName + "_" + i + "_Diffuse_bmp-surface");
                //<surface>
                xmlWriter.WriteStartElement("surface");
                xmlWriter.WriteAttributeString("type", "2D");
                //<init_from>
                xmlWriter.WriteStartElement("init_from");
                xmlWriter.WriteString(modelName + "_" + i + "_Diffuse_bmp");
                xmlWriter.WriteEndElement();
                //</init_from>
                xmlWriter.WriteEndElement();
                //</surface>
                xmlWriter.WriteEndElement();
                //</newparam>
                //<newparam>
                xmlWriter.WriteStartElement("newparam");
                xmlWriter.WriteAttributeString("sid", modelName + "_" + i + "_Diffuse_bmp-sampler");
                //<sampler2D>
                xmlWriter.WriteStartElement("sampler2D");
                //<source>
                xmlWriter.WriteStartElement("source");
                xmlWriter.WriteString(modelName + "_" + i + "_Diffuse_bmp-surface");
                xmlWriter.WriteEndElement();
                //</source>
                xmlWriter.WriteEndElement();
                //</sampler2D>
                xmlWriter.WriteEndElement();
                //</newparam>

                if (!meshData[i].IsBody)
                {
                    //<newparam>
                    xmlWriter.WriteStartElement("newparam");
                    xmlWriter.WriteAttributeString("sid", modelName + "_" + i + "_Specular_bmp-surface");
                    //<surface>
                    xmlWriter.WriteStartElement("surface");
                    xmlWriter.WriteAttributeString("type", "2D");
                    //<init_from>
                    xmlWriter.WriteStartElement("init_from");
                    xmlWriter.WriteString(modelName + "_" + i + "_Specular_bmp");
                    xmlWriter.WriteEndElement();
                    //</init_from>
                    xmlWriter.WriteEndElement();
                    //</surface>
                    xmlWriter.WriteEndElement();
                    //</newparam>
                    //<newparam>
                    xmlWriter.WriteStartElement("newparam");
                    xmlWriter.WriteAttributeString("sid", modelName + "_" + i + "_Specular_bmp-sampler");
                    //<sampler2D>
                    xmlWriter.WriteStartElement("sampler2D");
                    //<source>
                    xmlWriter.WriteStartElement("source");
                    xmlWriter.WriteString(modelName + "_" + i + "_Specular_bmp-surface");
                    xmlWriter.WriteEndElement();
                    //</source>
                    xmlWriter.WriteEndElement();
                    //</sampler2D>
                    xmlWriter.WriteEndElement();
                    //</newparam>
                }

                //<newparam>
                xmlWriter.WriteStartElement("newparam");
                xmlWriter.WriteAttributeString("sid", modelName + "_" + i + "_Normal_bmp-surface");
                //<surface>
                xmlWriter.WriteStartElement("surface");
                xmlWriter.WriteAttributeString("type", "2D");
                //<init_from>
                xmlWriter.WriteStartElement("init_from");
                xmlWriter.WriteString(modelName + "_" + i + "_Normal_bmp");
                xmlWriter.WriteEndElement();
                //</init_from>
                xmlWriter.WriteEndElement();
                //</surface>
                xmlWriter.WriteEndElement();
                //</newparam>
                //<newparam>
                xmlWriter.WriteStartElement("newparam");
                xmlWriter.WriteAttributeString("sid", modelName + "_" + i + "_Normal_bmp-sampler");
                //<sampler2D>
                xmlWriter.WriteStartElement("sampler2D");
                //<source>
                xmlWriter.WriteStartElement("source");
                xmlWriter.WriteString(modelName + "_" + i + "_Normal_bmp-surface");
                xmlWriter.WriteEndElement();
                //</source>
                xmlWriter.WriteEndElement();
                //</sampler2D>
                xmlWriter.WriteEndElement();
                //</newparam>

                if (!meshData[i].IsBody)
                {
                    //<newparam>
                    xmlWriter.WriteStartElement("newparam");
                    xmlWriter.WriteAttributeString("sid", modelName + "_" + i + "_Alpha_bmp-surface");
                    //<surface>
                    xmlWriter.WriteStartElement("surface");
                    xmlWriter.WriteAttributeString("type", "2D");
                    //<init_from>
                    xmlWriter.WriteStartElement("init_from");
                    xmlWriter.WriteString(modelName + "_" + i + "_Alpha_bmp");
                    xmlWriter.WriteEndElement();
                    //</init_from>
                    xmlWriter.WriteEndElement();
                    //</surface>
                    xmlWriter.WriteEndElement();
                    //</newparam>
                    //<newparam>
                    xmlWriter.WriteStartElement("newparam");
                    xmlWriter.WriteAttributeString("sid", modelName + "_" + i + "_Alpha_bmp-sampler");
                    //<sampler2D>
                    xmlWriter.WriteStartElement("sampler2D");
                    //<source>
                    xmlWriter.WriteStartElement("source");
                    xmlWriter.WriteString(modelName + "_" + i + "_Alpha_bmp-surface");
                    xmlWriter.WriteEndElement();
                    //</source>
                    xmlWriter.WriteEndElement();
                    //</sampler2D>
                    xmlWriter.WriteEndElement();
                    //</newparam>
                }

                //<technique>
                xmlWriter.WriteStartElement("technique");
                xmlWriter.WriteAttributeString("sid", "common");
                //<phong>
                xmlWriter.WriteStartElement("phong");
                //<diffuse>
                xmlWriter.WriteStartElement("diffuse");
                //<texture>
                xmlWriter.WriteStartElement("texture");
                xmlWriter.WriteAttributeString("texture", modelName + "_" + i + "_Diffuse_bmp-sampler");
                xmlWriter.WriteAttributeString("texcoord", "geom-" + modelName + "_" + i + "-map1");
                xmlWriter.WriteEndElement();
                //</texture>
                xmlWriter.WriteEndElement();
                //</diffuse>

                if (!meshData[i].IsBody)
                {
                    //<specular>
                    xmlWriter.WriteStartElement("specular");
                    //<texture>
                    xmlWriter.WriteStartElement("texture");
                    xmlWriter.WriteAttributeString("texture", modelName + "_" + i + "_Specular_bmp-sampler");
                    xmlWriter.WriteAttributeString("texcoord", "geom-" + modelName + "_" + i + "-map1");
                    xmlWriter.WriteEndElement();
                    //</texture>
                    xmlWriter.WriteEndElement();
                    //</specular>
                    //<transparent>
                    xmlWriter.WriteStartElement("transparent");
                    xmlWriter.WriteAttributeString("opaque", "A_ONE");
                    //<texture>
                    xmlWriter.WriteStartElement("texture");
                    xmlWriter.WriteAttributeString("texture", modelName + "_" + i + "_Alpha_bmp-sampler");
                    xmlWriter.WriteAttributeString("texcoord", "geom-" + modelName + "_" + i + "-map1");
                    xmlWriter.WriteEndElement();
                    //</texture>
                    xmlWriter.WriteEndElement();
                    //</transparent>
                }

                xmlWriter.WriteEndElement();
                //</phong>
                //<extra>
                xmlWriter.WriteStartElement("extra");
                //<technique>
                xmlWriter.WriteStartElement("technique");
                xmlWriter.WriteAttributeString("profile", "OpenCOLLADA3dsMax");

                if (!meshData[i].IsBody)
                {
                    //<specularLevel>
                    xmlWriter.WriteStartElement("specularLevel");
                    //<texture>
                    xmlWriter.WriteStartElement("texture");
                    xmlWriter.WriteAttributeString("texture", modelName + "_" + i + "_Specular_bmp-sampler");
                    xmlWriter.WriteAttributeString("texcoord", "geom-" + modelName + "_" + i + "-map1");
                    xmlWriter.WriteEndElement();
                    //</texture>
                    xmlWriter.WriteEndElement();
                    //</specularLevel>
                }

                //<bump>
                xmlWriter.WriteStartElement("bump");
                xmlWriter.WriteAttributeString("bumptype", "HEIGHTFIELD");
                //<texture>
                xmlWriter.WriteStartElement("texture");
                xmlWriter.WriteAttributeString("texture", modelName + "_" + i + "_Normal_bmp-sampler");
                xmlWriter.WriteAttributeString("texcoord", "geom-" + modelName + "_" + i + "-map1");
                xmlWriter.WriteEndElement();
                //</texture>
                xmlWriter.WriteEndElement();
                //</bump>
                xmlWriter.WriteEndElement();
                //</technique>
                xmlWriter.WriteEndElement();
                //</extra>
                xmlWriter.WriteEndElement();
                //</technique>
                xmlWriter.WriteEndElement();
                //</profile_COMMON>
                xmlWriter.WriteEndElement();
                //</effect>
            }

            xmlWriter.WriteEndElement();
            //</library_effects>
        }

        private static void XMLmaterials(XmlWriter xmlWriter, string modelName, int meshCount)
        {
            //<library_materials>
            xmlWriter.WriteStartElement("library_materials");
            for(int i = 0; i < meshCount; i++)
            {
                //<material>
                xmlWriter.WriteStartElement("material");
                xmlWriter.WriteAttributeString("id", modelName + "_" + i + "-material");
                xmlWriter.WriteAttributeString("name", modelName + "_" + i);
                //<instance_effect>
                xmlWriter.WriteStartElement("instance_effect");
                xmlWriter.WriteAttributeString("url", "#" + modelName + "_" + i);
                xmlWriter.WriteEndElement();
                //</instance_effect>
                xmlWriter.WriteEndElement();
                //</material>
            }

            xmlWriter.WriteEndElement();
            //</library_materials>
        }

        private static void XMLgeometries(XmlWriter xmlWriter, string modelName, List<ModelMeshData> meshList)
        {
            //<library_geometries>
            xmlWriter.WriteStartElement("library_geometries");

            for(int i = 0; i < meshList.Count; i++)
            {
                if(meshList[i].Vertices.Count > 0)
                {
                    var prevIndexCount = 0;
                    var totalVertices = 0;
                    for (int j = 0; j < meshList[i].MeshPartList.Count; j++)
                    {
                        var indexCount = meshList[i].MeshPartList[j].IndexCount;

                        if(indexCount > 0)
                        {
                            List<int> indexList = new List<int>();
                            HashSet<int> indexHashSet = new HashSet<int>();

                            indexList = meshList[i].Indices.GetRange(prevIndexCount, indexCount);

                            foreach (var index in indexList)
                            {
                                if (index > totalVertices)
                                {
                                    indexHashSet.Add(index);
                                }
                            }

                            int totalCount = indexHashSet.Count + 1;

                            int indexMin = 0;
                            int indexMax = 0;

                            if (indexList.Count != 0)
                            {
                                indexMin = indexList.Min();
                                indexMax = indexList.Max() + 1;
                            }

                            var partString = "." + j;

                            if (j == 0)
                            {
                                partString = "";
                            }

                            //<geometry>
                            xmlWriter.WriteStartElement("geometry");
                            xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + partString);
                            xmlWriter.WriteAttributeString("name", modelName + "_" + i + partString);
                            //<mesh>
                            xmlWriter.WriteStartElement("mesh");

                            /*
                             * --------------------
                             * Verticies
                             * --------------------
                             */

                            //<source>
                            xmlWriter.WriteStartElement("source");
                            xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + partString + "-positions");
                            //<float_array>
                            xmlWriter.WriteStartElement("float_array");
                            xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + partString + "-positions-array");
                            xmlWriter.WriteAttributeString("count", (totalCount * 3).ToString());

                            var positions = meshList[i].Vertices.GetRange(totalVertices, totalCount);

                            foreach (var v in positions)
                            {
                                xmlWriter.WriteString((v.X * Info.modelMultiplier).ToString() + " " + (v.Y * Info.modelMultiplier).ToString() + " " + (v.Z * Info.modelMultiplier).ToString() + " ");
                            }

                            xmlWriter.WriteEndElement();
                            //</float_array>

                            //<technique_common>
                            xmlWriter.WriteStartElement("technique_common");
                            //<accessor>
                            xmlWriter.WriteStartElement("accessor");
                            xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + partString + "-positions-array");
                            xmlWriter.WriteAttributeString("count", totalCount.ToString());
                            xmlWriter.WriteAttributeString("stride", "3");

                            //<param>
                            xmlWriter.WriteStartElement("param");
                            xmlWriter.WriteAttributeString("name", "X");
                            xmlWriter.WriteAttributeString("type", "float");
                            xmlWriter.WriteEndElement();
                            //</param>

                            //<param>
                            xmlWriter.WriteStartElement("param");
                            xmlWriter.WriteAttributeString("name", "Y");
                            xmlWriter.WriteAttributeString("type", "float");
                            xmlWriter.WriteEndElement();
                            //</param>

                            //<param>
                            xmlWriter.WriteStartElement("param");
                            xmlWriter.WriteAttributeString("name", "Z");
                            xmlWriter.WriteAttributeString("type", "float");
                            xmlWriter.WriteEndElement();
                            //</param>

                            xmlWriter.WriteEndElement();
                            //</accessor>
                            xmlWriter.WriteEndElement();
                            //</technique_common>
                            xmlWriter.WriteEndElement();
                            //</source>

                            /*
                             * --------------------
                             * Normals
                             * --------------------
                             */

                            //<source>
                            xmlWriter.WriteStartElement("source");
                            xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + partString + "-normals");
                            //<float_array>
                            xmlWriter.WriteStartElement("float_array");
                            xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + partString + "-normals-array");
                            xmlWriter.WriteAttributeString("count", (totalCount * 3).ToString());

                            var normals = meshList[i].Normals.GetRange(totalVertices, totalCount);

                            foreach (var n in normals)
                            {
                                xmlWriter.WriteString(n.X.ToString() + " " + n.Y.ToString() + " " + n.Z.ToString() + " ");
                            }

                            xmlWriter.WriteEndElement();
                            //</float_array>

                            //<technique_common>
                            xmlWriter.WriteStartElement("technique_common");
                            //<accessor>
                            xmlWriter.WriteStartElement("accessor");
                            xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + partString + "-normals-array");
                            xmlWriter.WriteAttributeString("count", (totalCount.ToString()));
                            xmlWriter.WriteAttributeString("stride", "3");

                            //<param>
                            xmlWriter.WriteStartElement("param");
                            xmlWriter.WriteAttributeString("name", "X");
                            xmlWriter.WriteAttributeString("type", "float");
                            xmlWriter.WriteEndElement();
                            //</param>

                            //<param>
                            xmlWriter.WriteStartElement("param");
                            xmlWriter.WriteAttributeString("name", "Y");
                            xmlWriter.WriteAttributeString("type", "float");
                            xmlWriter.WriteEndElement();
                            //</param>

                            //<param>
                            xmlWriter.WriteStartElement("param");
                            xmlWriter.WriteAttributeString("name", "Z");
                            xmlWriter.WriteAttributeString("type", "float");
                            xmlWriter.WriteEndElement();
                            //</param>

                            xmlWriter.WriteEndElement();
                            //</accessor>
                            xmlWriter.WriteEndElement();
                            //</technique_common>
                            xmlWriter.WriteEndElement();
                            //</source>

                            /*
                             * --------------------
                             * Texture Coordinates
                             * --------------------
                             */

                            //<source>
                            xmlWriter.WriteStartElement("source");
                            xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + partString + "-map0");
                            //<float_array>
                            xmlWriter.WriteStartElement("float_array");
                            xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + partString + "-map0-array");
                            xmlWriter.WriteAttributeString("count", (totalCount * 2).ToString());

                            //var texCoords = meshList[i].TextureCoordinates.GetRange(totalVertices, totalCount);
                            var texCoords = meshList[i].TextureCoordinates.GetRange(totalVertices, totalCount);

                            foreach (var tc in texCoords)
                            {
                                xmlWriter.WriteString(tc.X.ToString() + " " + (tc.Y * -1).ToString() + " ");
                            }

                            xmlWriter.WriteEndElement();
                            //</float_array>

                            //<technique_common>
                            xmlWriter.WriteStartElement("technique_common");
                            //<accessor>
                            xmlWriter.WriteStartElement("accessor");
                            xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + partString + "-map0-array");
                            xmlWriter.WriteAttributeString("count", totalCount.ToString());
                            xmlWriter.WriteAttributeString("stride", "2");

                            //<param>
                            xmlWriter.WriteStartElement("param");
                            xmlWriter.WriteAttributeString("name", "S");
                            xmlWriter.WriteAttributeString("type", "float");
                            xmlWriter.WriteEndElement();
                            //</param>

                            //<param>
                            xmlWriter.WriteStartElement("param");
                            xmlWriter.WriteAttributeString("name", "T");
                            xmlWriter.WriteAttributeString("type", "float");
                            xmlWriter.WriteEndElement();
                            //</param>

                            xmlWriter.WriteEndElement();
                            //</accessor>
                            xmlWriter.WriteEndElement();
                            //</technique_common>
                            xmlWriter.WriteEndElement();
                            //</source>


                            /*
                             * --------------------
                             * Tangents
                             * --------------------
                             */

                            //<source>
                            xmlWriter.WriteStartElement("source");
                            xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + partString + "-map0-textangents");
                            //<float_array>
                            xmlWriter.WriteStartElement("float_array");
                            xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + partString + "-map0-textangents-array");
                            xmlWriter.WriteAttributeString("count", (totalCount * 3).ToString());

                            var tangents = meshList[i].Tangents.GetRange(totalVertices, totalCount);

                            foreach (var tan in tangents)
                            {
                                xmlWriter.WriteString(tan.X.ToString() + " " + tan.Y.ToString() + " " + tan.Z.ToString() + " ");
                            }

                            xmlWriter.WriteEndElement();
                            //</float_array>

                            //<technique_common>
                            xmlWriter.WriteStartElement("technique_common");
                            //<accessor>
                            xmlWriter.WriteStartElement("accessor");
                            xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + partString + "-map0-textangents-array");
                            xmlWriter.WriteAttributeString("count", totalCount.ToString());
                            xmlWriter.WriteAttributeString("stride", "3");

                            //<param>
                            xmlWriter.WriteStartElement("param");
                            xmlWriter.WriteAttributeString("name", "X");
                            xmlWriter.WriteAttributeString("type", "float");
                            xmlWriter.WriteEndElement();
                            //</param>

                            //<param>
                            xmlWriter.WriteStartElement("param");
                            xmlWriter.WriteAttributeString("name", "Y");
                            xmlWriter.WriteAttributeString("type", "float");
                            xmlWriter.WriteEndElement();
                            //</param>

                            //<param>
                            xmlWriter.WriteStartElement("param");
                            xmlWriter.WriteAttributeString("name", "Z");
                            xmlWriter.WriteAttributeString("type", "float");
                            xmlWriter.WriteEndElement();
                            //</param>

                            xmlWriter.WriteEndElement();
                            //</accessor>
                            xmlWriter.WriteEndElement();
                            //</technique_common>
                            xmlWriter.WriteEndElement();
                            //</source>



                            /*
                             * --------------------
                             * Binormals
                             * --------------------
                             */

                            //<source>
                            xmlWriter.WriteStartElement("source");
                            xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + partString + "-map0-texbinormals");
                            //<float_array>
                            xmlWriter.WriteStartElement("float_array");
                            xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + partString + "-map0-texbinormals-array");
                            xmlWriter.WriteAttributeString("count", (totalCount * 3).ToString());

                            var biNormals = meshList[i].BiTangents.GetRange(totalVertices, totalCount);

                            foreach (var bn in biNormals)
                            {
                                xmlWriter.WriteString(bn.X.ToString() + " " + bn.Y.ToString() + " " + bn.Z.ToString() + " ");
                            }

                            xmlWriter.WriteEndElement();
                            //</float_array>

                            //<technique_common>
                            xmlWriter.WriteStartElement("technique_common");
                            //<accessor>
                            xmlWriter.WriteStartElement("accessor");
                            xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + partString + "-map0-texbinormals-array");
                            xmlWriter.WriteAttributeString("count", totalCount.ToString());
                            xmlWriter.WriteAttributeString("stride", "3");

                            //<param>
                            xmlWriter.WriteStartElement("param");
                            xmlWriter.WriteAttributeString("name", "X");
                            xmlWriter.WriteAttributeString("type", "float");
                            xmlWriter.WriteEndElement();
                            //</param>

                            //<param>
                            xmlWriter.WriteStartElement("param");
                            xmlWriter.WriteAttributeString("name", "Y");
                            xmlWriter.WriteAttributeString("type", "float");
                            xmlWriter.WriteEndElement();
                            //</param>

                            //<param>
                            xmlWriter.WriteStartElement("param");
                            xmlWriter.WriteAttributeString("name", "Z");
                            xmlWriter.WriteAttributeString("type", "float");
                            xmlWriter.WriteEndElement();
                            //</param>

                            xmlWriter.WriteEndElement();
                            //</accessor>
                            xmlWriter.WriteEndElement();
                            //</technique_common>
                            xmlWriter.WriteEndElement();
                            //</source>



                            //<vertices>
                            xmlWriter.WriteStartElement("vertices");
                            xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + partString + "-vertices");
                            //<input>
                            xmlWriter.WriteStartElement("input");
                            xmlWriter.WriteAttributeString("semantic", "POSITION");
                            xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + partString + "-positions");
                            xmlWriter.WriteEndElement();
                            //</input>
                            xmlWriter.WriteEndElement();
                            //</vertices>


                            //<triangles>
                            xmlWriter.WriteStartElement("triangles");
                            xmlWriter.WriteAttributeString("material", modelName + "_" + i);
                            xmlWriter.WriteAttributeString("count", (indexCount / 3).ToString());
                            //<input>
                            xmlWriter.WriteStartElement("input");
                            xmlWriter.WriteAttributeString("semantic", "VERTEX");
                            xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + partString + "-vertices");
                            xmlWriter.WriteAttributeString("offset", "0");
                            xmlWriter.WriteEndElement();
                            //</input>

                            //<input>
                            xmlWriter.WriteStartElement("input");
                            xmlWriter.WriteAttributeString("semantic", "NORMAL");
                            xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + partString + "-normals");
                            xmlWriter.WriteAttributeString("offset", "1");
                            xmlWriter.WriteEndElement();
                            //</input>

                            //<input>
                            xmlWriter.WriteStartElement("input");
                            xmlWriter.WriteAttributeString("semantic", "TEXCOORD");
                            xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + partString + "-map0");
                            xmlWriter.WriteAttributeString("offset", "2");
                            xmlWriter.WriteAttributeString("set", "0");
                            xmlWriter.WriteEndElement();
                            //</input>


                            //<input>
                            xmlWriter.WriteStartElement("input");
                            xmlWriter.WriteAttributeString("semantic", "TEXTANGENT");
                            xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + partString + "-map0-textangents");
                            xmlWriter.WriteAttributeString("offset", "3");
                            xmlWriter.WriteAttributeString("set", "1");
                            xmlWriter.WriteEndElement();
                            //</input>

                            //<input>
                            xmlWriter.WriteStartElement("input");
                            xmlWriter.WriteAttributeString("semantic", "TEXBINORMAL");
                            xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + partString + "-map0-texbinormals");
                            xmlWriter.WriteAttributeString("offset", "3");
                            xmlWriter.WriteAttributeString("set", "1");
                            xmlWriter.WriteEndElement();
                            //</input>

                            //<p>
                            xmlWriter.WriteStartElement("p");
                            foreach (var ind in indexList)
                            {
                                int p = ind - totalVertices;

                                if (p >= 0)
                                {
                                    xmlWriter.WriteString(p + " " + p + " " + p + " " + p + " ");
                                }
                            }
                            xmlWriter.WriteEndElement();
                            //</p>

                            xmlWriter.WriteEndElement();
                            //</triangles>
                            xmlWriter.WriteEndElement();
                            //</mesh>
                            xmlWriter.WriteEndElement();
                            //</geometry>

                            prevIndexCount += indexCount;
                            totalVertices += totalCount;
                        }
                    }
                }
            }

            xmlWriter.WriteEndElement();
            //</library_geometries>
        }

        private static void XMLcontrollers(XmlWriter xmlWriter, string modelName, List<ModelMeshData> meshDataList, Dictionary<string, JsonSkeleton> skelDict)
        {

            //<library_controllers>
            xmlWriter.WriteStartElement("library_controllers");
            for(int i = 0; i < meshDataList.Count; i++)
            {
                if(meshDataList[i].WeightCounts.Count > 0)
                {
                    var prevWeightCount = 0;
                    var prevIndexCount = 0;
                    var totalVertices = 0;
                    for (int j = 0; j < meshDataList[i].MeshPartList.Count; j++)
                    {
                        var indexCount = meshDataList[i].MeshPartList[j].IndexCount;

                        if(indexCount > 0)
                        {
                            List<int> indexList = meshDataList[i].Indices.GetRange(prevIndexCount, indexCount);

                            HashSet<int> indexHashSet = new HashSet<int>();

                            foreach (var index in indexList)
                            {
                                if (index > totalVertices)
                                {
                                    indexHashSet.Add(index);
                                }
                            }

                            int totalCount = indexHashSet.Count + 1;

                            int totalWeights = 0;

                            int indexMin = 0;
                            int indexMax = 0;

                            if (indexList.Count != 0)
                            {
                                indexMin = indexList.Min();
                                indexMax = indexList.Max() + 1;
                            }

                            var weights = meshDataList[i].WeightCounts.GetRange(totalVertices, totalCount);

                            foreach (var w in weights)
                            {
                                totalWeights += w;
                            }

                            var partString = "." + j;

                            if (j == 0)
                            {
                                partString = "";
                            }

                            //<controller>
                            xmlWriter.WriteStartElement("controller");
                            xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + partString + "-skin1");
                            //<skin>
                            xmlWriter.WriteStartElement("skin");
                            xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + partString);
                            //<bind_shape_matrix>
                            xmlWriter.WriteStartElement("bind_shape_matrix");
                            xmlWriter.WriteString("1 0 0 0 0 1 0 0 0 0 1 0 0 0 0 1");
                            xmlWriter.WriteEndElement();
                            //</bind_shape_matrix>

                            //<source>
                            xmlWriter.WriteStartElement("source");
                            xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + partString + "-skin1-joints");
                            //<Name_array>
                            xmlWriter.WriteStartElement("Name_array");
                            xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + partString + "-skin1-joints-array");
                            xmlWriter.WriteAttributeString("count", meshDataList[i].BoneStrings.Count.ToString());
                            foreach (var b in meshDataList[i].BoneStrings)
                            {
                                xmlWriter.WriteString(b + " ");
                            }
                            xmlWriter.WriteEndElement();
                            //</Name_array>

                            //<technique_common>
                            xmlWriter.WriteStartElement("technique_common");
                            //<accessor>
                            xmlWriter.WriteStartElement("accessor");
                            xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + partString + "-skin1-joints-array");
                            xmlWriter.WriteAttributeString("count", meshDataList[i].BoneStrings.Count.ToString());
                            xmlWriter.WriteAttributeString("stride", "1");
                            //<param>
                            xmlWriter.WriteStartElement("param");
                            xmlWriter.WriteAttributeString("name", "JOINT");
                            xmlWriter.WriteAttributeString("type", "name");
                            xmlWriter.WriteEndElement();
                            //</param>
                            xmlWriter.WriteEndElement();
                            //</accessor>
                            xmlWriter.WriteEndElement();
                            //</technique_common>
                            xmlWriter.WriteEndElement();
                            //</source>


                            //<source>
                            xmlWriter.WriteStartElement("source");
                            xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + partString + "-skin1-bind_poses");
                            //<Name_array>
                            xmlWriter.WriteStartElement("float_array");
                            xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + partString + "-skin1-bind_poses-array");
                            xmlWriter.WriteAttributeString("count", (16 * meshDataList[i].BoneStrings.Count).ToString());

                            for (int m = 0; m < meshDataList[i].BoneStrings.Count; m++)
                            {
                                try
                                {
                                    Matrix matrix = new Matrix(skelDict[meshDataList[i].BoneStrings[m]].InversePoseMatrix);

                                    xmlWriter.WriteString(matrix.Column1.X + " " + matrix.Column1.Y + " " + matrix.Column1.Z + " " + (matrix.Column1.W * Info.modelMultiplier) + " ");
                                    xmlWriter.WriteString(matrix.Column2.X + " " + matrix.Column2.Y + " " + matrix.Column2.Z + " " + (matrix.Column2.W * Info.modelMultiplier) + " ");
                                    xmlWriter.WriteString(matrix.Column3.X + " " + matrix.Column3.Y + " " + matrix.Column3.Z + " " + (matrix.Column3.W * Info.modelMultiplier) + " ");
                                    xmlWriter.WriteString(matrix.Column4.X + " " + matrix.Column4.Y + " " + matrix.Column4.Z + " " + (matrix.Column4.W * Info.modelMultiplier) + " ");
                                }
                                catch
                                {
                                    Debug.WriteLine("Error at " + meshDataList[i].BoneStrings[m]);
                                }

                            }
                            xmlWriter.WriteEndElement();
                            //</Name_array>

                            //<technique_common>
                            xmlWriter.WriteStartElement("technique_common");
                            //<accessor>
                            xmlWriter.WriteStartElement("accessor");
                            xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + partString + "-skin1-bind_poses-array");
                            xmlWriter.WriteAttributeString("count", meshDataList[i].BoneStrings.Count.ToString());
                            xmlWriter.WriteAttributeString("stride", "16");
                            //<param>
                            xmlWriter.WriteStartElement("param");
                            xmlWriter.WriteAttributeString("name", "TRANSFORM");
                            xmlWriter.WriteAttributeString("type", "float4x4");
                            xmlWriter.WriteEndElement();
                            //</param>
                            xmlWriter.WriteEndElement();
                            //</accessor>
                            xmlWriter.WriteEndElement();
                            //</technique_common>
                            xmlWriter.WriteEndElement();
                            //</source>


                            //<source>
                            xmlWriter.WriteStartElement("source");
                            xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + partString + "-skin1-weights");
                            //<Name_array>
                            xmlWriter.WriteStartElement("float_array");
                            xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + partString + "-skin1-weights-array");
                            xmlWriter.WriteAttributeString("count", totalWeights.ToString());

                            if (indexMax != 0)
                            {
                                for (int a = prevWeightCount; a < totalWeights + prevWeightCount; a++)
                                {
                                    xmlWriter.WriteString(meshDataList[i].BlendWeights[a] + " ");
                                }
                            }

                            xmlWriter.WriteEndElement();
                            //</Name_array>

                            //<technique_common>
                            xmlWriter.WriteStartElement("technique_common");
                            //<accessor>
                            xmlWriter.WriteStartElement("accessor");
                            xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + partString + "-skin1-weights-array");
                            xmlWriter.WriteAttributeString("count", totalWeights.ToString());
                            xmlWriter.WriteAttributeString("stride", "1");
                            //<param>
                            xmlWriter.WriteStartElement("param");
                            xmlWriter.WriteAttributeString("name", "WEIGHT");
                            xmlWriter.WriteAttributeString("type", "float");
                            xmlWriter.WriteEndElement();
                            //</param>
                            xmlWriter.WriteEndElement();
                            //</accessor>
                            xmlWriter.WriteEndElement();
                            //</technique_common>
                            xmlWriter.WriteEndElement();
                            //</source>

                            //<joints>
                            xmlWriter.WriteStartElement("joints");
                            //<input>
                            xmlWriter.WriteStartElement("input");
                            xmlWriter.WriteAttributeString("semantic", "JOINT");
                            xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + partString + "-skin1-joints");
                            xmlWriter.WriteEndElement();
                            //</input>

                            //<input>
                            xmlWriter.WriteStartElement("input");
                            xmlWriter.WriteAttributeString("semantic", "INV_BIND_MATRIX");
                            xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + partString + "-skin1-bind_poses");
                            xmlWriter.WriteEndElement();
                            //</input>
                            xmlWriter.WriteEndElement();
                            //</joints>

                            //<vertex_weights>
                            xmlWriter.WriteStartElement("vertex_weights");
                            xmlWriter.WriteAttributeString("count", indexMax.ToString());
                            //<input>
                            xmlWriter.WriteStartElement("input");
                            xmlWriter.WriteAttributeString("semantic", "JOINT");
                            xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + partString + "-skin1-joints");
                            xmlWriter.WriteAttributeString("offset", "0");
                            xmlWriter.WriteEndElement();
                            //</input>
                            //<input>
                            xmlWriter.WriteStartElement("input");
                            xmlWriter.WriteAttributeString("semantic", "WEIGHT");
                            xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + partString + "-skin1-weights");
                            xmlWriter.WriteAttributeString("offset", "1");
                            xmlWriter.WriteEndElement();
                            //</input>
                            //<vcount>
                            xmlWriter.WriteStartElement("vcount");

                            var weightlist = meshDataList[i].WeightCounts.GetRange(totalVertices, totalCount);

                            foreach (var wc in weightlist)
                            {
                                xmlWriter.WriteString(wc + " ");
                            }

                            xmlWriter.WriteEndElement();
                            //</vcount>
                            //<v>
                            xmlWriter.WriteStartElement("v");
                            int blin = 0;

                            var blendind = meshDataList[i].BlendIndices.GetRange(prevWeightCount, totalWeights);

                            foreach (var bi in blendind)
                            {
                                xmlWriter.WriteString(bi + " " + blin + " ");
                                blin++;
                            }

                            xmlWriter.WriteEndElement();
                            //</v>
                            xmlWriter.WriteEndElement();
                            //</vertex_weights>

                            xmlWriter.WriteEndElement();
                            //</skin>
                            xmlWriter.WriteEndElement();
                            //</controller>

                            prevWeightCount += totalWeights;
                            prevIndexCount += indexCount;
                            totalVertices += totalCount;
                        }
                    }
                }
            }

            xmlWriter.WriteEndElement();
            //</library_controllers>
        }

        private static void XMLscenes(XmlWriter xmlWriter, string modelName, List<ModelMeshData> meshDataList, Dictionary<string, JsonSkeleton> skelDict)
        {
            //<library_visual_scenes>
            xmlWriter.WriteStartElement("library_visual_scenes");
            //<visual_scene>
            xmlWriter.WriteStartElement("visual_scene");
            xmlWriter.WriteAttributeString("id", "Scene");

            List<string> boneParents = new List<string>();

            SortedSet<int> bsort = new SortedSet<int>();
            SortedSet<int> bsort2 = new SortedSet<int>();

            try
            {
                var firstBone = skelDict["n_root"];
                WriteBones(xmlWriter, firstBone, skelDict);
                boneParents.Add("n_root");
            }
            catch
            {
                var firstBone = skelDict["j_kao"];
                WriteBones(xmlWriter, firstBone, skelDict);
                boneParents.Add("j_kao");
            }


            for (int i = 0; i < meshDataList.Count; i++)
            {
                //<node>
                xmlWriter.WriteStartElement("node");
                xmlWriter.WriteAttributeString("id", "node-Group_" + i);
                xmlWriter.WriteAttributeString("name", "Group_" + i);
                for (int j = 0; j < meshDataList[i].MeshPartList.Count; j++)
                {
                    var partString = "." + j;

                    if (j == 0)
                    {
                        partString = "";
                    }

                    //<node>
                    xmlWriter.WriteStartElement("node");
                    xmlWriter.WriteAttributeString("id", "node-" + modelName + "_" + i + partString);
                    xmlWriter.WriteAttributeString("name", modelName + "_" + i + partString);

                    //<instance_controller>
                    xmlWriter.WriteStartElement("instance_controller");
                    xmlWriter.WriteAttributeString("url", "#geom-" + modelName + "_" + i + partString + "-skin1");

                    foreach (var b in boneParents)
                    {
                        //<skeleton> 
                        xmlWriter.WriteStartElement("skeleton");
                        xmlWriter.WriteString("#node-" + b);
                        xmlWriter.WriteEndElement();
                        //</skeleton> 
                    }


                    //<bind_material>
                    xmlWriter.WriteStartElement("bind_material");
                    //<technique_common>
                    xmlWriter.WriteStartElement("technique_common");
                    //<instance_material>
                    xmlWriter.WriteStartElement("instance_material");
                    xmlWriter.WriteAttributeString("symbol", modelName + "_" + i);
                    xmlWriter.WriteAttributeString("target", "#" + modelName + "_" + i + "-material");
                    //<bind_vertex_input>
                    xmlWriter.WriteStartElement("bind_vertex_input");
                    xmlWriter.WriteAttributeString("semantic", "geom-" + modelName + "_" + i + "-map1");
                    xmlWriter.WriteAttributeString("input_semantic", "TEXCOORD");
                    xmlWriter.WriteAttributeString("input_set", "0");
                    xmlWriter.WriteEndElement();
                    //</bind_vertex_input>   
                    xmlWriter.WriteEndElement();
                    //</instance_material>       
                    xmlWriter.WriteEndElement();
                    //</technique_common>            
                    xmlWriter.WriteEndElement();
                    //</bind_material>   
                    xmlWriter.WriteEndElement();
                    //</instance_controller>
                    xmlWriter.WriteEndElement();
                    //</node>
                }
                xmlWriter.WriteEndElement();
                //</node>
            }

            xmlWriter.WriteEndElement();
            //</visual_scene>
            xmlWriter.WriteEndElement();
            //</library_visual_scenes>

            //<scene>
            xmlWriter.WriteStartElement("scene");
            //<instance_visual_scenes>
            xmlWriter.WriteStartElement("instance_visual_scene");
            xmlWriter.WriteAttributeString("url", "#Scene");
            xmlWriter.WriteEndElement();
            //</instance_visual_scenes>
            xmlWriter.WriteEndElement();
            //</scene>
        }



        private static void WriteBones(XmlWriter xmlWriter, JsonSkeleton skeleton, Dictionary<string, JsonSkeleton> boneDictionary)
        {
            //<node>
            xmlWriter.WriteStartElement("node");
            xmlWriter.WriteAttributeString("id", "node-" + skeleton.BoneName);
            xmlWriter.WriteAttributeString("name", skeleton.BoneName);
            xmlWriter.WriteAttributeString("sid", skeleton.BoneName);
            xmlWriter.WriteAttributeString("type", "JOINT");

            //<matrix>
            xmlWriter.WriteStartElement("matrix");
            xmlWriter.WriteAttributeString("sid", "matrix");

            Matrix matrix = new Matrix(boneDictionary[skeleton.BoneName].PoseMatrix);

            xmlWriter.WriteString(matrix.Column1.X + " " + matrix.Column1.Y + " " + matrix.Column1.Z + " " + (matrix.Column1.W * Info.modelMultiplier) + " ");
            xmlWriter.WriteString(matrix.Column2.X + " " + matrix.Column2.Y + " " + matrix.Column2.Z + " " + (matrix.Column2.W * Info.modelMultiplier) + " ");
            xmlWriter.WriteString(matrix.Column3.X + " " + matrix.Column3.Y + " " + matrix.Column3.Z + " " + (matrix.Column3.W * Info.modelMultiplier) + " ");
            xmlWriter.WriteString(matrix.Column4.X + " " + matrix.Column4.Y + " " + matrix.Column4.Z + " " + (matrix.Column4.W * Info.modelMultiplier) + " ");

            xmlWriter.WriteEndElement();
            //</matrix>

            foreach (var sk in boneDictionary.Values)
            {
                if(sk.BoneParent == skeleton.BoneNumber)
                {
                    WriteBones(xmlWriter, sk, boneDictionary);
                }
            }

            xmlWriter.WriteEndElement();
            //</node>
        }

        private static void WriteRootBones(XmlWriter xmlWriter, JsonSkeleton skeleton, Dictionary<string, JsonSkeleton> boneDictionary)
        {
            //<node>
            xmlWriter.WriteStartElement("node");
            xmlWriter.WriteAttributeString("id", "node-" + skeleton.BoneName);
            xmlWriter.WriteAttributeString("name", skeleton.BoneName);
            xmlWriter.WriteAttributeString("sid", skeleton.BoneName);
            xmlWriter.WriteAttributeString("type", "JOINT");

            //<matrix>
            xmlWriter.WriteStartElement("matrix");
            xmlWriter.WriteAttributeString("sid", "matrix");

            Matrix matrix = new Matrix(boneDictionary[skeleton.BoneName].PoseMatrix);

            xmlWriter.WriteString(matrix.Column1.X + " " + matrix.Column1.Y + " " + matrix.Column1.Z + " " + matrix.Column1.W + " ");
            xmlWriter.WriteString(matrix.Column2.X + " " + matrix.Column2.Y + " " + matrix.Column2.Z + " " + matrix.Column2.W + " ");
            xmlWriter.WriteString(matrix.Column3.X + " " + matrix.Column3.Y + " " + matrix.Column3.Z + " " + matrix.Column3.W + " ");
            xmlWriter.WriteString(matrix.Column4.X + " " + matrix.Column4.Y + " " + matrix.Column4.Z + " " + matrix.Column4.W + " ");

            xmlWriter.WriteEndElement();
            //</matrix>

            xmlWriter.WriteEndElement();
            //</node>
        }
    }
}
