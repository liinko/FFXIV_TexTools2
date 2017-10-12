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


        public static void ImportDAE(string category, string itemName, string modelName, string selectedMesh, string internalPath)
        {
            var savePath = Properties.Settings.Default.Save_Directory + "/" + category + "/" + itemName + "/3D/" + modelName + ".DAE";

            if (File.Exists(savePath))
            {
                float[] vertex = null, normal = null, texCoord = null, weights = null;
                float[] vertex1 = null, normal1 = null, texCoord1 = null, weights1 = null;
                int[] index = null, bIndex = null;
                int[] index1 = null, bIndex1 = null;
                int[] vCount = null;
                int[] vCount1 = null;

                string geo1 = "_0";
                string geo2 = "_1";
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
                                //go to mesh 1
                                if (atr.Contains(geo1))
                                {
                                    while (reader.Read())
                                    {
                                        if (reader.IsStartElement())
                                        {
                                            if (reader.Name.Contains("float_array"))
                                            {
                                                //Vertex 
                                                if (reader["id"].ToLower().Contains(pos))
                                                {
                                                    vertex = (float[])reader.ReadElementContentAs(typeof(float[]), null);
                                                }
                                                //Normals
                                                else if (reader["id"].ToLower().Contains(norm))
                                                {
                                                    normal = (float[])reader.ReadElementContentAs(typeof(float[]), null);
                                                }
                                                //Texture Coordinates
                                                else if (reader["id"].ToLower().Contains(texc))
                                                {
                                                    texCoord = (float[])reader.ReadElementContentAs(typeof(float[]), null);
                                                }
                                            }
                                            //Indices
                                            if (reader.Name.Equals("p"))
                                            {
                                                index = (int[])reader.ReadElementContentAs(typeof(int[]), null);
                                                break;
                                            }
                                        }
                                    }
                                }
                                else if (atr.Contains(geo2))
                                {
                                    while (reader.Read())
                                    {
                                        if (reader.IsStartElement())
                                        {
                                            if (reader.Name.Contains("float_array"))
                                            {
                                                //Vertex 
                                                if (reader["id"].ToLower().Contains(pos))
                                                {
                                                    vertex1 = (float[])reader.ReadElementContentAs(typeof(float[]), null);
                                                }
                                                //Normals
                                                else if (reader["id"].ToLower().Contains(norm))
                                                {
                                                    normal1 = (float[])reader.ReadElementContentAs(typeof(float[]), null);
                                                }
                                                //Texture Coordinates
                                                else if (reader["id"].ToLower().Contains(texc))
                                                {
                                                    texCoord1 = (float[])reader.ReadElementContentAs(typeof(float[]), null);
                                                }
                                            }
                                            //Indices
                                            if (reader.Name.Equals("p"))
                                            {
                                                index1 = (int[])reader.ReadElementContentAs(typeof(int[]), null);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            //go to controller element
                            else if (reader.Name.Equals("controller"))
                            {
                                var atr = reader["id"];
                                //go to mesh 1
                                if (atr.Contains("_0"))
                                {
                                    while (reader.Read())
                                    {
                                        if (reader.IsStartElement())
                                        {
                                            if (reader.Name.Contains("float_array"))
                                            {
                                                //Blend Weight
                                                if (reader["id"].ToLower().Contains("weights-array"))
                                                {
                                                    weights = (float[])reader.ReadElementContentAs(typeof(float[]), null);
                                                }
                                            }
                                            //Blend counts
                                            else if (reader.Name.Equals("vcount"))
                                            {
                                                vCount = (int[])reader.ReadElementContentAs(typeof(int[]), null);
                                            }
                                            //Blend Indices
                                            else if (reader.Name.Equals("v"))
                                            {
                                                bIndex = (int[])reader.ReadElementContentAs(typeof(int[]), null);
                                                break;
                                            }
                                        }
                                    }
                                }
                                else if (atr.Contains("_1"))
                                {
                                    while (reader.Read())
                                    {
                                        if (reader.IsStartElement())
                                        {
                                            if (reader.Name.Contains("float_array"))
                                            {
                                                //Blend Weight
                                                if (reader["id"].ToLower().Contains("weights-array"))
                                                {
                                                    weights1 = (float[])reader.ReadElementContentAs(typeof(float[]), null);
                                                }
                                            }
                                            //Blend counts
                                            else if (reader.Name.Equals("vcount"))
                                            {
                                                vCount1 = (int[])reader.ReadElementContentAs(typeof(int[]), null);
                                            }
                                            //Blend Indices
                                            else if (reader.Name.Equals("v"))
                                            {
                                                bIndex1 = (int[])reader.ReadElementContentAs(typeof(int[]), null);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                Vector3Collection Vertex = new Vector3Collection();
                Vector2Collection TexCoord = new Vector2Collection();
                Vector3Collection Normals = new Vector3Collection();
                IntCollection Indices = new IntCollection();
                List<byte> blendIndices = new List<byte>();
                List<byte> blendWeights = new List<byte>();

                for (int i = 0; i < vertex.Length; i += 3)
                {
                    Vertex.Add(new SharpDX.Vector3(vertex[i], vertex[i + 1], vertex[i + 2]));
                }

                for (int i = 0; i < normal.Length; i += 3)
                {
                    Normals.Add(new SharpDX.Vector3(normal[i], normal[i + 1], normal[i + 2]));
                }

                for (int i = 0; i < texCoord.Length; i += tcStride)
                {
                    TexCoord.Add(new SharpDX.Vector2(texCoord[i], texCoord[i + 1]));
                }

                for (int i = 0; i < index.Length; i += 4)
                {
                    Indices.Add(index[i]);
                }


                int vTrack = 0;
                for (int i = 0; i < Vertex.Count; i++)
                {
                    int bCount = vCount[i];

                    for(int j = 0; j < bCount * 2; j += 2)
                    {
                        blendIndices.Add((byte)bIndex[vTrack*2 + j]);
                        blendWeights.Add((byte)Math.Round(weights[bIndex[vTrack*2 + j + 1]] * 255f));
                    }

                    if(bCount < 4)
                    {
                        int remainder = 4 - bCount;

                        for(int k =0; k < remainder; k++)
                        {
                            blendIndices.Add((byte)0);
                            blendWeights.Add((byte)0);
                        }
                    }

                    vTrack += bCount;
                }

                MeshGeometry3D mg = new MeshGeometry3D();
                mg.Positions = Vertex;
                mg.Indices = Indices;
                mg.Normals = Normals;
                mg.TextureCoordinates = TexCoord;
                MeshBuilder.ComputeTangents(mg);

                Vector3Collection Vertex1 = new Vector3Collection();
                Vector2Collection TexCoord1 = new Vector2Collection();
                Vector3Collection Normals1 = new Vector3Collection();
                IntCollection Indices1 = new IntCollection();
                List<byte> blendIndices1 = new List<byte>();
                List<byte> blendWeights1 = new List<byte>();

                for (int i = 0; i < vertex1.Length; i += 3)
                {
                    Vertex1.Add(new SharpDX.Vector3(vertex1[i], vertex1[i + 1], vertex1[i + 2]));
                }

                for (int i = 0; i < normal1.Length; i += 3)
                {
                    Normals1.Add(new SharpDX.Vector3(normal1[i], normal1[i + 1], normal1[i + 2]));
                }

                for (int i = 0; i < texCoord1.Length; i += tcStride)
                {
                    TexCoord1.Add(new SharpDX.Vector2(texCoord1[i], texCoord1[i + 1]));
                }

                for (int i = 0; i < index1.Length; i += 4)
                {
                    Indices1.Add(index1[i]);
                }

                vTrack = 0;
                for (int i = 0; i < Vertex1.Count; i++)
                {
                    int bCount = vCount1[i];

                    for (int j = 0; j < bCount * 2; j += 2)
                    {
                        blendIndices1.Add((byte)bIndex1[vTrack * 2 + j]);
                        blendWeights1.Add((byte)Math.Round(weights1[bIndex1[vTrack * 2 + j + 1]] * 255f));
                    }

                    if (bCount < 4)
                    {
                        int remainder = 4 - bCount;

                        for (int k = 0; k < remainder; k++)
                        {
                            blendIndices1.Add((byte)0);
                            blendWeights1.Add((byte)0);
                        }
                    }

                    vTrack += bCount;
                }

                MeshGeometry3D mg1 = new MeshGeometry3D();
                mg1.Positions = Vertex1;
                mg1.Indices = Indices1;
                mg1.Normals = Normals1;
                mg1.TextureCoordinates = TexCoord1;
                MeshBuilder.ComputeTangents(mg1);

                Create(mg, mg1, blendWeights.ToArray(), blendIndices.ToArray(), blendWeights1.ToArray(), blendIndices1.ToArray(), internalPath, selectedMesh, category, itemName);
            }
        }

        public static void Create(MeshGeometry3D mg, MeshGeometry3D mg1, byte[] blendWeights, byte[] blendIndex, byte[] blendWeights1, byte[] blendIndex1, string internalPath, string selectedMesh, string category, string itemName)
        {
            var biNormals = mg.BiTangents;
            var biNormals1 = mg1.BiTangents;
            int indexDiff0 = 0;
            int indexDiff1 = 0;
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
                    MessageBox.Show("[Main] Error Accessing .modlist File \n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            /*
            * Imported Vertex Data Start
            */


            List<byte> importedVert00 = new List<byte>();
            List<byte> importedVert10 = new List<byte>();

            //TODO need to check vertex type
            int bc = 0;
            for (int i = 0; i < mg.Positions.Count; i++)
            {
                //vertex points
                importedVert00.AddRange(BitConverter.GetBytes(mg.Positions[i].X));
                importedVert00.AddRange(BitConverter.GetBytes(mg.Positions[i].Y));
                importedVert00.AddRange(BitConverter.GetBytes(mg.Positions[i].Z));

                //blend weight
                importedVert00.Add(blendWeights[bc]);
                importedVert00.Add(blendWeights[bc + 1]);
                importedVert00.Add(blendWeights[bc + 2]);
                importedVert00.Add(blendWeights[bc + 3]);

                //blend index
                importedVert00.Add(blendIndex[bc]);
                importedVert00.Add(blendIndex[bc + 1]);
                importedVert00.Add(blendIndex[bc + 2]);
                importedVert00.Add(blendIndex[bc + 3]);

                bc += 4;
            }

            bc = 0;
            for (int i = 0; i < mg1.Positions.Count; i++)
            {
                //vertex points
                importedVert10.AddRange(BitConverter.GetBytes(mg1.Positions[i].X));
                importedVert10.AddRange(BitConverter.GetBytes(mg1.Positions[i].Y));
                importedVert10.AddRange(BitConverter.GetBytes(mg1.Positions[i].Z));

                //blend weight
                importedVert10.Add(blendWeights1[bc]);
                importedVert10.Add(blendWeights1[bc + 1]);
                importedVert10.Add(blendWeights1[bc + 2]);
                importedVert10.Add(blendWeights1[bc + 3]);

                //blend index
                importedVert10.Add(blendIndex1[bc]);
                importedVert10.Add(blendIndex1[bc + 1]);
                importedVert10.Add(blendIndex1[bc + 2]);
                importedVert10.Add(blendIndex1[bc + 3]);

                bc += 4;
            }
            List<byte> importedVert01 = new List<byte>();
            List<byte> importedVert11 = new List<byte>();

            for (int i = 0; i < mg.Normals.Count; i++)
            {
                //Normal X (Half)
                var hx = Half.Parse(mg.Normals[i].X.ToString());
                importedVert01.AddRange(Half.GetBytes(hx));

                //Normal Y (Half)
                var hy = Half.Parse(mg.Normals[i].Y.ToString());
                importedVert01.AddRange(Half.GetBytes(hy));

                //Normal Z (Half)
                var hz = Half.Parse(mg.Normals[i].Z.ToString());
                importedVert01.AddRange(Half.GetBytes(hz));

                //Normal W (Half)
                importedVert01.AddRange(new byte[2]);


                //tangent X
                byte tx = (byte)(((255 * biNormals[i].X) + 255) / 2);
                importedVert01.Add(tx);

                //tangent Y
                byte ty = (byte)(((255 * biNormals[i].Y) + 255) / 2);
                importedVert01.Add(ty);

                //tangent Z
                byte tz = (byte)(((255 * biNormals[i].Z) + 255) / 2);
                importedVert01.Add(tz);

                //tangent W
                importedVert01.Add((byte)0);

                //Color
                importedVert01.AddRange(BitConverter.GetBytes(4294967295));

                //TexCoord X
                var tcx = Half.Parse(mg.TextureCoordinates[i].X.ToString());
                importedVert01.AddRange(Half.GetBytes(tcx));

                //TexCoord Y
                var tcy = Half.Parse(mg.TextureCoordinates[i].Y.ToString()) * -1;
                importedVert01.AddRange(Half.GetBytes(tcy));

                importedVert01.AddRange(BitConverter.GetBytes((short)0));
                importedVert01.AddRange(BitConverter.GetBytes((short)15360));
            }

            for (int i = 0; i < mg1.Normals.Count; i++)
            {
                //Normal X (Half)
                var hx = Half.Parse(mg1.Normals[i].X.ToString());
                importedVert11.AddRange(Half.GetBytes(hx));

                //Normal Y (Half)
                var hy = Half.Parse(mg1.Normals[i].Y.ToString());
                importedVert11.AddRange(Half.GetBytes(hy));

                //Normal Z (Half)
                var hz = Half.Parse(mg1.Normals[i].Z.ToString());
                importedVert11.AddRange(Half.GetBytes(hz));

                //Normal W (Half)
                importedVert11.AddRange(new byte[2]);

                try
                {
                    //tangent X
                    byte tx = (byte)(((255 * biNormals1[i].X) + 255) / 2);
                    importedVert11.Add(tx);
                }
                catch
                {
                    //tangent X
                    byte tx = (byte)1;
                    importedVert11.Add(tx);
                }

                try
                {
                    //tangent Y
                    byte ty = (byte)(((255 * biNormals1[i].Y) + 255) / 2);
                    importedVert11.Add(ty);
                }
                catch
                {
                    //tangent Y
                    byte ty = (byte)1;
                    importedVert11.Add(ty);
                }

                try
                {
                    //tangent Z
                    byte tz = (byte)(((255 * biNormals1[i].Z) + 255) / 2);
                    importedVert11.Add(tz);
                }
                catch
                {
                    //tangent Z
                    byte tz = (byte)1;
                    importedVert11.Add(tz);
                }

                //tangent W
                importedVert11.Add((byte)0);

                //Color
                importedVert11.AddRange(BitConverter.GetBytes(4294967295));

                //TexCoord X
                var tcx = Half.Parse(mg1.TextureCoordinates[i].X.ToString());
                importedVert11.AddRange(Half.GetBytes(tcx));

                //TexCoord Y
                var tcy = Half.Parse(mg1.TextureCoordinates[i].Y.ToString()) * -1;
                importedVert11.AddRange(Half.GetBytes(tcy));

                importedVert11.AddRange(BitConverter.GetBytes((short)0));
                importedVert11.AddRange(BitConverter.GetBytes((short)15360));
            }

            List<byte> importedIndex = new List<byte>();
            foreach (var i in mg.Indices)
            {
                importedIndex.AddRange(BitConverter.GetBytes((short)i));
            }

            List<byte> importedIndex1 = new List<byte>();
            foreach (var i in mg1.Indices)
            {
                importedIndex1.AddRange(BitConverter.GetBytes((short)i));
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

                //Unknown (short) * 20
                modelDataBlock.AddRange(br.ReadBytes(40));

                //LoD Section
                int vertexSize0 = 0;
                int indexSize0 = 0;
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
                        lod.VertexDataSize = (mg.Positions.Count * 20) + (mg.Positions.Count * 24) + (mg1.Positions.Count * 20) + (mg1.Positions.Count * 24);
                        vertexSize0 = lod.VertexDataSize - originalSize;
                    }
                    else
                    {
                        lod.VertexDataSize = br.ReadInt32();
                    }

                    //LoD Index Buffer Size (int)
                    if(i == 0)
                    {
                        int originalSize = br.ReadInt32();


                        if(mg1.Indices.Count != 0)
                        {
                            lod.IndexDataSize = (mg.Indices.Count * 2) + (16 - ((mg.Indices.Count * 2) % 16)) + (mg1.Indices.Count * 2) + (16 - ((mg1.Indices.Count * 2) % 16));
                        }
                        else
                        {
                            lod.IndexDataSize = (mg.Indices.Count * 2) + (16 - ((mg.Indices.Count * 2) % 16));

                        }

                        indexSize0 = lod.IndexDataSize - originalSize;
                    }
                    else
                    {
                        lod.IndexDataSize = br.ReadInt32();
                    }

                    //LoD Vertex Offset (int)
                    if(i == 0)
                    {
                        lod.VertexOffset = br.ReadInt32();
                    }
                    else
                    {
                        br.ReadBytes(4);
                        lod.VertexOffset = lodList[i - 1].VertexOffset + lodList[i - 1].VertexDataSize + lodList[i - 1].IndexDataSize;
                    }


                    //LoD Index Offset (int)

                    if(i == 0)
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
                List<MeshInfo> meshInfoList = new List<MeshInfo>();

                for (int i = 0; i < meshCount; i++)
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
                        //Vertex Count (int)
                        modelDataBlock.AddRange(BitConverter.GetBytes(mg.Positions.Count));
                        meshInfo.VertexCount = mg.Positions.Count;

                        //Index Count (int)
                        modelDataBlock.AddRange(BitConverter.GetBytes(mg.Indices.Count));
                        indexDiff0 = mg.Indices.Count - meshInfo.IndexCount;
                        meshInfo.IndexCount = mg.Indices.Count;
                    }
                    else if (i == 1)
                    {
                        //Vertex Count (int)
                        modelDataBlock.AddRange(BitConverter.GetBytes(mg1.Positions.Count));
                        meshInfo.VertexCount = mg1.Positions.Count;

                        //Index Count (int)
                        modelDataBlock.AddRange(BitConverter.GetBytes(mg1.Indices.Count));
                        indexDiff1 = mg1.Indices.Count - meshInfo.IndexCount;
                        meshInfo.IndexCount = mg1.Indices.Count;
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

                    if(i == 1)
                    {
                        //index data offset (int)
                        int meshIndexPadding = 8 - (mg.Indices.Count % 8);
                        modelDataBlock.AddRange(BitConverter.GetBytes(mg.Indices.Count + meshIndexPadding));
                    }
                    else
                    {
                        //index data offset (int)
                        modelDataBlock.AddRange(BitConverter.GetBytes(meshInfo.IndexDataOffset));
                    }

                    if(i == 0)
                    {
                        //vertex data offset[0] (int)
                        modelDataBlock.AddRange(BitConverter.GetBytes(meshInfo.VertexDataOffsets[0]));

                        //vertex data offset[1] (int)
                        meshInfo.VertexDataOffsets[1] = mg.Positions.Count * meshInfo.VertexSizes[0];
                        modelDataBlock.AddRange(BitConverter.GetBytes(meshInfo.VertexDataOffsets[1]));


                        //vertex data offset[2] (int)
                        modelDataBlock.AddRange(BitConverter.GetBytes(meshInfo.VertexDataOffsets[2]));
                    }
                    else if(i == 1)
                    {
                        //vertex data offset[0] (int)
                        meshInfo.VertexDataOffsets[0] = meshInfoList[0].VertexDataOffsets[1] + (meshInfoList[0].VertexCount * meshInfoList[0].VertexSizes[1]);
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

                modelDataBlock.AddRange(br.ReadBytes(atrStringCount * 4));

                List<MeshPart> meshPart = new List<MeshPart>();

                int partCount = 1;
                int meshPadd = 0;

                if (meshCount > 1)
                {
                    partCount = meshInfoList[0].MeshPartCount + meshInfoList[1].MeshPartCount;
                }

                for (int i = 0; i < meshPartCount; i++)
                {
                    MeshPart mp = new MeshPart();

                    //Index Offset (int)
                    if (i < partCount && i != 0)
                    {
                        br.ReadBytes(4);
                        if(i == meshInfoList[0].MeshPartCount)
                        {
                            mp.IndexOffset = meshPart[i - 1].IndexOffset + meshPart[i - 1].IndexCount + meshPadd;
                        }
                        else
                        {
                            mp.IndexOffset = meshPart[i - 1].IndexOffset + meshPart[i - 1].IndexCount;
                        }
                        modelDataBlock.AddRange(BitConverter.GetBytes(mp.IndexOffset));
                    }
                    else
                    {
                        mp.IndexOffset = br.ReadInt32();
                        modelDataBlock.AddRange(BitConverter.GetBytes(mp.IndexOffset));
                    }

                    //Index Count (int)
                    if (i == meshInfoList[0].MeshPartCount - 1)
                    {
                        var indexCount = br.ReadInt32();
                        mp.IndexCount = indexCount + indexDiff0;
                        if(mp.IndexCount < 0)
                        {
                            mp.IndexCount = 0;
                        }
                        modelDataBlock.AddRange(BitConverter.GetBytes(mp.IndexCount));
                    }
                    else if(i == partCount - 1)
                    {
                        var indexCount = br.ReadInt32();
                        mp.IndexCount = indexCount + indexDiff1;
                        if (mp.IndexCount < 0)
                        {
                            mp.IndexCount = 0;
                        }
                        modelDataBlock.AddRange(BitConverter.GetBytes(importedIndex1.Count));
                    }
                    else
                    {
                        mp.IndexCount = br.ReadInt32();
                        modelDataBlock.AddRange(BitConverter.GetBytes(mp.IndexCount));
                    }

                    if (i == (meshInfoList[0].MeshPartCount - 1))
                    {
                        meshPadd = mp.IndexCount % 16;
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

                modelDataBlock.AddRange(br.ReadBytes(matStringCount * 4));

                modelDataBlock.AddRange(br.ReadBytes(boneStringCount * 4));

                for (int i = 0; i < boneListCount; i++)
                {
                    //bone list
                    modelDataBlock.AddRange(br.ReadBytes(128));

                    //bone count (int)
                    modelDataBlock.AddRange(br.ReadBytes(4));
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

                List<byte> VertexDataBlock1 = new List<byte>();
                List<byte> IndexDataBlock1 = new List<byte>();
                List<byte> VertexDataBlock2 = new List<byte>();
                List<byte> IndexDataBlock2 = new List<byte>();
                List<byte> VertexDataBlock3 = new List<byte>();
                List<byte> IndexDataBlock3 = new List<byte>();

                int compVertexDataBlock1Size = 0;
                int compIndexDataBlock1Size = 0;
                int compVertexDataBlock2Size = 0;
                int compIndexDataBlock2Size = 0;
                int compVertexDataBlock3Size = 0;
                int compIndexDataBlock3Size = 0;

                List<int> compMeshSizes = new List<int>();

                int VDB1Parts = 0;
                int VDB2Parts = 0;
                int VDB3Parts = 0;

                if (lodList[0].MeshCount > 1)
                {
                    //LoD[0]
                    VertexDataBlock1.AddRange(importedVert00);
                    VertexDataBlock1.AddRange(importedVert01);

                    VertexDataBlock1.AddRange(importedVert10);
                    VertexDataBlock1.AddRange(importedVert11);

                    IndexDataBlock1.AddRange(importedIndex);

                    var padding1 = importedIndex.Count % 16;
                    if (padding1 != 0)
                    {
                        IndexDataBlock1.AddRange(new byte[16 - padding1]);
                    }

                    IndexDataBlock1.AddRange(importedIndex1);

                    padding1 = importedIndex1.Count % 16;
                    if (padding1 != 0)
                    {
                        IndexDataBlock1.AddRange(new byte[16 - padding1]);
                    }


                    VDB1Parts = (int)Math.Ceiling(VertexDataBlock1.Count / 16000f);
                    int[] VDB1PartCounts = new int[VDB1Parts];
                    int VDB1Remaining = VertexDataBlock1.Count;

                    for (int i = 0; i < VDB1Parts; i++)
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


                    for (int i = 0; i < VDB1Parts; i++)
                    {
                        var compVertexData1 = Compressor(VertexDataBlock1.GetRange(i * 16000, VDB1PartCounts[i]).ToArray());

                        compressedData.AddRange(BitConverter.GetBytes(16));
                        compressedData.AddRange(BitConverter.GetBytes(0));
                        compressedData.AddRange(BitConverter.GetBytes(compVertexData1.Length));
                        compressedData.AddRange(BitConverter.GetBytes(VDB1PartCounts[i]));
                        compressedData.AddRange(compVertexData1);

                        var vertexPadding = 128 - ((compVertexData1.Length + 16) % 128);

                        compressedData.AddRange(new byte[vertexPadding]);

                        compVertexDataBlock1Size += compVertexData1.Length + 16 + vertexPadding;
                        compMeshSizes.Add(compVertexData1.Length + 16 + vertexPadding);
                    }

                    var compIndexData1 = Compressor(IndexDataBlock1.ToArray());

                    compressedData.AddRange(BitConverter.GetBytes(16));
                    compressedData.AddRange(BitConverter.GetBytes(0));
                    compressedData.AddRange(BitConverter.GetBytes(compIndexData1.Length));
                    compressedData.AddRange(BitConverter.GetBytes(IndexDataBlock1.Count));
                    compressedData.AddRange(compIndexData1);

                    var indexPadding = 128 - ((compIndexData1.Length + 16) % 128);

                    compressedData.AddRange(new byte[indexPadding]);

                    compIndexDataBlock1Size += compIndexData1.Length + 16 + indexPadding;


                    //LoD[1]
                    br.BaseStream.Seek(lodList[1].VertexOffset + meshInfoList[2].VertexDataOffsets[0], SeekOrigin.Begin);

                    VertexDataBlock2.AddRange(br.ReadBytes(meshInfoList[2].VertexCount * meshInfoList[2].VertexSizes[0]));
                    VertexDataBlock2.AddRange(br.ReadBytes(meshInfoList[2].VertexCount * meshInfoList[2].VertexSizes[1]));
                    VertexDataBlock2.AddRange(br.ReadBytes(meshInfoList[3].VertexCount * meshInfoList[3].VertexSizes[0]));
                    VertexDataBlock2.AddRange(br.ReadBytes(meshInfoList[3].VertexCount * meshInfoList[3].VertexSizes[1]));

                    br.BaseStream.Seek(lodList[1].IndexOffset + (meshInfoList[2].IndexDataOffset), SeekOrigin.Begin);

                    IndexDataBlock2.AddRange(br.ReadBytes(meshInfoList[2].IndexCount * 2));

                    indexPadding = (meshInfoList[2].IndexCount * 2) % 16;
                    if (indexPadding != 0)
                    {
                        IndexDataBlock2.AddRange(new byte[16 - indexPadding]);
                        br.ReadBytes(16 - indexPadding);
                    }

                    IndexDataBlock2.AddRange(br.ReadBytes(meshInfoList[3].IndexCount * 2));

                    indexPadding = (meshInfoList[3].IndexCount * 2) % 16;
                    if (indexPadding != 0)
                    {
                        IndexDataBlock2.AddRange(new byte[16 - indexPadding]);
                    }

                    VDB2Parts = (int)Math.Ceiling(VertexDataBlock2.Count / 16000f);
                    int[] VDB2PartCounts = new int[VDB2Parts];
                    int VDB2Remaining = VertexDataBlock2.Count;

                    for (int i = 0; i < VDB2Parts; i++)
                    {

                        if (VDB2Remaining >= 16000)
                        {
                            VDB2PartCounts[i] = 16000;
                            VDB2Remaining -= 16000;
                        }
                        else
                        {
                            VDB2PartCounts[i] = VDB2Remaining;
                        }
                    }

                    for (int i = 0; i < VDB2Parts; i++)
                    {
                        var compVertexData2 = Compressor(VertexDataBlock2.GetRange(i * 16000, VDB2PartCounts[i]).ToArray());

                        compressedData.AddRange(BitConverter.GetBytes(16));
                        compressedData.AddRange(BitConverter.GetBytes(0));
                        compressedData.AddRange(BitConverter.GetBytes(compVertexData2.Length));
                        compressedData.AddRange(BitConverter.GetBytes(VDB2PartCounts[i]));
                        compressedData.AddRange(compVertexData2);

                        var vertexPadding = 128 - ((compVertexData2.Length + 16) % 128);

                        compressedData.AddRange(new byte[vertexPadding]);

                        compVertexDataBlock2Size += compVertexData2.Length + 16 + vertexPadding;
                        compMeshSizes.Add(compVertexData2.Length + 16 + vertexPadding);
                    }


                    var compIndexData2 = Compressor(IndexDataBlock2.ToArray());

                    compressedData.AddRange(BitConverter.GetBytes(16));
                    compressedData.AddRange(BitConverter.GetBytes(0));
                    compressedData.AddRange(BitConverter.GetBytes(compIndexData2.Length));
                    compressedData.AddRange(BitConverter.GetBytes(IndexDataBlock2.Count));
                    compressedData.AddRange(compIndexData2);

                    indexPadding = 128 - ((compIndexData2.Length + 16) % 128);

                    compressedData.AddRange(new byte[indexPadding]);

                    compIndexDataBlock2Size += compIndexData2.Length + 16 + indexPadding;


                    //LoD[2]
                    br.BaseStream.Seek(lodList[2].VertexOffset + meshInfoList[4].VertexDataOffsets[0], SeekOrigin.Begin);

                    VertexDataBlock3.AddRange(br.ReadBytes(meshInfoList[4].VertexCount * meshInfoList[4].VertexSizes[0]));
                    VertexDataBlock3.AddRange(br.ReadBytes(meshInfoList[4].VertexCount * meshInfoList[4].VertexSizes[1]));
                    VertexDataBlock3.AddRange(br.ReadBytes(meshInfoList[5].VertexCount * meshInfoList[5].VertexSizes[0]));
                    VertexDataBlock3.AddRange(br.ReadBytes(meshInfoList[5].VertexCount * meshInfoList[5].VertexSizes[1]));

                    br.BaseStream.Seek(lodList[2].IndexOffset + (meshInfoList[4].IndexDataOffset), SeekOrigin.Begin);

                    IndexDataBlock3.AddRange(br.ReadBytes(meshInfoList[4].IndexCount * 2));

                    padding = (meshInfoList[4].IndexCount * 2) % 16;
                    if (padding != 0)
                    {
                        IndexDataBlock3.AddRange(new byte[16 - padding]);
                        br.ReadBytes(16 - padding);
                    }

                    IndexDataBlock3.AddRange(br.ReadBytes(meshInfoList[5].IndexCount * 2));

                    padding = (meshInfoList[5].IndexCount * 2) % 16;
                    if (padding != 0)
                    {
                        IndexDataBlock3.AddRange(new byte[16 - padding]);
                    }

                    VDB3Parts = (int)Math.Ceiling(VertexDataBlock3.Count / 16000f);
                    int[] VDB3PartCounts = new int[VDB3Parts];
                    int VDB3Remaining = VertexDataBlock3.Count;

                    for (int i = 0; i < VDB3Parts; i++)
                    {

                        if (VDB3Remaining >= 16000)
                        {
                            VDB3PartCounts[i] = 16000;
                            VDB3Remaining -= 16000;
                        }
                        else
                        {
                            VDB3PartCounts[i] = VDB3Remaining;
                        }
                    }


                    for (int i = 0; i < VDB3Parts; i++)
                    {
                        var compVertexData3 = Compressor(VertexDataBlock3.GetRange(i * 16000, VDB3PartCounts[i]).ToArray());

                        compressedData.AddRange(BitConverter.GetBytes(16));
                        compressedData.AddRange(BitConverter.GetBytes(0));
                        compressedData.AddRange(BitConverter.GetBytes(compVertexData3.Length));
                        compressedData.AddRange(BitConverter.GetBytes(VDB3PartCounts[i]));
                        compressedData.AddRange(compVertexData3);

                        var vertexPadding = 128 - ((compVertexData3.Length + 16) % 128);

                        compressedData.AddRange(new byte[vertexPadding]);

                        compVertexDataBlock3Size += compVertexData3.Length + 16 + vertexPadding;
                        compMeshSizes.Add(compVertexData3.Length + 16 + vertexPadding);
                    }

                    var compIndexData3 = Compressor(IndexDataBlock3.ToArray());

                    compressedData.AddRange(BitConverter.GetBytes(16));
                    compressedData.AddRange(BitConverter.GetBytes(0));
                    compressedData.AddRange(BitConverter.GetBytes(compIndexData3.Length));
                    compressedData.AddRange(BitConverter.GetBytes(IndexDataBlock3.Count));
                    compressedData.AddRange(compIndexData3);

                    indexPadding = 128 - ((compIndexData3.Length + 16) % 128);

                    compressedData.AddRange(new byte[indexPadding]);

                    compIndexDataBlock3Size += compIndexData3.Length + 16 + indexPadding;
                }
                else
                {
                    //LoD[0]
                    mdlImport.AddRange(importedVert00);
                    mdlImport.AddRange(importedVert01);

                    mdlImport.AddRange(importedIndex);

                    var padding1 = importedIndex.Count % 16;
                    if (padding1 != 0)
                    {
                        mdlImport.AddRange(new byte[16 - padding1]);
                    }

                    //LoD[1]
                    br.BaseStream.Seek(lodList[1].VertexOffset + meshInfoList[2].VertexDataOffsets[0], SeekOrigin.Begin);

                    mdlImport.AddRange(br.ReadBytes(meshInfoList[2].VertexCount * meshInfoList[2].VertexSizes[0]));
                    mdlImport.AddRange(br.ReadBytes(meshInfoList[2].VertexCount * meshInfoList[2].VertexSizes[1]));
                    mdlImport.AddRange(br.ReadBytes(meshInfoList[3].VertexCount * meshInfoList[3].VertexSizes[0]));
                    mdlImport.AddRange(br.ReadBytes(meshInfoList[3].VertexCount * meshInfoList[3].VertexSizes[1]));

                    br.BaseStream.Seek(lodList[1].IndexOffset + (meshInfoList[2].IndexDataOffset), SeekOrigin.Begin);

                    mdlImport.AddRange(br.ReadBytes(meshInfoList[2].IndexCount * 2));

                    padding = (meshInfoList[2].IndexCount * 2) % 16;
                    if (padding != 0)
                    {
                        mdlImport.AddRange(new byte[16 - padding]);
                    }

                    mdlImport.AddRange(br.ReadBytes(meshInfoList[3].IndexCount * 2));

                    padding = (meshInfoList[3].IndexCount * 2) % 16;
                    if (padding != 0)
                    {
                        mdlImport.AddRange(new byte[16 - padding]);
                    }


                    //LoD[2]
                    br.BaseStream.Seek(lodList[2].VertexOffset + meshInfoList[4].VertexDataOffsets[0], SeekOrigin.Begin);

                    mdlImport.AddRange(br.ReadBytes(meshInfoList[4].VertexCount * meshInfoList[4].VertexSizes[0]));
                    mdlImport.AddRange(br.ReadBytes(meshInfoList[4].VertexCount * meshInfoList[4].VertexSizes[1]));
                    mdlImport.AddRange(br.ReadBytes(meshInfoList[5].VertexCount * meshInfoList[5].VertexSizes[0]));
                    mdlImport.AddRange(br.ReadBytes(meshInfoList[5].VertexCount * meshInfoList[5].VertexSizes[1]));

                    br.BaseStream.Seek(lodList[2].IndexOffset + (meshInfoList[4].IndexDataOffset), SeekOrigin.Begin);

                    mdlImport.AddRange(br.ReadBytes(meshInfoList[4].IndexCount * 2));

                    padding = (meshInfoList[4].IndexCount * 2) % 16;
                    if (padding != 0)
                    {
                        mdlImport.AddRange(new byte[16 - padding]);
                    }

                    mdlImport.AddRange(br.ReadBytes(meshInfoList[5].IndexCount * 2));

                    padding = (meshInfoList[5].IndexCount * 2) % 16;
                    if (padding != 0)
                    {
                        mdlImport.AddRange(new byte[16 - padding]);
                    }
                }

                /*
                 * -----------------------------------
                 * Create Header Start
                 * -----------------------------------
                 */

                //Header Length
                datHeader.AddRange(BitConverter.GetBytes(256));
                //Data Type
                datHeader.AddRange(BitConverter.GetBytes(3));
                //Uncompressed Size
                var uncompSize = vertexInfoBlock.Count + modelDataBlock.Count +
                    VertexDataBlock1.Count + IndexDataBlock1.Count +
                    VertexDataBlock2.Count + IndexDataBlock2.Count +
                    VertexDataBlock3.Count + IndexDataBlock3.Count + 68;
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
                datPadding = 128 - (VertexDataBlock1.Count % 128);
                datPadding = (datPadding == 128 ? 0 : datPadding);
                datHeader.AddRange(BitConverter.GetBytes(VertexDataBlock1.Count + datPadding));
                //Vertex Data Block LoD[2] Uncompressed
                datPadding = 128 - (VertexDataBlock2.Count % 128);
                datPadding = (datPadding == 128 ? 0 : datPadding);
                datHeader.AddRange(BitConverter.GetBytes(VertexDataBlock2.Count + datPadding));
                //Vertex Data Block LoD[3] Uncompressed
                datPadding = 128 - (VertexDataBlock3.Count % 128);
                datPadding = (datPadding == 128 ? 0 : datPadding);
                datHeader.AddRange(BitConverter.GetBytes(VertexDataBlock3.Count + datPadding));
                //Blank 1
                datHeader.AddRange(BitConverter.GetBytes(0));
                //Blank 2
                datHeader.AddRange(BitConverter.GetBytes(0));
                //Blank 3
                datHeader.AddRange(BitConverter.GetBytes(0));
                //Index Data Block LoD[1] Uncompressed
                datPadding = 128 - (IndexDataBlock1.Count % 128);
                datPadding = (datPadding == 128 ? 0 : datPadding);
                datHeader.AddRange(BitConverter.GetBytes(IndexDataBlock1.Count + datPadding));
                //Index Data Block LoD[2] Uncompressed
                datPadding = 128 - (IndexDataBlock2.Count % 128);
                datPadding = (datPadding == 128 ? 0 : datPadding);
                datHeader.AddRange(BitConverter.GetBytes(IndexDataBlock2.Count + datPadding));
                //Index Data Block LoD[3] Uncompressed
                datPadding = 128 - (IndexDataBlock3.Count % 128);
                datPadding = (datPadding == 128 ? 0 : datPadding);
                datHeader.AddRange(BitConverter.GetBytes(IndexDataBlock3.Count + datPadding));

                //Vertex Info Block Compressed
                datHeader.AddRange(BitConverter.GetBytes(compVertexInfoSize));
                //Model Data Block Compressed
                datHeader.AddRange(BitConverter.GetBytes(compModelDataSize));
                //Vertex Data Block LoD[1] Compressed
                datHeader.AddRange(BitConverter.GetBytes(compVertexDataBlock1Size));
                //Vertex Data Block LoD[2] Compressed
                datHeader.AddRange(BitConverter.GetBytes(compVertexDataBlock2Size));
                //Vertex Data Block LoD[3] Compressed
                datHeader.AddRange(BitConverter.GetBytes(compVertexDataBlock3Size));
                //Blank 1
                datHeader.AddRange(BitConverter.GetBytes(0));
                //Blank 2
                datHeader.AddRange(BitConverter.GetBytes(0));
                //Blank 3
                datHeader.AddRange(BitConverter.GetBytes(0));
                //Index Data Block LoD[1] Compressed
                datHeader.AddRange(BitConverter.GetBytes(compIndexDataBlock1Size));
                //Index Data Block LoD[2] Compressed
                datHeader.AddRange(BitConverter.GetBytes(compIndexDataBlock2Size));
                //Index Data Block LoD[3] Compressed
                datHeader.AddRange(BitConverter.GetBytes(compIndexDataBlock3Size));

                var vertexInfoOffset = 0;
                var modelDataOffset = compVertexInfoSize;
                var vertexDataBlock1Offset = modelDataOffset + compModelDataSize;
                var indexDataBlock1Offset = vertexDataBlock1Offset + compVertexDataBlock1Size;
                var vertexDataBlock2Offset = indexDataBlock1Offset + compIndexDataBlock1Size;
                var indexDataBlock2Offset = vertexDataBlock2Offset + compVertexDataBlock2Size;
                var vertexDataBlock3Offset = indexDataBlock2Offset + compIndexDataBlock2Size;
                var indexDataBlock3Offset = vertexDataBlock3Offset + compVertexDataBlock3Size;

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


                var ind1 = 2 + VDB1Parts;
                var vert2 = ind1 + 1;
                var ind2 = vert2 + VDB2Parts;
                var vert3 = ind2 + 1;
                var ind3 = vert3 + VDB3Parts;

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
                datHeader.AddRange(BitConverter.GetBytes((short)VDB1Parts));
                //Vertex Data Block LoD[2] part count
                datHeader.AddRange(BitConverter.GetBytes((short)VDB2Parts));
                //Vertex Data Block LoD[3] part count
                datHeader.AddRange(BitConverter.GetBytes((short)VDB3Parts));
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
                for(int i = 0; i < VDB1Parts; i++)
                {
                    datHeader.AddRange(BitConverter.GetBytes((short)compMeshSizes[i]));
                }
                VDBPartCount += VDB1Parts;
                //Index Data Block LoD[1] padded size
                datHeader.AddRange(BitConverter.GetBytes((short)compIndexDataBlock1Size));
            
                //Vertex Data Block LoD[2] part padded sizes
                for (int i = 0; i < VDB2Parts; i++)
                {
                    datHeader.AddRange(BitConverter.GetBytes((short)compMeshSizes[VDBPartCount + i]));
                }
                VDBPartCount += VDB2Parts;
                //Index Data Block LoD[2] padded size
                datHeader.AddRange(BitConverter.GetBytes((short)compIndexDataBlock2Size));

                //Vertex Data Block LoD[3] part padded sizes
                for (int i = 0; i < VDB3Parts; i++)
                {
                    datHeader.AddRange(BitConverter.GetBytes((short)compMeshSizes[VDBPartCount + i]));
                }

                //Index Data Block LoD[3] padded size
                datHeader.AddRange(BitConverter.GetBytes((short)compIndexDataBlock3Size));

                //Rest of header
                var headerEnd = 256 - (datHeader.Count % 256);
                datHeader.AddRange(new byte[headerEnd]);
            }
            compressedData.InsertRange(0, datHeader);

            WriteToDat(compressedData, modEntry, inModList, internalPath, category, itemName, lineNum);
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
