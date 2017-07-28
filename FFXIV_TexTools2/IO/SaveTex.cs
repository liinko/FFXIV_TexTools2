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
using FFXIV_TexTools2.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;

namespace FFXIV_TexTools2.IO
{
    /// <summary>
    /// Handles IO operations for saving texture data
    /// </summary>
    public class SaveTex
    {
        /// <summary>
        /// Saves the currently displayed texture map as an image file.
        /// </summary>
        /// <param name="selectedCategory">The items category</param>
        /// <param name="selectedItem">The currently selected item</param>
        /// <param name="internalFilePath">The internal file path of the texture map</param>
        /// <param name="selectedBitmap">The bitmap of the texturemap currently being displayed</param>
        public static void SaveImage(string selectedCategory, string selectedItem, string internalFilePath, BitmapSource selectedBitmap)
        {
            string savePath = Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItem;
            Directory.CreateDirectory(savePath);

            var fullSavePath = Path.Combine(savePath, (Path.GetFileNameWithoutExtension(internalFilePath) + ".bmp"));

            using (var fileStream = new FileStream(fullSavePath, FileMode.Create))
            {
                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(selectedBitmap));
                encoder.Save(fileStream);
            }
        }



        /// <summary>
        /// Saves the currently displayed texture map as a DDS file.
        /// </summary>
        /// <param name="selectedCategory">The items category.</param>
        /// <param name="selectedItem">The currently selected item.</param>
        /// <param name="internalFilePath">The internal file path of the texture map.</param>
        /// <param name="textureMap">The name of the currently selected texture map.</param>
        /// <param name="mtrlData">The items mtrl file data</param>
        /// <param name="texData">The items tex file data</param>
        public static void SaveDDS(string selectedCategory, string selectedItem, string internalFilePath, string textureMap, MTRLData mtrlData, TEXData texData)
        {
            string savePath = Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selectedItem;
            Directory.CreateDirectory(savePath);

            var fullSavePath = Path.Combine(savePath, (Path.GetFileNameWithoutExtension(internalFilePath) + ".dds"));

            List<byte> DDS = new List<byte>();

            if (textureMap.Equals(Strings.ColorSet))
            {
                if (mtrlData.ColorFlags != null)
                {
                    var colorFlagsDir = Path.Combine(savePath, (Path.GetFileNameWithoutExtension(internalFilePath) + ".dat"));
                    File.WriteAllBytes(colorFlagsDir, mtrlData.ColorFlags);
                }

                DDS.AddRange(CreateColorDDSHeader());
                DDS.AddRange(mtrlData.ColorData);
            }
            else
            {
                DDS.AddRange(CreateDDSHeader(texData));
                DDS.AddRange(texData.RawTexData);
            }

            File.WriteAllBytes(fullSavePath, DDS.ToArray());
        }


