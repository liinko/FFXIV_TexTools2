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


using System;
using System.ComponentModel;

namespace FFXIV_TexTools2.Model
{
    public class ItemData : IComparable
    {
        /// <summary>
        /// The name of the item
        /// </summary>
        public string ItemName { get; set; }

        /// <summary>
        /// The category for the item
        /// </summary>
        public string ItemCategory { get; set; }

        /// <summary>
        /// The subcategory for the item
        /// </summary>
        public string ItemSubCategory { get; set; }

        /// <summary>
        /// The ID from the primary model data
        /// </summary>
        public string PrimaryModelID { get; set; }

        /// <summary>
        /// The body from the primary model data
        /// </summary>
        public string PrimaryModelBody { get; set; }

        /// <summary>
        /// the variant from the primary model data
        /// </summary>
        public string PrimaryModelVariant { get; set; }

        /// <summary>
        /// The ID from the secondary model data
        /// </summary>
        public string SecondaryModelID { get; set; }

        /// <summary>
        /// The body from the secondary model data
        /// </summary>
        public string SecondaryModelBody { get; set; }

        /// <summary>
        /// The variant from the secondary model data
        /// </summary>
        public string SecondaryModelVariant { get; set; }

        /// <summary>
        /// The MTRL folder for the item
        /// </summary>
        public string PrimaryMTRLFolder { get; set; }

        /// <summary>
        /// The MTRL folder for the item
        /// </summary>
        public string SecondaryMTRLFolder { get; set; }

        /// <summary>
        /// The icon number for the item
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// The UI Path for the item
        /// </summary>
        public string UIPath { get; set; }

        /// <summary>
        /// Compares Item names for sorting
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            return ItemName.CompareTo(((ItemData)obj).ItemName);
        }


        public override string ToString()
        {
            if(SecondaryModelID != null)
            {
                return "Name: " + ItemName + "\nCategory: " + ItemCategory + "\nID " + PrimaryModelID + "\nVariant: " + PrimaryModelVariant + "\nMTRL: " + PrimaryMTRLFolder +
                    "\nsID " + SecondaryModelID + "\nsVariant: " + SecondaryModelVariant + "\nsMTRL: " + SecondaryMTRLFolder;

            }
            else
            {
                return "Name: " + ItemName + "\nCategory: " + ItemCategory + "\nID " + PrimaryModelID + "\nVariant: " + PrimaryModelVariant + "\nMTRL: " + PrimaryMTRLFolder;
            }
        }
    }
}
