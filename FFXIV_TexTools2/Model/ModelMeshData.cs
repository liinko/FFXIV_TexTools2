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

using FFXIV_TexTools2.Material.ModelMaterial;
using HelixToolkit.Wpf.SharpDX.Core;
using SharpDX;
using System.Collections.Generic;

namespace FFXIV_TexTools2.Material
{
    public class ModelMeshData
    {
        /// <summary>
        /// The collection of vertices for the mesh
        /// </summary>
        public Vector3Collection Vertices = new Vector3Collection();

        /// <summary>
        /// The collection of texture coordinates for the mesh
        /// </summary>
        public Vector2Collection TextureCoordinates = new Vector2Collection();

        /// <summary>
        /// The collection of normals for the mesh
        /// </summary>
        public Vector3Collection Normals = new Vector3Collection();

        /// <summary>
        /// The collection of tangents for the mesh
        /// </summary>
        public Vector3Collection BiTangents = new Vector3Collection();


        /// <summary>
        /// The collection of tangents for the mesh
        /// </summary>
        public Vector3Collection Tangents = new Vector3Collection();


        /// <summary>
        /// The collection of indices for the mesh
        /// </summary>
        public IntCollection Indices = new IntCollection();

        /// <summary>
        /// The collection of vertex colors for the mesh
        /// </summary>
        public Color4Collection VertexColors = new Color4Collection();

        /// <summary>
        /// The list of bone strings
        /// </summary>
        public List<string> BoneStrings = new List<string>();

        /// <summary>
        /// The list of bone Indices
        /// </summary>
        public List<int> BoneIndices = new List<int>();
        
        /// <summary>
        /// The list of bone transforms
        /// </summary>
        public List<float> BoneTransforms = new List<float>();

        /// <summary>
        /// The list of blend indices
        /// </summary>
        public List<int> BlendIndices = new List<int>();

        /// <summary>
        /// The list of blend indices
        /// </summary>
        public List<int[]> BlendIndicesArrayList = new List<int[]>();

        /// <summary>
        /// The list of blend weights
        /// </summary>
        public List<float> BlendWeights = new List<float>();

        /// <summary>
        /// The list of blend weights
        /// </summary>
        public List<float[]> BlendWeightsArrayList = new List<float[]>();

        /// <summary>
        /// The list of weight coutns
        /// </summary>
        public List<int> WeightCounts = new List<int>();

        /// <summary>
        /// The mesh data in OBJ format
        /// </summary>
        public string[] OBJFileData { get; set; }

        /// <summary>
        /// The material number used by this mesh
        /// </summary>
        public int MaterialNum { get; set; }

        /// <summary>
        /// The mesh parts list
        /// </summary>
        public List<MeshPart> MeshPartList { get; set; }
    }
}
