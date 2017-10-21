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

using FFXIV_TexTools2.Helpers;
using FFXIV_TexTools2.Material.ModelMaterial;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml;

namespace FFXIV_TexTools2.IO
{
    public class ImportModel
    {

        /// <summary>
        /// The amount to add or subtract from the offste
        /// </summary>
        /// <remarks>
        /// This is calculated by multiplying the dat file number by 16 bytes [.dat4]: (4 * 16 = 64)
        /// This amount is added to the offset when reading and subtracted from the offset when writing
        /// </remarks>
        public static int DatOffsetAmount = 64;


        public static int ImportOBJ(string category, string itemName, string modelName, string selectedMesh, string internalPath)
        {
            var savePath = Properties.Settings.Default.Save_Directory + "/" + category + "/" + itemName + "/3D/" + modelName + "_mesh_" + selectedMesh + ".obj";

            if (File.Exists(savePath))
            {
                var lines = File.ReadAllLines(savePath);

                Vector3Collection Vertex = new Vector3Collection();
                Vector2Collection TexCoord = new Vector2Collection();
                Vector3Collection Normals = new Vector3Collection();
                IntCollection Indices = new IntCollection();

                char[] delimiterChars = { ' ' };

                foreach (var l in lines)
                {
                    var s = l.Split(delimiterChars);
                    if (s[0].Equals("v"))
                    {
                        Vertex.Add(new SharpDX.Vector3(float.Parse(s[1]), float.Parse(s[2]), float.Parse(s[3])));
                    }
                    else if (s[0].Equals("vt"))
                    {
                        TexCoord.Add(new SharpDX.Vector2(float.Parse(s[1]), float.Parse(s[2])));
                    }
                    else if (s[0].Equals("vn"))
                    {
                        Normals.Add(new SharpDX.Vector3(float.Parse(s[1]), float.Parse(s[2]), float.Parse(s[3])));
                    }
                    else if (s[0].Equals("f"))
                    {
                        var i1 = s[1].Substring(0, s[1].IndexOf("/"));
                        Indices.Add(int.Parse(i1) - 1);

                        var i2 = s[2].Substring(0, s[2].IndexOf("/"));
                        Indices.Add(int.Parse(i2) - 1);

                        var i3 = s[3].Substring(0, s[3].IndexOf("/"));
                        Indices.Add(int.Parse(i3) - 1);
                    }
                }
            }
            return 0;
        }


