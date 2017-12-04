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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace FFXIV_TexTools2.ViewModel
{
    public class ModListViewModel
    {
        readonly List<ModListTVViewModel> _categories = new List<ModListTVViewModel>();
        readonly ModListTVViewModel _category;

        public ModListViewModel()
        {
            HashSet<string> categorySet = new HashSet<string>();

            try
            {
                foreach (string line in File.ReadAllLines(Info.modListDir))
                {
                    JsonEntry entry = JsonConvert.DeserializeObject<JsonEntry>(line);

                    if (!entry.category.Equals(""))
                    {
                        categorySet.Add(entry.category);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("[VM] Error Accessing .modlist File \n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }


            foreach (string category in categorySet)
            {
                _category = new ModListTVViewModel(category);

                _categories.Add(_category);
            }
        }

        public List<ModListTVViewModel> Categories
        {
            get { return _categories; }
        }
    }
}
