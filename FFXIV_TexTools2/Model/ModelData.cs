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
using System.Collections.Generic;

namespace FFXIV_TexTools2.Model
{
    public class ModelData
    {
        /// <summary>
        /// The levels of detail of the model
        /// </summary>
        public List<LevelOfDetail> LoD = new List<LevelOfDetail>();

        /// <summary>
        /// The models bounding boxes
        /// </summary>
        public List<BoundingBox> BoundingBoxes = new List<BoundingBox>();

        /// <summary>
        /// The bone set for the model
        /// </summary>
        public List<Bones> BoneSet = new List<Bones>();

        /// <summary>
        /// The models bone indices
        /// </summary>
        public List<int> BoneIndicies = new List<int>();
    }
}
