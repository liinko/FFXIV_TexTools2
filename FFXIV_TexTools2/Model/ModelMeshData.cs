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

using HelixToolkit.Wpf.SharpDX.Core;

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
        /// The mesh data in OBJ format
        /// </summary>
        public string[] OBJFileData { get; set; }
    }
}