        /// <summary>
        /// Creates the DDS header for given texture data.
        /// <see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/bb943982(v=vs.85).aspx"/>
        /// </summary>
        /// <param name="texData">The texture data.</param>
        /// <returns>Byte array containing DDS header</returns>
        public static byte[] CreateDDSHeader(TEXData texData)
        {
            uint dwPitchOrLinearSize, pfFlags, dwFourCC;
            List<byte> header = new List<byte>();

            // DDS header magic number
            uint dwMagic = 0x20534444;
            header.AddRange(BitConverter.GetBytes(dwMagic));

            // Size of structure. This member must be set to 124.
            uint dwSize = 124;
            header.AddRange(BitConverter.GetBytes(dwSize));

            // Flags to indicate which members contain valid data.
            uint dwFlags = 528391;
            header.AddRange(BitConverter.GetBytes(dwFlags));

            // Surface height (in pixels).
            uint dwHeight = (uint)texData.Height;
            header.AddRange(BitConverter.GetBytes(dwHeight));

            // Surface width (in pixels).
            uint dwWidth = (uint)texData.Width;
            header.AddRange(BitConverter.GetBytes(dwWidth));

            // The pitch or number of bytes per scan line in an uncompressed texture; the total number of bytes in the top level texture for a compressed texture.
            if (texData.Type == 9312)
            {
                dwPitchOrLinearSize = 512;
            }
            else if (texData.Type == 5200)
            {
                dwPitchOrLinearSize = (uint)((dwHeight * dwWidth) * 4);
            }
            else if (texData.Type == 13344)
            {
                dwPitchOrLinearSize = (uint)((dwHeight * dwWidth) / 2);
            }
            else
            {
                dwPitchOrLinearSize = (uint)(dwHeight * dwWidth);
            }
            header.AddRange(BitConverter.GetBytes(dwPitchOrLinearSize));


            // Depth of a volume texture (in pixels), otherwise unused.
            uint dwDepth = 0;
            header.AddRange(BitConverter.GetBytes(dwDepth));

            // Number of mipmap levels, otherwise unused.
            uint dwMipMapCount = (uint)texData.MipCount;
            header.AddRange(BitConverter.GetBytes(dwMipMapCount));

            // Unused.
            byte[] dwReserved1 = new byte[44];
            Array.Clear(dwReserved1, 0, 44);
            header.AddRange(dwReserved1);

            // DDS_PIXELFORMAT start

            // Structure size; set to 32 (bytes).
            uint pfSize = 32;
            header.AddRange(BitConverter.GetBytes(pfSize));

            // Values which indicate what type of data is in the surface.
            if (texData.Type == 5200)
            {
                pfFlags = 65;
            }
            else
            {
                pfFlags = 4;
            }
            header.AddRange(BitConverter.GetBytes(pfFlags));

            // Four-character codes for specifying compressed or custom formats.
            if (texData.Type == 13344)
            {
                dwFourCC = 0x31545844;
            }
            else if (texData.Type == 13361)
            {
                dwFourCC = 0x35545844;
            }
            else if (texData.Type == 13360)
            {
                dwFourCC = 0x33545844;
            }
            else if (texData.Type == 9312)
            {
                dwFourCC = 0x71;
            }
            else if (texData.Type == 5200)
            {
                dwFourCC = 0;
            }
            else
            {
                return null;
            }
            header.AddRange(BitConverter.GetBytes(dwFourCC));

            if (texData.Type == 5200)
            {
                // Number of bits in an RGB (possibly including alpha) format.
                uint dwRGBBitCount = 32;
                header.AddRange(BitConverter.GetBytes(dwRGBBitCount));

                // Red (or lumiannce or Y) mask for reading color data. 
                uint dwRBitMask = 16711680;
                header.AddRange(BitConverter.GetBytes(dwRBitMask));

                // Green (or U) mask for reading color data.
                uint dwGBitMask = 65280;
                header.AddRange(BitConverter.GetBytes(dwGBitMask));

                // Blue (or V) mask for reading color data.
                uint dwBBitMask = 255;
                header.AddRange(BitConverter.GetBytes(dwBBitMask));

                // Alpha mask for reading alpha data.
                uint dwABitMask = 4278190080;
                header.AddRange(BitConverter.GetBytes(dwABitMask));

                // DDS_PIXELFORMAT End

                // Specifies the complexity of the surfaces stored.
                uint dwCaps = 4096;
                header.AddRange(BitConverter.GetBytes(dwCaps));

                // dwCaps2, dwCaps3, dwCaps4, dwReserved2.
                // Unused.
                byte[] blank1 = new byte[16];
                header.AddRange(blank1);

            }
            else
            {
                // dwRGBBitCount, dwRBitMask, dwGBitMask, dwBBitMask, dwABitMask, dwCaps, dwCaps2, dwCaps3, dwCaps4, dwReserved2.
                // Unused.
                byte[] blank1 = new byte[40];
                header.AddRange(blank1);
            }

            return header.ToArray();
        }

        /// <summary>
        /// Creates the DDS header for given texture data.
        /// <see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/bb943982(v=vs.85).aspx"/>
        /// </summary>
        /// <returns>Byte array containing DDS header</returns>
        public static byte[] CreateColorDDSHeader()
        {
            List<byte> header = new List<byte>();

            // DDS header magic number
            uint dwMagic = 0x20534444;
            header.AddRange(BitConverter.GetBytes(dwMagic));

            // Size of structure. This member must be set to 124.
            uint dwSize = 124;
            header.AddRange(BitConverter.GetBytes(dwSize));

            // Flags to indicate which members contain valid data.
            uint dwFlags = 528399;
            header.AddRange(BitConverter.GetBytes(dwFlags));

            // Surface height (in pixels).
            uint dwHeight = 16;
            header.AddRange(BitConverter.GetBytes(dwHeight));

            // Surface width (in pixels).
            uint dwWidth = 4;
            header.AddRange(BitConverter.GetBytes(dwWidth));

            // The pitch or number of bytes per scan line in an uncompressed texture; the total number of bytes in the top level texture for a compressed texture.
            uint dwPitchOrLinearSize = 512;
            header.AddRange(BitConverter.GetBytes(dwPitchOrLinearSize));

            // Depth of a volume texture (in pixels), otherwise unused.
            uint dwDepth = 0;
            header.AddRange(BitConverter.GetBytes(dwDepth));

            // Number of mipmap levels, otherwise unused.
            uint dwMipMapCount = 0;
            header.AddRange(BitConverter.GetBytes(dwMipMapCount));

            // Unused.
            byte[] dwReserved1 = new byte[44];
            header.AddRange(dwReserved1);

            // DDS_PIXELFORMAT start

            // Structure size; set to 32 (bytes).
            uint pfSize = 32;
            header.AddRange(BitConverter.GetBytes(pfSize));

            // Values which indicate what type of data is in the surface.
            uint pfFlags = 4;
            header.AddRange(BitConverter.GetBytes(pfFlags));

            // Four-character codes for specifying compressed or custom formats.
            uint dwFourCC = 0x71;
            header.AddRange(BitConverter.GetBytes(dwFourCC));

            // dwRGBBitCount, dwRBitMask, dwGBitMask, dwBBitMask, dwABitMask, dwCaps, dwCaps2, dwCaps3, dwCaps4, dwReserved2.
            // Unused.
            byte[] blank1 = new byte[40];
            header.AddRange(blank1);

            return header.ToArray();
        }
    }
}
