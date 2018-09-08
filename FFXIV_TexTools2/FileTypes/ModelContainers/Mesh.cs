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

using SharpDX;
using System.Collections.Generic;

namespace FFXIV_TexTools2.Material.ModelMaterial
{
    public class Mesh
    {
        /// <summary>
        /// The mesh data information 
        /// </summary>
        public MeshDataInfo[] MeshDataInfoList { get; set; }

        /// <summary>
        /// The meshes detailed information
        /// </summary>
        public MeshInfo MeshInfo { get; set; }

        /// <summary>
        /// The mesh parts contained within the mesh
        /// </summary>
        public List<MeshPart> MeshPartList = new List<MeshPart>();

        /// <summary>
        /// Material used in this mesh.
        /// </summary>
        public int MaterialId;

        /// <summary>
        /// The meshes index data
        /// </summary>
        public byte[] IndexData { get; set; }

        /// <summary>
        /// The meshes vertex data
        /// </summary>
        public List<byte[]> MeshVertexData = new List<byte[]>();

        /// <summary>
        /// The meshes extra vertex data
        /// </summary>
        public Dictionary<int, Vector3> extraVertDict = new Dictionary<int, Vector3>();

        /// <summary>
        /// True if mesh has a body texture, false otherwise.
        /// </summary>
        public bool IsBody { get; set; }

        /// <summary>
        /// Bone Set Index use by this mesh.
        /// </summary>
        public int BoneListIndex;
    }
}
