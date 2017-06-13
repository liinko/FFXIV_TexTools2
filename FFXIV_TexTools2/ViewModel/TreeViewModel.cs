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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FFXIV_TexTools2.ViewModel
{
    public class TreeViewModel
    {
        readonly ReadOnlyCollection<CategoryViewModel> _categories;

        public TreeViewModel(Category[] categories, List<Items> itemList)
        {
            _categories = new ReadOnlyCollection<CategoryViewModel>((from category in categories select new CategoryViewModel(category, itemList)).ToList());
        }

        public ReadOnlyCollection<CategoryViewModel> Category
        {
            get { return _categories; }
        }
    }
}
