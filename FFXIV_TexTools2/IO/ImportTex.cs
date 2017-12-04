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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows;

namespace FFXIV_TexTools2.IO
{
    /// <summary>
    /// Handles IO operations for importing texture data
    /// </summary>
    public class ImportTex
    {
        /// <summary>
        /// Imports the items texture into a dat file
        /// </summary>
        /// <param name="texData">Data for the currently displayed texture</param>
        /// <param name="category">The items category</param>
        /// <param name="itemName">The items name</param>
        /// <param name="internalFilePath">The internal file path of the texture map</param>
        /// <returns>The offset in which the data was placed</returns>
        public static int ImportTexture(TEXData texData, string category, string subCategory, string itemName, string internalFilePath)
        {
            int textureType, lineNum = 0, offset = 0;
            bool inModList = false;
            JsonEntry modEntry = null;

            string dxPath = Path.GetFileNameWithoutExtension(internalFilePath);
            var savePath = Properties.Settings.Default.Save_Directory + "/" + category + "/" + itemName + "/" + dxPath + ".dds";

            if (category.Equals("UI"))
            {
                savePath = Properties.Settings.Default.Save_Directory + "/" + category + "/" + subCategory + "/" + itemName + "/" + dxPath + ".dds";
            }


            if (File.Exists(savePath))
            {

                try
                {
                    using (StreamReader sr = new StreamReader(Info.modListDir))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            modEntry = JsonConvert.DeserializeObject<JsonEntry>(line);
                            if (modEntry.fullPath.Equals(internalFilePath))
                            {
                                inModList = true;
                                break;
                            }
                            lineNum++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("[Import] Error Accessing .modlist File \n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                using (BinaryReader br = new BinaryReader(File.OpenRead(savePath)))
                {
                    br.BaseStream.Seek(12, SeekOrigin.Begin);

                    var newHeight = br.ReadInt32();
                    var newWidth = br.ReadInt32();
                    br.ReadBytes(8);
                    var newMipCount = br.ReadInt32();

                    br.BaseStream.Seek(80, SeekOrigin.Begin);

                    var textureFlags = br.ReadInt32();

                    textureType = Info.DDSType[br.ReadInt32()];

                    if(textureFlags == 2 && textureType == 5200)
                    {
                        textureType = TextureTypes.A8;
                    }
                    else if(textureFlags == 65 && textureType == 5200)
                    {
                        int bpp = br.ReadInt32();
                        if(bpp == 32)
                        {
                            textureType = TextureTypes.A8R8G8B8;
                        }
                        else
                        {
                            int red = br.ReadInt32();

                            if(red == 31744)
                            {
                                textureType = TextureTypes.A1R5G5B5;
                            }
                            else if (red == 3840)
                            {
                                textureType = TextureTypes.A4R4G4B4;
                            }
                        }
                    }

                    if (textureType == texData.Type)
                    {
                        List<byte> newTEX = new List<byte>();

                        int uncompressedLength = (int)new FileInfo(savePath).Length - 128;

                        var DDSInfo = ReadDDS(br, texData, newWidth, newHeight, newMipCount);

                        newTEX.AddRange(MakeType4DATHeader(texData, DDSInfo.Item2, DDSInfo.Item3, uncompressedLength, newMipCount, newWidth, newHeight));
                        newTEX.AddRange(MakeTextureInfoHeader(texData, newWidth, newHeight, newMipCount));
                        newTEX.AddRange(DDSInfo.Item1);

                        offset = WriteToDat(newTEX, modEntry, inModList, internalFilePath, category, itemName, lineNum, texData.TEXDatName);
                    }
                    else
                    {
                        MessageBox.Show("Incorrect file type \nExpected: " + Info.TextureTypes[texData.Type] + " Given: " + Info.TextureTypes[textureType], "Texture format error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Could not find file \n" + savePath, "File read Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return offset;
        }

        /// <summary>
        /// Imports the items color set into a dat file
        /// </summary>
        /// <param name="mtrlData">MTRL data for the currently displayed color set</param>
        /// <param name="category">The items category</param>
        /// <param name="itemName">The items name</param>
        public static Tuple<int, byte[]> ImportColor(MTRLData mtrlData, string category, string itemName)
        {
            var savePath = Properties.Settings.Default.Save_Directory + "/" + category + "/" + itemName + "/" + Path.GetFileNameWithoutExtension(mtrlData.MTRLPath) + ".dds";
            int newOffset = 0;

            if (File.Exists(savePath))
            {
                List<byte> mtrlBytes = new List<byte>();
                List<byte> newMTRL = new List<byte>();

                JsonEntry modEntry = null;
                int offset = mtrlData.MTRLOffset;
                int lineNum = 0;
                short fileSize;
                bool inModList = false;
                byte[] colorData;

                int datNum = ((offset / 8) & 0x000f) / 2;

                try
                {
                    using (StreamReader sr = new StreamReader(Info.modListDir))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            modEntry = JsonConvert.DeserializeObject<JsonEntry>(line);
                            if (modEntry.fullPath.Equals(mtrlData.MTRLPath))
                            {
                                inModList = true;
                                break;
                            }
                            lineNum++;
                        }
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show("[Import] Error Accessing .modlist File \n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }


                using (BinaryReader br = new BinaryReader(new MemoryStream(Helper.GetType2DecompressedData(offset, datNum, Strings.ItemsDat))))
                {
                    br.BaseStream.Seek(4, SeekOrigin.Begin);
                    fileSize = br.ReadInt16();
                    short colorDataSize = br.ReadInt16();
                    short textureNameSize = br.ReadInt16();
                    br.ReadBytes(2);
                    byte numOfTextures = br.ReadByte();
                    byte numOfMaps = br.ReadByte();
                    byte numOfColorSets = br.ReadByte();
                    byte unknown = br.ReadByte();

                    int endOfHeader = 16 + ((numOfTextures + numOfMaps + numOfColorSets) * 4);

                    br.BaseStream.Seek(0, SeekOrigin.Begin);

                    mtrlBytes.AddRange(br.ReadBytes(endOfHeader));
                    mtrlBytes.AddRange(br.ReadBytes(textureNameSize + 4));
                    br.ReadBytes(colorDataSize);

                    if (colorDataSize == 544)
                    {
                        using (BinaryReader br1 = new BinaryReader(File.OpenRead(savePath)))
                        {
                            br1.BaseStream.Seek(128, SeekOrigin.Begin);
                            colorData = br1.ReadBytes(colorDataSize - 32);

                            mtrlBytes.AddRange(colorData);
                        }

                        string flagsPath = Path.Combine(Path.GetDirectoryName(savePath), (Path.GetFileNameWithoutExtension(savePath) + ".dat"));

                        using (BinaryReader br1 = new BinaryReader(File.OpenRead(flagsPath)))
                        {
                            br1.BaseStream.Seek(0, SeekOrigin.Begin);

                            mtrlBytes.AddRange(br1.ReadBytes(32));
                        }
                    }
                    else
                    {
                        using (BinaryReader br1 = new BinaryReader(File.OpenRead(savePath)))
                        {
                            br1.BaseStream.Seek(128, SeekOrigin.Begin);
                            colorData = br1.ReadBytes(colorDataSize);

                            mtrlBytes.AddRange(colorData);
                        }
                    }

                    mtrlBytes.AddRange(br.ReadBytes(fileSize - (int)br.BaseStream.Position));
                }

                var compressed = Compressor(mtrlBytes.ToArray());
                int padding = 128 - (compressed.Length % 128);

                newMTRL.AddRange(MakeMTRLHeader(fileSize, compressed.Length + padding));
                newMTRL.AddRange(BitConverter.GetBytes(16));
                newMTRL.AddRange(BitConverter.GetBytes(0));
                newMTRL.AddRange(BitConverter.GetBytes(compressed.Length));
                newMTRL.AddRange(BitConverter.GetBytes((int)fileSize));
                newMTRL.AddRange(compressed);
                newMTRL.AddRange(new byte[padding]);

                newOffset = WriteToDat(newMTRL, modEntry, inModList, mtrlData.MTRLPath, category, itemName, lineNum, Strings.ItemsDat);

                return new Tuple<int, byte[]>(newOffset, colorData);
            }
            else
            {
                MessageBox.Show("Could not find file \n" + savePath, "File read Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return new Tuple<int, byte[]>(0, null);
            }


        }

        /// <summary>
        /// Imports VFX files
        /// </summary>
        /// <param name="category">The category of the item</param>
        /// <param name="itemName">The selected items name</param>
        /// <param name="internalFilePath">The full path of the internal file</param>
        public static int ImportVFX(TEXData texData, string category, string itemName, string internalFilePath)
        {
            JsonEntry modEntry = null;
            int textureType, newHeight, newWidth, newMipCount;
            int lineNum = 0;
            int newOffset = 0;
            bool inModList = false;

            string dxPath = Path.GetFileNameWithoutExtension(internalFilePath);
            var savePath = Properties.Settings.Default.Save_Directory + "/" + category + "/" + itemName + "/" + dxPath + ".dds";

            if (File.Exists(savePath))
            {
                List<byte> newVFX = new List<byte>();
                List<byte> uncompressedVFX = new List<byte>();
                List<byte> headerData = new List<byte>();
                List<byte> dataBlocks = new List<byte>();

                try
                {
                    using (StreamReader sr = new StreamReader(Info.modListDir))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            modEntry = JsonConvert.DeserializeObject<JsonEntry>(line);
                            if (modEntry.fullPath.Equals(internalFilePath))
                            {
                                inModList = true;
                                break;
                            }
                            lineNum++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("[Import] Error Accessing .modlist File \n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }


                using (BinaryReader br = new BinaryReader(File.OpenRead(savePath)))
                {
                    br.BaseStream.Seek(12, SeekOrigin.Begin);

                    newHeight = br.ReadInt32();
                    newWidth = br.ReadInt32();
                    br.ReadBytes(8);
                    newMipCount = br.ReadInt32();

                    br.BaseStream.Seek(80, SeekOrigin.Begin);

                    var textureFlags = br.ReadInt32();

                    textureType = Info.DDSType[br.ReadInt32()];

                    if (textureFlags == 2 && textureType == 5200)
                    {
                        textureType = TextureTypes.A8;
                    }

                    if (textureType == texData.Type)
                    {
                        br.BaseStream.Seek(128, SeekOrigin.Begin);

                        uncompressedVFX.AddRange(MakeTextureInfoHeader(texData, newWidth, newHeight, newMipCount));

                        int dataLength = (int)new FileInfo(savePath).Length - 128;

                        uncompressedVFX.AddRange(br.ReadBytes(dataLength));
                    }
                    else
                    {
                        MessageBox.Show("Incorrect file type \nExpected: " + Info.TextureTypes[texData.Type] + " Given: " + Info.TextureTypes[textureType], "Texture format error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return 0;
                    }
                }

                headerData.AddRange(BitConverter.GetBytes(128));
                headerData.AddRange(BitConverter.GetBytes(2));
                headerData.AddRange(BitConverter.GetBytes(uncompressedVFX.Count));

                int dataOffset = 0;
                int totalCompSize = 0;
                int uncompressedLength = uncompressedVFX.Count;

                int partCount = (int)Math.Ceiling(uncompressedLength / 16000f);

                headerData.AddRange(BitConverter.GetBytes(partCount));

                int remainder = uncompressedLength;

                using (BinaryReader br = new BinaryReader(new MemoryStream(uncompressedVFX.ToArray())))
                {
                    br.BaseStream.Seek(0, SeekOrigin.Begin);

                    for (int i = 1; i <= partCount; i++)
                    {
                        if (i == partCount)
                        {
                            var compData = Compressor(br.ReadBytes(remainder));
                            int padding = 128 - ((compData.Length + 16) % 128);

                            dataBlocks.AddRange(BitConverter.GetBytes(16));
                            dataBlocks.AddRange(BitConverter.GetBytes(0));
                            dataBlocks.AddRange(BitConverter.GetBytes(compData.Length));
                            dataBlocks.AddRange(BitConverter.GetBytes(remainder));
                            dataBlocks.AddRange(compData);
                            dataBlocks.AddRange(new byte[padding]);

                            headerData.AddRange(BitConverter.GetBytes(dataOffset));
                            headerData.AddRange(BitConverter.GetBytes((short)((compData.Length + 16) + padding)));
                            headerData.AddRange(BitConverter.GetBytes((short)remainder));

                            totalCompSize = dataOffset + ((compData.Length + 16) + padding);
                        }
                        else
                        {
                            var compData = Compressor(br.ReadBytes(16000));
                            int padding = 128 - ((compData.Length + 16) % 128);

                            dataBlocks.AddRange(BitConverter.GetBytes(16));
                            dataBlocks.AddRange(BitConverter.GetBytes(0));
                            dataBlocks.AddRange(BitConverter.GetBytes(compData.Length));
                            dataBlocks.AddRange(BitConverter.GetBytes(16000));
                            dataBlocks.AddRange(compData);
                            dataBlocks.AddRange(new byte[padding]);

                            headerData.AddRange(BitConverter.GetBytes(dataOffset));
                            headerData.AddRange(BitConverter.GetBytes((short)((compData.Length + 16) + padding)));
                            headerData.AddRange(BitConverter.GetBytes((short)16000));

                            dataOffset += ((compData.Length + 16) + padding);
                            remainder -= 16000;
                        }
                    }
                }

                headerData.InsertRange(12, BitConverter.GetBytes(totalCompSize / 128));
                headerData.InsertRange(16, BitConverter.GetBytes(totalCompSize / 128));

                int headerPadding = 128 - headerData.Count;

                headerData.AddRange(new byte[headerPadding]);

                newVFX.AddRange(headerData);
                newVFX.AddRange(dataBlocks);

                newOffset = WriteToDat(newVFX, modEntry, inModList, internalFilePath, category, itemName, lineNum, texData.TEXDatName);
            }
            return newOffset;
        }


        /// <summary>
        /// Reads and parses data from the DDS file to be imported.
        /// </summary>
        /// <param name="br">The currently active BinaryReader.</param>
        /// <param name="texData">Data for the currently displayed texture.</param>
        /// <param name="newWidth">The width of the DDS texture to be imported.</param>
        /// <param name="newHeight">The height of the DDS texture to be imported.</param>
        /// <param name="newMipCount">The number of mipmaps the DDS texture to be imported contains.</param>
        /// <returns>A tuple containing the compressed DDS data, a list of offsets to the mipmap parts, a list with the number of parts per mipmap.</returns>
        private static Tuple<List<byte>, List<short>, List<short>> ReadDDS(BinaryReader br, TEXData texData, int newWidth, int newHeight, int newMipCount)
        {
            List<byte> compressedDDS = new List<byte>();
            List<short> mipPartOffsets = new List<short>();
            List<short> mipPartCount = new List<short>();

            int mipLength;

            if (texData.Type == TextureTypes.DXT1)
            {
                mipLength = (newWidth * newHeight) / 2;
            }
            else if (texData.Type == TextureTypes.DXT5 || texData.Type == TextureTypes.A8)
            {
                mipLength = newWidth * newHeight;
            }
            else if (texData.Type == TextureTypes.A1R5G5B5 || texData.Type == TextureTypes.A4R4G4B4)
            {
                mipLength = (newWidth * newHeight) * 2;
            }
            else
            {
                mipLength = (newWidth * newHeight) * 4;
            }


            br.BaseStream.Seek(128, SeekOrigin.Begin);

            for (int i = 0; i < newMipCount; i++)
            {
                int mipParts = (int)Math.Ceiling(mipLength / 16000f);
                mipPartCount.Add((short)mipParts);

                if (mipParts > 1)
                {
                    for (int j = 0; j < mipParts; j++)
                    {
                        int uncompLength;
                        byte[] compressed;
                        bool comp = true;

                        if (j == mipParts - 1)
                        {
                            uncompLength = mipLength % 16000;
                        }
                        else
                        {
                            uncompLength = 16000;
                        }

                        var uncompBytes = br.ReadBytes(uncompLength);
                        compressed = Compressor(uncompBytes);

                        if (compressed.Length > uncompLength)
                        {
                            compressed = uncompBytes;
                            comp = false;
                        }

                        compressedDDS.AddRange(BitConverter.GetBytes(16));
                        compressedDDS.AddRange(BitConverter.GetBytes(0));

                        if (!comp)
                        {
                            compressedDDS.AddRange(BitConverter.GetBytes(32000));
                        }
                        else
                        {
                            compressedDDS.AddRange(BitConverter.GetBytes(compressed.Length));
                        }

                        compressedDDS.AddRange(BitConverter.GetBytes(uncompLength));
                        compressedDDS.AddRange(compressed);

                        int padding = 128 - (compressed.Length % 128);

                        compressedDDS.AddRange(new byte[padding]);

                        mipPartOffsets.Add((short)(compressed.Length + padding + 16));
                    }
                }
                else
                {
                    int uncompLength;
                    byte[] compressed;
                    bool comp = true;

                    if (mipLength != 16000)
                    {
                        uncompLength = mipLength % 16000;
                    }
                    else
                    {
                        uncompLength = 16000;
                    }

                    var uncompBytes = br.ReadBytes(uncompLength);
                    compressed = Compressor(uncompBytes);

                    if (compressed.Length > uncompLength)
                    {
                        compressed = uncompBytes;
                        comp = false;
                    }

                    compressedDDS.AddRange(BitConverter.GetBytes(16));
                    compressedDDS.AddRange(BitConverter.GetBytes(0));

                    if (!comp)
                    {
                        compressedDDS.AddRange(BitConverter.GetBytes(32000));
                    }
                    else
                    {
                        compressedDDS.AddRange(BitConverter.GetBytes(compressed.Length));
                    }

                    compressedDDS.AddRange(BitConverter.GetBytes(uncompLength));
                    compressedDDS.AddRange(compressed);

                    int padding = 128 - (compressed.Length % 128);

                    compressedDDS.AddRange(new byte[padding]);

                    mipPartOffsets.Add((short)(compressed.Length + padding + 16));
                }

                if (mipLength > 32)
                {
                    mipLength = mipLength / 4;
                }
                else
                {
                    mipLength = 8;
                }
            }

            return new Tuple<List<byte>, List<short>, List<short>>(compressedDDS, mipPartOffsets, mipPartCount);
        }

        /// <summary>
        /// Writes the newly imported data to the .dat for modifications.
        /// </summary>
        /// <param name="data">The data to be written.</param>
        /// <param name="modEntry">The modlist entry (if any) for the given file.</param>
        /// <param name="inModList">Is the item already contained within the mod list.</param>
        /// <param name="internalFilePath">The internal file path of the item being modified.</param>
        /// <param name="category">The category of the item.</param>
        /// <param name="itemName">The name of the item being modified.</param>
        /// <param name="lineNum">The line number of the existing mod list entry for the item if it exists.</param>
        /// <returns>The new offset in which the modified data was placed.</returns>
        public static int WriteToDat(List<byte> data, JsonEntry modEntry, bool inModList, string internalFilePath, string category, string itemName, int lineNum, string datName)
        {
            int offset = 0;
            bool dataOverwritten = false;

            string datNum = Info.ModDatDict[datName];

            var modDatPath = string.Format(Info.datDir, datName, datNum);

            var datOffsetAmount = 16 * int.Parse(datNum);

            try
            {
                using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(modDatPath)))
                {
                    /* 
                     * If the item has been previously modified and the compressed data being imported is smaller or equal to the exisiting data
                    *  replace the existing data with new data.
                    */
                    if (inModList && data.Count <= modEntry.modSize)
                    {
                        int sizeDiff = modEntry.modSize - data.Count;

                        bw.BaseStream.Seek(modEntry.modOffset - datOffsetAmount, SeekOrigin.Begin);

                        bw.Write(data.ToArray());

                        bw.Write(new byte[sizeDiff]);

                        Helper.UpdateIndex(modEntry.modOffset, internalFilePath, datName);
                        Helper.UpdateIndex2(modEntry.modOffset, internalFilePath, datName);

                        offset = modEntry.modOffset;

                        dataOverwritten = true;
                    }
                    else if(!inModList)
                    {
                        int emptyLength = 0;
                        int emptyLine = 0;

                        /* 
                         * If there is an empty entry in the modlist and the compressed data being imported is smaller or equal to the available space
                        *  write the compressed data in the existing space.
                        */
                        try
                        {
                            foreach (string line in File.ReadAllLines(Info.modListDir))
                            {
                                JsonEntry emptyEntry = JsonConvert.DeserializeObject<JsonEntry>(line);

                                if (emptyEntry.fullPath.Equals(""))
                                {
                                    emptyLength = emptyEntry.modSize;

                                    if (emptyLength > data.Count)
                                    {
                                        int sizeDiff = emptyLength - data.Count;

                                        bw.BaseStream.Seek(emptyEntry.modOffset - datOffsetAmount, SeekOrigin.Begin);

                                        bw.Write(data.ToArray());

                                        bw.Write(new byte[sizeDiff]);

                                        int originalOffset = Helper.UpdateIndex(emptyEntry.modOffset, internalFilePath, datName) * 8;
                                        Helper.UpdateIndex2(emptyEntry.modOffset, internalFilePath, datName);

                                        JsonEntry replaceEntry = new JsonEntry()
                                        {
                                            category = category,
                                            name = itemName,
                                            fullPath = internalFilePath,
                                            originalOffset = originalOffset,
                                            modOffset = emptyEntry.modOffset,
                                            modSize = emptyEntry.modSize,
                                            datFile = datName
                                        };

                                        string[] lines = File.ReadAllLines(Info.modListDir);
                                        lines[emptyLine] = JsonConvert.SerializeObject(replaceEntry);
                                        File.WriteAllLines(Info.modListDir, lines);

                                        offset = emptyEntry.modOffset;

                                        dataOverwritten = true;
                                        break;
                                    }
                                }
                                emptyLine++;
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("[Import] Error Accessing .modlist File \n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }

                        if (!dataOverwritten)
                        {
                            bw.BaseStream.Seek(0, SeekOrigin.End);

                            while ((bw.BaseStream.Position & 0xFF) != 0)
                            {
                                bw.Write((byte)0);
                            }

                            int eof = (int)bw.BaseStream.Position + data.Count;

                            while ((eof & 0xFF) != 0)
                            {
                                data.AddRange(new byte[16]);
                                eof = eof + 16;
                            }

                            offset = (int)bw.BaseStream.Position + datOffsetAmount;

                            bw.Write(data.ToArray());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("[Import] Error Accessing .dat4 File \n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return 0;
            }


            if (!dataOverwritten)
            {
                int oldOffset = Helper.UpdateIndex(offset, internalFilePath, datName) * 8;
                Helper.UpdateIndex2(offset, internalFilePath, datName);

                /*
                 * If the item has been previously modifed, but the new compressed data to be imported is larger than the existing data
                 * remove the data from the modlist, leaving the offset and size intact for future use
                */
                if (inModList && data.Count > modEntry.modSize && modEntry != null)
                {
                    oldOffset = modEntry.originalOffset;

                    JsonEntry replaceEntry = new JsonEntry()
                    {
                        category = String.Empty,
                        name = String.Empty,
                        fullPath = String.Empty,
                        originalOffset = 0,
                        modOffset = modEntry.modOffset,
                        modSize = modEntry.modSize,
                        datFile = datName
                    };

                    string[] lines = File.ReadAllLines(Info.modListDir);
                    lines[lineNum] = JsonConvert.SerializeObject(replaceEntry);
                    File.WriteAllLines(Info.modListDir, lines);
                }

                JsonEntry entry = new JsonEntry()
                {
                    category = category,
                    name = itemName,
                    fullPath = internalFilePath,
                    originalOffset = oldOffset,
                    modOffset = offset,
                    modSize = data.Count,
                    datFile = datName
                };

                try
                {
                    using (StreamWriter modFile = new StreamWriter(Info.modListDir, true))
                    {
                        modFile.BaseStream.Seek(0, SeekOrigin.End);
                        modFile.WriteLine(JsonConvert.SerializeObject(entry));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("[Import] Error Accessing .modlist File \n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            return offset;
        }

        /// <summary>
        /// Creates the header for the compressed texture data to be imported.
        /// </summary>
        /// <param name="texData">Data for the currently displayed texture.</param>
        /// <param name="mipPartOffsets">List of part offsets.</param>
        /// <param name="mipPartCount">List containing the amount of parts per mipmap.</param>
        /// <param name="uncompressedLength">Length of the uncompressed texture file.</param>
        /// <param name="newMipCount">The number of mipmaps the DDS texture to be imported contains.</param>
        /// <param name="newWidth">The width of the DDS texture to be imported.</param>
        /// <param name="newHeight">The height of the DDS texture to be imported.</param>
        /// <returns>The created header data.</returns>
        private static List<byte> MakeType4DATHeader(TEXData texData, List<short> mipPartOffsets, List<short> mipPartCount, int uncompressedLength, int newMipCount, int newWidth, int newHeight)
        {
            List<byte> headerData = new List<byte>();

            int headerSize = 24 + (newMipCount * 20) + (mipPartOffsets.Count * 2);
            int headerPadding = 128 - (headerSize % 128);

            headerData.AddRange(BitConverter.GetBytes(headerSize + headerPadding));
            headerData.AddRange(BitConverter.GetBytes(4));
            headerData.AddRange(BitConverter.GetBytes(uncompressedLength));
            headerData.AddRange(BitConverter.GetBytes(0));
            headerData.AddRange(BitConverter.GetBytes(0));
            headerData.AddRange(BitConverter.GetBytes(newMipCount));


            int partIndex = 0;
            int mipOffsetIndex = 80;
            int uncompMipSize = newHeight * newWidth;

            if (texData.Type == TextureTypes.DXT1)
            {
                uncompMipSize = (newWidth * newHeight) / 2;
            }
            else if (texData.Type == TextureTypes.DXT5 || texData.Type == TextureTypes.A8)
            {
                uncompMipSize = newWidth * newHeight;
            }
            else if (texData.Type == TextureTypes.A1R5G5B5 || texData.Type == TextureTypes.A4R4G4B4)
            {
                uncompMipSize = (newWidth * newHeight) * 2;
            }
            else
            {
                uncompMipSize = (newWidth * newHeight) * 4;
            }

            for (int i = 0; i < newMipCount; i++)
            {
                headerData.AddRange(BitConverter.GetBytes(mipOffsetIndex));

                int paddedSize = 0;

                for (int j = 0; j < mipPartCount[i]; j++)
                {
                    paddedSize = paddedSize + mipPartOffsets[j + partIndex];
                }

                headerData.AddRange(BitConverter.GetBytes(paddedSize));

                if (uncompMipSize > 16)
                {
                    headerData.AddRange(BitConverter.GetBytes(uncompMipSize));
                }
                else
                {
                    headerData.AddRange(BitConverter.GetBytes(16));
                }

                uncompMipSize = uncompMipSize / 4;

                headerData.AddRange(BitConverter.GetBytes(partIndex));
                headerData.AddRange(BitConverter.GetBytes((int)mipPartCount[i]));

                partIndex = partIndex + mipPartCount[i];
                mipOffsetIndex = mipOffsetIndex + paddedSize;
            }

            foreach (short part in mipPartOffsets)
            {
                headerData.AddRange(BitConverter.GetBytes(part));
            }

            headerData.AddRange(new byte[headerPadding]);

            return headerData;
        }


        /// <summary>
        /// Creates the header for the texture info from the data to be imported.
        /// </summary>
        /// <param name="texData">Data for the currently displayed texture.</param>
        /// <param name="newWidth">The width of the DDS texture to be imported.</param>
        /// <param name="newHeight">The height of the DDS texture to be imported.</param>
        /// <param name="newMipCount">The number of mipmaps the DDS texture to be imported contains.</param>
        /// <returns>The created header data.</returns>
        private static List<byte> MakeTextureInfoHeader(TEXData texData, int newWidth, int newHeight, int newMipCount)
        {
            List<byte> headerData = new List<byte>();

            headerData.AddRange(BitConverter.GetBytes((short)0));
            headerData.AddRange(BitConverter.GetBytes((short)128));
            headerData.AddRange(BitConverter.GetBytes((short)texData.Type));
            headerData.AddRange(BitConverter.GetBytes((short)0));
            headerData.AddRange(BitConverter.GetBytes((short)newWidth));
            headerData.AddRange(BitConverter.GetBytes((short)newHeight));
            headerData.AddRange(BitConverter.GetBytes((short)1));
            headerData.AddRange(BitConverter.GetBytes((short)newMipCount));


            headerData.AddRange(BitConverter.GetBytes(0));
            headerData.AddRange(BitConverter.GetBytes(1));
            headerData.AddRange(BitConverter.GetBytes(2));

            int mipLength;

            if (texData.Type == TextureTypes.DXT1)
            {
                mipLength = (newWidth * newHeight) / 2;
            }
            else if (texData.Type == TextureTypes.DXT5 || texData.Type == TextureTypes.A8)
            {
                mipLength = newWidth * newHeight;
            }
            else if (texData.Type == TextureTypes.A1R5G5B5 || texData.Type == TextureTypes.A4R4G4B4)
            {
                mipLength = (newWidth * newHeight) * 2;
            }
            else
            {
                mipLength = (newWidth * newHeight) * 4;
            }

            int combinedLength = 80;

            for (int i = 0; i < newMipCount; i++)
            {
                headerData.AddRange(BitConverter.GetBytes(combinedLength));
                combinedLength = combinedLength + mipLength;

                if (mipLength > 16)
                {
                    mipLength = mipLength / 4;
                }
                else
                {
                    mipLength = 16;
                }
            }

            int padding = 80 - headerData.Count;

            headerData.AddRange(new byte[padding]);

            return headerData;
        }

        /// <summary>
        /// Creates the header for the MTRL file from the color data to be imported.
        /// </summary>
        /// <param name="fileSize"></param>
        /// <param name="paddedSize"></param>
        /// <returns>The created header data.</returns>
        private static List<byte> MakeMTRLHeader(short fileSize, int paddedSize)
        {
            List<byte> headerData = new List<byte>();

            headerData.AddRange(BitConverter.GetBytes(128));
            headerData.AddRange(BitConverter.GetBytes(2));
            headerData.AddRange(BitConverter.GetBytes((int)fileSize));
            headerData.AddRange(BitConverter.GetBytes(4));
            headerData.AddRange(BitConverter.GetBytes(4));
            headerData.AddRange(BitConverter.GetBytes(1));
            headerData.AddRange(BitConverter.GetBytes(0));
            headerData.AddRange(BitConverter.GetBytes((short)paddedSize));
            headerData.AddRange(BitConverter.GetBytes(fileSize));
            headerData.AddRange(new byte[96]);

            return headerData;
        }

        /// <summary>
        /// Compresses raw byte data.
        /// </summary>
        /// <param name="uncomp">The data to be compressed.</param>
        /// <returns>The compressed byte data.</returns>
        private static byte[] Compressor(byte[] uncomp)
        {
            using (MemoryStream uncompressedMS = new MemoryStream(uncomp))
            {
                byte[] compbytes = null;
                using (var compressedMS = new MemoryStream())
                {
                    using (var ds = new DeflateStream(compressedMS, CompressionMode.Compress))
                    {
                        uncompressedMS.CopyTo(ds);
                        ds.Close();
                        compbytes = compressedMS.ToArray();
                    }
                }
                return compbytes;
            }
        }
    }
}
