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
    public class LevelOfDetail
    {
        /// <summary>
        /// The meshes contained within the current LoD
        /// </summary>
        public List<Mesh> MeshList = new List<Mesh>();

        /// <summary>
        /// Offset of the mesh data
        /// </summary>
        public int MeshOffset { get; set; }

        /// <summary>
        /// The number of meshes in the current LoD
        /// </summary>
        public int MeshCount { get; set; }

        /// <summary>
        /// The size of the vertex data
        /// </summary>
        public int VertexDataSize { get; set; }

        /// <summary>
        /// The size of the index data
        /// </summary>
        public int IndexDataSize { get; set; }

        /// <summary>
        /// The offset to the vertex data
        /// </summary>
        public int VertexOffset { get; set; }

        /// <summary>
        /// The offset to the index data
        /// </summary>
        public int IndexOffset { get; set; }
    }
}
