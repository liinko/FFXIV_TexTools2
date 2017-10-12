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
using FFXIV_TexTools2.Material;
using FFXIV_TexTools2.Model;
using FFXIV_TexTools2.Resources;
using Newtonsoft.Json;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

                }
            }

        }


        public static void SaveCollada(string selectedCategory, string modelName, string selectedMesh, string selectedItemName, List<MDLTEXData> meshData, List<ModelMeshData> meshList)
        {
            string skelName = modelName.Substring(0, 5);
            if (modelName.Contains("w"))
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

                string meshName = modelName + "_" + selectedMesh;

                //Images
                XMLimages(xmlWriter, modelName, meshList.Count);

                //effects
                XMLeffects(xmlWriter, modelName, meshList.Count);

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
            xmlWriter.WriteString("Z_UP");
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

        private static void XMLimages(XmlWriter xmlWriter, string modelName, int meshCount)
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
            }

            xmlWriter.WriteEndElement();
            //</library_images>
        }

        private static void XMLeffects(XmlWriter xmlWriter, string modelName, int meshCount)
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
                //<reflective>
                xmlWriter.WriteStartElement("reflective");
                //<texture>
                xmlWriter.WriteStartElement("texture");
                xmlWriter.WriteAttributeString("texture", modelName + "_" + i + "_Diffuse_bmp-sampler");
                xmlWriter.WriteAttributeString("texcoord", "geom-" + modelName + "_" + i + "-map1");
                xmlWriter.WriteEndElement();
                //</texture>
                xmlWriter.WriteEndElement();
                //</reflective>
                //<transparent>
                xmlWriter.WriteStartElement("transparent");
                xmlWriter.WriteAttributeString("opaque", "A_ONE");
                //<texture>
                xmlWriter.WriteStartElement("texture");
                xmlWriter.WriteAttributeString("texture", modelName + "_" + i + "_Diffuse_bmp-sampler");
                xmlWriter.WriteAttributeString("texcoord", "geom-" + modelName + "_" + i + "-map1");
                xmlWriter.WriteEndElement();
                //</texture>
                xmlWriter.WriteEndElement();
                //</transparent>
                xmlWriter.WriteEndElement();
                //</phong>
                //<extra>
                xmlWriter.WriteStartElement("extra");
                //<technique>
                xmlWriter.WriteStartElement("technique");
                xmlWriter.WriteAttributeString("profile", "OpenCOLLADA3dsMax");
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
                //<geometry>
                xmlWriter.WriteStartElement("geometry");
                xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i);
                xmlWriter.WriteAttributeString("name", modelName + "_" + i);
                //<mesh>
                xmlWriter.WriteStartElement("mesh");

                /*
                 * --------------------
                 * Verticies
                 * --------------------
                 */

                //<source>
                xmlWriter.WriteStartElement("source");
                xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + "-positions");
                //<float_array>
                xmlWriter.WriteStartElement("float_array");
                xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + "-positions-array");
                xmlWriter.WriteAttributeString("count", (meshList[i].Vertices.Count * 3).ToString());
                foreach (var v in meshList[i].Vertices)
                {
                    xmlWriter.WriteString(v.X.ToString() + " " + v.Y.ToString() + " " + v.Z.ToString() + " ");
                }
                xmlWriter.WriteEndElement();
                //</float_array>

                //<technique_common>
                xmlWriter.WriteStartElement("technique_common");
                //<accessor>
                xmlWriter.WriteStartElement("accessor");
                xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + "-positions-array");
                xmlWriter.WriteAttributeString("count", meshList[i].Vertices.Count.ToString());
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
                xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + "-normals");
                //<float_array>
                xmlWriter.WriteStartElement("float_array");
                xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + "-normals-array");
                xmlWriter.WriteAttributeString("count", (meshList[i].Normals.Count * 3).ToString());
                foreach (var n in meshList[i].Normals)
                {
                    xmlWriter.WriteString(n.X.ToString() + " " + n.Y.ToString() + " " + n.Z.ToString() + " ");
                }
                xmlWriter.WriteEndElement();
                //</float_array>

                //<technique_common>
                xmlWriter.WriteStartElement("technique_common");
                //<accessor>
                xmlWriter.WriteStartElement("accessor");
                xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + "-normals-array");
                xmlWriter.WriteAttributeString("count", meshList[i].Normals.Count.ToString());
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
                xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + "-map0");
                //<float_array>
                xmlWriter.WriteStartElement("float_array");
                xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + "-map0-array");
                xmlWriter.WriteAttributeString("count", (meshList[i].TextureCoordinates.Count * 2).ToString());
                foreach (var tc in meshList[i].TextureCoordinates)
                {
                    xmlWriter.WriteString(tc.X.ToString() + " " + (tc.Y * -1).ToString() + " ");
                }
                xmlWriter.WriteEndElement();
                //</float_array>

                //<technique_common>
                xmlWriter.WriteStartElement("technique_common");
                //<accessor>
                xmlWriter.WriteStartElement("accessor");
                xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + "-map0-array");
                xmlWriter.WriteAttributeString("count", meshList[i].Normals.Count.ToString());
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
                xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + "-map0-textangents");
                //<float_array>
                xmlWriter.WriteStartElement("float_array");
                xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + "-map0-textangents-array");
                xmlWriter.WriteAttributeString("count", (meshList[i].BiTangents.Count * 3).ToString());
                foreach (var tan in meshList[i].Tangents)
                {
                    xmlWriter.WriteString(tan.X.ToString() + " " + tan.Y.ToString() + " " + tan.Z.ToString() + " ");
                }
                xmlWriter.WriteEndElement();
                //</float_array>

                //<technique_common>
                xmlWriter.WriteStartElement("technique_common");
                //<accessor>
                xmlWriter.WriteStartElement("accessor");
                xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + "-map0-textangents-array");
                xmlWriter.WriteAttributeString("count", meshList[i].Normals.Count.ToString());
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
                xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + "-map0-texbinormals");
                //<float_array>
                xmlWriter.WriteStartElement("float_array");
                xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + "-map0-texbinormals-array");
                xmlWriter.WriteAttributeString("count", (meshList[i].BiTangents.Count * 3).ToString());
                foreach (var bn in meshList[i].BiTangents)
                {
                    xmlWriter.WriteString(bn.X.ToString() + " " + bn.Y.ToString() + " " + bn.Z.ToString() + " ");
                }
                xmlWriter.WriteEndElement();
                //</float_array>

                //<technique_common>
                xmlWriter.WriteStartElement("technique_common");
                //<accessor>
                xmlWriter.WriteStartElement("accessor");
                xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + "-map0-texbinormals-array");
                xmlWriter.WriteAttributeString("count", meshList[i].Normals.Count.ToString());
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
                xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + "-vertices");
                //<input>
                xmlWriter.WriteStartElement("input");
                xmlWriter.WriteAttributeString("semantic", "POSITION");
                xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + "-positions");
                xmlWriter.WriteEndElement();
                //</input>
                xmlWriter.WriteEndElement();
                //</vertices>


                //<triangles>
                xmlWriter.WriteStartElement("triangles");
                xmlWriter.WriteAttributeString("material", modelName + "_" + i);
                xmlWriter.WriteAttributeString("count", (meshList[i].Indices.Count / 3).ToString());
                //<input>
                xmlWriter.WriteStartElement("input");
                xmlWriter.WriteAttributeString("semantic", "VERTEX");
                xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + "-vertices");
                xmlWriter.WriteAttributeString("offset", "0");
                xmlWriter.WriteEndElement();
                //</input>

                //<input>
                xmlWriter.WriteStartElement("input");
                xmlWriter.WriteAttributeString("semantic", "NORMAL");
                xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + "-normals");
                xmlWriter.WriteAttributeString("offset", "1");
                xmlWriter.WriteEndElement();
                //</input>

                //<input>
                xmlWriter.WriteStartElement("input");
                xmlWriter.WriteAttributeString("semantic", "TEXCOORD");
                xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + "-map0");
                xmlWriter.WriteAttributeString("offset", "2");
                xmlWriter.WriteAttributeString("set", "0");
                xmlWriter.WriteEndElement();
                //</input>


                //<input>
                xmlWriter.WriteStartElement("input");
                xmlWriter.WriteAttributeString("semantic", "TEXTANGENT");
                xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + "-map0-textangents");
                xmlWriter.WriteAttributeString("offset", "3");
                xmlWriter.WriteAttributeString("set", "1");
                xmlWriter.WriteEndElement();
                //</input>

                //<input>
                xmlWriter.WriteStartElement("input");
                xmlWriter.WriteAttributeString("semantic", "TEXBINORMAL");
                xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + "-map0-texbinormals");
                xmlWriter.WriteAttributeString("offset", "3");
                xmlWriter.WriteAttributeString("set", "1");
                xmlWriter.WriteEndElement();
                //</input>

                //<p>
                xmlWriter.WriteStartElement("p");
                foreach (var ind in meshList[i].Indices)
                {
                    xmlWriter.WriteString(ind + " " + ind + " " + ind + " " + ind + " ");
                }
                xmlWriter.WriteEndElement();
                //</p>

                xmlWriter.WriteEndElement();
                //</triangles>
                xmlWriter.WriteEndElement();
                //</mesh>
                xmlWriter.WriteEndElement();
                //</geometry>
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
                //<controller>
                xmlWriter.WriteStartElement("controller");
                xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + "-skin1");
                //<skin>
                xmlWriter.WriteStartElement("skin");
                xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i);
                //<bind_shape_matrix>
                xmlWriter.WriteStartElement("bind_shape_matrix");
                xmlWriter.WriteString("1 0 0 0 0 1 0 0 0 0 1 0 0 0 0 1");
                xmlWriter.WriteEndElement();
                //</bind_shape_matrix>

                //<source>
                xmlWriter.WriteStartElement("source");
                xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + "-skin1-joints");
                //<Name_array>
                xmlWriter.WriteStartElement("Name_array");
                xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + "-skin1-joints-array");
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
                xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + "-skin1-joints-array");
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
                xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + "-skin1-bind_poses");
                //<Name_array>
                xmlWriter.WriteStartElement("float_array");
                xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + "-skin1-bind_poses-array");
                xmlWriter.WriteAttributeString("count", (16 * meshDataList[i].BoneStrings.Count).ToString());

                for (int m = 0; m < meshDataList[i].BoneStrings.Count; m++)
                {
                    try
                    {
                        Matrix matrix = new Matrix(skelDict[meshDataList[i].BoneStrings[m]].InversePoseMatrix);

                        xmlWriter.WriteString(matrix.Column1.X + " " + matrix.Column1.Y + " " + matrix.Column1.Z + " " + matrix.Column1.W + " ");
                        xmlWriter.WriteString(matrix.Column2.X + " " + matrix.Column2.Y + " " + matrix.Column2.Z + " " + matrix.Column2.W + " ");
                        xmlWriter.WriteString(matrix.Column3.X + " " + matrix.Column3.Y + " " + matrix.Column3.Z + " " + matrix.Column3.W + " ");
                        xmlWriter.WriteString(matrix.Column4.X + " " + matrix.Column4.Y + " " + matrix.Column4.Z + " " + matrix.Column4.W + " ");
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
                xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + "-skin1-bind_poses-array");
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
                xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + "-skin1-weights");
                //<Name_array>
                xmlWriter.WriteStartElement("float_array");
                xmlWriter.WriteAttributeString("id", "geom-" + modelName + "_" + i + "-skin1-weights-array");
                xmlWriter.WriteAttributeString("count", meshDataList[i].BlendWeights.Count.ToString());

                foreach (var bw in meshDataList[i].BlendWeights)
                {
                    xmlWriter.WriteString(bw + " ");
                }
                xmlWriter.WriteEndElement();
                //</Name_array>

                //<technique_common>
                xmlWriter.WriteStartElement("technique_common");
                //<accessor>
                xmlWriter.WriteStartElement("accessor");
                xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + "-skin1-weights-array");
                xmlWriter.WriteAttributeString("count", meshDataList[i].BlendWeights.Count.ToString());
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
                xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + "-skin1-joints");
                xmlWriter.WriteEndElement();
                //</input>

                //<input>
                xmlWriter.WriteStartElement("input");
                xmlWriter.WriteAttributeString("semantic", "INV_BIND_MATRIX");
                xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + "-skin1-bind_poses");
                xmlWriter.WriteEndElement();
                //</input>
                xmlWriter.WriteEndElement();
                //</joints>

                //<vertex_weights>
                xmlWriter.WriteStartElement("vertex_weights");
                xmlWriter.WriteAttributeString("count", meshDataList[i].Vertices.Count.ToString());
                //<input>
                xmlWriter.WriteStartElement("input");
                xmlWriter.WriteAttributeString("semantic", "JOINT");
                xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + "-skin1-joints");
                xmlWriter.WriteAttributeString("offset", "0");
                xmlWriter.WriteEndElement();
                //</input>
                //<input>
                xmlWriter.WriteStartElement("input");
                xmlWriter.WriteAttributeString("semantic", "WEIGHT");
                xmlWriter.WriteAttributeString("source", "#geom-" + modelName + "_" + i + "-skin1-weights");
                xmlWriter.WriteAttributeString("offset", "1");
                xmlWriter.WriteEndElement();
                //</input>
                //<vcount>
                xmlWriter.WriteStartElement("vcount");
                foreach (var wc in meshDataList[i].WeightCounts)
                {
                    xmlWriter.WriteString(wc + " ");
                }
                xmlWriter.WriteEndElement();
                //</vcount>
                //<v>
                xmlWriter.WriteStartElement("v");
                int blin = 0;
                foreach (var bi in meshDataList[i].BlendIndices)
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

            var firstBone = skelDict["n_root"];
            WriteBones(xmlWriter, firstBone, skelDict);
            boneParents.Add("n_root");

            for (int i = 0; i < meshDataList.Count; i++)
            {
                //<node>
                xmlWriter.WriteStartElement("node");
                xmlWriter.WriteAttributeString("id", "node-" + modelName + "_" + i);
                xmlWriter.WriteAttributeString("name", modelName + "_" + i);

                //<instance_controller>
                xmlWriter.WriteStartElement("instance_controller");
                xmlWriter.WriteAttributeString("url", "#geom-" + modelName + "_" + i + "-skin1");

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

            xmlWriter.WriteString(matrix.Column1.X + " " + matrix.Column1.Y + " " + matrix.Column1.Z + " " + matrix.Column1.W + " ");
            xmlWriter.WriteString(matrix.Column2.X + " " + matrix.Column2.Y + " " + matrix.Column2.Z + " " + matrix.Column2.W + " ");
            xmlWriter.WriteString(matrix.Column3.X + " " + matrix.Column3.Y + " " + matrix.Column3.Z + " " + matrix.Column3.W + " ");
            xmlWriter.WriteString(matrix.Column4.X + " " + matrix.Column4.Y + " " + matrix.Column4.Z + " " + matrix.Column4.W + " ");

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