        public static void ImportDAE(string category, string itemName, string modelName, string selectedMesh, string internalPath, List<string> boneStrings)
        {
            var savePath = Properties.Settings.Default.Save_Directory + "/" + category + "/" + itemName + "/3D/" + modelName + ".DAE";

            if (File.Exists(savePath))
            {
                Dictionary<int, ColladaData> cdDict = new Dictionary<int, ColladaData>();

                Dictionary<string, string> boneJointDict = new Dictionary<string, string>();
           
                string texc = "-map0-array";
                string pos = "-positions-array";
                string norm = "-normals-array";
                int tcStride = 2;

                using (XmlReader reader = XmlReader.Create(savePath))
                {
                    while (reader.Read())
                    {
                        if (reader.IsStartElement())
                        {
                            if (reader.Name.Contains("authoring_tool"))
                            {
                                var tool = reader.ReadElementContentAsString();

                                if (tool.Contains("OpenCOLLADA"))
                                {
                                    texc = "-map1-array";
                                    tcStride = 3;
                                }
                                else if (tool.Contains("FBX"))
                                {
                                    pos = "-position-array";
                                    norm = "-normal0-array";
                                    texc = "-uv0-array";
                                }
                                else if (tool.Contains("Blender"))
                                {
                                    texc = "-map-0-array";
                                }
                            }

                            //go to geometry element
                            if (reader.Name.Equals("geometry"))
                            {
                                var atr = reader["name"];

                                var meshNum = int.Parse(atr.Substring(atr.LastIndexOf("_") + 1, 1));
                                //if mesh alredy exists in colladaDataList
                                if (cdDict.ContainsKey(meshNum))
                                {
                                    int[] tempIndex = null;

                                    while (reader.Read())
                                    {
                                        if (reader.IsStartElement())
                                        {
                                            if (reader.Name.Contains("float_array"))
                                            {
                                                //Vertex 
                                                if (reader["id"].ToLower().Contains(pos))
                                                {
                                                    cdDict[meshNum].vertex.AddRange((float[])reader.ReadElementContentAs(typeof(float[]), null));
                                                }
                                                //Normals
                                                else if (reader["id"].ToLower().Contains(norm) && cdDict[meshNum].vertex.Count > 0)
                                                {
                                                    cdDict[meshNum].normal.AddRange((float[])reader.ReadElementContentAs(typeof(float[]), null));
                                                }
                                                //Texture Coordinates
                                                else if (reader["id"].ToLower().Contains(texc) && cdDict[meshNum].vertex.Count > 0)
                                                {
                                                    cdDict[meshNum].texCoord.AddRange((float[])reader.ReadElementContentAs(typeof(float[]), null));
                                                }
                                            }
                                            //Indices
                                            if (reader.Name.Equals("p"))
                                            {
                                                var lastIndex = cdDict[meshNum].index.Max();
                                                tempIndex = (int[])reader.ReadElementContentAs(typeof(int[]), null);

                                                foreach (var i in tempIndex)
                                                {
                                                    cdDict[meshNum].index.Add(i + lastIndex + 1);
                                                }
                                                break;
                                            }
                                        }
                                    }

                                    var num = atr.Substring(atr.LastIndexOf(".") + 1, atr.Length - (atr.LastIndexOf(".") + 1));
                                    cdDict[meshNum].partsDict.Add(int.Parse(num), tempIndex.Length / 4);
                                }
                                else
                                {
                                    ColladaData cData = new ColladaData();

                                    while (reader.Read())
                                    {
                                        if (reader.IsStartElement())
                                        {
                                            if (reader.Name.Contains("float_array"))
                                            {
                                                //Vertex 
                                                if (reader["id"].ToLower().Contains(pos))
                                                {
                                                    cData.vertex.AddRange((float[])reader.ReadElementContentAs(typeof(float[]), null));
                                                }
                                                //Normals
                                                else if (reader["id"].ToLower().Contains(norm) && cData.vertex.Count > 0)
                                                {
                                                    cData.normal.AddRange((float[])reader.ReadElementContentAs(typeof(float[]), null));
                                                }
                                                //Texture Coordinates
                                                else if (reader["id"].ToLower().Contains(texc) && cData.vertex.Count > 0)
                                                {
                                                    cData.texCoord.AddRange((float[])reader.ReadElementContentAs(typeof(float[]), null));
                                                }
                                            }
                                            //Indices
                                            if (reader.Name.Equals("p"))
                                            {
                                                cData.index.AddRange((int[])reader.ReadElementContentAs(typeof(int[]), null));
                                                break;
                                            }
                                        }
                                    }

                                    cData.partsDict.Add(0, cData.index.Count / 4);

                                    cdDict.Add(meshNum, cData);
                                }
                            }
                            //go to controller element
                            else if (reader.Name.Equals("controller"))
                            {
                                var atr = reader["id"];

                                var meshNum = int.Parse(atr.Substring(atr.LastIndexOf("_") + 1, 1));
                                //go to mesh 1
                                if (cdDict[meshNum].weights.Count > 0)
                                {
                                    int[] tempbIndex = null;

                                    while (reader.Read())
                                    {
                                        if (reader.IsStartElement())
                                        {
                                            if (reader.Name.Contains("float_array"))
                                            {
                                                //Blend Weight
                                                if (reader["id"].ToLower().Contains("weights-array"))
                                                {
                                                    cdDict[meshNum].weights.AddRange((float[])reader.ReadElementContentAs(typeof(float[]), null));
                                                }
                                            }
                                            //Blend counts
                                            else if (reader.Name.Equals("vcount"))
                                            {
                                                cdDict[meshNum].vCount.AddRange((int[])reader.ReadElementContentAs(typeof(int[]), null));
                                            }
                                            //Blend Indices
                                            else if (reader.Name.Equals("v"))
                                            {
                                                var lastIndex = cdDict[meshNum].bIndex.Max() + 1;
                                                tempbIndex = (int[])reader.ReadElementContentAs(typeof(int[]), null);

                                                for (int a = 0; a < tempbIndex.Length; a += 2)
                                                {
                                                    cdDict[meshNum].bIndex.Add(tempbIndex[a]);
                                                    cdDict[meshNum].bIndex.Add(tempbIndex[a + 1] + lastIndex);
                                                }

                                                break;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    while (reader.Read())
                                    {
                                        if (reader.IsStartElement())
                                        {

                                            if (reader.Name.Contains("Name_array"))
                                            {
                                                cdDict[meshNum].bones = (string[])reader.ReadElementContentAs(typeof(string[]), null);
                                            }

                                            if (reader.Name.Contains("float_array"))
                                            {
                                                //Blend Weight
                                                if (reader["id"].ToLower().Contains("weights-array"))
                                                {
                                                    cdDict[meshNum].weights.AddRange((float[])reader.ReadElementContentAs(typeof(float[]), null));
                                                }
                                            }
                                            //Blend counts
                                            else if (reader.Name.Equals("vcount"))
                                            {
                                                cdDict[meshNum].vCount.AddRange((int[])reader.ReadElementContentAs(typeof(int[]), null));
                                            }
                                            //Blend Indices
                                            else if (reader.Name.Equals("v"))
                                            {
                                                cdDict[meshNum].bIndex.AddRange((int[])reader.ReadElementContentAs(typeof(int[]), null));
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            else if (reader.Name.Equals("visual_scene"))
                            {
                                while (reader.Read())
                                {
                                    if (reader.IsStartElement())
                                    {
                                        if (reader.Name.Contains("node"))
                                        {
                                            var sid = reader["sid"];
                                            if (sid != null)
                                            {
                                                var name = reader["name"];

                                                boneJointDict.Add(sid, name);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                Dictionary<string, int> boneDict = new Dictionary<string, int>();

                for (int i = 0; i < boneStrings.Count; i++)
                {
                    boneDict.Add(boneStrings[i], i);
                }

                List<ColladaMeshData> cmdList = new List<ColladaMeshData>();

                foreach (var cd in cdDict.Values)
                {
                    ColladaMeshData cmd = new ColladaMeshData();

                    Vector3Collection Vertex = new Vector3Collection();
                    Vector2Collection TexCoord = new Vector2Collection();
                    Vector3Collection Normals = new Vector3Collection();
                    IntCollection Indices = new IntCollection();
                    List<byte> blendIndices = new List<byte>();
                    List<byte> blendWeights = new List<byte>();
                    List<string> boneStringList = new List<string>();

                    for (int i = 0; i < cd.vertex.Count; i += 3)
                    {
                        Vertex.Add(new SharpDX.Vector3(cd.vertex[i], cd.vertex[i + 1], cd.vertex[i + 2]));
                    }

                    for (int i = 0; i < cd.normal.Count; i += 3)
                    {
                        Normals.Add(new SharpDX.Vector3(cd.normal[i], cd.normal[i + 1], cd.normal[i + 2]));
                    }

                    for (int i = 0; i < cd.texCoord.Count; i += tcStride)
                    {
                        TexCoord.Add(new SharpDX.Vector2(cd.texCoord[i], cd.texCoord[i + 1]));
                    }

                    for (int i = 0; i < cd.index.Count; i += 4)
                    {
                        Indices.Add(cd.index[i]);
                    }

                    if (Vertex.Count != Normals.Count)
                    {
                        MessageBox.Show("[Import] The number of Vertices, Normals, and Texture Coordinates should be equal. \n", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (Vertex.Count != TexCoord.Count)
                    {
                        MessageBox.Show("[Import] The number of Vertices, Normals, and Texture Coordinates should be equal. \n", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    int vTrack = 0;
                    for (int i = 0; i < Vertex.Count; i++)
                    {
                        int bCount = cd.vCount[i];

                        for (int j = 0; j < bCount * 2; j += 2)
                        {
                            blendIndices.Add((byte)cd.bIndex[vTrack * 2 + j]);
                            blendWeights.Add((byte)Math.Round(cd.weights[cd.bIndex[vTrack * 2 + j + 1]] * 255f));
                        }

                        if (bCount < 4)
                        {
                            int remainder = 4 - bCount;

                            for (int k = 0; k < remainder; k++)
                            {
                                blendIndices.Add((byte)0);
                                blendWeights.Add((byte)0);
                            }
                        }

                        vTrack += bCount;
                    }

                    foreach (var bn in blendIndices)
                    {
                        boneStringList.Add(boneJointDict[cd.bones[bn]]);
                    }

                    blendIndices.Clear();

                    foreach (var bs in boneStringList)
                    {
                        try
                        {
                            var bString = bs;
                            if (!bs.Contains("h0"))
                            {
                                bString = Regex.Replace(bs, @"[\d]", string.Empty);
                            }
                            byte bia = (byte)boneDict[bString];
                            blendIndices.Add(bia);
                        }
                        catch(Exception ex)
                        {
                            MessageBox.Show("[Import] Bone \"" + bs + "\" does not exist in the mdl file. \n", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    MeshGeometry3D mg = new MeshGeometry3D
                    {
                        Positions = Vertex,
                        Indices = Indices,
                        Normals = Normals,
                        TextureCoordinates = TexCoord
                    };
                    MeshBuilder.ComputeTangents(mg);

                    cmd.meshGeometry = mg;
                    cmd.blendIndices = blendIndices;
                    cmd.blendWeights = blendWeights;
                    cmd.partsDict = cd.partsDict;

                    cmdList.Add(cmd);
                }

                Create(cmdList, internalPath, selectedMesh, category, itemName);
            }
        }

        public static void Create(List<ColladaMeshData> cmdList, string internalPath, string selectedMesh, string category, string itemName)
        {
            var type = Helper.GetCategoryType(category);

            int lineNum = 0;
            bool inModList = false;
            JsonEntry modEntry = null;

            List<byte> mdlImport = new List<byte>();

            if (Properties.Settings.Default.Mod_List == 0)
            {
                try
                {
                    using (StreamReader sr = new StreamReader(Info.modListDir))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            modEntry = JsonConvert.DeserializeObject<JsonEntry>(line);
                            if (modEntry.fullPath.Equals(internalPath))
                            {
                                inModList = true;
                                break;
                            }
                            lineNum++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("[Import] Error Accessing .modlist File \n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            /*
            * Imported Vertex Data Start
            */

            Dictionary<int, ImportData> importDict = new Dictionary<int, ImportData>();

            int importMeshNum = 0;
            foreach(var cmd in cmdList)
            {
                ImportData id = new ImportData();
                int bc = 0;
                var mg = cmd.meshGeometry;

                for (int i = 0; i < mg.Positions.Count; i++)
                {
                    if (type.Equals("weapon"))
                    {
                        var hx = Half.Parse(mg.Positions[i].X.ToString());
                        id.dataSet1.AddRange(Half.GetBytes(hx));

                        var hy = Half.Parse(mg.Positions[i].Y.ToString());
                        id.dataSet1.AddRange(Half.GetBytes(hy));

                        var hz = Half.Parse(mg.Positions[i].Z.ToString());
                        id.dataSet1.AddRange(Half.GetBytes(hz));

                        id.dataSet1.AddRange(BitConverter.GetBytes((short)15360));
                    }
                    else
                    {
                        //vertex points
                        id.dataSet1.AddRange(BitConverter.GetBytes(mg.Positions[i].X));
                        id.dataSet1.AddRange(BitConverter.GetBytes(mg.Positions[i].Y));
                        id.dataSet1.AddRange(BitConverter.GetBytes(mg.Positions[i].Z));
                    }


                    //blend weight
                    id.dataSet1.Add(cmd.blendWeights[bc]);
                    id.dataSet1.Add(cmd.blendWeights[bc + 1]);
                    id.dataSet1.Add(cmd.blendWeights[bc + 2]);
                    id.dataSet1.Add(cmd.blendWeights[bc + 3]);

                    //blend index
                    id.dataSet1.Add(cmd.blendIndices[bc]);
                    id.dataSet1.Add(cmd.blendIndices[bc + 1]);
                    id.dataSet1.Add(cmd.blendIndices[bc + 2]);
                    id.dataSet1.Add(cmd.blendIndices[bc + 3]);

                    bc += 4;
                }

                for (int i = 0; i < mg.Normals.Count; i++)
                {
                    //Normal X (Half)
                    var hx = Half.Parse(mg.Normals[i].X.ToString());
                    id.dataSet2.AddRange(Half.GetBytes(hx));

                    //Normal Y (Half)
                    var hy = Half.Parse(mg.Normals[i].Y.ToString());
                    id.dataSet2.AddRange(Half.GetBytes(hy));

                    //Normal Z (Half)
                    var hz = Half.Parse(mg.Normals[i].Z.ToString());
                    id.dataSet2.AddRange(Half.GetBytes(hz));

                    //Normal W (Half)
                    id.dataSet2.AddRange(new byte[2]);


                    //tangent X
                    byte tx = (byte)(((255 * (mg.BiTangents[i].X * -1)) + 255) / 2);
                    id.dataSet2.Add(tx);

                    //tangent Y
                    byte ty = (byte)(((255 * (mg.BiTangents[i].Y * -1)) + 255) / 2);
                    id.dataSet2.Add(ty);

                    //tangent Z
                    byte tz = (byte)(((255 * (mg.BiTangents[i].Z * -1)) + 255) / 2);
                    id.dataSet2.Add(tz);

                    //tangent W
                    id.dataSet2.Add((byte)0);

                    //Color
                    id.dataSet2.AddRange(BitConverter.GetBytes(4294967295));

                    //TexCoord X
                    var tcx = Half.Parse(mg.TextureCoordinates[i].X.ToString());
                    id.dataSet2.AddRange(Half.GetBytes(tcx));

                    //TexCoord Y
                    var tcy = Half.Parse(mg.TextureCoordinates[i].Y.ToString()) * -1;
                    id.dataSet2.AddRange(Half.GetBytes(tcy));

                    id.dataSet2.AddRange(BitConverter.GetBytes((short)0));
                    id.dataSet2.AddRange(BitConverter.GetBytes((short)15360));
                }

                foreach (var i in mg.Indices)
                {
                    id.indexSet.AddRange(BitConverter.GetBytes((short)i));
                }

                importDict.Add(importMeshNum, id);
                importMeshNum++;
            }

            /*
            * Imported Vertex Data End
            */


            /*
             * Open oringial MDL file
             */
            var MDLFile = Path.GetFileName(internalPath);
            var MDLFolder = internalPath.Substring(0, internalPath.LastIndexOf("/"));

            int offset = Helper.GetItemOffset(FFCRC.GetHash(MDLFolder), FFCRC.GetHash(MDLFile));

            int datNum = ((offset / 8) & 0x000f) / 2;

            offset = Helper.OffsetCorrection(datNum, offset);

            var MDLDatData = Helper.GetType3DecompressedData(offset, datNum);

            List<byte> compressedData = new List<byte>();
            List<byte> datHeader = new List<byte>();

            using (BinaryReader br = new BinaryReader(new MemoryStream(MDLDatData.Item1)))
            {
                //skip blank header
                br.BaseStream.Seek(68, SeekOrigin.Begin);

                /* 
                 * -------------------------------
                 * Vertex Info Block Start
                 * -------------------------------
                 */

                List<byte> vertexInfoBlock = new List<byte>();
                int compVertexInfoSize;

                //vertex info section
                int vertexInfoSize = MDLDatData.Item2 * 136;
                vertexInfoBlock.AddRange(br.ReadBytes(vertexInfoSize));
                var compVertexInfo = Compressor(vertexInfoBlock.ToArray());

                compressedData.AddRange(BitConverter.GetBytes(16));
                compressedData.AddRange(BitConverter.GetBytes(0));
                compressedData.AddRange(BitConverter.GetBytes(compVertexInfo.Length));
                compressedData.AddRange(BitConverter.GetBytes(vertexInfoBlock.Count));
                compressedData.AddRange(compVertexInfo);

                var padding = 128 - ((compVertexInfo.Length + 16) % 128);

                compressedData.AddRange(new byte[padding]);
                compVertexInfoSize = compVertexInfo.Length + 16 + padding;

                /* 
                 * -------------------------------
                 * Vertex Info Block End
                 * -------------------------------
                 */


                /* 
                 * -------------------------------
                 * Model Data Block Start
                 * -------------------------------
                 */

                List<byte> modelDataBlock = new List<byte>();
                int compModelDataSize;

                //number of strings (int)
                modelDataBlock.AddRange(br.ReadBytes(4));

                //string block size (int)
                int stringBlockSize = br.ReadInt32();
                modelDataBlock.AddRange(BitConverter.GetBytes(stringBlockSize));

                //string block
                modelDataBlock.AddRange(br.ReadBytes(stringBlockSize));

                //unknown (int)
                modelDataBlock.AddRange(br.ReadBytes(4));

                //mesh count (short)
                short meshCount = br.ReadInt16();
                modelDataBlock.AddRange(BitConverter.GetBytes(meshCount));

                //num of atr strings (short)
                short atrStringCount = br.ReadInt16();
                modelDataBlock.AddRange(BitConverter.GetBytes(atrStringCount));

                //num of mesh parts (short)
                short meshPartCount = br.ReadInt16();
                modelDataBlock.AddRange(BitConverter.GetBytes(meshPartCount));

                //num of material strings (short)
                short matStringCount = br.ReadInt16();
                modelDataBlock.AddRange(BitConverter.GetBytes(matStringCount));

                //num of bone strings (short)
                short boneStringCount = br.ReadInt16();
                modelDataBlock.AddRange(BitConverter.GetBytes(boneStringCount));

                //bone list count (short)
                short boneListCount = br.ReadInt16();
                modelDataBlock.AddRange(BitConverter.GetBytes(boneListCount));

                short unk1 = br.ReadInt16();
                modelDataBlock.AddRange(BitConverter.GetBytes(unk1));

                short unk2 = br.ReadInt16();
                modelDataBlock.AddRange(BitConverter.GetBytes(unk2));

                short unk3 = br.ReadInt16();
                modelDataBlock.AddRange(BitConverter.GetBytes(unk3));

                short unk4 = br.ReadInt16();
                modelDataBlock.AddRange(BitConverter.GetBytes(unk4));

                short unk5 = br.ReadInt16();
                modelDataBlock.AddRange(BitConverter.GetBytes(unk5));

                short unk6 = br.ReadInt16();
                modelDataBlock.AddRange(BitConverter.GetBytes(unk6));

                short unk7 = br.ReadInt16();
                modelDataBlock.AddRange(BitConverter.GetBytes(unk7));

                short unk8 = br.ReadInt16();
                modelDataBlock.AddRange(BitConverter.GetBytes(unk8));

                short unk9 = br.ReadInt16();
                modelDataBlock.AddRange(BitConverter.GetBytes(unk9));

                short unk10 = br.ReadInt16();
                modelDataBlock.AddRange(BitConverter.GetBytes(unk10));

                short unk11 = br.ReadInt16();
                modelDataBlock.AddRange(BitConverter.GetBytes(unk11));

                short unk12 = br.ReadInt16();
                modelDataBlock.AddRange(BitConverter.GetBytes(unk12));


                //Unknown (short) * 20
                modelDataBlock.AddRange(br.ReadBytes(16));

                if(unk5 > 0)
                {
                    modelDataBlock.AddRange(br.ReadBytes(unk5 * 32));
                }

                //LoD Section
                List<LevelOfDetail> lodList = new List<LevelOfDetail>();
                for (int i = 0; i < 3; i++)
                {
                    LevelOfDetail lod = new LevelOfDetail()
                    {
                        MeshOffset = br.ReadInt16(),
                        MeshCount = br.ReadInt16(),
                    };

                    List<byte> LoDChunk = new List<byte>();
                    //LoD UNK
                    LoDChunk.AddRange(br.ReadBytes(28));
                    br.ReadBytes(4);
                    LoDChunk.AddRange(br.ReadBytes(8));

                    //LoD Vetex Buffer Size (int)
                    if (i == 0)
                    {
                        int originalSize = br.ReadInt32();
                        int vertSize = 0;
                        int vDataSize = 20;

                        if (type.Equals("weapon"))
                        {
                            vDataSize = 16;
                        }

                        for(int m = 0; m < cmdList.Count; m++)
                        {
                            var mg = cmdList[m].meshGeometry;
                            vertSize += (mg.Positions.Count * vDataSize) + (mg.Positions.Count * 24);
                        }

                        lod.VertexDataSize = vertSize;
                        //lod.VertexDataSize = (mg.Positions.Count * 20) + (mg.Positions.Count * 24) + (mg1.Positions.Count * 20) + (mg1.Positions.Count * 24);
                    }
                    else
                    {
                        lod.VertexDataSize = br.ReadInt32();
                    }

                    //LoD Index Buffer Size (int)
                    if (i == 0)
                    {
                        int originalSize = br.ReadInt32();

                        int idSize = 0;
                        foreach(var cmd in cmdList)
                        {
                            var mg = cmd.meshGeometry;

                            int pad = (16 - ((mg.Indices.Count * 2) % 16));
                            if(pad == 16)
                            {
                                pad = 0;
                            }

                            idSize += (mg.Indices.Count * 2) + pad;
                        }

                        lod.IndexDataSize = idSize;
                    }
                    else
                    {
                        lod.IndexDataSize = br.ReadInt32();
                    }

                    //LoD Vertex Offset (int)
                    if (i == 0)
                    {
                        lod.VertexOffset = br.ReadInt32();
                    }
                    else
                    {
                        br.ReadBytes(4);
                        lod.VertexOffset = lodList[i - 1].VertexOffset + lodList[i - 1].VertexDataSize + lodList[i - 1].IndexDataSize;
                    }

                    //LoD Index Offset (int)

                    if (i == 0)
                    {
                        br.ReadBytes(4);
                        lod.IndexOffset = lod.VertexOffset + lod.VertexDataSize;
                        LoDChunk.InsertRange(28, BitConverter.GetBytes(lod.IndexOffset));
                    }
                    else
                    {
                        br.ReadBytes(4);
                        lod.IndexOffset = lod.VertexOffset + lod.VertexDataSize;
                        LoDChunk.InsertRange(28, BitConverter.GetBytes(lod.IndexOffset));
                    }

                    //LoD Mesh Offset (short)
                    modelDataBlock.AddRange(BitConverter.GetBytes((short)lod.MeshOffset));

                    //LoD Mesh Count (short)
                    modelDataBlock.AddRange(BitConverter.GetBytes((short)lod.MeshCount));

                    //LoD Chunk 
                    modelDataBlock.AddRange(LoDChunk.ToArray());

                    //LoD Vetex Buffer Size (int)
                    modelDataBlock.AddRange(BitConverter.GetBytes(lod.VertexDataSize));

                    //LoD Index Buffer Size (int)
                    modelDataBlock.AddRange(BitConverter.GetBytes(lod.IndexDataSize));

                    //LoD Vertex Offset (int)
                    modelDataBlock.AddRange(BitConverter.GetBytes(lod.VertexOffset));

                    //LoD Index Offset (int)
                    modelDataBlock.AddRange(BitConverter.GetBytes(lod.IndexOffset));

                    lodList.Add(lod);
                }

                //Meshes

                Dictionary<int, List<MeshInfo>> meshInfoDict = new Dictionary<int, List<MeshInfo>>();

                for(int i = 0; i < lodList.Count; i++)
                {
                    List<MeshInfo> meshInfoList = new List<MeshInfo>();

                    for (int j = 0; j < lodList[i].MeshCount; j++)
                    {
                        MeshInfo meshInfo = new MeshInfo()
                        {
                            VertexCount = br.ReadInt32(),
                            IndexCount = br.ReadInt32(),
                            MaterialNum = br.ReadInt16(),
                            MeshPartOffset = br.ReadInt16(),
                            MeshPartCount = br.ReadInt16(),
                            BoneListIndex = br.ReadInt16(),
                            IndexDataOffset = br.ReadInt32(),
                            VertexDataOffsets = new List<int> { br.ReadInt32(), br.ReadInt32(), br.ReadInt32() },
                            VertexSizes = new List<int> { br.ReadByte(), br.ReadByte(), br.ReadByte() },
                            VertexDataBlockCount = br.ReadByte()
                        };

                        if (i == 0)
                        {
                            var mg = cmdList[j].meshGeometry;

                            //Vertex Count (int)
                            modelDataBlock.AddRange(BitConverter.GetBytes(mg.Positions.Count));
                            meshInfo.VertexCount = mg.Positions.Count;

                            //Index Count (int)
                            modelDataBlock.AddRange(BitConverter.GetBytes(mg.Indices.Count));
                            meshInfo.IndexCount = mg.Indices.Count;
                        }
                        else
                        {
                            //Vertex Count (int)
                            modelDataBlock.AddRange(BitConverter.GetBytes(meshInfo.VertexCount));

                            //Index Count (int)
                            modelDataBlock.AddRange(BitConverter.GetBytes(meshInfo.IndexCount));
                        }

                        //material index (short)
                        modelDataBlock.AddRange(BitConverter.GetBytes((short)meshInfo.MaterialNum));

                        //mesh part table offset (short)
                        modelDataBlock.AddRange(BitConverter.GetBytes((short)meshInfo.MeshPartOffset));

                        //mesh part count (short)
                        modelDataBlock.AddRange(BitConverter.GetBytes((short)meshInfo.MeshPartCount));

                        //bone list index (short)
                        modelDataBlock.AddRange(BitConverter.GetBytes((short)meshInfo.BoneListIndex));

                        if(j != 0)
                        {
                            //index data offset (int)
                            int meshIndexPadding = 8 - (meshInfoList[j - 1].IndexCount % 8);
                            if (meshIndexPadding == 8)
                            {
                                meshIndexPadding = 0;
                            }
                            modelDataBlock.AddRange(BitConverter.GetBytes(meshInfoList[j - 1].IndexDataOffset + meshInfoList[j - 1].IndexCount + meshIndexPadding));
                        }
                        else
                        {
                            //index data offset (int)
                            modelDataBlock.AddRange(BitConverter.GetBytes(meshInfo.IndexDataOffset));
                        }

                        if (i == 0)
                        {
                            var mg = cmdList[j].meshGeometry;

                            if(j != 0)
                            {
                                //vertex data offset[0] (int)
                                meshInfo.VertexDataOffsets[0] = meshInfoList[j - 1].VertexDataOffsets[1] + (meshInfoList[j - 1].VertexCount * meshInfoList[j - 1].VertexSizes[1]);
                                modelDataBlock.AddRange(BitConverter.GetBytes(meshInfo.VertexDataOffsets[0]));

                                //vertex data offset[1] (int)
                                meshInfo.VertexDataOffsets[1] = meshInfo.VertexDataOffsets[0] + (meshInfo.VertexCount * meshInfo.VertexSizes[0]);
                                modelDataBlock.AddRange(BitConverter.GetBytes(meshInfo.VertexDataOffsets[1]));

                                //vertex data offset[2] (int)
                                modelDataBlock.AddRange(BitConverter.GetBytes(meshInfo.VertexDataOffsets[2]));
                            }
                            else
                            {
                                //vertex data offset[0] (int)
                                modelDataBlock.AddRange(BitConverter.GetBytes(meshInfo.VertexDataOffsets[0]));

                                //vertex data offset[1] (int)
                                meshInfo.VertexDataOffsets[1] = mg.Positions.Count * meshInfo.VertexSizes[0];
                                modelDataBlock.AddRange(BitConverter.GetBytes(meshInfo.VertexDataOffsets[1]));


                                //vertex data offset[2] (int)
                                modelDataBlock.AddRange(BitConverter.GetBytes(meshInfo.VertexDataOffsets[2]));
                            }

                        }
                        else
                        {
                            //vertex data offset[0] (int)
                            modelDataBlock.AddRange(BitConverter.GetBytes(meshInfo.VertexDataOffsets[0]));

                            //vertex data offset[1] (int)
                            modelDataBlock.AddRange(BitConverter.GetBytes(meshInfo.VertexDataOffsets[1]));

                            //vertex data offset[2] (int)
                            modelDataBlock.AddRange(BitConverter.GetBytes(meshInfo.VertexDataOffsets[2]));
                        }

                        //vertex data size[0] (byte)
                        modelDataBlock.Add((byte)meshInfo.VertexSizes[0]);

                        //vertex data size[1] (byte)
                        modelDataBlock.Add((byte)meshInfo.VertexSizes[1]);

                        //vertex data size[2] (byte)
                        modelDataBlock.Add((byte)meshInfo.VertexSizes[2]);

                        //Data block count (byte)
                        modelDataBlock.Add((byte)meshInfo.VertexDataBlockCount);

                        meshInfoList.Add(meshInfo);
                    }
                    meshInfoDict.Add(i, meshInfoList);

                }

                modelDataBlock.AddRange(br.ReadBytes(atrStringCount * 4));

                if(unk6 > 0)
                {
                    modelDataBlock.AddRange(br.ReadBytes(unk6 * 20));
                }

                List<MeshPart> meshPart = new List<MeshPart>();

                int meshPadd = 0;

                for(int l = 0; l < lodList.Count; l++)
                {
                    for (int i = 0; i < meshInfoDict[l].Count; i++)
                    {
                        var mList = meshInfoDict[l];
                        var mPartCount = mList[i].MeshPartCount;

                        for (int j = 0; j < mPartCount; j++)
                        {
                            MeshPart mp = new MeshPart();

                            //Index Offset (int)
                            if (i == 0)
                            {
                                if (j != 0)
                                {
                                    br.ReadBytes(4);
                                    mp.IndexOffset = meshPart[meshPart.Count - 1].IndexOffset + meshPart[meshPart.Count - 1].IndexCount;
                                    modelDataBlock.AddRange(BitConverter.GetBytes(mp.IndexOffset));
                                }
                                else
                                {
                                    mp.IndexOffset = br.ReadInt32();
                                    modelDataBlock.AddRange(BitConverter.GetBytes(mp.IndexOffset));
                                }
                            }
                            else if (i == 1)
                            {
                                br.ReadBytes(4);
                                if (j == 0)
                                {
                                    mp.IndexOffset = meshPart[meshPart.Count - 1].IndexOffset + meshPart[meshPart.Count - 1].IndexCount + meshPadd;
                                }
                                else
                                {
                                    mp.IndexOffset = meshPart[meshPart.Count - 1].IndexOffset + meshPart[meshPart.Count - 1].IndexCount;
                                }

                                modelDataBlock.AddRange(BitConverter.GetBytes(mp.IndexOffset));
                            }
                            else
                            {
                                mp.IndexOffset = br.ReadInt32();
                                modelDataBlock.AddRange(BitConverter.GetBytes(mp.IndexOffset));
                            }

                            //Index Count (int)
                            if (i < cmdList.Count)
                            {
                                var partsDict = cmdList[i].partsDict;

                                var indexCount = br.ReadInt32();
                                if (partsDict.ContainsKey(j))
                                {
                                    mp.IndexCount = partsDict[j];
                                }
                                else
                                {
                                    mp.IndexCount = 0;
                                }

                                modelDataBlock.AddRange(BitConverter.GetBytes(mp.IndexCount));
                            }
                            else
                            {
                                mp.IndexCount = br.ReadInt32();
                                modelDataBlock.AddRange(BitConverter.GetBytes(mp.IndexCount));
                            }

                            if (i == 0)
                            {
                                if (j == mPartCount - 1)
                                {
                                    meshPadd = 8 - ((mp.IndexOffset + mp.IndexCount) % 8);
                                }
                            }

                            //Attributes (int)
                            mp.Attributes = br.ReadInt32();
                            modelDataBlock.AddRange(BitConverter.GetBytes(mp.Attributes));

                            //Bone reference offset (short)
                            mp.BoneOffset = br.ReadInt16();
                            modelDataBlock.AddRange(BitConverter.GetBytes((short)mp.BoneOffset));

                            //Bone reference count (short)
                            mp.BoneCount = br.ReadInt16();
                            modelDataBlock.AddRange(BitConverter.GetBytes((short)mp.BoneCount));

                            meshPart.Add(mp);
                        }
                    }
                }



                if(unk12 > 0)
                {
                    modelDataBlock.AddRange(br.ReadBytes(unk12 * 12));
                }

                modelDataBlock.AddRange(br.ReadBytes(matStringCount * 4));

                modelDataBlock.AddRange(br.ReadBytes(boneStringCount * 4));

                for (int i = 0; i < boneListCount; i++)
                {
                    //bone list
                    modelDataBlock.AddRange(br.ReadBytes(128));

                    //bone count (int)
                    modelDataBlock.AddRange(br.ReadBytes(4));
                }

                if(unk1 > 0)
                {
                    modelDataBlock.AddRange(br.ReadBytes(unk1 * 16));
                }

                if(unk2 > 0)
                {
                    modelDataBlock.AddRange(br.ReadBytes(unk2 * 12));
                }

                if(unk3 > 0)
                {
                    modelDataBlock.AddRange(br.ReadBytes(unk3 * 4));
                }

                //Bone index count (int)
                int boneIndexCount = br.ReadInt32();
                modelDataBlock.AddRange(BitConverter.GetBytes(boneIndexCount));

                //Bone indices
                modelDataBlock.AddRange(br.ReadBytes(boneIndexCount));

                //Padding count
                byte paddingCount = br.ReadByte();
                br.ReadBytes(paddingCount);
                modelDataBlock.Add(paddingCount);
                modelDataBlock.AddRange(new byte[paddingCount]);

                //Bounding Boxes
                modelDataBlock.AddRange(br.ReadBytes(128));

                //Bones
                modelDataBlock.AddRange(br.ReadBytes(boneStringCount * 32));

                var compModelData = Compressor(modelDataBlock.ToArray());

                compressedData.AddRange(BitConverter.GetBytes(16));
                compressedData.AddRange(BitConverter.GetBytes(0));
                compressedData.AddRange(BitConverter.GetBytes(compModelData.Length));
                compressedData.AddRange(BitConverter.GetBytes(modelDataBlock.Count));
                compressedData.AddRange(compModelData);

                padding = 128 - ((compModelData.Length + 16) % 128);

                compressedData.AddRange(new byte[padding]);

                compModelDataSize = compModelData.Length + 16 + padding;

                /* 
                 * -------------------------------
                 * Model Data Block End
                 * -------------------------------
                 */


                /*
                 * ---------------------
                 * Vertex Data Start
                 * ---------------------
                */

                List<int> compMeshSizes = new List<int>();

                List<DataBlocks> dbList = new List<DataBlocks>();
                DataBlocks db = new DataBlocks();

                for (int i = 0; i < importDict.Count; i++)
                {
                    db.VertexDataBlock.AddRange(importDict[i].dataSet1);
                    db.VertexDataBlock.AddRange(importDict[i].dataSet2);
                    db.IndexDataBlock.AddRange(importDict[i].indexSet);

                    var indexPadd = importDict[i].indexSet.Count % 16;
                    if(indexPadd != 0)
                    {
                        db.IndexDataBlock.AddRange(new byte[16 - indexPadd]);
                    }
                }

                db.VDBParts = (int)Math.Ceiling(db.VertexDataBlock.Count / 16000f);
                int[] VDB1PartCounts = new int[db.VDBParts];
                int VDB1Remaining = db.VertexDataBlock.Count;

                for (int i = 0; i < db.VDBParts; i++)
                {

                    if (VDB1Remaining >= 16000)
                    {
                        VDB1PartCounts[i] = 16000;
                        VDB1Remaining -= 16000;
                    }
                    else
                    {
                        VDB1PartCounts[i] = VDB1Remaining;
                    }
                }

                for (int i = 0; i < db.VDBParts; i++)
                {
                    var compVertexData1 = Compressor(db.VertexDataBlock.GetRange(i * 16000, VDB1PartCounts[i]).ToArray());

                    compressedData.AddRange(BitConverter.GetBytes(16));
                    compressedData.AddRange(BitConverter.GetBytes(0));
                    compressedData.AddRange(BitConverter.GetBytes(compVertexData1.Length));
                    compressedData.AddRange(BitConverter.GetBytes(VDB1PartCounts[i]));
                    compressedData.AddRange(compVertexData1);

                    var vertexPadding = 128 - ((compVertexData1.Length + 16) % 128);

                    compressedData.AddRange(new byte[vertexPadding]);

                    db.compVertexDataBlockSize += compVertexData1.Length + 16 + vertexPadding;
                    compMeshSizes.Add(compVertexData1.Length + 16 + vertexPadding);
                }

                var compIndexData1 = Compressor(db.IndexDataBlock.ToArray());

                compressedData.AddRange(BitConverter.GetBytes(16));
                compressedData.AddRange(BitConverter.GetBytes(0));
                compressedData.AddRange(BitConverter.GetBytes(compIndexData1.Length));
                compressedData.AddRange(BitConverter.GetBytes(db.IndexDataBlock.Count));
                compressedData.AddRange(compIndexData1);

                var indexPadding = 128 - ((compIndexData1.Length + 16) % 128);

                compressedData.AddRange(new byte[indexPadding]);

                db.compIndexDataBlockSize += compIndexData1.Length + 16 + indexPadding;

                dbList.Add(db);

                for(int i = 1; i < 3; i++)
                {
                    db = new DataBlocks();

                    for (int j = 0; j < lodList[i].MeshCount; j++)
                    {
                        var meshInfoList = meshInfoDict[i];

                        br.BaseStream.Seek(lodList[i].VertexOffset + meshInfoList[j].VertexDataOffsets[0], SeekOrigin.Begin);

                        db.VertexDataBlock.AddRange(br.ReadBytes(meshInfoList[j].VertexCount * meshInfoList[j].VertexSizes[0]));
                        db.VertexDataBlock.AddRange(br.ReadBytes(meshInfoList[j].VertexCount * meshInfoList[j].VertexSizes[1]));
                    }

                    for (int j = 0; j < lodList[i].MeshCount; j++)
                    {
                        var meshInfoList = meshInfoDict[i];

                        br.BaseStream.Seek(lodList[i].IndexOffset + (meshInfoList[j].IndexDataOffset), SeekOrigin.Begin);

                        db.IndexDataBlock.AddRange(br.ReadBytes(meshInfoList[j].IndexCount * 2));

                        indexPadding = (meshInfoList[j].IndexCount * 2) % 16;
                        if (indexPadding != 0)
                        {
                            db.IndexDataBlock.AddRange(new byte[16 - indexPadding]);

                            if(j != lodList[i].MeshCount - 1)
                            {
                                br.ReadBytes(16 - indexPadding);
                            }
                        }
                    }

                    db.VDBParts = (int)Math.Ceiling(db.VertexDataBlock.Count / 16000f);
                    int[] VDB2PartCounts = new int[db.VDBParts];
                    int VDB2Remaining = db.VertexDataBlock.Count;

                    for (int j = 0; j < db.VDBParts; j++)
                    {

                        if (VDB2Remaining >= 16000)
                        {
                            VDB2PartCounts[j] = 16000;
                            VDB2Remaining -= 16000;
                        }
                        else
                        {
                            VDB2PartCounts[j] = VDB2Remaining;
                        }
                    }

                    for (int j = 0; j < db.VDBParts; j++)
                    {
                        var compVertexData2 = Compressor(db.VertexDataBlock.GetRange(j * 16000, VDB2PartCounts[j]).ToArray());

                        compressedData.AddRange(BitConverter.GetBytes(16));
                        compressedData.AddRange(BitConverter.GetBytes(0));
                        compressedData.AddRange(BitConverter.GetBytes(compVertexData2.Length));
                        compressedData.AddRange(BitConverter.GetBytes(VDB2PartCounts[j]));
                        compressedData.AddRange(compVertexData2);

                        var vertexPadding = 128 - ((compVertexData2.Length + 16) % 128);

                        compressedData.AddRange(new byte[vertexPadding]);

                        db.compVertexDataBlockSize += compVertexData2.Length + 16 + vertexPadding;
                        compMeshSizes.Add(compVertexData2.Length + 16 + vertexPadding);
                    }


                    var compIndexData2 = Compressor(db.IndexDataBlock.ToArray());

                    compressedData.AddRange(BitConverter.GetBytes(16));
                    compressedData.AddRange(BitConverter.GetBytes(0));
                    compressedData.AddRange(BitConverter.GetBytes(compIndexData2.Length));
                    compressedData.AddRange(BitConverter.GetBytes(db.IndexDataBlock.Count));
                    compressedData.AddRange(compIndexData2);

                    indexPadding = 128 - ((compIndexData2.Length + 16) % 128);

                    compressedData.AddRange(new byte[indexPadding]);

                    db.compIndexDataBlockSize += compIndexData2.Length + 16 + indexPadding;

                    dbList.Add(db);
                }

                /*
                 * -----------------------------------
                 * Create Header Start
                 * -----------------------------------
                 */

                int headerLength = 256;

                if(compMeshSizes.Count > 24)
                {
                    headerLength = 384;
                }

                //Header Length
                datHeader.AddRange(BitConverter.GetBytes(headerLength));
                //Data Type
                datHeader.AddRange(BitConverter.GetBytes(3));
                //Uncompressed Size
                var uncompSize = vertexInfoBlock.Count + modelDataBlock.Count + 68;

                foreach(var dataBlock in dbList)
                {
                    uncompSize += dataBlock.VertexDataBlock.Count + dataBlock.IndexDataBlock.Count;
                }

                datHeader.AddRange(BitConverter.GetBytes(uncompSize));
                //Max Buffer Size?
                datHeader.AddRange(BitConverter.GetBytes((compressedData.Count / 128) + 16));
                //Buffer Size
                datHeader.AddRange(BitConverter.GetBytes(compressedData.Count / 128));
                //Block Count
                datHeader.AddRange(BitConverter.GetBytes((short)5));
                //Unknown
                datHeader.AddRange(BitConverter.GetBytes((short)256));

                //Vertex Info Block Uncompressed
                var datPadding = 128 - (vertexInfoBlock.Count % 128);
                datPadding = (datPadding == 128 ? 0 : datPadding);
                datHeader.AddRange(BitConverter.GetBytes(vertexInfoBlock.Count + datPadding));
                //Model Data Block Uncompressed
                datPadding = 128 - (modelDataBlock.Count % 128);
                datPadding = (datPadding == 128 ? 0 : datPadding);
                datHeader.AddRange(BitConverter.GetBytes(modelDataBlock.Count + datPadding));
                //Vertex Data Block LoD[1] Uncompressed
                datPadding = 128 - (dbList[0].VertexDataBlock.Count % 128);
                datPadding = (datPadding == 128 ? 0 : datPadding);
                datHeader.AddRange(BitConverter.GetBytes(dbList[0].VertexDataBlock.Count + datPadding));
                //Vertex Data Block LoD[2] Uncompressed
                datPadding = 128 - (dbList[1].VertexDataBlock.Count % 128);
                datPadding = (datPadding == 128 ? 0 : datPadding);
                datHeader.AddRange(BitConverter.GetBytes(dbList[1].VertexDataBlock.Count + datPadding));
                //Vertex Data Block LoD[3] Uncompressed
                datPadding = 128 - (dbList[2].VertexDataBlock.Count % 128);
                datPadding = (datPadding == 128 ? 0 : datPadding);
                datHeader.AddRange(BitConverter.GetBytes(dbList[2].VertexDataBlock.Count + datPadding));
                //Blank 1
                datHeader.AddRange(BitConverter.GetBytes(0));
                //Blank 2
                datHeader.AddRange(BitConverter.GetBytes(0));
                //Blank 3
                datHeader.AddRange(BitConverter.GetBytes(0));
                //Index Data Block LoD[1] Uncompressed
                datPadding = 128 - (dbList[0].IndexDataBlock.Count % 128);
                datPadding = (datPadding == 128 ? 0 : datPadding);
                datHeader.AddRange(BitConverter.GetBytes(dbList[0].IndexDataBlock.Count + datPadding));
                //Index Data Block LoD[2] Uncompressed
                datPadding = 128 - (dbList[1].IndexDataBlock.Count % 128);
                datPadding = (datPadding == 128 ? 0 : datPadding);
                datHeader.AddRange(BitConverter.GetBytes(dbList[1].IndexDataBlock.Count + datPadding));
                //Index Data Block LoD[3] Uncompressed
                datPadding = 128 - (dbList[2].IndexDataBlock.Count % 128);
                datPadding = (datPadding == 128 ? 0 : datPadding);
                datHeader.AddRange(BitConverter.GetBytes(dbList[2].IndexDataBlock.Count + datPadding));

                //Vertex Info Block Compressed
                datHeader.AddRange(BitConverter.GetBytes(compVertexInfoSize));
                //Model Data Block Compressed
                datHeader.AddRange(BitConverter.GetBytes(compModelDataSize));
                //Vertex Data Block LoD[1] Compressed
                datHeader.AddRange(BitConverter.GetBytes(dbList[0].compVertexDataBlockSize));
                //Vertex Data Block LoD[2] Compressed
                datHeader.AddRange(BitConverter.GetBytes(dbList[1].compVertexDataBlockSize));
                //Vertex Data Block LoD[3] Compressed
                datHeader.AddRange(BitConverter.GetBytes(dbList[2].compVertexDataBlockSize));
                //Blank 1
                datHeader.AddRange(BitConverter.GetBytes(0));
                //Blank 2
                datHeader.AddRange(BitConverter.GetBytes(0));
                //Blank 3
                datHeader.AddRange(BitConverter.GetBytes(0));
                //Index Data Block LoD[1] Compressed
                datHeader.AddRange(BitConverter.GetBytes(dbList[0].compIndexDataBlockSize));
                //Index Data Block LoD[2] Compressed
                datHeader.AddRange(BitConverter.GetBytes(dbList[1].compIndexDataBlockSize));
                //Index Data Block LoD[3] Compressed
                datHeader.AddRange(BitConverter.GetBytes(dbList[2].compIndexDataBlockSize));

                var vertexInfoOffset = 0;
                var modelDataOffset = compVertexInfoSize;
                var vertexDataBlock1Offset = modelDataOffset + compModelDataSize;
                var indexDataBlock1Offset = vertexDataBlock1Offset + dbList[0].compVertexDataBlockSize;
                var vertexDataBlock2Offset = indexDataBlock1Offset + dbList[0].compIndexDataBlockSize;
                var indexDataBlock2Offset = vertexDataBlock2Offset + dbList[1].compVertexDataBlockSize;
                var vertexDataBlock3Offset = indexDataBlock2Offset + dbList[1].compIndexDataBlockSize;
                var indexDataBlock3Offset = vertexDataBlock3Offset + dbList[2].compVertexDataBlockSize;

                //Vertex Info Offset
                datHeader.AddRange(BitConverter.GetBytes(vertexInfoOffset));
                //Model Data Offset
                datHeader.AddRange(BitConverter.GetBytes(modelDataOffset));
                //Vertex Data Block LoD[1] Offset
                datHeader.AddRange(BitConverter.GetBytes(vertexDataBlock1Offset));
                //Vertex Data Block LoD[2] Offset
                datHeader.AddRange(BitConverter.GetBytes(vertexDataBlock2Offset));
                //Vertex Data Block LoD[3] Offset
                datHeader.AddRange(BitConverter.GetBytes(vertexDataBlock3Offset));
                //Blank 1
                datHeader.AddRange(BitConverter.GetBytes(0));
                //Blank 2
                datHeader.AddRange(BitConverter.GetBytes(0));
                //Blank 3
                datHeader.AddRange(BitConverter.GetBytes(0));
                //Index Data Block LoD[1] Offset
                datHeader.AddRange(BitConverter.GetBytes(indexDataBlock1Offset));
                //Index Data Block LoD[2] Offset
                datHeader.AddRange(BitConverter.GetBytes(indexDataBlock2Offset));
                //Index Data Block LoD[3] Offset
                datHeader.AddRange(BitConverter.GetBytes(indexDataBlock3Offset));


                var ind1 = 2 + dbList[0].VDBParts;
                var vert2 = ind1 + 1;
                var ind2 = vert2 + dbList[1].VDBParts;
                var vert3 = ind2 + 1;
                var ind3 = vert3 + dbList[2].VDBParts;

                //Vertex Info Index
                datHeader.AddRange(BitConverter.GetBytes((short)0));
                //Model Data Index
                datHeader.AddRange(BitConverter.GetBytes((short)1));
                //Vertex Data Block LoD[1] Index
                datHeader.AddRange(BitConverter.GetBytes((short)2));
                //Vertex Data Block LoD[2] Index
                datHeader.AddRange(BitConverter.GetBytes((short)vert2));
                //Vertex Data Block LoD[3] Index
                datHeader.AddRange(BitConverter.GetBytes((short)vert3));
                //Blank 1 (copies indices?)
                datHeader.AddRange(BitConverter.GetBytes((short)ind1));
                //Blank 2 (copies indices?)
                datHeader.AddRange(BitConverter.GetBytes((short)ind2));
                //Blank 3 (copies indices?)
                datHeader.AddRange(BitConverter.GetBytes((short)ind3));
                //Index Data Block LoD[1] Index
                datHeader.AddRange(BitConverter.GetBytes((short)ind1));
                //Index Data Block LoD[2] Index
                datHeader.AddRange(BitConverter.GetBytes((short)ind2));
                //Index Data Block LoD[3] Index
                datHeader.AddRange(BitConverter.GetBytes((short)ind3));


                //Vertex Info part count
                datHeader.AddRange(BitConverter.GetBytes((short)1));
                //Model Data part count
                datHeader.AddRange(BitConverter.GetBytes((short)1));
                //Vertex Data Block LoD[1] part count
                datHeader.AddRange(BitConverter.GetBytes((short)dbList[0].VDBParts));
                //Vertex Data Block LoD[2] part count
                datHeader.AddRange(BitConverter.GetBytes((short)dbList[1].VDBParts));
                //Vertex Data Block LoD[3] part count
                datHeader.AddRange(BitConverter.GetBytes((short)dbList[2].VDBParts));
                //Blank 1 
                datHeader.AddRange(BitConverter.GetBytes((short)0));
                //Blank 2 
                datHeader.AddRange(BitConverter.GetBytes((short)0));
                //Blank 3 
                datHeader.AddRange(BitConverter.GetBytes((short)0));
                //Index Data Block LoD[1] part count
                datHeader.AddRange(BitConverter.GetBytes((short)1));
                //Index Data Block LoD[2] part count
                datHeader.AddRange(BitConverter.GetBytes((short)1));
                //Index Data Block LoD[3] part count
                datHeader.AddRange(BitConverter.GetBytes((short)1));

                //Mesh Count
                datHeader.AddRange(BitConverter.GetBytes((short)meshCount));
                //Material Count
                datHeader.AddRange(BitConverter.GetBytes((short)matStringCount));
                //Unknown 1
                datHeader.AddRange(BitConverter.GetBytes((short)259));
                //Unknown 2
                datHeader.AddRange(BitConverter.GetBytes((short)0));

                int VDBPartCount = 0;
                //Vertex Info padded size
                datHeader.AddRange(BitConverter.GetBytes((short)compVertexInfoSize));
                //Model Data padded size
                datHeader.AddRange(BitConverter.GetBytes((short)compModelDataSize));
                //Vertex Data Block LoD[1] part padded sizes
                for(int i = 0; i < dbList[0].VDBParts; i++)
                {
                    datHeader.AddRange(BitConverter.GetBytes((short)compMeshSizes[i]));
                }
                VDBPartCount += dbList[0].VDBParts;
                //Index Data Block LoD[1] padded size
                datHeader.AddRange(BitConverter.GetBytes((short)dbList[0].compIndexDataBlockSize));
            
                //Vertex Data Block LoD[2] part padded sizes
                for (int i = 0; i < dbList[1].VDBParts; i++)
                {
                    datHeader.AddRange(BitConverter.GetBytes((short)compMeshSizes[VDBPartCount + i]));
                }
                VDBPartCount += dbList[1].VDBParts;
                //Index Data Block LoD[2] padded size
                datHeader.AddRange(BitConverter.GetBytes((short)dbList[1].compIndexDataBlockSize));

                //Vertex Data Block LoD[3] part padded sizes
                for (int i = 0; i < dbList[2].VDBParts; i++)
                {
                    datHeader.AddRange(BitConverter.GetBytes((short)compMeshSizes[VDBPartCount + i]));
                }

                //Index Data Block LoD[3] padded size
                datHeader.AddRange(BitConverter.GetBytes((short)dbList[2].compIndexDataBlockSize));

                //Rest of header
                var headerEnd = headerLength - (datHeader.Count % headerLength);
                datHeader.AddRange(new byte[headerEnd]);
            }
            compressedData.InsertRange(0, datHeader);

            WriteToDat(compressedData, modEntry, inModList, internalPath, category, itemName, lineNum);
        }

        public class DataBlocks
        {
            public int compVertexDataBlockSize = 0;
            public int compIndexDataBlockSize = 0;
            public int VDBParts = 0;

            public List<byte> VertexDataBlock = new List<byte>();
            public List<byte> IndexDataBlock = new List<byte>();
        }

        public class ImportData
        {
            public List<byte> dataSet1 = new List<byte>();
            public List<byte> dataSet2 = new List<byte>();
            public List<byte> indexSet = new List<byte>();
        }

        public class ColladaData
        {
            public string[] bones;

            public List<float> vertex = new List<float>();
            public List<float> normal = new List<float>();
            public List<float> texCoord = new List<float>();
            public List<float> weights = new List<float>();
            public List<int> index = new List<int>();
            public List<int> bIndex = new List<int>();
            public List<int> vCount = new List<int>();

            public Dictionary<int, int> partsDict = new Dictionary<int, int>();
        }

        public class ColladaMeshData
        {
            public MeshGeometry3D meshGeometry = new MeshGeometry3D();

            public List<byte> blendIndices = new List<byte>();
            public List<byte> blendWeights = new List<byte>();

            public Dictionary<int, int> partsDict = new Dictionary<int, int>();
        }


        /// <summary>
        /// Writes the newly imported data to the .dat for modifications.
        /// </summary>
        /// <param name="data">The data to be written.</param>
        /// <param name="modEntry">The modlist entry (if any) for the given file.</param>
        /// <param name="inModList">Is the item already contained within the mod list.</param>
        /// <param name="internalFilePath">The internal file path of the item being modified.</param>
        /// <param name="category">The category of the item.</param>
        /// <param name="itemName">The name of the item being modified.</param>
        /// <param name="lineNum">The line number of the existing mod list entry for the item if it exists.</param>
        /// <returns>The new offset in which the modified data was placed.</returns>
        public static int WriteToDat(List<byte> data, JsonEntry modEntry, bool inModList, string internalFilePath, string category, string itemName, int lineNum)
        {
            int offset = 0;
            bool dataOverwritten = false;
            try
            {
                using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(Info.modDatDir)))
                {
                    /* 
                     * If the item has been previously modified and the compressed data being imported is smaller or equal to the exisiting data
                    *  replace the existing data with new data.
                    */
                    if (inModList && data.Count <= modEntry.modSize)
                    {
                        int sizeDiff = modEntry.modSize - data.Count;

                        bw.BaseStream.Seek(modEntry.modOffset - DatOffsetAmount, SeekOrigin.Begin);

                        bw.Write(data.ToArray());

                        bw.Write(new byte[sizeDiff]);

                        Helper.UpdateIndex(modEntry.modOffset, internalFilePath);
                        Helper.UpdateIndex2(modEntry.modOffset, internalFilePath);

                        offset = modEntry.modOffset;

                        dataOverwritten = true;
                    }
                    else
                    {
                        int emptyLength = 0;
                        int emptyLine = 0;

                        /* 
                         * If there is an empty entry in the modlist and the compressed data being imported is smaller or equal to the available space
                        *  write the compressed data in the existing space.
                        */
                        if (Properties.Settings.Default.Mod_List == 0)
                        {
                            try
                            {
                                foreach (string line in File.ReadAllLines(Info.modListDir))
                                {
                                    JsonEntry emptyEntry = JsonConvert.DeserializeObject<JsonEntry>(line);

                                    if (emptyEntry.fullPath.Equals(""))
                                    {
                                        emptyLength = emptyEntry.modSize;

                                        if (emptyLength > data.Count)
                                        {
                                            int sizeDiff = emptyLength - data.Count;

                                            bw.BaseStream.Seek(emptyEntry.modOffset - DatOffsetAmount, SeekOrigin.Begin);

                                            bw.Write(data.ToArray());

                                            bw.Write(new byte[sizeDiff]);

                                            int originalOffset = Helper.UpdateIndex(emptyEntry.modOffset, internalFilePath) * 8;
                                            Helper.UpdateIndex2(emptyEntry.modOffset, internalFilePath);

                                            JsonEntry replaceEntry = new JsonEntry()
                                            {
                                                category = category,
                                                name = itemName,
                                                fullPath = internalFilePath,
                                                originalOffset = originalOffset,
                                                modOffset = emptyEntry.modOffset,
                                                modSize = emptyEntry.modSize
                                            };

                                            string[] lines = File.ReadAllLines(Info.modListDir);
                                            lines[emptyLine] = JsonConvert.SerializeObject(replaceEntry);
                                            File.WriteAllLines(Info.modListDir, lines);

                                            offset = emptyEntry.modOffset;

                                            dataOverwritten = true;
                                            break;
                                        }
                                    }
                                    emptyLine++;
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("[Import] Error Accessing .modlist File \n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }


                        if (!dataOverwritten)
                        {
                            bw.BaseStream.Seek(0, SeekOrigin.End);

                            while ((bw.BaseStream.Position & 0xFF) != 0)
                            {
                                bw.Write((byte)0);
                            }

                            int eof = (int)bw.BaseStream.Position + data.Count;

                            while ((eof & 0xFF) != 0)
                            {
                                data.AddRange(new byte[16]);
                                eof = eof + 16;
                            }

                            offset = (int)bw.BaseStream.Position + DatOffsetAmount;

                            bw.Write(data.ToArray());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("[Import] Error Accessing .dat4 File \n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return 0;
            }


            if (!dataOverwritten && Properties.Settings.Default.Mod_List == 0)
            {
                int oldOffset = Helper.UpdateIndex(offset, internalFilePath) * 8;
                Helper.UpdateIndex2(offset, internalFilePath);

                /*
                 * If the item has been previously modifed, but the new compressed data to be imported is larger than the existing data
                 * remove the data from the modlist, leaving the offset and size intact for future use
                */
                if (inModList && data.Count > modEntry.modSize && modEntry != null)
                {
                    oldOffset = modEntry.originalOffset;

                    JsonEntry replaceEntry = new JsonEntry()
                    {
                        category = String.Empty,
                        name = String.Empty,
                        fullPath = String.Empty,
                        originalOffset = 0,
                        modOffset = modEntry.modOffset,
                        modSize = modEntry.modSize
                    };

                    string[] lines = File.ReadAllLines(Info.modListDir);
                    lines[lineNum] = JsonConvert.SerializeObject(replaceEntry);
                    File.WriteAllLines(Info.modListDir, lines);
                }

                JsonEntry entry = new JsonEntry()
                {
                    category = category,
                    name = itemName,
                    fullPath = internalFilePath,
                    originalOffset = oldOffset,
                    modOffset = offset,
                    modSize = data.Count
                };

                try
                {
                    using (StreamWriter modFile = new StreamWriter(Info.modListDir, true))
                    {
                        modFile.BaseStream.Seek(0, SeekOrigin.End);
                        modFile.WriteLine(JsonConvert.SerializeObject(entry));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("[Import] Error Accessing .modlist File \n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            return offset;
        }

        /// <summary>
        /// Compresses raw byte data.
        /// </summary>
        /// <param name="uncomp">The data to be compressed.</param>
        /// <returns>The compressed byte data.</returns>
        private static byte[] Compressor(byte[] uncomp)
        {
            using (MemoryStream uncompressedMS = new MemoryStream(uncomp))
            {
                byte[] compbytes = null;
                using (var compressedMS = new MemoryStream())
                {
                    using (var ds = new DeflateStream(compressedMS, CompressionMode.Compress))
                    {
                        uncompressedMS.CopyTo(ds);
                        ds.Close();
                        compbytes = compressedMS.ToArray();
                    }
                }
                return compbytes;
            }
        }
    }
}
