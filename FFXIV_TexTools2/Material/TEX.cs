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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using FFXIV_TexTools2.Helpers;
using Microsoft.Xna.Framework.Graphics;

namespace FFXIV_TexTools2.Material
{
    public static class TEX
    {
        public static TexInfo GetTex(int offset)
        {
            int textureType, width, height, mipMapCount;

            int datNum = ((offset / 8) & 0x000f) / 2;

            offset = Helper.OffsetCorrection(datNum, offset);
           
            List<byte> byteList = new List<byte>();

            TexInfo info = new TexInfo();

            using (BinaryReader br = new BinaryReader(File.OpenRead(Info.datDir + datNum)))
            {

                br.BaseStream.Seek(offset, SeekOrigin.Begin);

                int headerLength = br.ReadInt32();
                int type = br.ReadInt32();
                int decompSize = br.ReadInt32();
                br.ReadBytes(8);
                mipMapCount = br.ReadInt32();


                int endOfHeader = offset + headerLength;
                int mipMapInfoStart = offset + 24;

                br.BaseStream.Seek(endOfHeader + 4, SeekOrigin.Begin);

                textureType = br.ReadInt32();
                width = br.ReadInt16();
                height = br.ReadInt16();

                for (int i = 0, j = 0; i < mipMapCount; i++)
                {
                    br.BaseStream.Seek(mipMapInfoStart + j, SeekOrigin.Begin);

                    int offsetFromHeaderEnd = br.ReadInt32();
                    int mipMapLength = br.ReadInt32();
                    int mipMapSize = br.ReadInt32();
                    int mipMapStart = br.ReadInt32();
                    int mipMapParts = br.ReadInt32();

                    int mipMapOffset = endOfHeader + offsetFromHeaderEnd;

                    br.BaseStream.Seek(mipMapOffset, SeekOrigin.Begin);

                    br.ReadBytes(8);
                    int compressedSize = br.ReadInt32();
                    int decompressedSize = br.ReadInt32();

                    if (mipMapParts > 1)
                    {
                        byte[] compressedData = br.ReadBytes(compressedSize);
                        byte[] decompressedData = new byte[decompressedSize];

                        using (MemoryStream ms = new MemoryStream(compressedData))
                        {
                            using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
                            {
                                ds.Read(decompressedData, 0x00, decompressedSize);
                            }
                        }

                        byteList.AddRange(decompressedData);

                        //start MipMap Parts Read
                        for (int k = 1; k < mipMapParts; k++)
                        {
                            byte check = br.ReadByte();
                            while (check != 0x10)
                            {
                                check = br.ReadByte();
                            }

                            br.ReadBytes(7);
                            compressedSize = br.ReadInt32();
                            decompressedSize = br.ReadInt32();

                            if(compressedSize != 32000)
                            {
                                compressedData = br.ReadBytes(compressedSize);
                                decompressedData = new byte[decompressedSize];
                                using (MemoryStream ms = new MemoryStream(compressedData))
                                {
                                    using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
                                    {
                                        ds.Read(decompressedData, 0x00, decompressedSize);
                                    }
                                }
                                byteList.AddRange(decompressedData);
                            }
                            else
                            {
                                decompressedData = br.ReadBytes(decompressedSize);
                                byteList.AddRange(decompressedData);
                            }
                        }
                    }
                    else
                    {
                        byte[] compressedData, decompressedData;

                        if (compressedSize != 32000)
                        {
                            compressedData = br.ReadBytes(compressedSize);
                            decompressedData = new byte[decompressedSize];

                            using (MemoryStream ms = new MemoryStream(compressedData))
                            {
                                using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
                                {
                                    ds.Read(decompressedData, 0x00, decompressedSize);
                                }
                            }

                            byteList.AddRange(decompressedData);
                        }
                        else
                        {
                            decompressedData = br.ReadBytes(decompressedSize);
                            byteList.AddRange(decompressedData);
                        }
                    }
                    j = j + 20;
                }

                if (byteList.Count < decompSize)
                {
                    int difference = decompSize - byteList.Count;
                    byte[] padd = new byte[difference];
                    Array.Clear(padd, 0, difference);
                    byteList.AddRange(padd);
                }
            }

            Bitmap bmp = TextureToBitmap(byteList.ToArray(), textureType, width, height);

            info.Width = width;
            info.Height = height;
            info.Type = textureType;
            info.BMP = bmp;
            info.TypeString = GetTextureType(textureType);
            info.RawTexData = byteList.ToArray();
            info.MipCount = mipMapCount;

            return info;
        }


