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

namespace FFXIV_TexTools2.Model
{
    public class ComboBoxInfo : IComparable
    {
        /// <summary>
        /// The name to be displayed in the combo box
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The ID of the combo box item
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Is the value a number
        /// </summary>
        public bool IsNum { get; set; }


        /// <summary>
        /// Compares names in combobox for sorting
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            int compare;
            if (IsNum)
            {
                compare = int.Parse(Name).CompareTo(int.Parse(((ComboBoxInfo)obj).Name));
            }
            else
            {
                compare = Name.CompareTo(((ComboBoxInfo)obj).Name);
            }


            return compare;
        }
    }
}
