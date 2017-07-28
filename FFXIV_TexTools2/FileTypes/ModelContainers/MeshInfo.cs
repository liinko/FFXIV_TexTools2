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

namespace FFXIV_TexTools2.Material.ModelMaterial
{
    public class MeshInfo
    {
        /// <summary>
        /// The amount of vertices in the mesh
        /// </summary>
        public int VertexCount { get; set; }

        /// <summary>
        /// The amount of indices in the mesh
        /// </summary>
        public int IndexCount { get; set; }

        /// <summary>
        /// Which material the mesh will use
        /// </summary>
        public int MaterialNum { get; set; }

        /// <summary>
        /// The offest to the mesh part
        /// </summary>
        public int MeshPartOffset { get; set; }

        /// <summary>
        /// The amount of parts the mesh has
        /// </summary>
        public int MeshPartCount { get; set; }

        /// <summary>
        /// The index to the bone list
        /// </summary>
        public int BoneListIndex { get; set; }

        /// <summary>
        /// The offset to the index data
        /// </summary>
        public int IndexDataOffset { get; set; }

        /// <summary>
        /// The amount of vertex data blocks
        /// </summary>
        public int VertexDataBlockCount { get; set; }

        /// <summary>
        /// The list of offsets to the vertex data
        /// </summary>
        public List<int> VertexDataOffsets = new List<int>();

        /// <summary>
        /// A list containing the size of each set of vertex information 
        /// </summary>
        public List<int> VertexSizes = new List<int>();
    }
}
