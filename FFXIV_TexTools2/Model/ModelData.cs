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

using System.Collections.Generic;

namespace FFXIV_TexTools2.Model
{
    public class ModelData
    {
        public ModelQuality[] Quality = new ModelQuality[3];
        public int numStrings, stringBlockSize, numTotalMeshes, numAtrStrings, numParts, numMaterialStrings, numBoneStrings, numBoneLists, boneIndexSize;
        public int unk1, unk2, unk3, unk4, unk5, unk6, unk7;
        public MeshPart[] meshPart = null;
        public BoneList[] boneList;
        public int[] boneIndicies;
        public BoundingBoxes[] bb = new BoundingBoxes[4];

        public ModelData(int numTotalMeshes, int MaterialCount)
        {
            this.numTotalMeshes = numTotalMeshes;
            for (int i = 0; i < 3; i++)
            {
                Quality[i] = new ModelQuality(numTotalMeshes / 2);
            }
        }

        public void SetMeshParts()
        {
            meshPart = new MeshPart[numParts];
        }

        public void SetBoneList()
        {
            boneList = new BoneList[numBoneLists];
        }

        public void SetBoneIndicies()
        {
            boneIndicies = new int[boneIndexSize / 2];
        }

        public class ModelQuality
        {
            public Dictionary<int, MeshInfo[]> meshInfoDict = new Dictionary<int, MeshInfo[]>();

            public int meshOffset, numMeshes, vertDataSize, indexDataSize, vertOffset, indexOffset;

            public Mesh[] mesh;

            public ModelQuality(int meshCount)
            {
                mesh = new Mesh[meshCount];
            }
        }


        public class Mesh
        {
            public int numVerts, numIndex, materialNumber, partTableOffset, partTableCount, boneListIndex, indexDataOffset, numBuffers;
            public int[] vertexDataOffsets = new int[3];
            public int[] vertexSizes = new int[3];
            public MeshData[] meshData;
            public byte[] indexData;

            public void SetMeshData()
            {
                meshData = new MeshData[numBuffers];
            }
        }


        public class BoneList
        {
            public int boneCount;
            public int[] boneList = new int[64];
        }

        public class BoundingBoxes
        {
            public float[] pointA = new float[4];

            public float[] pointB = new float[4];
        }

        public class MeshPart
        {
            public int indexOffset, indexCount, attributes, boneReferenceOffset, boneReferenceCount;
        }

        public class MeshData
        {
            public byte[] meshData;
        }

        public class MeshInfo
        {
            public int dataArrayNum, offset, dataType, useType;
            public MeshInfo(int dataArrayNum, int offset, int dataType, int useType)
            {
                this.dataArrayNum = dataArrayNum;
                this.offset = offset;
                this.dataType = dataType;
                this.useType = useType;
            }
        }

    }
}
