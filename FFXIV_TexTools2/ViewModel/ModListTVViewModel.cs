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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace FFXIV_TexTools2.ViewModel
{
    public class ModListTVViewModel : INotifyPropertyChanged
    {
        readonly List<ModListTVViewModel> _children = new List<ModListTVViewModel>();
        readonly ModListTVViewModel _parent;
        readonly string _category;

        bool _isExpanded;
        bool _isSelected;


        public ModListTVViewModel(string category) : this(category, null)
        {
        }

        public ModListTVViewModel(string category, ModListTVViewModel parent)
        {
            _parent = parent;
            _category = category;

            HashSet<string> itemSet = new HashSet<string>();

            try
            {
                foreach (string line in File.ReadAllLines(Info.modListDir))
                {
                    JsonEntry entry = JsonConvert.DeserializeObject<JsonEntry>(line);

                    if (entry.category.Equals(category) && _parent == null)
                    {
                        itemSet.Add(entry.name);

                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("[VM] Error Accessing .modlist File \n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }


            foreach (string name in itemSet)
            {
                _children.Add(new ModListTVViewModel(name, this));
            }
        }

        public List<ModListTVViewModel> Children
        {
            get { return _children; }
        }

        public ModListTVViewModel Parent
        {
            get { return _parent; }
        }

        public string Name
        {
            get
            { return _category; }
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if(value != _isExpanded)
                {
                    _isExpanded = value;
                    this.OnPropertyChanged("IsExpanded");
                }

                if(_isExpanded && _parent != null)
                {
                    _parent.IsExpanded = true;
                }
            }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if(value != _isSelected)
                {
                    _isSelected = value;
                    this.OnPropertyChanged("IsSelected");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if(this.PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}