        public static Bitmap TextureToBitmap(byte[] decompressedData, int textureType, int width, int height)
        {
            Bitmap bmp = null;

            byte[] decompressedTexture;

            switch (textureType)
            {
                //DXT1
                case 13344:
                    decompressedTexture = DxtUtil.DecompressDxt1(decompressedData, width, height);
                    bmp = ReadLinearImage(decompressedTexture, width, height);
                    break;
                //DXT3
                case 13360:
                    decompressedTexture = DxtUtil.DecompressDxt3(decompressedData, width, height);
                    bmp = ReadLinearImage(decompressedTexture, width, height);
                    break;
                //DXT5	
                case 13361:
                    decompressedTexture = DxtUtil.DecompressDxt5(decompressedData, width, height);
                    bmp = ReadLinearImage(decompressedTexture, width, height);
                    break;
                //D3DFMT_A8	
                case 4401:
                //D3DFMT_L8
                case 4400:
                    bmp = Read8bitImage(decompressedData, width, height);
                    break;
                //D3DFMT_A4R4G4B4
                case 5184:
                    bmp = Read4444Image(decompressedData, width, height);
                    break;
                //16-bit image in RGB5551 format
                case 5185:
                    bmp = Read5551Image(decompressedData, width, height);
                    break;
                //D3DFMT_A8R8G8B8
                case 5200:
                case 4440:
                //D3DFMT_X8R8G8B8	
                case 5201:
                    bmp = new Bitmap(width, height, width * 4, System.Drawing.Imaging.PixelFormat.Format32bppArgb, Marshal.UnsafeAddrOfPinnedArrayElement(decompressedData, 0));
                    break;
                //D3DFMT_A16B16G16R16F
                case 9312:
                    bmp = ReadRGBAFImage(decompressedData, width, height);
                    break;
            }

            return bmp;
        }

        private static string GetTextureType(int textureType)
        {
            string typeString = "None";

            switch (textureType)
            {
                case 4400:
                    typeString = "L8";
                    break;
                case 4401:
                    typeString = "A8";
                    break;
                //Unknown
                case 4440:
                    typeString = "Unknown";
                    break;
                case 5184:
                    typeString = "A4R4G4B4";
                    break;
                case 5185:
                    typeString = "A1R5G5B5";
                    break;
                case 5200:
                    typeString = "A8R8G8B8";
                    break;	
                case 5201:
                    typeString = "X8R8G8B8";
                    break;
                case 8528:
                    typeString = "R32F";
                    break;
                case 8784:
                    typeString = "G16R16F";
                    break;
                case 8800:
                    typeString = "G32R32F";
                    break;
                case 9312:
                    typeString = "A16B16G16R16F";
                    break;
                case 9328:
                    typeString = "A32B32G32R32F";
                    break;
                case 13344:
                    typeString = "DXT1";
                    break;
                case 13360:
                    typeString = "DXT3";
                    break;
                case 13361:
                    typeString = "DXT5";
                    break;
                case 16704:
                    typeString = "D16";
                    break;
            }

            return typeString;
        }

