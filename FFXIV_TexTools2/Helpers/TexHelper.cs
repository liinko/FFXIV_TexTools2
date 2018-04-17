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
using FFXIV_TexTools2.Resources;
using HelixToolkit.Wpf.SharpDX;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

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
        public static BitmapSource[] MakeModelTextureMaps(TEXData normalTexData, TEXData diffuseTexData, TEXData maskTexData, TEXData specularTexData, MTRLData mtrlData)
        {
            int height = normalTexData.Height;
            int width = normalTexData.Width;
            int tSize = height * width;
            var normalBitmap = normalTexData.BMPSouceAlpha;

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
            List<System.Drawing.Color> emissiveList = new List<System.Drawing.Color>();

            BitmapSource[] texBitmaps = new BitmapSource[5];

            if (mtrlData.ColorData != null)
            {
                var colorBitmap = TEX.ColorSetToBitmap(mtrlData.ColorData);
                var cbmp = SetAlpha(colorBitmap, 255);
                var colorMap1 = Imaging.CreateBitmapSourceFromHBitmap(cbmp.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                int colorSetStride = colorMap1.PixelWidth * (colorMap1.Format.BitsPerPixel / 8);
                byte[] colorPixels = new byte[colorMap1.PixelHeight * colorSetStride];

                colorMap1.CopyPixels(colorPixels, colorSetStride, 0);

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

                    var r1 = colorPixels[i + 10];
                    var g1 = colorPixels[i + 9];
                    var b1 = colorPixels[i + 8];
                    var a1 = colorPixels[i + 11];

                    emissiveList.Add(System.Drawing.Color.FromArgb(255, r1, g1, b1));
                }
            }
            else if (mtrlData.ColorData == null)
            {
                for (int i = 0; i < 1024; i += 16)
                {
                    colorList.Add(System.Drawing.Color.FromArgb(255, 255, 255, 255));
                    specularList.Add(System.Drawing.Color.FromArgb(255, 0, 0, 0));
                    emissiveList.Add(System.Drawing.Color.FromArgb(255, 0, 0, 0));
                }
            }

            if (maskTexData != null)
            {
                if (tSize > (maskTexData.Height * maskTexData.Width))
                {
                    var resized = CreateResizedImage(maskTexData.BMPSouceAlpha, width, height);
                    maskPixels = GetBytesFromBitmapSource((BitmapSource)resized);
                }
                else
                {
                    maskPixels = GetBytesFromBitmapSource(maskTexData.BMPSouceAlpha);
                }
            }

            if (diffuseTexData != null)
            {
                if (tSize > (diffuseTexData.Height * diffuseTexData.Width))
                {
                    var resized = CreateResizedImage(diffuseTexData.BMPSouceAlpha, width, height);
                    diffusePixels = GetBytesFromBitmapSource((BitmapSource)resized);
                }
                else
                {
                    diffusePixels = GetBytesFromBitmapSource(diffuseTexData.BMPSouceAlpha);
                }
            }

            if (specularTexData != null)
            {
                if (tSize > (specularTexData.Height * specularTexData.Width))
                {
                    var resized = CreateResizedImage(specularTexData.BMPSouceAlpha, width, height);
                    specularPixels = GetBytesFromBitmapSource((BitmapSource)resized);
                }
                else
                {
                    specularPixels = GetBytesFromBitmapSource(specularTexData.BMPSouceAlpha);
                }
            }

            if (normalTexData != null)
            {
                if (tSize > (normalTexData.Height * normalTexData.Width))
                {
                    var resized = CreateResizedImage(normalTexData.BMPSouceAlpha, width, height);
                    normalBitmap = (BitmapSource)resized;
                    normalPixels = GetBytesFromBitmapSource((BitmapSource)resized);
                }
                else
                {
                    normalPixels = GetBytesFromBitmapSource(normalTexData.BMPSouceAlpha);
                }
            }

            List<byte> diffuseMap = new List<byte>();
            List<byte> specularMap = new List<byte>();
            List<byte> emissiveMap = new List<byte>();
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
                    if(diffusePixels != null)
                    {

                        B = diffusePixels[i - 3];
                        G = diffusePixels[i - 2];
                        R = diffusePixels[i - 1];
                    }
                    else
                    {
                        B = 255;
                        G = 255;
                        R = 255;
                    }

                    if(specularPixels != null)
                    {
                        B1 = specularPixels[i - 2];
                        G1 = specularPixels[i - 2];
                        R1 = specularPixels[i - 2];
                    }
                    else
                    {
                        B1 = 255;
                        G1 = 255;
                        R1 = 255;
                    }

                }

                System.Drawing.Color diffuseColor;
                System.Drawing.Color specularColor;
                System.Drawing.Color emissiveColor;
                System.Drawing.Color alphaColor;


                float pixel = (normalPixels[i] / 255f) * 15f;
                //int colorLoc = (int)Math.Floor(pixel + 0.5f);
                float percent = (float)(pixel - Math.Truncate(pixel));

                if (percent != 0)
                {
                    var color2Loc = (int)(Math.Truncate(pixel));
                    var color1Loc = color2Loc + 1;

                    var color1 = System.Drawing.Color.FromArgb(alpha, colorList[color1Loc].R, colorList[color1Loc].G, colorList[color1Loc].B);
                    var color2 = System.Drawing.Color.FromArgb(alpha, colorList[color2Loc].R, colorList[color2Loc].G, colorList[color2Loc].B);

                    var diffuseBlend = Blend(color1, color2, percent);

                    color1 = System.Drawing.Color.FromArgb(255, specularList[color1Loc].R, specularList[color1Loc].G, specularList[color1Loc].B);
                    color2 = System.Drawing.Color.FromArgb(255, specularList[color2Loc].R, specularList[color2Loc].G, specularList[color2Loc].B);

                    var specBlend = Blend(color1, color2, percent);

                    color1 = System.Drawing.Color.FromArgb(255, emissiveList[color1Loc].R, emissiveList[color1Loc].G, emissiveList[color1Loc].B);
                    color2 = System.Drawing.Color.FromArgb(255, emissiveList[color2Loc].R, emissiveList[color2Loc].G, emissiveList[color2Loc].B);

                    var emisBlend = Blend(color1, color2, percent);

                    diffuseColor = System.Drawing.Color.FromArgb(alpha, (int)((diffuseBlend.R / 255f) * R), (int)((diffuseBlend.G / 255f) * G), (int)((diffuseBlend.B / 255f) * B));
                    specularColor = System.Drawing.Color.FromArgb(255, (int)((specBlend.R / 255f) * R1), (int)((specBlend.G / 255f) * G1), (int)((specBlend.B / 255f) * B1));
                    emissiveColor = System.Drawing.Color.FromArgb(255, emisBlend.R, emisBlend.G, emisBlend.B);
                }
                else
                {
                    var colorLoc = (int)Math.Floor(pixel + 0.5f);

                    diffuseColor = System.Drawing.Color.FromArgb(alpha, (int)((colorList[colorLoc].R / 255f) * R), (int)((colorList[colorLoc].G / 255f) * G), (int)((colorList[colorLoc].B / 255f) * B));
                    specularColor = System.Drawing.Color.FromArgb(255, (int)((specularList[colorLoc].R / 255f) * R1), (int)((specularList[colorLoc].G / 255f) * G1), (int)((specularList[colorLoc].B / 255f) * B1));
                    emissiveColor = System.Drawing.Color.FromArgb(255, emissiveList[colorLoc].R, emissiveList[colorLoc].G, emissiveList[colorLoc].B);
                }


                alphaColor = System.Drawing.Color.FromArgb(255, alpha, alpha, alpha);

                diffuseMap.AddRange(BitConverter.GetBytes(diffuseColor.ToArgb()));
                specularMap.AddRange(BitConverter.GetBytes(specularColor.ToArgb()));
                emissiveMap.AddRange(BitConverter.GetBytes(emissiveColor.ToArgb()));
                alphaMap.AddRange(BitConverter.GetBytes(alphaColor.ToArgb()));
            }

            int stride = (int)normalBitmap.Width * (32 / 8);


            var scale = 1;

            if (width >= 4096 || height >= 4096)
            {
                scale = 4;
            }
            else if (width >= 2048 || height >= 2048)
            {
                scale = 2;
            }

            var nWidth = width / scale;
            var nHeight = height / scale;

            BitmapSource bitmapSource = BitmapSource.Create(width, height, normalBitmap.DpiX, normalBitmap.DpiY, PixelFormats.Bgra32, null, diffuseMap.ToArray(), stride);
            texBitmaps[0] = (BitmapSource)CreateResizedImage(bitmapSource, nWidth, nHeight);

            bitmapSource = BitmapSource.Create(width, height, normalBitmap.DpiX, normalBitmap.DpiY, PixelFormats.Bgra32, null, specularMap.ToArray(), stride);
            texBitmaps[1] = (BitmapSource)CreateResizedImage(bitmapSource, nWidth, nHeight);

            //texBitmaps[2] = normalTexData.BMPSouceNoAlpha;
            texBitmaps[2] = (BitmapSource)CreateResizedImage(normalTexData.BMPSouceNoAlpha, nWidth, nHeight);

            bitmapSource = BitmapSource.Create(width, height, normalBitmap.DpiX, normalBitmap.DpiY, PixelFormats.Bgra32, null, alphaMap.ToArray(), stride);
            texBitmaps[3] = (BitmapSource)CreateResizedImage(bitmapSource, nWidth, nHeight);

            bitmapSource = BitmapSource.Create(width, height, normalBitmap.DpiX, normalBitmap.DpiY, PixelFormats.Bgra32, null, emissiveMap.ToArray(), stride);
            texBitmaps[4] = (BitmapSource)CreateResizedImage(bitmapSource, nWidth, nHeight);

            foreach (var tb in texBitmaps)
            {
                tb.Freeze();
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
        public static BitmapSource[] MakeCharacterMaps(TEXData normalTexData, TEXData diffuseTexData, TEXData maskTexData, TEXData specularTexData, string itemName, string path)
        {
            int height = normalTexData.Height;
            int width = normalTexData.Width;
            int tSize = height * width;
            var normalBitmap = normalTexData.BMPSouceAlpha;
            Color charaColor;

            if (path.Contains("/body/b"))
            {
                charaColor = (Color)ColorConverter.ConvertFromString(Properties.Settings.Default.Skin_Color);
            }
            else if (path.Contains("/hair/h"))
            {
                charaColor = (Color)ColorConverter.ConvertFromString(Properties.Settings.Default.Hair_Color);
            }
            else if (path.Contains("/face/f"))
            {
                if (path.Contains("_etc_"))
                {
                    charaColor = (Color)ColorConverter.ConvertFromString(Properties.Settings.Default.Etc_Color);
                }
                else if (path.Contains("_iri_"))
                {
                    charaColor = (Color)ColorConverter.ConvertFromString(Properties.Settings.Default.Iris_Color);
                }
                else
                {
                    charaColor = (Color)ColorConverter.ConvertFromString(Properties.Settings.Default.Skin_Color);
                }
            }
            else if (path.Contains("tail/t"))
            {
                if (!path.Contains("c1401") && !path.Contains("c1301"))
                {
                    charaColor = (Color)ColorConverter.ConvertFromString(Properties.Settings.Default.Hair_Color);
                }
                else
                {
                    charaColor = Color.FromArgb(255, 255, 255, 255);
                }
            }
            else
            {
                charaColor = Color.FromArgb(255, 96, 57, 19);
            }

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
                    var resized = CreateResizedImage(maskTexData.BMPSouceAlpha, width, height);
                    maskPixels = GetBytesFromBitmapSource((BitmapSource)resized);
                }
                else
                {
                    maskPixels = GetBytesFromBitmapSource(maskTexData.BMPSouceAlpha);
                }
            }

            if (diffuseTexData != null)
            {
                try
                {
                    if (tSize > (diffuseTexData.Height * diffuseTexData.Width))
                    {
                        var resized = CreateResizedImage(diffuseTexData.BMPSouceAlpha, width, height);
                        diffusePixels = GetBytesFromBitmapSource((BitmapSource)resized);
                    }
                    else
                    {
                        diffusePixels = GetBytesFromBitmapSource(diffuseTexData.BMPSouceAlpha);

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
                        var resized = CreateResizedImage(specularTexData.BMPSouceAlpha, width, height);
                        specularPixels = GetBytesFromBitmapSource((BitmapSource)resized);
                    }
                    else
                    {
                        specularPixels = GetBytesFromBitmapSource(specularTexData.BMPSouceAlpha);
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
                    var resized = CreateResizedImage(normalTexData.BMPSouceAlpha, width, height);
                    normalBitmap = (BitmapSource)resized;
                    normalPixels = GetBytesFromBitmapSource((BitmapSource)resized);
                }
                else
                {
                    normalPixels = GetBytesFromBitmapSource(normalTexData.BMPSouceAlpha);
                }
            }

            List<byte> diffuseMap = new List<byte>();
            List<byte> specularMap = new List<byte>();
            List<byte> alphaMap = new List<byte>();

            BitmapSource bitmapSource;

            System.Drawing.Color diffuseColor;
            System.Drawing.Color specularColor;
            System.Drawing.Color alphaColor;

            int stride = (int)normalBitmap.Width * (32 / 8);

            var scale = 1;

            if (width >= 4096 || height >= 4096)
            {
                scale = 4;
            }
            else if (width >= 2048 || height >= 2048)
            {
                scale = 2;
            }

            var nWidth = width / scale;
            var nHeight = height / scale;

            if (diffuseTexData == null)
            {
                for (int i = 3; i < normalPixels.Length; i += 4)
                {
                    int alpha = normalPixels[i];

                    diffuseColor = System.Drawing.Color.FromArgb(alpha, (int)((charaColor.R / 255f) * specularPixels[i - 1]), (int)((charaColor.G / 255f) * specularPixels[i - 1]), (int)((charaColor.B / 255f) * specularPixels[i - 1]));

                    specularColor = System.Drawing.Color.FromArgb(255, (int)(specularPixels[i - 2] * 0.1), (int)(specularPixels[i - 2] * 0.1), (int)(specularPixels[i - 2] * 0.1));

                    alphaColor = System.Drawing.Color.FromArgb(255, alpha, alpha, alpha);

                    diffuseMap.AddRange(BitConverter.GetBytes(diffuseColor.ToArgb()));
                    specularMap.AddRange(BitConverter.GetBytes(specularColor.ToArgb()));
                    alphaMap.AddRange(BitConverter.GetBytes(alphaColor.ToArgb()));
                }

                bitmapSource = BitmapSource.Create(width, height, normalBitmap.DpiX, normalBitmap.DpiY, PixelFormats.Bgra32, null, diffuseMap.ToArray(), stride);
                texBitmaps[0] = (BitmapSource)CreateResizedImage(bitmapSource, nWidth, nHeight);
            }
            else
            {
                for (int i = 3; i < normalPixels.Length; i += 4)
                {
                    int alpha = normalPixels[i-3];

                    diffuseColor = System.Drawing.Color.FromArgb(alpha, (int)((charaColor.R / 255f) * diffusePixels[i - 1]), (int)((charaColor.G / 255f) * diffusePixels[i - 2]), (int)((charaColor.B / 255f) * diffusePixels[i - 3]));
                    diffuseMap.AddRange(BitConverter.GetBytes(diffuseColor.ToArgb()));

                    specularColor = System.Drawing.Color.FromArgb(255, (int)(specularPixels[i - 2] * 0.1), (int)(specularPixels[i - 2] * 0.1), (int)(specularPixels[i - 2] * 0.1));
                    specularMap.AddRange(BitConverter.GetBytes(specularColor.ToArgb()));

                    alphaColor = System.Drawing.Color.FromArgb(255, alpha, alpha, alpha);
                    alphaMap.AddRange(BitConverter.GetBytes(alphaColor.ToArgb()));
                }

                bitmapSource = BitmapSource.Create(width, height, normalBitmap.DpiX, normalBitmap.DpiY, PixelFormats.Bgra32, null, diffuseMap.ToArray(), stride);
                texBitmaps[0] = (BitmapSource)CreateResizedImage(bitmapSource, nWidth, nHeight);
            }

            bitmapSource = BitmapSource.Create(width, height, normalBitmap.DpiX, normalBitmap.DpiY, PixelFormats.Bgra32, null, specularMap.ToArray(), stride);
            texBitmaps[1] = (BitmapSource)CreateResizedImage(bitmapSource, nWidth, nHeight);

            texBitmaps[2] = (BitmapSource)CreateResizedImage(normalTexData.BMPSouceNoAlpha, nWidth, nHeight);

            bitmapSource = BitmapSource.Create(width, height, normalBitmap.DpiX, normalBitmap.DpiY, PixelFormats.Bgra32, null, alphaMap.ToArray(), stride);
            texBitmaps[3] = (BitmapSource)CreateResizedImage(bitmapSource, nWidth, nHeight);

            foreach(var tb in texBitmaps)
            {
                tb.Freeze();
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
        /// Creates a new ImageSource with the specified width/height
        /// </summary>
        /// <param name="source">Source image to resize</param>
        /// <param name="width">Width of resized image</param>
        /// <param name="height">Height of resized image</param>
        /// <returns>Resized image</returns>
        public static ImageSource CreateResizedImage(ImageSource source, int width, int height)
        {
            // Target Rect for the resize operation
            Rect rect = new Rect(0, 0, width, height);

            // Create a DrawingVisual/Context to render with
            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawImage(source, rect);
            }

            // Use RenderTargetBitmap to resize the original image
            RenderTargetBitmap resizedImage = new RenderTargetBitmap(
                (int)rect.Width, (int)rect.Height,  // Resized dimensions
                96, 96,                             // Default DPI values
                PixelFormats.Default);              // Default pixel format
            resizedImage.Render(drawingVisual);

            // Return the resized image
            return resizedImage;
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

        public static Bitmap GetBitmap(BitmapSource source)
        {
            Bitmap bmp = new Bitmap(
              source.PixelWidth,
              source.PixelHeight,
              System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            BitmapData data = bmp.LockBits(
              new Rectangle(System.Drawing.Point.Empty, bmp.Size),
              ImageLockMode.WriteOnly,
              System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            source.CopyPixels(
              Int32Rect.Empty,
              data.Scan0,
              data.Height * data.Stride,
              data.Stride);
            bmp.UnlockBits(data);
            return bmp;
        }

        public static byte[] GetBytesFromBitmapSource(BitmapSource bmp)
        {
            int width = bmp.PixelWidth;
            int height = bmp.PixelHeight;
            int stride = width * ((bmp.Format.BitsPerPixel + 7) / 8);

            byte[] pixels = new byte[height * stride];

            bmp.CopyPixels(pixels, stride, 0);

            return pixels;
        }

        /// <summary>Blends the specified colors together.</summary>
        /// <param name="color">Color to blend onto the background color.</param>
        /// <param name="backColor">Color to blend the other color onto.</param>
        /// <param name="amount">How much of <paramref name="color"/> to keep,
        /// “on top of” <paramref name="backColor"/>.</param>
        /// <returns>The blended colors.</returns>
        public static System.Drawing.Color Blend(this System.Drawing.Color color, System.Drawing.Color backColor, double amount)
        {
            byte r = (byte)((color.R * amount) + backColor.R * (1 - amount));
            byte g = (byte)((color.G * amount) + backColor.G * (1 - amount));
            byte b = (byte)((color.B * amount) + backColor.B * (1 - amount));
            return System.Drawing.Color.FromArgb(r, g, b);
        }
    }
}
