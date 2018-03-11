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

namespace FFXIV_TexTools2.Model
{
    public class JsonEntry
    {
        /// <summary>
        /// The modified items category
        /// </summary>
        public string category { get; set; }

        /// <summary>
        /// The modified items name
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// The internal path of the modified item
        /// </summary>
        public string fullPath { get; set; }

        /// <summary>
        /// The oringial offset of the modified item
        /// </summary>
        /// <remarks>
        /// Used to revert to the items original texture
        /// </remarks>
        public int originalOffset { get; set; }

        /// <summary>
        /// The modified offset of the modified item
        /// </summary>
        public int modOffset { get; set; }

        /// <summary>
        /// The size of the modified items data
        /// </summary>
        /// <remarks>
        /// When importing a previously modified texture, this value is used to determine whether the modified data will be overwritten
        /// </remarks>
        public int modSize { get; set; }

        /// <summary>
        /// The dat file where the modified item is located
        /// </summary>
        public string datFile { get; set; }
    }
}
