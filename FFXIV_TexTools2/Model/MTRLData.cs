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

using FFXIV_TexTools2.Model;
using System.Collections.ObjectModel;

namespace FFXIV_TexTools2.Model
{
    public class MTRLData
    {
        /// <summary>
        /// The texture maps 
        /// </summary>
        public ObservableCollection<ComboBoxInfo> TextureMaps { get; set; } = new ObservableCollection<ComboBoxInfo>();

        /// <summary>
        /// The items internal MTRL file path
        /// </summary>
        public string MTRLPath { get; set; }

        /// <summary>
        /// The items MTRL data offset
        /// </summary>
        public int MTRLOffset { get; set; }

        /// <summary>
        /// The items internal Specular Map path
        /// </summary>
        public string SpecularPath { get; set; }

        /// <summary>
        /// The items Specular Map data offset
        /// </summary>
        public int SpecularOffset { get; set; }

        /// <summary>
        /// The items internal Mask Map path
        /// </summary>
        public string MaskPath { get; set; }

        /// <summary>
        /// The items Mask Map data offset
        /// </summary>
        public int MaskOffset { get; set; }

        /// <summary>
        /// The items internal Normal Map path
        /// </summary>
        public string NormalPath { get; set; }

        /// <summary>
        /// The items Normal Map data offset
        /// </summary>
        public int NormalOffset { get; set; }

        /// <summary>
        /// The items internal Diffuse Map path
        /// </summary>
        public string DiffusePath { get; set; }

        /// <summary>
        /// The items Diffuse Map data offset
        /// </summary>
        public int DiffuseOffset { get; set; }

        /// <summary>
        /// The items ColorSet Data
        /// </summary>
        public byte[] ColorData { get; set; }

        /// <summary>
        /// The items Color Flags
        /// </summary>
        public byte[] ColorFlags { get; set; }

        /// <summary>
        /// The items internal Icon path
        /// </summary>
        public string UIPath { get; set; }

        /// <summary>
        /// The items internal Icon path
        /// </summary>
        public string UIHQPath { get; set; }

        /// <summary>
        /// The items Icon data offset
        /// </summary>
        public int UIOffset { get; set; }

        /// <summary>
        /// The items Icon data offset
        /// </summary>
        public int UIHQOffset { get; set; }

        /// <summary>
        /// The shader number
        /// </summary>
        public int ShaderNum { get; set; }
    }
}
