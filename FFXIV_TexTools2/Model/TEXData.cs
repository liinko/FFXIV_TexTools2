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

using System.Drawing;
using System.Windows.Media.Imaging;

namespace FFXIV_TexTools2.Model
{
    public class TEXData
    {
        /// <summary>
        /// The textures width
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// The texture height
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// The DDS type of the texture
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// The name of the DDS type 
        /// </summary>
        public string TypeString { get; set; }

        /// <summary>
        /// The number of mip maps in the texture
        /// </summary>
        public int MipCount { get; set; }

        /// <summary>
        /// The raw DDS texture data
        /// </summary>
        //public byte[] RawTexData { get; set; }

        /// <summary>
        /// The texture bitmapSouce with Alpha
        /// </summary>
        public BitmapSource BMPSouceAlpha { get; set; }

        /// <summary>
        /// The texture bitmapSouce with no Alpha
        /// </summary>
        public BitmapSource BMPSouceNoAlpha { get; set; }

        /// <summary>
        /// The texture dat name
        /// </summary>
        public string TEXDatName { get; set; }

        /// <summary>
        /// The texture offset
        /// </summary>
        public int TexOffset { get; set; }
    }
}
