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
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FFXIV_TexTools2.Model
{
    public class ModListModel : INotifyPropertyChanged
    {
        private SolidColorBrush _active, _activeBorder;
        private float _opacity;

        public string Race { get; set; }
        public string Map { get; set; }
        public string Part { get; set; }
        public string Type { get; set; }
        public SolidColorBrush Active
        {
            get { return _active; }
            set
            {
                _active = value;
                OnPropertyChanged("Active");
            }
        }
        public float ActiveOpacity
        {
            get{ return _opacity; }

            set
            {
                _opacity = value;
                OnPropertyChanged("ActiveOpacity");
            }
        }
        public SolidColorBrush ActiveBorder
        {
            get { return _activeBorder; }
            set
            {
                _activeBorder = value;
                OnPropertyChanged("ActiveBorder");
            }
        }
        public JsonEntry Entry { get; set; }

        public BitmapSource BMP { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }
}
