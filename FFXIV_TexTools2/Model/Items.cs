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


namespace FFXIV_TexTools2.Model
{
    public class Items
    {
        public string itemName, itemID, itemID1, itemSlot, imcVariant, imcVariant1, weaponBody, weaponBody1, mtrlFolder, sMtrlFolder;
        public bool hasSecondary;

        public Items(string itemName, string itemSlot) : this(itemName, "", "", itemSlot, "", "", "", "", false, "", "")
        {
        }


        public Items(string itemName, string itemID, string itemID1, string itemSlot, string imcVariant, 
            string imcVartiant1, string weaponBody, string weaponBody1, bool hasSecondary, string mtrlFolder, string sMtrlFolder)
        {
            this.itemName = itemName;
            this.itemID = itemID;
            this.itemID1 = itemID1;
            this.itemSlot = itemSlot;
            this.imcVariant = imcVariant;
            this.imcVariant1 = imcVartiant1;
            this.weaponBody = weaponBody;
            this.weaponBody1 = weaponBody1;
            this.hasSecondary = hasSecondary;
            this.mtrlFolder = mtrlFolder;
            this.sMtrlFolder = sMtrlFolder;

        }
    }
}
