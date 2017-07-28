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

namespace FFXIV_TexTools2.Material.ModelMaterial
{
    public class MeshDataInfo
    {
        /// <summary>
        /// The DataBlock in which the vertex information is located
        /// </summary>
        public int VertexDataBlock { get; set; }

        /// <summary>
        /// The offset of the data within the VertexDataBlock
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// The data type of data at the particular offset in the VertexDataBlock
        /// </summary>
        public int DataType { get; set; }

        /// <summary>
        /// The datas use
        /// </summary>
        public int UseType { get; set; }
    }
}
