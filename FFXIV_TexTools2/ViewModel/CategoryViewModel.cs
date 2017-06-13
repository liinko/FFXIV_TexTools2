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
using System.Linq;

namespace FFXIV_TexTools2.ViewModel
{
    public class CategoryViewModel : TreeViewItemViewModel
    {
        readonly Category _category;
        List<Items> _itemList;

        public CategoryViewModel(Category category, List<Items> itemList) : base(null, false)
        {
            _category = category;

            _itemList = itemList;

            IEnumerable<Items> items;
            if (!CategoryName.Equals(Strings.Character) && !CategoryName.Equals(Strings.Pets))
            {
                items = from item in _itemList where item.itemSlot.Equals(Info.IDSlot[CategoryName]) orderby item.itemName select item;
            }
            else
            {
                items = from item in _itemList where item.itemSlot.Equals(Info.IDSlot[CategoryName]) select item;
            }

            foreach (Items item in items)
            {
                Children.Add(new ItemViewModel(item, this));
            }
        }

        public string CategoryName
        {
            get { return _category.CategoryName; }
        }

        protected override void LoadChildren()
        {
            IEnumerable<Items> items;

            if(!CategoryName.Equals(Strings.Character) && !CategoryName.Equals(Strings.Pets))
            {
                items = from item in _itemList where item.itemSlot.Equals(Info.IDSlot[CategoryName]) orderby item.itemName select item;
            }
            else
            {
                items = from item in _itemList where item.itemSlot.Equals(Info.IDSlot[CategoryName]) select item;
            }

            foreach (Items item in items)
            {
                Children.Add(new ItemViewModel(item, this));
            }
        }
    }
}
