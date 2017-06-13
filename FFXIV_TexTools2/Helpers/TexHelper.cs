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
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FFXIV_TexTools2.Helpers
{
    public static class TexHelper
    {

        public static BitmapSource MakeDiffuseMap(Bitmap normalMap, BitmapSource colorMap, TexInfo maskInfo)
        {

            byte[] mPixels = null;
            byte[] nPixels = null;
            List<System.Drawing.Color> colorList = new List<System.Drawing.Color>();

            if (colorMap != null)
            {
                int cstride = (int)colorMap.PixelWidth * (colorMap.Format.BitsPerPixel / 8);
                byte[] cpixels = new byte[colorMap.PixelHeight * cstride];

                colorMap.CopyPixels(cpixels, cstride, 0);

                SortedSet<byte> cb = new SortedSet<byte>();

                for (int i = 0; i < cpixels.Length; i += 16)
                {
                    int red = cpixels[i + 2];
                    int green = cpixels[i + 1];
                    int blue = cpixels[i];
                    int alpha = cpixels[i + 3];

                    colorList.Add(System.Drawing.Color.FromArgb(255, red, green, blue));
                }
            }
            else if(colorMap == null)
            {
                for (int i = 0; i < 1024; i += 16)
                {
                    colorList.Add(System.Drawing.Color.FromArgb(255, 255, 255, 255));
                }
            }


            if(maskInfo != null)
            {
                var maskMap = maskInfo.BMP;
                var nMaskMap = ResizeImage(Image.FromHbitmap(maskMap.GetHbitmap()), maskMap.Width * 2, maskMap.Height * 2);

                var maskData = nMaskMap.LockBits(new Rectangle(0, 0, nMaskMap.Width, nMaskMap.Height), ImageLockMode.ReadOnly, nMaskMap.PixelFormat);
                var mLength = maskData.Stride * maskData.Height;
                mPixels = new byte[mLength];
                Marshal.Copy(maskData.Scan0, mPixels, 0, mLength);
                nMaskMap.UnlockBits(maskData);
            }

            if(normalMap != null)
            {
                var normalData = normalMap.LockBits(new Rectangle(0, 0, normalMap.Width, normalMap.Height), ImageLockMode.ReadOnly, normalMap.PixelFormat);
                var length = normalData.Stride * normalData.Height;
                nPixels = new byte[length];
                Marshal.Copy(normalData.Scan0, nPixels, 0, length);
                normalMap.UnlockBits(normalData);
            }


            List<byte> diffuseMap = new List<byte>();

            SortedSet<byte> b = new SortedSet<byte>();

            float R = 1;
            float G = 1;
            float B = 1;

            for (int i = 3; i < nPixels.Length; i += 4)
            {
                int alpha = nPixels[i - 3];

                if(maskInfo != null)
                {
                    B = mPixels[i - 1] / 255f;
                    R = mPixels[i - 1] / 255f;
                    G = mPixels[i - 1] / 255f;
                }

                System.Drawing.Color mColor;

                if(nPixels[i] >= 0 && nPixels[i] <= 16)
                    {
                    mColor = System.Drawing.Color.FromArgb(alpha, (int)(colorList[0].R * R), (int)(colorList[0].G * G), (int)(colorList[0].B * B));
                    diffuseMap.AddRange(BitConverter.GetBytes(mColor.ToArgb()));
                }
                else if (nPixels[i] > 16 && nPixels[i] <= 32)
                {
                    mColor = System.Drawing.Color.FromArgb(alpha, (int)(colorList[1].R * R), (int)(colorList[1].G * G), (int)(colorList[1].B * B));
                    diffuseMap.AddRange(BitConverter.GetBytes(mColor.ToArgb()));
                }
                else if (nPixels[i] > 32 && nPixels[i] <= 48)
                {
                    mColor = System.Drawing.Color.FromArgb(alpha, (int)(colorList[2].R * R), (int)(colorList[2].G * G), (int)(colorList[2].B * B));
                    diffuseMap.AddRange(BitConverter.GetBytes(mColor.ToArgb()));
                }
                else if (nPixels[i] > 48 && nPixels[i] <= 64)
                {
                    mColor = System.Drawing.Color.FromArgb(alpha, (int)(colorList[3].R * R), (int)(colorList[3].G * G), (int)(colorList[3].B * B));
                    diffuseMap.AddRange(BitConverter.GetBytes(mColor.ToArgb()));
                }
                else if (nPixels[i] > 64 && nPixels[i] <= 80)
                {
                    mColor = System.Drawing.Color.FromArgb(alpha, (int)(colorList[4].R * R), (int)(colorList[4].G * G), (int)(colorList[4].B * B));
                    diffuseMap.AddRange(BitConverter.GetBytes(mColor.ToArgb()));
                }
                else if (nPixels[i] > 80 && nPixels[i] <= 96)
                {
                    mColor = System.Drawing.Color.FromArgb(alpha, (int)(colorList[5].R * R), (int)(colorList[5].G * G), (int)(colorList[5].B * B));
                    diffuseMap.AddRange(BitConverter.GetBytes(mColor.ToArgb()));
                }
                else if (nPixels[i] > 96 && nPixels[i] <= 112)
                {
                    mColor = System.Drawing.Color.FromArgb(alpha, (int)(colorList[6].R * R), (int)(colorList[6].G * G), (int)(colorList[6].B * B));
                    diffuseMap.AddRange(BitConverter.GetBytes(mColor.ToArgb()));
                }
                else if (nPixels[i] > 112 && nPixels[i] <= 128)
                {
                    mColor = System.Drawing.Color.FromArgb(alpha, (int)(colorList[7].R * R), (int)(colorList[7].G * G), (int)(colorList[7].B * B));
                    diffuseMap.AddRange(BitConverter.GetBytes(mColor.ToArgb()));
                }
                else if (nPixels[i] > 128 && nPixels[i] <= 144)
                {
                    mColor = System.Drawing.Color.FromArgb(alpha, (int)(colorList[8].R * R), (int)(colorList[8].G * G), (int)(colorList[8].B * B));
                    diffuseMap.AddRange(BitConverter.GetBytes(mColor.ToArgb()));
                }
                else if (nPixels[i] > 144 && nPixels[i] <= 160)
                {
                    mColor = System.Drawing.Color.FromArgb(alpha, (int)(colorList[9].R * R), (int)(colorList[9].G * G), (int)(colorList[9].B * B));
                    diffuseMap.AddRange(BitConverter.GetBytes(mColor.ToArgb()));
                }
                else if (nPixels[i] > 160 && nPixels[i] <= 176)
                {
                    mColor = System.Drawing.Color.FromArgb(alpha, (int)(colorList[10].R * R), (int)(colorList[10].G * G), (int)(colorList[10].B * B));
                    diffuseMap.AddRange(BitConverter.GetBytes(mColor.ToArgb()));
                }
                else if (nPixels[i] > 176 && nPixels[i] <= 192)
                {
                    mColor = System.Drawing.Color.FromArgb(alpha, (int)(colorList[11].R * R), (int)(colorList[11].G * G), (int)(colorList[11].B * B));
                    diffuseMap.AddRange(BitConverter.GetBytes(mColor.ToArgb()));
                }
                else if (nPixels[i] > 192 && nPixels[i] <= 208)
                {
                    mColor = System.Drawing.Color.FromArgb(alpha, (int)(colorList[12].R * R), (int)(colorList[12].G * G), (int)(colorList[12].B * B));
                    diffuseMap.AddRange(BitConverter.GetBytes(mColor.ToArgb()));
                }
                else if (nPixels[i] > 208 && nPixels[i] <= 224)
                {
                    mColor = System.Drawing.Color.FromArgb(alpha, (int)(colorList[13].R * R), (int)(colorList[13].G * G), (int)(colorList[13].B * B));
                    diffuseMap.AddRange(BitConverter.GetBytes(mColor.ToArgb()));
                }
                else if (nPixels[i] > 224 && nPixels[i] <= 240)
                {
                    mColor = System.Drawing.Color.FromArgb(alpha, (int)(colorList[14].R * R), (int)(colorList[14].G * G), (int)(colorList[14].B * B));
                    diffuseMap.AddRange(BitConverter.GetBytes(mColor.ToArgb()));
                }
                else if (nPixels[i] > 240 && nPixels[i] <= 255)
                {
                    mColor = System.Drawing.Color.FromArgb(alpha, (int)(colorList[15].R * R), (int)(colorList[15].G * G), (int)(colorList[15].B * B));
                    diffuseMap.AddRange(BitConverter.GetBytes(mColor.ToArgb()));
                }
                else
                {
                    mColor = System.Drawing.Color.FromArgb(alpha, 0, 0, 0);
                    diffuseMap.AddRange(BitConverter.GetBytes(mColor.ToArgb()));
                }
            }

            int stride = (int)normalMap.Width * (32 / 8);
            BitmapSource bitmapSource = BitmapSource.Create(normalMap.Width, normalMap.Height, normalMap.HorizontalResolution, normalMap.VerticalResolution, PixelFormats.Bgra32, null, diffuseMap.ToArray(), stride);

            return bitmapSource;
        }

        public static BitmapSource MakeDisplaceMap(TexInfo maskInfo)
        {

            byte[] mPixels = null;


            var maskMap = maskInfo.BMP;
            var nMaskMap = ResizeImage(Image.FromHbitmap(maskMap.GetHbitmap()), maskMap.Width * 2, maskMap.Height * 2);

            var maskData = nMaskMap.LockBits(new Rectangle(0, 0, nMaskMap.Width, nMaskMap.Height), ImageLockMode.ReadOnly, nMaskMap.PixelFormat);
            var mLength = maskData.Stride * maskData.Height;
            mPixels = new byte[mLength];
            Marshal.Copy(maskData.Scan0, mPixels, 0, mLength);
            nMaskMap.UnlockBits(maskData);

            List<byte> displaceList = new List<byte>();

            for (int i = 0; i < mPixels.Length; i += 4)
            {
                var color = System.Drawing.Color.FromArgb(255, mPixels[i], mPixels[i], mPixels[i]);
                displaceList.AddRange(BitConverter.GetBytes(color.ToArgb()));
            }

            int stride = (int)nMaskMap.Width * (32 / 8);
            BitmapSource bitmapSource = BitmapSource.Create(nMaskMap.Width, nMaskMap.Height, nMaskMap.HorizontalResolution, nMaskMap.VerticalResolution, PixelFormats.Bgra32, null, displaceList.ToArray(), stride);

            return bitmapSource;

        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
    }
}
