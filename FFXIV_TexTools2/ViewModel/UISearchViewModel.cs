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
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace FFXIV_TexTools2.ViewModel
{
    public class UISearchViewModel : INotifyPropertyChanged
    {
        private string idText;
        private MainViewModel parentVM;
        private Window window;

        public ICommand OpenAction { get { return new RelayCommand(Open); } }
        public string IdText { get { return idText; } set { idText = value; } }

        public UISearchViewModel(MainViewModel parent, Window self)
        {
            parentVM = parent;
            window = self;
        }

        public void Open(object o)
        {
            var idNum = 0;

            if(!int.TryParse(idText, out idNum))
            {
                FlexibleMessageBox.Show("Invalid Texture ID.","Invalid ID Error");
                return;
            }

            int folderNumber = (idNum / 1000) * 1000;
            var folderName = folderNumber.ToString();
            while(folderName.Length < 6)
            {
                folderName = "0" + folderName;
            }

            var fileName = idText + ".tex";
            var filePath = "ui/icon/" + folderName;

            ItemData itemData = new ItemData()
            {
                ItemName = fileName,
                ItemCategory = Strings.Other,
                ItemSubCategory = "Other",
                UIPath = filePath,
            };

            parentVM.TextureVM.UpdateTexture(itemData, "UI");
            window.Close();
        }





        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        
    }
}
