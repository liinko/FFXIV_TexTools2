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
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace FFXIV_TexTools2.Material
{
    /// <summary>
    /// Handles files with TEX extension
    /// </summary>
    public static class TEX
    {
        /// <summary>
        /// Gets the texture data of an item
        /// </summary>
        /// <param name="offset">The offset of the item</param>
        /// <returns>The texture data</returns>
        public static TEXData GetTex(int offset)
        {
            int datNum = ((offset / 8) & 0x0F) / 2;

            offset = Helper.OffsetCorrection(datNum, offset);
           
            List<byte> decompressedData = new List<byte>();

            TEXData texData = new TEXData();

            using (BinaryReader br = new BinaryReader(File.OpenRead(Info.datDir + datNum)))
            {
                br.BaseStream.Seek(offset, SeekOrigin.Begin);

                int headerLength = br.ReadInt32();
                int fileType = br.ReadInt32();
                int uncompressedFileSize = br.ReadInt32();
                br.ReadBytes(8);
                texData.MipCount = br.ReadInt32();

                int endOfHeader = offset + headerLength;
                int mipMapInfoOffset = offset + 24;

                br.BaseStream.Seek(endOfHeader + 4, SeekOrigin.Begin);

                texData.Type = br.ReadInt32();
                texData.Width = br.ReadInt16();
                texData.Height = br.ReadInt16();

                for (int i = 0, j = 0; i < texData.MipCount; i++)
                {
                    br.BaseStream.Seek(mipMapInfoOffset + j, SeekOrigin.Begin);

                    int offsetFromHeaderEnd = br.ReadInt32();
                    int mipMapLength = br.ReadInt32();
                    int mipMapSize = br.ReadInt32();
                    int mipMapStart = br.ReadInt32();
                    int mipMapParts = br.ReadInt32();

                    int mipMapPartOffset = endOfHeader + offsetFromHeaderEnd;

                    br.BaseStream.Seek(mipMapPartOffset, SeekOrigin.Begin);

                    br.ReadBytes(8);
                    int compressedSize = br.ReadInt32();
                    int uncompressedSize = br.ReadInt32();

                    if (mipMapParts > 1)
                    {
                        byte[] compressedData = br.ReadBytes(compressedSize);
                        byte[] decompressedPartData = new byte[uncompressedSize];

                        using (MemoryStream ms = new MemoryStream(compressedData))
                        {
                            using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
                            {
                                ds.Read(decompressedPartData, 0, uncompressedSize);
                            }
                        }

                        decompressedData.AddRange(decompressedPartData);

                        for (int k = 1; k < mipMapParts; k++)
                        {
                            byte check = br.ReadByte();
                            while (check != 0x10)
                            {
                                check = br.ReadByte();
                            }

                            br.ReadBytes(7);
                            compressedSize = br.ReadInt32();
                            uncompressedSize = br.ReadInt32();

                            if (compressedSize != 32000)
                            {
                                compressedData = br.ReadBytes(compressedSize);
                                decompressedPartData = new byte[uncompressedSize];
                                using (MemoryStream ms = new MemoryStream(compressedData))
                                {
                                    using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
                                    {
                                        ds.Read(decompressedPartData, 0, uncompressedSize);
                                    }
                                }
                                decompressedData.AddRange(decompressedPartData);
                            }
                            else
                            {
                                decompressedPartData = br.ReadBytes(uncompressedSize);
                                decompressedData.AddRange(decompressedPartData);
                            }
                        }
                    }
                    else
                    {
                        if (compressedSize != 32000)
                        {
                            var compressedData = br.ReadBytes(compressedSize);
                            var uncompressedData = new byte[uncompressedSize];

                            using (MemoryStream ms = new MemoryStream(compressedData))
                            {
                                using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
                                {
                                    ds.Read(uncompressedData, 0, uncompressedSize);
                                }
                            }

                            decompressedData.AddRange(uncompressedData);
                        }
                        else
                        {
                            var decompressedPartData = br.ReadBytes(uncompressedSize);
                            decompressedData.AddRange(decompressedPartData);
                        }
                    }
                    j = j + 20;
                }

                if (decompressedData.Count < uncompressedFileSize)
                {
                    int difference = uncompressedFileSize - decompressedData.Count;
                    byte[] padding = new byte[difference];
                    Array.Clear(padding, 0, difference);
                    decompressedData.AddRange(padding);
                }
            }

            texData.BMP = TextureToBitmap(decompressedData.ToArray(), texData.Type, texData.Width, texData.Height);
            texData.TypeString = Info.TextureTypes[texData.Type];
            texData.RawTexData = decompressedData.ToArray();

            return texData;
        }

        public static TEXData GetVFX(int offset)
        {
            int datNum = ((offset / 8) & 0x0F) / 2;

            var VFXData = Helper.GetType2DecompressedData(offset, datNum);

            TEXData texData = new TEXData();

            using(BinaryReader br = new BinaryReader(new MemoryStream(VFXData)))
            {
                br.BaseStream.Seek(4, SeekOrigin.Begin);

                texData.Type = br.ReadInt32();
                texData.Width = br.ReadInt16();
                texData.Height = br.ReadInt16();

                br.ReadBytes(2);

                texData.MipCount = br.ReadInt16();

                br.ReadBytes(64);

                texData.RawTexData = br.ReadBytes(VFXData.Length - 80);

                texData.TypeString = Info.TextureTypes[texData.Type];
                texData.BMP = TextureToBitmap(texData.RawTexData, texData.Type, texData.Width, texData.Height);
            }

            return texData;
        }

        /// <summary>
        /// Creates a bitmap from the texture data.
        /// </summary>
        /// <param name="decompressedData">The decompressed data of the texture.</param>
        /// <param name="textureType">The textures type.</param>
        /// <param name="width">The textures width.</param>
        /// <param name="height">The textures height.</param>
        /// <returns>The created bitmap.</returns>
        public static Bitmap TextureToBitmap(byte[] decompressedData, int textureType, int width, int height)
        {
            Bitmap bmp = null;

            byte[] decompressedTextureData;

            switch (textureType)
            {
                case TextureTypes.DXT1:
                    decompressedTextureData = DxtUtil.DecompressDxt1(decompressedData, width, height);
                    bmp = ReadLinearImage(decompressedTextureData, width, height);
                    break;

                case TextureTypes.DXT3:
                    decompressedTextureData = DxtUtil.DecompressDxt3(decompressedData, width, height);
                    bmp = ReadLinearImage(decompressedTextureData, width, height);
                    break;

                case TextureTypes.DXT5:
                    decompressedTextureData = DxtUtil.DecompressDxt5(decompressedData, width, height);
                    bmp = ReadLinearImage(decompressedTextureData, width, height);
                    break;

                case TextureTypes.A8:
                case TextureTypes.L8:
                    bmp = Read8bitImage(decompressedData, width, height);
                    break;

                case TextureTypes.A4R4G4B4:
                    bmp = Read4444Image(decompressedData, width, height);
                    break;

                case TextureTypes.A1R5G5B5:
                    bmp = Read5551Image(decompressedData, width, height);
                    break;

                case TextureTypes.A8R8G8B8:
                case TextureTypes.X8R8G8B8:
                    bmp = new Bitmap(width, height, width * 4, System.Drawing.Imaging.PixelFormat.Format32bppArgb, Marshal.UnsafeAddrOfPinnedArrayElement(decompressedData, 0));
                    break;

                case TextureTypes.A16B16G16R16F:
                    bmp = ReadRGBAFImage(decompressedData, width, height);
                    break;
            }

            return bmp;
        }

        /// <summary>
        /// Creates bitmap from decompressed Linear texture data.
        /// </summary>
        /// <param name="textureData">The decompressed texture data.</param>
        /// <param name="width">The textures width.</param>
        /// <param name="height">The textures height.</param>
        /// <returns>The created bitmap.</returns>
        private static Bitmap ReadLinearImage(byte[] textureData, int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height);
            using (MemoryStream ms = new MemoryStream(textureData))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            byte red = br.ReadByte();
                            byte green = br.ReadByte();
                            byte blue = br.ReadByte();
                            byte alpha = br.ReadByte();

                            bmp.SetPixel(x, y, Color.FromArgb(alpha, red, green, blue));
                        }
                    }
                }
            }
            return bmp;
        }

        /// <summary>
        /// Creates bitmap from decompressed RGBAF texture data.
        /// </summary>
        /// <param name="textureData">The decompressed texture data.</param>
        /// <param name="width">The textures width.</param>
        /// <param name="height">The textures height.</param>
        /// <returns>The created bitmap.</returns>
        private static Bitmap ReadRGBAFImage(byte[] textureData, int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height);
            using (MemoryStream ms = new MemoryStream(textureData))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            ushort red = BitConverter.ToUInt16(br.ReadBytes(2), 0);
                            Half hRed = Half.ToHalf(red);

                            ushort green = BitConverter.ToUInt16(br.ReadBytes(2), 0);
                            Half hGreen = Half.ToHalf(green);

                            ushort blue = BitConverter.ToUInt16(br.ReadBytes(2), 0);
                            Half hBlue = Half.ToHalf(blue);

                            ushort alpha = BitConverter.ToUInt16(br.ReadBytes(2), 0);
                            Half hAlpha = Half.ToHalf(alpha);

                            if (hRed > 1)
                            {
                                hRed = 1;
                            }
                            else if (hRed < 0)
                            {
                                hRed = 0;
                            }
                            if (hGreen > 1)
                            {
                                hGreen = 1;
                            }
                            else if (hGreen < 0)
                            {
                                hGreen = 0;
                            }
                            if (hBlue > 1)
                            {
                                hBlue = 1;
                            }
                            else if (hBlue < 0)
                            {
                                hBlue = 0;
                            }
                            if (hAlpha > 1)
                            {
                                hAlpha = 1;
                            }
                            else if (hAlpha < 0)
                            {
                                hAlpha = 0;
                            }

                            bmp.SetPixel(x, y, Color.FromArgb((int)(hAlpha * 255), (int)(hRed * 255), (int)(hGreen * 255), (int)(hBlue * 255)));
                        }
                    }
                }
            }
            return bmp;
        }

        /// <summary>
        /// Creates bitmap from decompressed A4R4G4B4 texture data.
        /// </summary>
        /// <param name="textureData">The decompressed texture data.</param>
        /// <param name="width">The textures width.</param>
        /// <param name="height">The textures height.</param>
        /// <returns>The created bitmap.</returns>
        private static Bitmap Read4444Image(byte[] textureData, int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height);
            using (MemoryStream ms = new MemoryStream(textureData))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int pixel = br.ReadUInt16() & 0xFFFF;
                            int red = ((pixel & 0xF)) * 16;
                            int green = ((pixel & 0xF0 >> 4)) * 16;
                            int blue = ((pixel & 0xF00 >> 8)) * 16;
                            int alpha = ((pixel & 0xF000 >> 12)) * 16;

                            bmp.SetPixel(x, y, Color.FromArgb(alpha, red, green, blue));
                        }
                    }
                }
            }
            return bmp;
        }

        /// <summary>
        /// Creates bitmap from decompressed A1R5G5B5 texture data.
        /// </summary>
        /// <param name="textureData">The decompressed texture data.</param>
        /// <param name="width">The textures width.</param>
        /// <param name="height">The textures height.</param>
        /// <returns>The created bitmap.</returns>
        private static Bitmap Read5551Image(byte[] textureData, int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height);
            using (MemoryStream ms = new MemoryStream(textureData))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int pixel = br.ReadUInt16() & 0xFFFF;
                            int red = ((pixel & 0x7E00 >> 10)) * 8;
                            int green = ((pixel & 0x3E0 >> 5)) * 8;
                            int blue = ((pixel & 0x1F)) * 8;
                            int alpha = ((pixel & 0x80000 >> 15)) * 255;

                            bmp.SetPixel(x, y, Color.FromArgb(alpha, red, green, blue));
                        }
                    }
                }
            }
            return bmp;
        }


        /// <summary>
        /// Creates bitmap from decompressed A8/L8 texture data.
        /// </summary>
        /// <param name="textureData">The decompressed texture data.</param>
        /// <param name="width">The textures width.</param>
        /// <param name="height">The textures height.</param>
        /// <returns>The created bitmap.</returns>
        private static Bitmap Read8bitImage(byte[] textureData, int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height);

            using (MemoryStream ms = new MemoryStream(textureData))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int pixel = br.ReadByte() & 0xFF;

                            int red = pixel;
                            int green = pixel;
                            int blue = pixel;
                            byte alpha = 255;

                            bmp.SetPixel(x, y, Color.FromArgb(alpha, red, green, blue));
                        }
                    }
                }
            }
            return bmp;
        }
    }
}
