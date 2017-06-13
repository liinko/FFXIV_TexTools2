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
using FFXIV_TexTools2.Model;
using FFXIV_TexTools2.Resources;
using HelixToolkit.Wpf.SharpDX.Core;
using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Windows.Media.Media3D;

namespace FFXIV_TexTools2.Material
{
    public class MDL
    {
        byte[] decompBytes;
        int meshCount, materialCount;
        ModelData modelData;
        Point3DCollection pVertexs;
        Vector3Collection vertexList, normalList, tangentList;
        Vector2Collection coordList;
        IntCollection indList;
        List<Mesh> meshList = new List<Mesh>();
        List<string> objBytes = new List<string>();
        string MDLFolder, MDLFile;


        public MDL(Items selectedItem, string selectedRace, string selectedParent)
        {
            string iType = Helper.GetItemType(selectedParent);


            if(iType.Equals("weapon") || iType.Equals("food"))
            {
                MDLFolder = "chara/weapon/w" + selectedItem.itemID + "/obj/body/b" + selectedItem.weaponBody + "/model";
                MDLFile = "w" + selectedItem.itemID + "b" + selectedItem.weaponBody + ".mdl";
            }
            else if (iType.Equals("accessory"))
            {
                MDLFolder = "chara/accessory/a" + selectedItem.itemID + "/model";
                MDLFile = "c" + selectedRace + "a" + selectedItem.itemID + "_" + Info.slotAbr[selectedParent] + ".mdl";
            }
            else if (iType.Equals("character"))
            {
                if (selectedItem.itemName.Equals(Strings.Body))
                {
                    MDLFolder = "chara/human/c" + selectedRace + "/obj/body/b0001/model";
                    MDLFile = "c" + selectedRace + "b0001_top.mdl";

                }
                else if (selectedItem.itemName.Equals(Strings.Face))
                {
                    MDLFolder = "chara/human/c" + selectedRace + "/obj/face/f0001/model";
                    MDLFile = "c" + selectedRace + "f0001_fac.mdl";

                }
                else if (selectedItem.itemName.Equals(Strings.Hair))
                {
                    MDLFolder = "chara/human/c" + selectedRace + "/obj/hair/h0001/model";
                    MDLFile = "c" + selectedRace + "h0001_hir.mdl";

                }
                else if (selectedItem.itemName.Equals(Strings.Tail))
                {
                    MDLFolder = "chara/human/c" + selectedRace + "/obj/tail/t0001/model";
                    MDLFile = "c" + selectedRace + "t0001_til.mdl";

                }
                else
                {
                    MDLFolder = "";
                    MDLFile = "";
                }
            }
            else if (iType.Equals("monster"))
            {
                MDLFolder = "chara/monster/m" + selectedItem.itemID.PadLeft(4, '0') + "/obj/body/b" + selectedItem.weaponBody + "/model";
                MDLFile = "m" + selectedItem.itemID.PadLeft(4, '0') + "b" + selectedItem.weaponBody + ".mdl";
            }
            else
            {
                MDLFolder = "chara/equipment/e" + selectedItem.itemID + "/model";
                MDLFile = "c" + selectedRace + "e" + selectedItem.itemID + "_" + Info.slotAbr[selectedParent] + ".mdl";
            }

            int offset = Helper.GetOffset(FFCRC.GetHash(MDLFolder), FFCRC.GetHash(MDLFile));

            int datNum = ((offset / 8) & 0x000f) / 2;
                 
            offset = Helper.OffsetCorrection(datNum, offset);

            using (BinaryReader br = new BinaryReader(File.OpenRead(Info.datDir + datNum)))
            {
                List<byte> byteList = new List<byte>();

                br.BaseStream.Seek(offset, SeekOrigin.Begin);

                int headerLength = br.ReadInt32();
                int type = br.ReadInt32();
                int decompressedSize = br.ReadInt32();
                br.ReadBytes(8);
                int parts = br.ReadInt16();

                int endOfHeader = offset + headerLength;

                byteList.AddRange(new byte[68]);

                br.BaseStream.Seek(offset + 24, SeekOrigin.Begin);

                int[] chunkUncompSizes = new int[11];
                int[] chunkLengths = new int[11];
                int[] chunkOffsets = new int[11];
                int[] chunkBlockStart = new int[11];
                int[] chunkNumBlocks = new int[11];

                for(int i = 0; i < 11; i++)
                {
                    chunkUncompSizes[i] = br.ReadInt32();
                }
                for (int i = 0; i < 11; i++)
                {
                    chunkLengths[i] = br.ReadInt32();
                }
                for (int i = 0; i < 11; i++)
                {
                    chunkOffsets[i] = br.ReadInt32();
                }
                for (int i = 0; i < 11; i++)
                {
                    chunkBlockStart[i] = br.ReadInt16();
                }
                int totalBlocks = 0;
                for (int i = 0; i < 11; i++)
                {
                    chunkNumBlocks[i] = br.ReadInt16();

                    totalBlocks += chunkNumBlocks[i];
                }

                meshCount = br.ReadInt16();
                materialCount = br.ReadInt16();

                br.ReadBytes(4);

                int[] blockSizes = new int[totalBlocks];

                for(int i = 0; i < totalBlocks; i++)
                {
                    blockSizes[i] = br.ReadInt16();
                }

                br.BaseStream.Seek(offset + headerLength + chunkOffsets[0], SeekOrigin.Begin);

                for(int i = 0; i < totalBlocks; i++)
                {
                    int lastPos = (int)br.BaseStream.Position;

                    br.ReadBytes(8);

                    int partCompSize = br.ReadInt32();
                    int partDecompSize = br.ReadInt32();

                    if (partCompSize == 32000)
                    {
                        byteList.AddRange(br.ReadBytes(partDecompSize));
                    }
                    else
                    {
                        byte[] partDecompBytes = new byte[partDecompSize];
                        using(MemoryStream ms = new MemoryStream(br.ReadBytes(partCompSize)))
                        {
                            using(DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
                            {
                                ds.Read(partDecompBytes, 0, partDecompSize);
                            }
                        }
                        byteList.AddRange(partDecompBytes);
                    }

                    br.BaseStream.Seek(lastPos + blockSizes[i], SeekOrigin.Begin);
                }

                decompBytes = byteList.ToArray();
            }

            using(BinaryReader br = new BinaryReader(new MemoryStream(decompBytes)))
            {
                modelData = new ModelData(meshCount, materialCount);

                for(int i = 0; i < 3; i++)
                {
                    List<ModelData.MeshInfo> mInfo = new List<ModelData.MeshInfo>();

                    for(int j = 0; j < meshCount / 3; j++)
                    {
                        mInfo.Clear();

                        br.BaseStream.Seek((i * 136) + 68, SeekOrigin.Begin);
                        int dataArrayNum = br.ReadByte();

                        while(dataArrayNum != 255)
                        {
                            ModelData.MeshInfo meshInfo = new ModelData.MeshInfo(dataArrayNum, br.ReadByte(), br.ReadByte(), br.ReadByte());
                            mInfo.Add(meshInfo);
                            br.ReadBytes(4);
                            dataArrayNum = br.ReadByte();
                        }

                        modelData.Quality[i].meshInfoDict.Add(j, mInfo.ToArray());
                    }
                }

                br.BaseStream.Seek(136 * meshCount + 68, SeekOrigin.Begin);
                modelData.numStrings = br.ReadInt32();
                modelData.stringBlockSize = br.ReadInt32();

                br.ReadBytes(modelData.stringBlockSize);
                br.ReadBytes(4);

                modelData.numTotalMeshes = br.ReadInt16();
                modelData.numAtrStrings = br.ReadInt16();
                modelData.numParts = br.ReadInt16();
                modelData.numMaterialStrings = br.ReadInt16();
                modelData.numBoneStrings = br.ReadInt16();
                modelData.numBoneLists = br.ReadInt16();
                modelData.unk1 = br.ReadInt16();
                modelData.unk2 = br.ReadInt16();
                modelData.unk3 = br.ReadInt16();
                modelData.unk4 = br.ReadInt16();
                modelData.unk5 = br.ReadInt16();
                modelData.unk6 = br.ReadInt16();
                br.ReadBytes(10);
                modelData.unk7 = br.ReadInt16();
                br.ReadBytes(16);

                br.ReadBytes(32 * modelData.unk5);

                for (int i = 0; i < 3; i++)
                {
                    modelData.Quality[i].meshOffset = br.ReadInt16();
                    modelData.Quality[i].numMeshes = br.ReadInt16();

                    br.ReadBytes(40);

                    modelData.Quality[i].vertDataSize = br.ReadInt32();
                    modelData.Quality[i].indexDataSize = br.ReadInt32();
                    modelData.Quality[i].vertOffset = br.ReadInt32();
                    modelData.Quality[i].indexOffset = br.ReadInt32();
                }

                for (int x = 0; x < 3; x++)
                {
                    for (int i = 0; i < meshCount / 3; i++)
                    {
                        ModelData.Mesh m = new ModelData.Mesh()
                        {
                            numVerts = br.ReadInt32(),
                            numIndex = br.ReadInt32(),
                            materialNumber = br.ReadInt16(),
                            partTableOffset = br.ReadInt16(),
                            partTableCount = br.ReadInt16(),
                            boneListIndex = br.ReadInt16(),
                            indexDataOffset = br.ReadInt32()
                        };
                        for (int j = 0; j < 3; j++)
                        {
                            m.vertexDataOffsets[j] = br.ReadInt32();
                        }
                        for (int k = 0; k < 3; k++)
                        {
                            m.vertexSizes[k] = br.ReadByte();
                        }
                        m.numBuffers = br.ReadByte();

                        modelData.Quality[x].mesh[i] = m;
                    }
                }

                br.ReadBytes(modelData.numAtrStrings * 4);
                br.ReadBytes(modelData.unk6 * 20);

                modelData.SetMeshParts();

                for (int i = 0; i < modelData.numParts; i++)
                {

                    ModelData.MeshPart mp = new ModelData.MeshPart()
                    {
                        indexOffset = br.ReadInt32(),
                        indexCount = br.ReadInt32(),
                        attributes = br.ReadInt32(),
                        boneReferenceOffset = br.ReadInt16(),
                        boneReferenceCount = br.ReadInt16()
                    };
                    modelData.meshPart[i] = mp;
                }

                br.ReadBytes(modelData.unk7 * 12);
                br.ReadBytes(modelData.numMaterialStrings * 4);
                br.ReadBytes(modelData.numBoneStrings * 4);

                modelData.SetBoneList();

                for (int i = 0; i < modelData.numBoneLists; i++)
                {
                    ModelData.BoneList bl = new ModelData.BoneList();
                    for (int j = 0; j < 64; j++)
                    {
                        bl.boneList[j] = br.ReadInt16();
                    }
                    bl.boneCount = br.ReadInt32();

                    modelData.boneList[i] = bl;
                }

                br.ReadBytes(modelData.unk1 * 16);
                br.ReadBytes(modelData.unk2 * 12);
                br.ReadBytes(modelData.unk3 * 4);

                modelData.boneIndexSize = br.ReadInt32();

                modelData.SetBoneIndicies();

                for (int i = 0; i < modelData.boneIndexSize / 2; i++)
                {
                    modelData.boneIndicies[i] = br.ReadInt16();
                }

                int padding = br.ReadByte();
                br.ReadBytes(padding);

                for (int i = 0; i < modelData.bb.Length; i++)
                {
                    ModelData.BoundingBoxes bb = new ModelData.BoundingBoxes();
                    for (int j = 0; j < 4; j++)
                    {
                        bb.pointA[j] = br.ReadSingle();
                    }
                    for (int k = 0; k < 4; k++)
                    {
                        bb.pointB[k] = br.ReadSingle();
                    }

                    modelData.bb[i] = bb;
                }

                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < modelData.Quality[i].numMeshes; j++)
                    {
                        ModelData.Mesh m = modelData.Quality[i].mesh[j];

                        m.SetMeshData();

                        for (int k = 0; k < m.numBuffers; k++)
                        {
                            br.BaseStream.Seek(modelData.Quality[i].vertOffset + m.vertexDataOffsets[k], SeekOrigin.Begin);

                            ModelData.MeshData md = new ModelData.MeshData()
                            {
                                meshData = br.ReadBytes(m.vertexSizes[k] * m.numVerts)
                            };
                            m.meshData[k] = md;
                        }

                        br.BaseStream.Seek(modelData.Quality[i].indexOffset + (m.indexDataOffset * 2), SeekOrigin.Begin);

                        m.indexData = br.ReadBytes(2 * m.numIndex);
                    }
                }

                int vertexs = 0, coordinates = 0, normals = 0, tangents = 0;

                for (int i = 0; i < modelData.Quality[0].numMeshes; i++)
                {
                    objBytes.Clear();

                    pVertexs = new Point3DCollection();
                    vertexList = new Vector3Collection();
                    coordList = new Vector2Collection();
                    normalList = new Vector3Collection();
                    tangentList = new Vector3Collection();
                    indList = new IntCollection();

                    Vector3Collection _vertexList = new Vector3Collection();
                    Vector2Collection _coordList = new Vector2Collection();
                    Vector3Collection _normalList = new Vector3Collection();
                    Mesh _mesh = new Mesh();

                    ModelData.Mesh m = modelData.Quality[0].mesh[i];

                    ModelData.MeshInfo[] mi = modelData.Quality[0].meshInfoDict[i];

                    int c = 0;
                    foreach (var a in mi)
                    {
                        if (a.useType == 0)
                        {
                            vertexs = c;
                        }
                        else if (a.useType == 3)
                        {
                            normals = c;
                        }
                        else if (a.useType == 4)
                        {
                            coordinates = c;
                        }
                        else if(a.useType == 6)
                        {
                            tangents = c;
                        }
                        c++;
                    }

                    using (BinaryReader br1 = new BinaryReader(new MemoryStream(m.meshData[mi[vertexs].dataArrayNum].meshData)))
                    {
                        for (int j = 0; j < m.numVerts; j++)
                        {
                            int offset1 = j * m.vertexSizes[mi[vertexs].dataArrayNum] + mi[vertexs].offset;
                            br1.BaseStream.Seek(offset1, SeekOrigin.Begin);

                            if (mi[vertexs].dataType == 13 || mi[vertexs].dataType == 14)
                            {
                                float f1, f2, f3;

                                System.Half h1 = System.Half.ToHalf((ushort)br1.ReadInt16());
                                System.Half h2 = System.Half.ToHalf((ushort)br1.ReadInt16());
                                System.Half h3 = System.Half.ToHalf((ushort)br1.ReadInt16());


                                f1 = HalfHelper.HalfToSingle(h1);
                                f2 = HalfHelper.HalfToSingle(h2);
                                f3 = HalfHelper.HalfToSingle(h3);

                                objBytes.Add("v " + f1.ToString() + " " + f2.ToString() + " " + f3.ToString() + " ");
                                vertexList.Add(new Vector3(f1, f2, f3));
                            }
                            else if (mi[vertexs].dataType == 2)
                            {
                                float f1, f2, f3;

                                f1 = br1.ReadSingle();
                                f2 = br1.ReadSingle();
                                f3 = br1.ReadSingle();

                                objBytes.Add("v " + f1.ToString() + " " + f2.ToString() + " " + f3.ToString() + " ");
                                vertexList.Add(new Vector3(f1, f2, f3));
                                pVertexs.Add(new Point3D(f1, f2, f3));

                            }
                        }
                    }

                    using (BinaryReader br1 = new BinaryReader(new MemoryStream(m.meshData[mi[coordinates].dataArrayNum].meshData)))
                    {
                        for (int j = 0; j < m.numVerts; j++)
                        {

                            int offset1 = j * m.vertexSizes[mi[coordinates].dataArrayNum] + mi[coordinates].offset;

                            br1.BaseStream.Seek(offset1, SeekOrigin.Begin);

                            System.Half a1 = System.Half.ToHalf((ushort)br1.ReadInt16());
                            System.Half b1 = System.Half.ToHalf((ushort)br1.ReadInt16());

                            float a = HalfHelper.HalfToSingle(a1);
                            float b = HalfHelper.HalfToSingle(b1);

                            objBytes.Add("vt " + a.ToString() + " " + b.ToString() + " ");
                            coordList.Add(new Vector2(a, b));
                        }
                    }

                    using (BinaryReader br1 = new BinaryReader(new MemoryStream(m.meshData[mi[normals].dataArrayNum].meshData)))
                    {
                        for (int j = 0; j < m.numVerts; j++)
                        {
                            br1.BaseStream.Seek(j * m.vertexSizes[mi[normals].dataArrayNum] + mi[normals].offset, SeekOrigin.Begin);

                            System.Half h1 = System.Half.ToHalf((ushort)br1.ReadInt16());
                            System.Half h2 = System.Half.ToHalf((ushort)br1.ReadInt16());
                            System.Half h3 = System.Half.ToHalf((ushort)br1.ReadInt16());

                            objBytes.Add("vn " + HalfHelper.HalfToSingle(h1).ToString() + " " + HalfHelper.HalfToSingle(h2).ToString() + " " + HalfHelper.HalfToSingle(h3).ToString() + " ");
                            normalList.Add(new Vector3(HalfHelper.HalfToSingle(h1), HalfHelper.HalfToSingle(h2), HalfHelper.HalfToSingle(h3)));
                        }
                    }

                    using (BinaryReader br1 = new BinaryReader(new MemoryStream(m.meshData[mi[tangents].dataArrayNum].meshData)))
                    {
                        for (int j = 0; j < m.numVerts; j++)
                        {
                            br1.BaseStream.Seek(j * m.vertexSizes[mi[tangents].dataArrayNum] + mi[tangents].offset, SeekOrigin.Begin);

                            float x = br1.ReadByte() / 255f;
                            float y = br1.ReadByte() / 255f;
                            float z = br1.ReadByte() / 255f;

                            tangentList.Add(new Vector3(x, y, z));
                        }
                    }

                    using (BinaryReader br1 = new BinaryReader(new MemoryStream(m.indexData)))
                    {
                        for (int j = 0; j < m.numIndex; j += 3)
                        {
                            int a1 = br1.ReadInt16();
                            int b1 = br1.ReadInt16();
                            int c1 = br1.ReadInt16();

                            objBytes.Add("f " + (a1 + 1) + "/" + (a1 + 1) + "/" + (a1 + 1) + " " + (b1 + 1) + "/" + (b1 + 1) + "/" + (b1 + 1) + " " + (c1 + 1) + "/" + (c1 + 1) + "/" + (c1 + 1) + " ");

                            indList.Add(a1);
                            indList.Add(b1);
                            indList.Add(c1);

                            _vertexList.Add(vertexList[a1]);
                            _normalList.Add(normalList[a1]);
                            _coordList.Add(coordList[a1]);

                            _vertexList.Add(vertexList[b1]);
                            _normalList.Add(normalList[b1]);
                            _coordList.Add(coordList[b1]);

                            _vertexList.Add(vertexList[c1]);
                            _normalList.Add(normalList[c1]);
                            _coordList.Add(coordList[c1]);
                        }
                    }

                    _mesh.VertexList = vertexList;
                    _mesh.NormalList = normalList;
                    _mesh.CoordList = coordList;
                    _mesh.TangentList = tangentList;
                    _mesh.IndList = indList;
                    _mesh.Obj = objBytes.ToArray();
                    meshList.Add(_mesh);
                }
            }
        }
        public int GetNumMeshes()
        {
            return meshCount;
        }

        public List<Mesh> GetMeshList()
        {
            return meshList;
        }

        public ModelData GetModelData()
        {
            return modelData;
        }

        public Vector3Collection GetVertexList()
        {
            return vertexList;
        }

        public Vector2Collection GetCoordList()
        {

            return coordList;
        }

        public Vector3Collection GetNormalList()
        {

            return normalList;
        }

        public IntCollection GetIndList()
        {
            return indList;
        }

        public string GetModelName()
        {
            return Path.GetFileNameWithoutExtension(MDLFile);
        }
    }
}
