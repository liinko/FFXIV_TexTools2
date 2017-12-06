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
using FFXIV_TexTools2.Model;
using FFXIV_TexTools2.Resources;
using HelixToolkit.Wpf.SharpDX.Core;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace FFXIV_TexTools2.Material
{
    /// <summary>
    /// Handles files with MDL extension
    /// </summary>
    public class MDL
    {
        string MDLFile = "";
        string fullPath = "";
        List<ModelMeshData> meshList = new List<ModelMeshData>();
        List<string> objBytes = new List<string>();
        List<string> materialStrings = new List<string>();
        List<string> boneStrings = new List<string>();
        List<float> boneTransforms = new List<float>();


        /// <summary>
        /// Parses the MDL file to obtain model information
        /// </summary>
        /// <param name="selectedItem">The currently selected item</param>
        /// <param name="selectedRace">The currently selected race</param>
        /// <param name="selectedBody">The currently selected body</param>
        /// <param name="selectedPart">The currently selected part</param>
        /// <param name="selectedCategory">The items category </param>
        public MDL(ItemData selectedItem, string selectedCategory, string selectedRace, string selectedBody, string selectedPart)
        {
            string itemType = Helper.GetCategoryType(selectedCategory);
            string MDLFolder = "";

            if(itemType.Equals("weapon") || itemType.Equals("food"))
            {
                if (selectedPart.Equals("Secondary"))
                {
                    MDLFolder = string.Format(Strings.WeapMDLFolder, selectedItem.SecondaryModelID, selectedItem.SecondaryModelBody);
                    MDLFile = string.Format(Strings.WeapMDLFile, selectedItem.SecondaryModelID, selectedItem.SecondaryModelBody);
                }
                else
                {
                    MDLFolder = string.Format(Strings.WeapMDLFolder, selectedItem.PrimaryModelID, selectedItem.PrimaryModelBody);
                    MDLFile = string.Format(Strings.WeapMDLFile, selectedItem.PrimaryModelID, selectedItem.PrimaryModelBody);
                }
            }
            else if (itemType.Equals("accessory"))
            {
                MDLFolder = string.Format(Strings.AccMDLFolder, selectedItem.PrimaryModelID);
                MDLFile = string.Format(Strings.AccMDLFile, selectedRace, selectedItem.PrimaryModelID, Info.slotAbr[selectedCategory]);
            }
            else if (itemType.Equals("character"))
            {
                if (selectedItem.ItemName.Equals(Strings.Body))
                {
                    MDLFolder = string.Format(Strings.BodyMDLFolder, selectedRace, selectedBody.PadLeft(4, '0'));
                    MDLFile = string.Format(Strings.BodyMDLFile, selectedRace, selectedBody.PadLeft(4, '0'), selectedPart);
                }
                else if (selectedItem.ItemName.Equals(Strings.Face))
                {
                    MDLFolder = string.Format(Strings.FaceMDLFolder, selectedRace, selectedBody.PadLeft(4, '0'));
                    MDLFile = string.Format(Strings.FaceMDLFile, selectedRace, selectedBody.PadLeft(4, '0'), selectedPart);
                }
                else if (selectedItem.ItemName.Equals(Strings.Hair))
                {
                    MDLFolder = string.Format(Strings.HairMDLFolder, selectedRace, selectedBody.PadLeft(4, '0'));
                    MDLFile = string.Format(Strings.HairMDLFile, selectedRace, selectedBody.PadLeft(4, '0'), selectedPart);
                }
                else if (selectedItem.ItemName.Equals(Strings.Tail))
                {
                    MDLFolder = string.Format(Strings.TailMDLFolder, selectedRace, selectedBody.PadLeft(4, '0'));
                    MDLFile = string.Format(Strings.TailMDLFile, selectedRace, selectedBody.PadLeft(4, '0'), selectedPart);
                }
            }
            else if (itemType.Equals("monster"))
            {
                bool isDemiHuman = false;
                if (selectedItem.PrimaryMTRLFolder != null)
                {
                    isDemiHuman = selectedItem.PrimaryMTRLFolder.Contains("demihuman");
                }

                string ID = "";
                string body = "";

                if (selectedCategory.Equals(Strings.Pets))
                {
                    int part = 1;

                    if (selectedItem.ItemName.Equals(Strings.Selene) || selectedItem.ItemName.Equals(Strings.Bishop_Autoturret))
                    {
                        part = 2;
                    }

                    ID = Info.petID[selectedItem.ItemName];
                    body = part.ToString().PadLeft(4, '0');
                }
                else
                {
                    ID = selectedItem.PrimaryModelID.PadLeft(4, '0');
                    body = selectedItem.PrimaryModelBody;
                }

                if (isDemiHuman)
                {
                    MDLFolder = string.Format(Strings.DemiMDLFolder, ID, body);
                    MDLFile = string.Format(Strings.DemiMDLFile, ID, body, selectedPart);
                }
                else
                {
                    MDLFolder = string.Format(Strings.MonsterMDLFolder, ID, body);
                    MDLFile = string.Format(Strings.MonsterMDLFile, ID, body);
                }
            }
            else
            {
                MDLFolder = string.Format(Strings.EquipMDLFolder, selectedItem.PrimaryModelID);

                if (selectedPart.Equals("-"))
                {
                    MDLFile = string.Format(Strings.EquipMDLFile, selectedRace, selectedItem.PrimaryModelID, Info.slotAbr[selectedCategory]);
                }
                else
                {
                    MDLFile = string.Format(Strings.EquipMDLFile, selectedRace, selectedItem.PrimaryModelID, selectedPart);
                }
            }

            fullPath = MDLFolder + "/" + MDLFile;
            int offset = Helper.GetDataOffset(FFCRC.GetHash(MDLFolder), FFCRC.GetHash(MDLFile), Strings.ItemsDat);

            if(offset == 0)
            {
                if (itemType.Equals("weapon"))
                {
                    if (selectedPart.Equals("Secondary"))
                    {
                        MDLFolder = string.Format(Strings.EquipMDLFolder, selectedItem.SecondaryModelID);
                        MDLFile = string.Format(Strings.EquipMDLFile, "0101", selectedItem.SecondaryModelID, Info.slotAbr[Strings.Hands]);

                        offset = Helper.GetDataOffset(FFCRC.GetHash(MDLFolder), FFCRC.GetHash(MDLFile), Strings.ItemsDat);
                    }
                }
            }

            int datNum = ((offset / 8) & 0x000f) / 2;
     
            var MDLDatData = Helper.GetType3DecompressedData(offset, datNum, Strings.ItemsDat);

            using (BinaryReader br = new BinaryReader(new MemoryStream(MDLDatData.Item1)))
            {
                ModelData modelData = new ModelData();

                // The size of the header + (size of the mesh information block (136 bytes) * the number of meshes) + padding
                br.BaseStream.Seek(64 + 136 * MDLDatData.Item2 + 4, SeekOrigin.Begin);

                var modelStringCount = br.ReadInt32();
                var stringBlockSize = br.ReadInt32();
                var stringBlock = br.ReadBytes(stringBlockSize);

                var unknown = br.ReadBytes(4);

                var totalMeshCount = br.ReadInt16();
                var attributeStringCount = br.ReadInt16();
                var meshPartsCount = br.ReadInt16();
                var materialStringCount = br.ReadInt16();
                var boneStringCount = br.ReadInt16();
                var boneListCount = br.ReadInt16();

                var unknown1 = br.ReadInt16();
                var unknown2 = br.ReadInt16();
                var unknown3 = br.ReadInt16();
                var unknown4 = br.ReadInt16();
                var unknown5 = br.ReadInt16();
                var unknown6 = br.ReadInt16();

                br.ReadBytes(10);

                var unknown7 = br.ReadInt16();

                br.ReadBytes(16);

                using (BinaryReader br1 = new BinaryReader(new MemoryStream(stringBlock)))
                {
                    br1.BaseStream.Seek(0, SeekOrigin.Begin);

                    for(int i= 0; i < attributeStringCount; i++)
                    {
                        while(br1.ReadByte() != 0)
                        {
                            //extract each atribute string here
                        }
                    }

                    for(int i = 0; i < boneStringCount; i++)
                    {
                        byte b;
                        List<byte> boneName = new List<byte>();
                        while ((b = br1.ReadByte()) != 0)
                        {
                            boneName.Add(b);
                        }
                        string bone = Encoding.ASCII.GetString(boneName.ToArray());
                        bone = bone.Replace("\0", "");

                        boneStrings.Add(bone);
                    }

                    for (int i = 0; i < materialStringCount; i++)
                    {
                        byte b;
                        List<byte> name = new List<byte>();
                        while ((b = br1.ReadByte()) != 0)
                        {
                            name.Add(b);
                        }
                        string material = Encoding.ASCII.GetString(name.ToArray());
                        material = material.Replace("\0", "");

                        materialStrings.Add(material);
                    }
                }

                br.ReadBytes(32 * unknown5);

                for (int i = 0; i < 3; i++)
                {
                    LevelOfDetail LoD = new LevelOfDetail();
                    LoD.MeshOffset = br.ReadInt16();
                    LoD.MeshCount = br.ReadInt16();

                    br.ReadBytes(40);

                    LoD.VertexDataSize = br.ReadInt32();
                    LoD.IndexDataSize = br.ReadInt32();
                    LoD.VertexOffset = br.ReadInt32();
                    LoD.IndexOffset = br.ReadInt32();

                    modelData.LoD.Add(LoD);    
                }

                var savePos = br.BaseStream.Position;

                for (int i = 0; i < modelData.LoD.Count; i++)
                {
                    List<MeshDataInfo> meshInfoList = new List<MeshDataInfo>();

                    for (int j = 0; j < modelData.LoD[i].MeshCount; j++)
                    {
                        modelData.LoD[i].MeshList.Add(new Mesh());
                        meshInfoList.Clear();

                        br.BaseStream.Seek((i * 136) + 68, SeekOrigin.Begin);
                        var dataBlockNum = br.ReadByte();

                        while (dataBlockNum != 255)
                        {
                            MeshDataInfo meshInfo = new MeshDataInfo()
                            {
                                VertexDataBlock = dataBlockNum,
                                Offset = br.ReadByte(),
                                DataType = br.ReadByte(),
                                UseType = br.ReadByte()
                            };

                            meshInfoList.Add(meshInfo);

                            br.ReadBytes(4);

                            dataBlockNum = br.ReadByte();
                        }

                        modelData.LoD[i].MeshList[j].MeshDataInfoList = meshInfoList.ToArray();
                    }
                }

                br.BaseStream.Seek(savePos, SeekOrigin.Begin);

                for (int x = 0; x < modelData.LoD.Count; x++)
                {
                    for (int i = 0; i < modelData.LoD[x].MeshCount; i++)
                    {
                        MeshInfo meshInfo = new MeshInfo()
                        {
                            VertexCount = br.ReadInt32(),
                            IndexCount = br.ReadInt32(),
                            MaterialNum = br.ReadInt16(),
                            MeshPartOffset = br.ReadInt16(),
                            MeshPartCount = br.ReadInt16(),
                            BoneListIndex = br.ReadInt16(),
                            IndexDataOffset = br.ReadInt32()
                        };

                        for (int j = 0; j < 3; j++)
                        {
                            meshInfo.VertexDataOffsets.Add(br.ReadInt32());
                        }

                        for (int k = 0; k < 3; k++)
                        {
                            meshInfo.VertexSizes.Add(br.ReadByte());
                        }

                        meshInfo.VertexDataBlockCount = br.ReadByte();

                        modelData.LoD[x].MeshList[i].MeshInfo = meshInfo;
                    }
                }

                br.ReadBytes(attributeStringCount * 4);
                br.ReadBytes(unknown6 * 20);

                for(int i = 0; i < modelData.LoD.Count; i++)
                {
                    foreach (var mesh in modelData.LoD[i].MeshList)
                    {
                        for(int j = 0; j < mesh.MeshInfo.MeshPartCount; j++)
                        {
                            MeshPart meshPart = new MeshPart()
                            {
                                IndexOffset = br.ReadInt32(),
                                IndexCount = br.ReadInt32(),
                                Attributes = br.ReadInt32(),
                                BoneOffset = br.ReadInt16(),
                                BoneCount = br.ReadInt16()
                            };

                            mesh.MeshPartList.Add(meshPart);
                        }
                    }
                }

                br.ReadBytes(unknown7 * 12);
                br.ReadBytes(materialStringCount * 4);
                br.ReadBytes(boneStringCount * 4);

                for (int i = 0; i < boneListCount; i++)
                {
                    Bones bones = new Bones();

                    for (int j = 0; j < 64; j++)
                    {
                        bones.BoneData.Add(br.ReadInt16());
                    }

                    bones.BoneCount = br.ReadInt32();

                    modelData.BoneSet.Add(bones);
                }

                br.ReadBytes(unknown1 * 16);
                br.ReadBytes(unknown2 * 12);
                br.ReadBytes(unknown3 * 4);

                var boneIndexSize = br.ReadInt32();

                for (int i = 0; i < boneIndexSize / 2; i++)
                {
                    modelData.BoneIndicies.Add(br.ReadInt16());
                }

                int padding = br.ReadByte();
                br.ReadBytes(padding);

                for (int i = 0; i < 4; i++)
                {
                    ModelMaterial.BoundingBox boundingBox = new ModelMaterial.BoundingBox();
                    for (int j = 0; j < 4; j++)
                    {
                        boundingBox.PointA.Add(br.ReadSingle());
                    }
                    for (int k = 0; k < 4; k++)
                    {
                        boundingBox.PointB.Add(br.ReadSingle());
                    }

                    modelData.BoundingBoxes.Add(boundingBox);
                }

                //float4x4
                for(int i = 0; i < boneStringCount; i++)
                {
                    boneTransforms.Add(br.ReadSingle());
                    boneTransforms.Add(br.ReadSingle());
                    boneTransforms.Add(br.ReadSingle());
                    boneTransforms.Add(br.ReadSingle());

                    boneTransforms.Add(br.ReadSingle());
                    boneTransforms.Add(br.ReadSingle());
                    boneTransforms.Add(br.ReadSingle());
                    boneTransforms.Add(br.ReadSingle());
                }

                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < modelData.LoD[i].MeshCount; j++)
                    {
                        Mesh mesh = modelData.LoD[i].MeshList[j];

                        for (int k = 0; k < mesh.MeshInfo.VertexDataBlockCount; k++)
                        {
                            br.BaseStream.Seek(modelData.LoD[i].VertexOffset + mesh.MeshInfo.VertexDataOffsets[k], SeekOrigin.Begin);

                            mesh.MeshVertexData.Add(br.ReadBytes(mesh.MeshInfo.VertexSizes[k] * mesh.MeshInfo.VertexCount));
                        }

                        br.BaseStream.Seek(modelData.LoD[i].IndexOffset + (mesh.MeshInfo.IndexDataOffset * 2), SeekOrigin.Begin);

                        mesh.IndexData = br.ReadBytes(2 * mesh.MeshInfo.IndexCount);
                    }
                }

                int vertex = 0, coordinates = 0, normals = 0, tangents = 0, colors = 0, blendWeights = 0, blendIndices = 0;

                for (int i = 0; i < modelData.LoD[0].MeshCount; i++)
                {
                    objBytes.Clear();

                    var vertexList = new Vector3Collection();
                    var texCoordList = new Vector2Collection();
                    var normalList = new Vector3Collection();
                    var tangentList = new Vector3Collection();
                    var colorsList = new Color4Collection();
                    var indexList = new IntCollection();
                    var blendWeightList = new List<float>();
                    var blendWeightList2 = new List<float[]>();
                    var blendIndicesList = new List<int>();
                    var blendIndicesList2 = new List<int[]>();
                    var weightCounts = new List<int>();


                    Mesh mesh = modelData.LoD[0].MeshList[i];

                    MeshDataInfo[] meshDataInfoList = mesh.MeshDataInfoList;

                    int c = 0;
                    foreach (var meshDataInfo in meshDataInfoList)
                    {
                        if (meshDataInfo.UseType == 0)
                        {
                            vertex = c;
                        }
                        else if (meshDataInfo.UseType == 1)
                        {
                            blendWeights = c;
                        }
                        else if (meshDataInfo.UseType == 2)
                        {
                            blendIndices = c;
                        }
                        else if (meshDataInfo.UseType == 3)
                        {
                            normals = c;
                        }
                        else if (meshDataInfo.UseType == 4)
                        {
                            coordinates = c;
                        }
                        else if(meshDataInfo.UseType == 6)
                        {
                            tangents = c;
                        }
                        else if (meshDataInfo.UseType == 7)
                        {
                            colors = c;
                        }

                        c++;
                    }

                    /* 
                     * -----------------
                     * Vertex
                     * -----------------
                     */ 
                    using (BinaryReader br1 = new BinaryReader(new MemoryStream(mesh.MeshVertexData[meshDataInfoList[vertex].VertexDataBlock])))
                    {
                        for (int j = 0; j < mesh.MeshInfo.VertexCount; j++)
                        {
                            br1.BaseStream.Seek(j * mesh.MeshInfo.VertexSizes[meshDataInfoList[vertex].VertexDataBlock] + meshDataInfoList[vertex].Offset, SeekOrigin.Begin);

                            Vector3 vVector = new Vector3();
                            if (meshDataInfoList[vertex].DataType == 13 || meshDataInfoList[vertex].DataType == 14)
                            {
                                System.Half h1 = System.Half.ToHalf((ushort)br1.ReadInt16());
                                System.Half h2 = System.Half.ToHalf((ushort)br1.ReadInt16());
                                System.Half h3 = System.Half.ToHalf((ushort)br1.ReadInt16());

                                float x = HalfHelper.HalfToSingle(h1);
                                float y = HalfHelper.HalfToSingle(h2);
                                float z = HalfHelper.HalfToSingle(h3);

                                vVector = new Vector3(x, y, z);
                                objBytes.Add("v " + x.ToString("N5") + " " + y.ToString("N5") + " " + z.ToString("N5") + " ");
                            }
                            else if (meshDataInfoList[vertex].DataType == 2)
                            {
                                var x = BitConverter.ToSingle(br1.ReadBytes(4), 0);
                                var y = BitConverter.ToSingle(br1.ReadBytes(4), 0);
                                var z = BitConverter.ToSingle(br1.ReadBytes(4), 0);

                                vVector = new Vector3(x, y, z);
                                objBytes.Add("v " + x.ToString("N5") + " " + y.ToString("N5") + " " + z.ToString("N5") + " ");

                            }

                            vertexList.Add(vVector);
                        }
                    }


                    /* 
                     * -----------------
                     * Blend Weight
                     * -----------------
                     */
                    using (BinaryReader br1 = new BinaryReader(new MemoryStream(mesh.MeshVertexData[meshDataInfoList[blendWeights].VertexDataBlock])))
                    {
                        for (int j = 0; j < mesh.MeshInfo.VertexCount; j++)
                        {
                            br1.BaseStream.Seek(j * mesh.MeshInfo.VertexSizes[meshDataInfoList[blendWeights].VertexDataBlock] + meshDataInfoList[blendWeights].Offset, SeekOrigin.Begin);

                            float x = br1.ReadByte() / 255.0f;
                            float y = br1.ReadByte() / 255.0f;
                            float z = br1.ReadByte() / 255.0f;
                            float w = br1.ReadByte() / 255.0f;

                            int count = 0;
                            if(x != 0)
                            {
                                blendWeightList.Add(x);
                                count++;

                                if(y != 0)
                                {
                                    blendWeightList.Add(y);
                                    count++;

                                    if(z != 0)
                                    {
                                        blendWeightList.Add(z);
                                        count++;

                                        if(w != 0)
                                        {
                                            blendWeightList.Add(w);
                                            count++;
                                        }
                                    }
                                }
                            }

                            if(count == 1)
                            {
                                blendWeightList2.Add(new float[] { x });
                            }
                            else if(count == 2)
                            {
                                blendWeightList2.Add(new float[] { x, y });
                            }
                            else if (count == 3)
                            {
                                blendWeightList2.Add(new float[] { x, y, z });
                            }
                            else if (count == 4)
                            {
                                blendWeightList2.Add(new float[] { x, y, z, w });
                            }

                            weightCounts.Add(count);
                        }
                    }


                    /* 
                     * -----------------
                     * Blend Index
                     * -----------------
                     */
                    using (BinaryReader br1 = new BinaryReader(new MemoryStream(mesh.MeshVertexData[meshDataInfoList[blendIndices].VertexDataBlock])))
                    {
                        for (int j = 0; j < mesh.MeshInfo.VertexCount; j++)
                        {
                            br1.BaseStream.Seek(j * mesh.MeshInfo.VertexSizes[meshDataInfoList[blendIndices].VertexDataBlock] + meshDataInfoList[blendIndices].Offset, SeekOrigin.Begin);

                            int x = br1.ReadByte();
                            int y = br1.ReadByte();
                            int z = br1.ReadByte();
                            int w = br1.ReadByte();

                            if(weightCounts[j] == 1)
                            {
                                blendIndicesList.Add(x);
                                blendIndicesList2.Add(new int[] { x });
                            }
                            else if (weightCounts[j] == 2)
                            {
                                blendIndicesList.Add(x);
                                blendIndicesList.Add(y);
                                blendIndicesList2.Add(new int[] { x, y });

                            }
                            else if (weightCounts[j] == 3)
                            {
                                blendIndicesList.Add(x);
                                blendIndicesList.Add(y);
                                blendIndicesList.Add(z);
                                blendIndicesList2.Add(new int[] { x, y, z });

                            }
                            else if (weightCounts[j] == 4)
                            {
                                blendIndicesList.Add(x);
                                blendIndicesList.Add(y);
                                blendIndicesList.Add(z);
                                blendIndicesList.Add(w);
                                blendIndicesList2.Add(new int[] { x, y, z, w });

                            }
                        }
                    }


                    /* 
                     * -----------------
                     * Texture Coordinates
                     * -----------------
                     */
                    using (BinaryReader br1 = new BinaryReader(new MemoryStream(mesh.MeshVertexData[meshDataInfoList[coordinates].VertexDataBlock])))
                    {
                        for (int j = 0; j < mesh.MeshInfo.VertexCount; j++)
                        {
                            br1.BaseStream.Seek(j * mesh.MeshInfo.VertexSizes[meshDataInfoList[coordinates].VertexDataBlock] + meshDataInfoList[coordinates].Offset, SeekOrigin.Begin);

                            float x = 0;
                            float y = 0;
                            float z = 0;
                            float w = 0; 
                            if (meshDataInfoList[coordinates].DataType == 13 || meshDataInfoList[coordinates].DataType == 14)
                            {
                                var sx = (ushort)br1.ReadInt16();
                                var sy = (ushort)br1.ReadInt16();
                                var sz = (ushort)br1.ReadInt16();
                                var sw = (ushort)br1.ReadInt16();

                                var h1 = new SharpDX.Half(sx);
                                var h2 = new SharpDX.Half(sy);
                                var h3 = new SharpDX.Half(sz);
                                var h4 = new SharpDX.Half(sw);

                                x = h1;
                                y = h2;
                                z = h3;
                                w = h4;
                            }
                            else
                            {
                                x = br1.ReadSingle();
                                y = br1.ReadSingle();
                            }


                            var ox = x - Math.Truncate(x);
                            var oy = y - Math.Truncate(y);

                            objBytes.Add("vt " + ox.ToString("N5") + " " + (1 - y).ToString("N5") + " ");
                            texCoordList.Add(new Vector2(x, y));
                        }
                    }



                    /* 
                     * -----------------
                     * Normals
                     * -----------------
                     */
                    using (BinaryReader br1 = new BinaryReader(new MemoryStream(mesh.MeshVertexData[meshDataInfoList[normals].VertexDataBlock])))
                    {
                        for (int j = 0; j < mesh.MeshInfo.VertexCount; j++)
                        {
                            br1.BaseStream.Seek(j * mesh.MeshInfo.VertexSizes[meshDataInfoList[normals].VertexDataBlock] + meshDataInfoList[normals].Offset, SeekOrigin.Begin);

                            float x = 0;
                            float y = 0;
                            float z = 0;
                            float w = 0;

                            if (meshDataInfoList[normals].DataType == 13 || meshDataInfoList[normals].DataType == 14)
                            {
                                System.Half h1 = System.Half.ToHalf((ushort)br1.ReadInt16());
                                System.Half h2 = System.Half.ToHalf((ushort)br1.ReadInt16());
                                System.Half h3 = System.Half.ToHalf((ushort)br1.ReadInt16());
                                System.Half h4 = System.Half.ToHalf((ushort)br1.ReadInt16());

                                x = HalfHelper.HalfToSingle(h1);
                                y = HalfHelper.HalfToSingle(h2);
                                z = HalfHelper.HalfToSingle(h3);
                                w = HalfHelper.HalfToSingle(h4);

                            }
                            else
                            {
                                x = br1.ReadSingle();
                                y = br1.ReadSingle();
                                z = br1.ReadSingle();
                            }

                            var nv = new Vector3(x, y, z);

                            objBytes.Add("vn " + x.ToString("N5") + " " + y.ToString("N5") + " " + z.ToString("N5") + " ");
                            normalList.Add(nv);
                        }
                    }


                    /* 
                     * -----------------
                     * Tangents
                     * -----------------
                     */
                    using (BinaryReader br1 = new BinaryReader(new MemoryStream(mesh.MeshVertexData[meshDataInfoList[tangents].VertexDataBlock])))
                    {
                        for (int j = 0; j < mesh.MeshInfo.VertexCount; j++)
                        {
                            br1.BaseStream.Seek(j * mesh.MeshInfo.VertexSizes[meshDataInfoList[tangents].VertexDataBlock] + meshDataInfoList[tangents].Offset, SeekOrigin.Begin);

                            int x = br1.ReadByte();
                            int y = br1.ReadByte();
                            int z = br1.ReadByte();
                            int w = br1.ReadByte();

                            var x1  = x * 2 / 255f - 1f;
                            var y1  = y * 2 / 255f - 1f;
                            var z1  = z * 2 / 255f - 1f;
                            var w1  = w * 2 / 255f - 1f;


                            var nv = new Vector3(x1, y1, z1);

                            tangentList.Add(nv);
                        }
                    }


                    /* 
                     * -----------------
                     * Vertex Color
                     * -----------------
                     */
                    using (BinaryReader br1 = new BinaryReader(new MemoryStream(mesh.MeshVertexData[meshDataInfoList[colors].VertexDataBlock])))
                    {
                        for (int j = 0; j < mesh.MeshInfo.VertexCount; j++)
                        {
                            br1.BaseStream.Seek(j * mesh.MeshInfo.VertexSizes[meshDataInfoList[colors].VertexDataBlock] + meshDataInfoList[colors].Offset, SeekOrigin.Begin);

                            int a = br1.ReadByte();
                            int r = br1.ReadByte();
                            int g = br1.ReadByte();
                            int b = br1.ReadByte();

                            colorsList.Add(new Color4(r, g, b, a));
                        }
                    }


                    /* 
                     * -----------------
                     * Index
                     * -----------------
                     */
                    using (BinaryReader br1 = new BinaryReader(new MemoryStream(mesh.IndexData)))
                    {
                        for (int j = 0; j < mesh.MeshInfo.IndexCount; j += 3)
                        {
                            int i1 = br1.ReadInt16();
                            int i2 = br1.ReadInt16();
                            int i3 = br1.ReadInt16();

                            objBytes.Add("f " + (i1 + 1) + "/" + (i1 + 1) + "/" + (i1 + 1) + " " + (i2 + 1) + "/" + (i2 + 1) + "/" + (i2 + 1) + " " + (i3 + 1) + "/" + (i3 + 1) + "/" + (i3 + 1) + " ");

                            indexList.Add(i1);
                            indexList.Add(i2);
                            indexList.Add(i3);
                        }
                    }

                    ModelMeshData modelMeshData = new ModelMeshData()
                    {
                        Vertices = vertexList,
                        Normals = normalList,
                        TextureCoordinates = texCoordList,
                        BiTangents = tangentList,
                        Indices = indexList,
                        VertexColors = colorsList,
                        OBJFileData = objBytes.ToArray(),
                        BoneStrings = boneStrings,
                        BoneIndices = modelData.BoneIndicies,
                        BoneTransforms = boneTransforms,
                        BlendWeights = blendWeightList,
                        BlendIndices = blendIndicesList,
                        WeightCounts = weightCounts,
                        MeshPartList = mesh.MeshPartList,
                        BlendIndicesArrayList = blendIndicesList2,
                        BlendWeightsArrayList = blendWeightList2,
                        MaterialNum = mesh.MeshInfo.MaterialNum
                    };

                    meshList.Add(modelMeshData);
                }
            }
        }

        /// <summary>
        /// Gets the mesh list
        /// </summary>
        /// <returns></returns>
        public List<ModelMeshData> GetMeshList()
        {
            return meshList;
        }

        /// <summary>
        /// Gets the internal path file name
        /// </summary>
        /// <returns></returns>
        public string GetModelName()
        {
            return Path.GetFileNameWithoutExtension(MDLFile);
        }

        /// <summary>
        /// Gets the material strings
        /// </summary>
        /// <returns></returns>
        public List<string> GetMaterialStrings()
        {
            return materialStrings;
        }

        /// <summary>
        /// Gets the bone strings
        /// </summary>
        /// <returns></returns>
        public List<string> GetBoneStrings()
        {
            return boneStrings;
        }

        /// <summary>
        /// Gets the full internal file path
        /// </summary>
        /// <returns></returns>
        public string GetInternalPath()
        {
            return fullPath;
        }
    }
}
