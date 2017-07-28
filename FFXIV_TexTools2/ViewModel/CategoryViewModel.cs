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

using FFXIV_TexTools2.Helpers;
using FFXIV_TexTools2.Model;
using FFXIV_TexTools2.Resources;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FFXIV_TexTools2.ViewModel
{
    public class CategoryViewModel : TreeViewItemViewModel
    {
        readonly string _category;
        ObservableCollection<ItemData> _itemList;

        /// <summary>
        /// View model that attaches the items to their respective category 
        /// </summary>
        /// <param name="category">The current category to have items assigned to</param>
        /// <param name="itemList">The list of items obtained from the exd file</param>
        public CategoryViewModel(string category, ObservableCollection<ItemData> itemList) : base(null, false)
        {
            _category = category;
            _itemList = itemList;

            IEnumerable<ItemData> filteredItems;

            //for aesthetic purposes the items in the character and pets category are not sorted by name
            if (!CategoryName.Equals(Strings.Character) && !CategoryName.Equals(Strings.Pets))
            {
                filteredItems = from item in _itemList where item.ItemCategory.Equals(Info.IDSlot[CategoryName]) orderby item.ItemName select item;
            }
            else
            {
                filteredItems = from item in _itemList where item.ItemCategory.Equals(Info.IDSlot[CategoryName]) select item;
            }

            foreach (ItemData item in filteredItems)
            {
                Children.Add(new ItemViewModel(item, this));
            }
        }

        public string CategoryName
        {
            get { return _category; }
        }
    }
}
