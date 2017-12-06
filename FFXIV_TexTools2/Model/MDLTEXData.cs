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

using FFXIV_TexTools2.Material;
using System.Windows.Media.Imaging;

namespace FFXIV_TexTools2.Model
{
    public class MDLTEXData
    {
        /// <summary>
        /// The models mesh data
        /// </summary>
        public ModelMeshData Mesh { get; set; }

        /// <summary>
        /// The normal map as an image
        /// </summary>
        public BitmapSource Normal { get; set; }

        /// <summary>
        /// The diffuse map as an image
        /// </summary>
        public BitmapSource Diffuse { get; set; }

        /// <summary>
        /// The specular map as an image
        /// </summary>
        public BitmapSource Specular { get; set; }

        /// <summary>
        /// The mask map as an image
        /// </summary>
        public BitmapSource Mask { get; set; }

        /// <summary>
        /// The mask map as an image
        /// </summary>
        public BitmapSource Alpha { get; set; }

        /// <summary>
        /// The color set as an image
        /// </summary>
        public BitmapSource ColorTable { get; set; }

        /// <summary>
        /// The emissive map as an image
        /// </summary>
        public BitmapSource Emissive { get; set; }

        /// <summary>
        /// Is the mesh part of the models body
        /// </summary>
        public bool IsBody { get; set; }

        /// <summary>
        /// Is the mesh part of the models face
        /// </summary>
        public bool IsFace { get; set; }
    }
}
