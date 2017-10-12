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

using FFXIV_TexTools2.ViewModel;
using System.Windows;

namespace FFXIV_TexTools2.Views
{
    /// <summary>
    /// Interaction logic for ModelSearch.xaml
    /// </summary>
    public partial class ModelSearch : Window
    {
        public ModelSearch(MainViewModel parent)
        {
            InitializeComponent();
            this.DataContext = new ModelSearchViewModel(parent);
        }
    }
}
