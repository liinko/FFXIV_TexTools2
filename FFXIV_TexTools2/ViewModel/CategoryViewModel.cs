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
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace FFXIV_TexTools2.ViewModel
{
    public class CategoryViewModel : INotifyPropertyChanged
    {
        ObservableCollection<CategoryViewModel> _children;
        CategoryViewModel _parent;
        TreeNode _nodeData;

        bool _isExpanded;
        bool _isSelected;

        public CategoryViewModel(TreeNode categoryData) : this(categoryData, null)
        {

        }

        private CategoryViewModel(TreeNode categoryData, CategoryViewModel parent)
        {
            _nodeData = categoryData;
            _parent = parent;

            _children = new ObservableCollection<CategoryViewModel>((from child in _nodeData.SubNode select new CategoryViewModel(child, this)).ToList<CategoryViewModel>());
        }

        public ObservableCollection<CategoryViewModel> Children
        {
            get { return _children; }
            set { _children = value; OnPropertyChanged("Children"); }
        }

        public string Name
        {
            get { return _nodeData.Name; }
        }

        public ItemData ItemData
        {
            get { return _nodeData.ItemData; }
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;
                    this.OnPropertyChanged("IsExpanded");
                }

                // Expand all the way up to the root.
                if (_isExpanded && _parent != null)
                    _parent.IsExpanded = true;
            }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    this.OnPropertyChanged("IsSelected");
                }
            }
        }

        public void ExpandAll()
        {
            IsExpanded = true;
            if(_children != null)
            {
                foreach(var c in _children)
                {
                    c.ExpandAll();
                }
            }
        }

        public CategoryViewModel Parent
        {
            get { return _parent; }
            set
            {
                _parent = value;
                OnPropertyChanged("Parent");
            }
        }

        public bool NameContainsText(string text)
        {
            if (String.IsNullOrEmpty(text) || String.IsNullOrEmpty(this.Name))
                return false;

            return this.Name.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) > -1;
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
