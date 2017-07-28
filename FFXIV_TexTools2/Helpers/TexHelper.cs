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
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FFXIV_TexTools2.Helpers
{
    public static class TexHelper
    {
        /// <summary>
        /// Creates the bitmap data for the model
        /// </summary>
        /// <remarks>
        /// Because the original textures use channel packing, this method gets the pixel data of each texture
        /// and then recombines them to create the unpacked textures to use in the 3D model.
        /// <see cref="http://wiki.polycount.com/wiki/ChannelPacking"/>
        /// </remarks>
        /// <param name="normalTexData">The texture data of the normal map</param>
        /// <param name="diffuseTexData">The texture data of the diffuse map</param>
        /// <param name="maskTexData">The texture data of the mask map</param>
        /// <param name="specularTexData">The texture data of the specular map</param>
        /// <param name="colorMap">The bitmap of the color map</param>
        /// <returns>An array of bitmaps to be used on the model</returns>
        public static BitmapSource[] MakeModelTextureMaps(TEXData normalTexData, TEXData diffuseTexData, TEXData maskTexData, TEXData specularTexData, BitmapSource colorMap)
        {
            int height = normalTexData.Height;
            int width = normalTexData.Width;
            int tSize = height * width;
            Bitmap normalBitmap = null;

            if (diffuseTexData != null && (diffuseTexData.Height * diffuseTexData.Width) > tSize)
            {
                height = diffuseTexData.Height;
                width = diffuseTexData.Width;
                tSize = height * width;
            }

            if (maskTexData != null && (maskTexData.Height * maskTexData.Width) > tSize)
            {
                height = maskTexData.Height;
                width = maskTexData.Width;
                tSize = height * width;
            }

            if (specularTexData != null && (specularTexData.Height * specularTexData.Width) > tSize)
            {
                height = specularTexData.Height;
                width = specularTexData.Width;
                tSize = height * width;
            }

            byte[] maskPixels = null;
            byte[] specularPixels = null;
            byte[] normalPixels = null;
            byte[] diffusePixels = null;

            List<System.Drawing.Color> colorList = new List<System.Drawing.Color>();
            List<System.Drawing.Color> specularList = new List<System.Drawing.Color>();

            BitmapSource[] texBitmaps = new BitmapSource[4];

            if (colorMap != null)
            {
                int colorSetStride = colorMap.PixelWidth * (colorMap.Format.BitsPerPixel / 8);
                byte[] colorPixels = new byte[colorMap.PixelHeight * colorSetStride];

                colorMap.CopyPixels(colorPixels, colorSetStride, 0);

                for (int i = 0; i < colorPixels.Length; i += 16)
                {
                    int red = colorPixels[i + 2];
                    int green = colorPixels[i + 1];
                    int blue = colorPixels[i];
                    int alpha = colorPixels[i + 3];

                    colorList.Add(System.Drawing.Color.FromArgb(255, red, green, blue));

                    red = colorPixels[i + 6];
                    green = colorPixels[i + 5];
                    blue = colorPixels[i + 4];
                    alpha = colorPixels[i + 7];

                    specularList.Add(System.Drawing.Color.FromArgb(255, red, green, blue));
                }
            }
            else if (colorMap == null)
            {
                for (int i = 0; i < 1024; i += 16)
                {
                    colorList.Add(System.Drawing.Color.FromArgb(255, 255, 255, 255));
                }
            }


            if (maskTexData != null)
            {
                if (tSize > (maskTexData.Height * maskTexData.Width))
                {
                    var maskBitmap = ResizeImage(Image.FromHbitmap(maskTexData.BMP.GetHbitmap()), width, height);
                    var maskData = maskBitmap.LockBits(new System.Drawing.Rectangle(0, 0, maskBitmap.Width, maskBitmap.Height), ImageLockMode.ReadOnly, maskBitmap.PixelFormat);
                    var bitmapLength = maskData.Stride * maskData.Height;
                    maskPixels = new byte[bitmapLength];
                    Marshal.Copy(maskData.Scan0, maskPixels, 0, bitmapLength);
                    maskBitmap.UnlockBits(maskData);
                }
                else
                {
                    var maskData = maskTexData.BMP.LockBits(new System.Drawing.Rectangle(0, 0, maskTexData.Width, maskTexData.Height), ImageLockMode.ReadOnly, maskTexData.BMP.PixelFormat);
                    var bitmapLength = maskData.Stride * maskData.Height;
                    maskPixels = new byte[bitmapLength];
                    Marshal.Copy(maskData.Scan0, maskPixels, 0, bitmapLength);
                    maskTexData.BMP.UnlockBits(maskData);
                }
            }

            if (diffuseTexData != null)
            {
                if (tSize > (diffuseTexData.Height * diffuseTexData.Width))
                {
                    var diffuseBitmap = ResizeImage(Image.FromHbitmap(diffuseTexData.BMP.GetHbitmap()), width, height);
                    var diffData = diffuseBitmap.LockBits(new System.Drawing.Rectangle(0, 0, diffuseBitmap.Width, diffuseBitmap.Height), ImageLockMode.ReadOnly, diffuseBitmap.PixelFormat);
                    var bitmapLength = diffData.Stride * diffData.Height;
                    diffusePixels = new byte[bitmapLength];
                    Marshal.Copy(diffData.Scan0, diffusePixels, 0, bitmapLength);
                    diffuseBitmap.UnlockBits(diffData);
                }
                else
                {
                    var diffData = diffuseTexData.BMP.LockBits(new System.Drawing.Rectangle(0, 0, diffuseTexData.Width, diffuseTexData.Height), ImageLockMode.ReadOnly, diffuseTexData.BMP.PixelFormat);
                    var bitmapLength = diffData.Stride * diffData.Height;
                    diffusePixels = new byte[bitmapLength];
                    Marshal.Copy(diffData.Scan0, diffusePixels, 0, bitmapLength);
                    diffuseTexData.BMP.UnlockBits(diffData);
                }

            }

            if (specularTexData != null)
            {
                if (tSize > (specularTexData.Height * specularTexData.Width))
                {
                    var specularBitmap = ResizeImage(Image.FromHbitmap(specularTexData.BMP.GetHbitmap()), width, height);
                    var specData = specularBitmap.LockBits(new System.Drawing.Rectangle(0, 0, specularBitmap.Width, specularBitmap.Height), ImageLockMode.ReadOnly, specularBitmap.PixelFormat);
                    var bitmapLength = specData.Stride * specData.Height;
                    specularPixels = new byte[bitmapLength];
                    Marshal.Copy(specData.Scan0, specularPixels, 0, bitmapLength);
                    specularBitmap.UnlockBits(specData);
                }
                else
                {
                    var specData = specularTexData.BMP.LockBits(new System.Drawing.Rectangle(0, 0, specularTexData.BMP.Width, specularTexData.BMP.Height), ImageLockMode.ReadOnly, specularTexData.BMP.PixelFormat);
                    var bitmapLength = specData.Stride * specData.Height;
                    specularPixels = new byte[bitmapLength];
                    Marshal.Copy(specData.Scan0, specularPixels, 0, bitmapLength);
                    specularTexData.BMP.UnlockBits(specData);
                }
            }

            if (normalTexData != null)
            {
                if (tSize > (normalTexData.Height * normalTexData.Height))
                {
                    var normBitmap = ResizeImage(Image.FromHbitmap(normalTexData.BMP.GetHbitmap()), width, height);
                    var normalData = normBitmap.LockBits(new System.Drawing.Rectangle(0, 0, normBitmap.Width, normBitmap.Height), ImageLockMode.ReadOnly, normBitmap.PixelFormat);
                    var bitmapLength = normalData.Stride * normalData.Height;
                    normalPixels = new byte[bitmapLength];
                    Marshal.Copy(normalData.Scan0, normalPixels, 0, bitmapLength);
                    normBitmap.UnlockBits(normalData);
                    normalBitmap = normBitmap;
                }
                else
                {
                    var normalData = normalTexData.BMP.LockBits(new System.Drawing.Rectangle(0, 0, normalTexData.Width, normalTexData.Height), ImageLockMode.ReadOnly, normalTexData.BMP.PixelFormat);
                    var bitmapLength = normalData.Stride * normalData.Height;
                    normalPixels = new byte[bitmapLength];
                    Marshal.Copy(normalData.Scan0, normalPixels, 0, bitmapLength);
                    normalTexData.BMP.UnlockBits(normalData);
                    normalBitmap = normalTexData.BMP;
                }
            }

            List<byte> diffuseMap = new List<byte>();
            List<byte> specularMap = new List<byte>();
            List<byte> alphaMap = new List<byte>();

            float R = 1;
            float G = 1;
            float B = 1;
            float R1 = 1;
            float G1 = 1;
            float B1 = 1;

            for (int i = 3; i < normalPixels.Length; i += 4)
            {
                int alpha = normalPixels[i - 3];

                if (maskTexData != null)
                {
                    B = maskPixels[i - 1];
                    R = maskPixels[i - 1];
                    G = maskPixels[i - 1];

                    B1 = maskPixels[i - 3];
                    R1 = maskPixels[i - 3];
                    G1 = maskPixels[i - 3];
                }
                else
                {
                    B = diffusePixels[i - 3];
                    G = diffusePixels[i - 2];
                    R = diffusePixels[i - 1];

                    B1 = specularPixels[i - 2];
                    G1 = specularPixels[i - 2];
                    R1 = specularPixels[i - 2];
                }

                float pixel = (normalPixels[i] / 255f) * 15f;
                int colorLoc = (int)Math.Floor(pixel + 0.5f);

                System.Drawing.Color diffuseColor;
                System.Drawing.Color specularColor;
                System.Drawing.Color alphaColor;

                diffuseColor = System.Drawing.Color.FromArgb(alpha, (int)((colorList[colorLoc].R / 255f) * R), (int)((colorList[colorLoc].G / 255f) * G), (int)((colorList[colorLoc].B / 255f) * B));
                specularColor = System.Drawing.Color.FromArgb(255, (int)((specularList[colorLoc].R / 255f) * R1), (int)((specularList[colorLoc].G / 255f) * G1), (int)((specularList[colorLoc].B / 255f) * B1));
                alphaColor = System.Drawing.Color.FromArgb(255, alpha, alpha, alpha);

                diffuseMap.AddRange(BitConverter.GetBytes(diffuseColor.ToArgb()));
                specularMap.AddRange(BitConverter.GetBytes(specularColor.ToArgb()));
                alphaMap.AddRange(BitConverter.GetBytes(alphaColor.ToArgb()));
            }

            int stride = normalBitmap.Width * (32 / 8);

            BitmapSource bitmapSource = BitmapSource.Create(width, height, normalBitmap.HorizontalResolution, normalBitmap.VerticalResolution, PixelFormats.Bgra32, null, diffuseMap.ToArray(), stride);
            texBitmaps[0] = bitmapSource;

            bitmapSource = BitmapSource.Create(width, height, normalBitmap.HorizontalResolution, normalBitmap.VerticalResolution, PixelFormats.Bgra32, null, specularMap.ToArray(), stride);
            texBitmaps[1] = bitmapSource;

            var noAlphaNormal = SetAlpha(normalBitmap, 255);
            texBitmaps[2] = Imaging.CreateBitmapSourceFromHBitmap(noAlphaNormal.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            bitmapSource = BitmapSource.Create(width, height, normalBitmap.HorizontalResolution, normalBitmap.VerticalResolution, PixelFormats.Bgra32, null, alphaMap.ToArray(), stride);
            texBitmaps[3] = bitmapSource;


            if (normalTexData != null)
            {
                normalTexData.Dispose();
            }

            if (normalBitmap != null)
            {
                normalBitmap.Dispose();
            }

            if (diffuseTexData != null)
            {
                diffuseTexData.Dispose();
            }

            if (maskTexData != null)
            {
                maskTexData.Dispose();
            }

            if (specularTexData != null)
            {
                specularTexData.Dispose();
            }

            return texBitmaps;
        }


        /// <summary>
        /// Creates the bitmap data for the models in the character category
        /// </summary>
        /// <remarks>
        /// Because the original textures use channel packing, this method gets the pixel data of each texture
        /// and then recombines them to create the unpacked textures to use in the 3D model.
        /// <see cref="http://wiki.polycount.com/wiki/ChannelPacking"/>
        /// </remarks>
        /// <param name="normalTexData">The texture data of the normal map</param>
        /// <param name="diffuseTexData">The texture data of the diffuse map</param>
        /// <param name="maskTexData">The texture data of the mask map</param>
        /// <param name="specularTexData">The texture data of the normal map</param>
        /// <returns>An array of bitmaps to be used on the model</returns>
        public static BitmapSource[] MakeCharacterMaps(TEXData normalTexData, TEXData diffuseTexData, TEXData maskTexData, TEXData specularTexData)
        {
            int height = normalTexData.Height;
            int width = normalTexData.Width;
            int tSize = height * width;
            Bitmap normalBitmap = null;

            if (diffuseTexData != null && (diffuseTexData.Height * diffuseTexData.Width) > tSize)
            {
                height = diffuseTexData.Height;
                width = diffuseTexData.Width;
                tSize = height * width;
            }

            if (maskTexData != null && (maskTexData.Height * maskTexData.Width) > tSize)
            {
                height = maskTexData.Height;
                width = maskTexData.Width;
                tSize = height * width;
            }

            if (specularTexData != null && (specularTexData.Height * specularTexData.Width) > tSize)
            {
                height = specularTexData.Height;
                width = specularTexData.Width;
                tSize = height * width;
            }

            byte[] maskPixels = null;
            byte[] specularPixels = null;
            byte[] normalPixels = null;
            byte[] diffusePixels = null;

            BitmapSource[] texBitmaps = new BitmapSource[4];

            if (maskTexData != null)
            {
                if (tSize > (maskTexData.Height * maskTexData.Width))
                {
                    var maskBitmap = ResizeImage(Image.FromHbitmap(maskTexData.BMP.GetHbitmap()), width, height);
                    var maskData = maskBitmap.LockBits(new System.Drawing.Rectangle(0, 0, maskBitmap.Width, maskBitmap.Height), ImageLockMode.ReadOnly, maskBitmap.PixelFormat);
                    var bitmapLength = maskData.Stride * maskData.Height;
                    maskPixels = new byte[bitmapLength];
                    Marshal.Copy(maskData.Scan0, maskPixels, 0, bitmapLength);
                    maskBitmap.UnlockBits(maskData);
                }
                else
                {
                    var maskData = maskTexData.BMP.LockBits(new System.Drawing.Rectangle(0, 0, maskTexData.Width, maskTexData.Height), ImageLockMode.ReadOnly, maskTexData.BMP.PixelFormat);
                    var bitmapLength = maskData.Stride * maskData.Height;
                    maskPixels = new byte[bitmapLength];
                    Marshal.Copy(maskData.Scan0, maskPixels, 0, bitmapLength);
                    maskTexData.BMP.UnlockBits(maskData);
                }
            }

            if (diffuseTexData != null)
            {
                try
                {
                    if (tSize > (diffuseTexData.Height * diffuseTexData.Width))
                    {
                        var diffuseBitmap = ResizeImage(Image.FromHbitmap(diffuseTexData.BMP.GetHbitmap()), width, height);
                        var diffuseData = diffuseBitmap.LockBits(new System.Drawing.Rectangle(0, 0, diffuseBitmap.Width, diffuseBitmap.Height), ImageLockMode.ReadOnly, diffuseBitmap.PixelFormat);
                        var bitmapLength = diffuseData.Stride * diffuseData.Height;
                        diffusePixels = new byte[bitmapLength];
                        Marshal.Copy(diffuseData.Scan0, diffusePixels, 0, bitmapLength);
                        diffuseBitmap.UnlockBits(diffuseData);
                    }
                    else
                    {
                        var diffuseData = diffuseTexData.BMP.LockBits(new System.Drawing.Rectangle(0, 0, diffuseTexData.Width, diffuseTexData.Height), ImageLockMode.ReadOnly, diffuseTexData.BMP.PixelFormat);
                        var bitmapLength = diffuseData.Stride * diffuseData.Height;
                        diffusePixels = new byte[bitmapLength];
                        Marshal.Copy(diffuseData.Scan0, diffusePixels, 0, bitmapLength);
                        diffuseTexData.BMP.UnlockBits(diffuseData);
                    }

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);
                }
            }

            if (specularTexData != null)
            {
                try
                {
                    if (tSize > (specularTexData.Height * specularTexData.Width))
                    {
                        var specularBitmap = ResizeImage(Image.FromHbitmap(specularTexData.BMP.GetHbitmap()), width, height);
                        var specularData = specularBitmap.LockBits(new System.Drawing.Rectangle(0, 0, specularBitmap.Width, specularBitmap.Height), ImageLockMode.ReadOnly, specularBitmap.PixelFormat);
                        var bitmapLength = specularData.Stride * specularData.Height;
                        specularPixels = new byte[bitmapLength];
                        Marshal.Copy(specularData.Scan0, specularPixels, 0, bitmapLength);
                        specularBitmap.UnlockBits(specularData);
                    }
                    else
                    {
                        var specularData = specularTexData.BMP.LockBits(new System.Drawing.Rectangle(0, 0, specularTexData.BMP.Width, specularTexData.BMP.Height), ImageLockMode.ReadOnly, specularTexData.BMP.PixelFormat);
                        var bitmapLength = specularData.Stride * specularData.Height;
                        specularPixels = new byte[bitmapLength];
                        Marshal.Copy(specularData.Scan0, specularPixels, 0, bitmapLength);
                        specularTexData.BMP.UnlockBits(specularData);
                    }

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);
                }

            }


            if (normalTexData != null)
            {
                if (tSize > (normalTexData.Height * normalTexData.Width))
                {
                    var normBitmap = ResizeImage(Image.FromHbitmap(normalTexData.BMP.GetHbitmap()), width, height);
                    var normData = normBitmap.LockBits(new System.Drawing.Rectangle(0, 0, normBitmap.Width, normBitmap.Height), ImageLockMode.ReadOnly, normBitmap.PixelFormat);
                    var bitmapLength = normData.Stride * normData.Height;
                    normalPixels = new byte[bitmapLength];
                    Marshal.Copy(normData.Scan0, normalPixels, 0, bitmapLength);
                    normBitmap.UnlockBits(normData);
                    normalBitmap = normBitmap;
                }
                else
                {
                    var normData = normalTexData.BMP.LockBits(new System.Drawing.Rectangle(0, 0, normalTexData.Width, normalTexData.Height), ImageLockMode.ReadOnly, normalTexData.BMP.PixelFormat);
                    var bitmapLength = normData.Stride * normData.Height;
                    normalPixels = new byte[bitmapLength];
                    Marshal.Copy(normData.Scan0, normalPixels, 0, bitmapLength);
                    normalTexData.BMP.UnlockBits(normData);
                    normalBitmap = normalTexData.BMP;
                }
            }

            List<byte> diffuseMap = new List<byte>();
            List<byte> specularMap = new List<byte>();
            List<byte> alphaMap = new List<byte>();

            BitmapSource bitmapSource;

            System.Drawing.Color diffuseColor;
            System.Drawing.Color specularColor;
            System.Drawing.Color alphaColor;

            int stride = normalBitmap.Width * (32 / 8);

            if (diffuseTexData == null)
            {
                for (int i = 3; i < normalPixels.Length; i += 4)
                {
                    int alpha = normalPixels[i];

                    diffuseColor = System.Drawing.Color.FromArgb(alpha, (int)((96f / 255f) * specularPixels[i - 1]), (int)((57f / 255f) * specularPixels[i - 1]), (int)((19f / 255f) * specularPixels[i - 1]));

                    specularColor = System.Drawing.Color.FromArgb(255, specularPixels[i - 2], specularPixels[i - 2], specularPixels[i - 2]);

                    alphaColor = System.Drawing.Color.FromArgb(255, alpha, alpha, alpha);

                    diffuseMap.AddRange(BitConverter.GetBytes(diffuseColor.ToArgb()));
                    specularMap.AddRange(BitConverter.GetBytes(specularColor.ToArgb()));
                    alphaMap.AddRange(BitConverter.GetBytes(alphaColor.ToArgb()));
                }

                bitmapSource = BitmapSource.Create(width, height, normalBitmap.HorizontalResolution, normalBitmap.VerticalResolution, PixelFormats.Bgra32, null, diffuseMap.ToArray(), stride);
                texBitmaps[0] = bitmapSource;
            }
            else
            {
                for (int i = 3; i < normalPixels.Length; i += 4)
                {
                    int alpha = normalPixels[i-3];

                    diffuseColor = System.Drawing.Color.FromArgb(alpha, diffusePixels[i - 1], diffusePixels[i - 2], diffusePixels[i - 3]);
                    diffuseMap.AddRange(BitConverter.GetBytes(diffuseColor.ToArgb()));

                    specularColor = System.Drawing.Color.FromArgb(255, specularPixels[i - 2], specularPixels[i - 2], specularPixels[i - 2]);
                    specularMap.AddRange(BitConverter.GetBytes(specularColor.ToArgb()));

                    alphaColor = System.Drawing.Color.FromArgb(255, alpha, alpha, alpha);
                    alphaMap.AddRange(BitConverter.GetBytes(alphaColor.ToArgb()));
                }

                bitmapSource = BitmapSource.Create(width, height, normalBitmap.HorizontalResolution, normalBitmap.VerticalResolution, PixelFormats.Bgra32, null, diffuseMap.ToArray(), stride);
                texBitmaps[0] = bitmapSource;
            }

            bitmapSource = BitmapSource.Create(width, height, normalBitmap.HorizontalResolution, normalBitmap.VerticalResolution, PixelFormats.Bgra32, null, specularMap.ToArray(), stride);
            texBitmaps[1] = bitmapSource;

            texBitmaps[2] = Imaging.CreateBitmapSourceFromHBitmap(normalBitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            bitmapSource = BitmapSource.Create(width, height, normalBitmap.HorizontalResolution, normalBitmap.VerticalResolution, PixelFormats.Bgra32, null, alphaMap.ToArray(), stride);
            texBitmaps[3] = bitmapSource;

            if (normalTexData != null)
            {
                normalTexData.Dispose();
            }

            if (normalBitmap != null)
            {
                normalBitmap.Dispose();
            }

            if (diffuseTexData != null)
            {
                diffuseTexData.Dispose();
            }

            if (maskTexData != null)
            {
                maskTexData.Dispose();
            }

            if (specularTexData != null)
            {
                specularTexData.Dispose();
            }

            return texBitmaps;
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
            var destRect = new System.Drawing.Rectangle(0, 0, width, height);
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

        /// <summary>
        /// Sets the alpha channel a bitmap.
        /// </summary>
        /// <param name="bmp">The bitmap to modify.</param>
        /// <param name="alpha">The alpha value to use.</param>
        /// <returns>A bitmap with the new alpha value.</returns>
        public static Bitmap SetAlpha(Bitmap bmp, byte alpha)
        {
            var data = bmp.LockBits(
                new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadWrite,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            var line = data.Scan0;
            var eof = line + data.Height * data.Stride;
            while (line != eof)
            {
                var pixelAlpha = line + 3;
                var eol = pixelAlpha + data.Width * 4;
                while (pixelAlpha != eol)
                {
                    Marshal.WriteByte(pixelAlpha, alpha);
                    pixelAlpha += 4;
                }
                line += data.Stride;
            }
            bmp.UnlockBits(data);

            return bmp;
        }
    }
}
