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
        public string Name { get; set; }
        public string ID { get; set; }

        public ComboBoxInfo(string name, string id)
        {
            Name = name;
            ID = id;
        }

        public int CompareTo(object obj)
        {
            int compare;
            try
            {
                compare = int.Parse(Name).CompareTo(int.Parse(((ComboBoxInfo)obj).Name));
            }
            catch
            {
                compare = Name.CompareTo(((ComboBoxInfo)obj).Name);
            }

            return compare;
        }
    }
}