        private static Bitmap ReadLinearImage(byte[] data, int w, int h)
        {
            Bitmap res = new Bitmap(w, h);
            using (MemoryStream ms = new MemoryStream(data))
            {
                using (BinaryReader r = new BinaryReader(ms))
                {
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            byte red = r.ReadByte();
                            byte green = r.ReadByte();
                            byte blue = r.ReadByte();
                            byte alpha = r.ReadByte();

                            res.SetPixel(x, y, Color.FromArgb(alpha, red, green, blue));
                        }
                    }
                }
            }
            return res;
        }

        private static Bitmap ReadRGBAFImage(byte[] data, int w, int h)
        {
            Bitmap res = new Bitmap(w, h);
            using (MemoryStream ms = new MemoryStream(data))
            {
                using (BinaryReader r = new BinaryReader(ms))
                {
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            ushort sred = BitConverter.ToUInt16(r.ReadBytes(2), 0);
                            Half h1 = Half.ToHalf(sred);
                            ushort sgreen = BitConverter.ToUInt16(r.ReadBytes(2), 0);
                            Half h2 = Half.ToHalf(sgreen);
                            ushort sblue = BitConverter.ToUInt16(r.ReadBytes(2), 0);
                            Half h3 = Half.ToHalf(sblue);
                            ushort salpha = BitConverter.ToUInt16(r.ReadBytes(2), 0);
                            Half h4 = Half.ToHalf(salpha);
                            if (h1 > 1)
                            {
                                h1 = 1;
                            }
                            else if (h1 < 0)
                            {
                                h1 = 0;
                            }
                            if (h2 > 1)
                            {
                                h2 = 1;
                            }
                            else if (h2 < 0)
                            {
                                h2 = 0;
                            }
                            if (h3 > 1)
                            {
                                h3 = 1;
                            }
                            else if (h3 < 0)
                            {
                                h3 = 0;
                            }
                            if (h4 > 1)
                            {
                                h4 = 1;
                            }
                            else if (h4 < 0)
                            {
                                h4 = 0;
                            }

                            res.SetPixel(x, y, Color.FromArgb((int)(h4 * 255), (int)(h1 * 255), (int)(h2 * 255), (int)(h3 * 255)));
                        }
                    }
                }
            }
            return res;
        }

        private static Bitmap Read4444Image(byte[] data, int w, int h)
        {
            Bitmap res = new Bitmap(w, h);
            using (MemoryStream ms = new MemoryStream(data))
            {
                using (BinaryReader r = new BinaryReader(ms))
                {
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            int pixel = r.ReadUInt16() & 0xffff;
                            int red = ((pixel & 0xF)) * 16;
                            int green = ((pixel & 0xF0 >> 4)) * 16;
                            int blue = ((pixel & 0xF00 >> 8)) * 16;
                            int alpha = ((pixel & 0xF000 >> 12)) * 16;

                            res.SetPixel(x, y, Color.FromArgb(alpha, red, green, blue));
                        }
                    }
                }
            }
            return res;
        }

        private static Bitmap Read5551Image(byte[] data, int w, int h)
        {
            Bitmap res = new Bitmap(w, h);
            using (MemoryStream ms = new MemoryStream(data))
            {
                using (BinaryReader r = new BinaryReader(ms))
                {
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            int pixel = r.ReadUInt16() & 0xffff;
                            int red = ((pixel & 0x7e00 >> 10)) * 8;
                            int green = ((pixel & 0x3E0 >> 5)) * 8;
                            int blue = ((pixel & 0x1F)) * 8;
                            int alpha = ((pixel & 0x80000 >> 15)) * 255;

                            res.SetPixel(x, y, Color.FromArgb(alpha, red, green, blue));
                        }
                    }
                }
            }
            return res;
        }


        private static Bitmap Read8bitImage(byte[] data, int w, int h)
        {
            Bitmap res = new Bitmap(w, h);

            using (MemoryStream ms = new MemoryStream(data))
            {
                using (BinaryReader r = new BinaryReader(ms))
                {
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            int pixel = r.ReadByte() & 0xff;

                            int red = pixel;
                            int green = pixel;
                            int blue = pixel;
                            byte alpha = 255;

                            res.SetPixel(x, y, Color.FromArgb(alpha, red, green, blue));
                        }
                    }
                }
            }
            return res;
        }
    }
}
