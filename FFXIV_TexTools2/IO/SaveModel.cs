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

using FFXIV_TexTools2.Material;
using FFXIV_TexTools2.Model;
using FFXIV_TexTools2.Resources;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;

namespace FFXIV_TexTools2.IO
{
    public class SaveModel
    {
        /// <summary>
        /// Saves the model as an OBJ file with its associated textures
        /// </summary>
        /// <param name="selectedCategory">The category of the item</param>
        /// <param name="modelName">The internal file name of the items model</param>
        /// <param name="selectedMesh">The currently selected mesh</param>
        /// <param name="selectedItemName">the currently selected items name</param>
        /// <param name="meshData">The mesh data for the selected items model</param>
        /// <param name="meshList">The list of mesh data for the selected items model</param>
        public static void Save(string selectedCategory, string modelName, string selectedMesh, string selectedItemName, List<MDLTEXData> meshData, List<ModelMeshData> meshList)
        {
            Directory.CreateDirectory(Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItemName + "/3D/");

            if (!selectedMesh.Equals(Strings.All))
            {
                int meshNum = int.Parse(selectedMesh);

                File.WriteAllLines(Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItemName + "/3D/" + modelName + "_mesh_" + meshNum + ".obj", meshList[meshNum].OBJFileData);

                var saveDir = Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItemName + "/3D/" + modelName + "_mesh_" + meshNum + "_Diffuse.bmp";

                using (var fileStream = new FileStream(saveDir, FileMode.Create))
                {
                    BitmapEncoder encoder = new BmpBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(meshData[meshNum].Diffuse));
                    encoder.Save(fileStream);
                }

                saveDir = Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItemName + "/3D/" + modelName + "_mesh_" + meshNum + "_Normal.bmp";

                using (var fileStream = new FileStream(saveDir, FileMode.Create))
                {
                    BitmapEncoder encoder = new BmpBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(meshData[meshNum].Normal));
                    encoder.Save(fileStream);
                }


                if (meshData[meshNum].Specular != null)
                {
                    saveDir = Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItemName + "/3D/" + modelName + "_mesh_" + meshNum + "_Specular.png";

                    using (var fileStream = new FileStream(saveDir, FileMode.Create))
                    {
                        BitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(meshData[meshNum].Specular));
                        encoder.Save(fileStream);
                    }
                }
            }
            else
            {
                for (int i = 0; i < meshList.Count; i++)
                {

                    File.WriteAllLines(Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItemName + "/3D/" + modelName + "_mesh_" + i + ".obj", meshList[i].OBJFileData);

                    var saveDir = Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItemName + "/3D/" + modelName + "_mesh_" + i + "_Diffuse.bmp";

                    using (var fileStream = new FileStream(saveDir, FileMode.Create))
                    {
                        BitmapEncoder encoder = new BmpBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(meshData[i].Diffuse));
                        encoder.Save(fileStream);
                    }

                    saveDir = Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItemName + "/3D/" + modelName + "_mesh_" + i + "_Normal.bmp";

                    using (var fileStream = new FileStream(saveDir, FileMode.Create))
                    {
                        BitmapEncoder encoder = new BmpBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(meshData[i].Normal));
                        encoder.Save(fileStream);
                    }


                    if (meshData[i].Specular != null)
                    {
                        saveDir = Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItemName + "/3D/" + modelName + "_mesh_" + i + "_Specular.png";

                        using (var fileStream = new FileStream(saveDir, FileMode.Create))
                        {
                            BitmapEncoder encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(meshData[i].Specular));
                            encoder.Save(fileStream);
                        }
                    }

                }
            }

        }

    }
}
