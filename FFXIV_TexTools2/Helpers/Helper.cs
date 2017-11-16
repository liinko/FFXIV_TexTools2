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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;

namespace FFXIV_TexTools2.Helpers
{
    public static class Helper
    {

        #region Decompressors

        /// <summary>
        /// Decompresses the data for the given EXD file.
        /// </summary>
        /// <param name="offset">Offset to the EXD data.</param>
        /// <returns>The decompressed data.</returns>
        public static byte[] GetDecompressedEXDData(int offset)
        {
            List<byte> decompressedData = new List<byte>();

            var EXDDatPath = string.Format(Info.datDir, Strings.EXDDat, Info.EXDDatNum);

            using (BinaryReader br = new BinaryReader(File.OpenRead(EXDDatPath)))
            {
                br.BaseStream.Seek(offset, SeekOrigin.Begin);

                int headerLength = br.ReadInt32();
                int fileType = br.ReadInt32();
                int uncompressedSize = br.ReadInt32();
                br.ReadBytes(8);
                int parts = br.ReadInt32();

                int endOfHeader = offset + headerLength;
                int partDataOffset = offset + 24;

                for (int f = 0, g = 0; f < parts; f++)
                {
                    br.BaseStream.Seek(partDataOffset + g, SeekOrigin.Begin);
                    int offsetFromHeaderEnd = br.ReadInt32();
                    int partLength = br.ReadInt16();
                    int partSize = br.ReadInt16();
                    int partOffset = endOfHeader + offsetFromHeaderEnd;

                    br.BaseStream.Seek(partOffset, SeekOrigin.Begin);
                    br.ReadBytes(8);

                    int partCompressedSize = br.ReadInt32();
                    int partUncompressedSize = br.ReadInt32();

                    if (partCompressedSize == 32000)
                    {
                        byte[] uncompressedPartData = br.ReadBytes(partUncompressedSize);
                        decompressedData.AddRange(uncompressedPartData);
                    }
                    else
                    {
                        byte[] compressedPartData = br.ReadBytes(partCompressedSize);
                        byte[] decompressedPartData = new byte[partUncompressedSize];

                        using (MemoryStream ms = new MemoryStream(compressedPartData))
                        {
                            using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
                            {
                                int count = ds.Read(decompressedPartData, 0x00, partUncompressedSize);
                            }
                        }
                        decompressedData.AddRange(decompressedPartData);
                    }
                    g += 8;
                }

                if (decompressedData.Count < uncompressedSize)
                {
                    int difference = uncompressedSize - decompressedData.Count;
                    byte[] padding = new byte[difference];
                    Array.Clear(padding, 0, difference);
                    decompressedData.AddRange(padding);
                }
            }

            return decompressedData.ToArray();
        }


        /// <summary>
        /// Decompresses the data for the given texture file.
        /// </summary>
        /// <remarks>
        /// Type 4 is texture data
        /// </remarks>
        /// <param name="offset">Offset to the texture data.</param>
        /// <param name="datNum">The .dat number to read from.</param>
        /// <returns>The decompressed data.</returns>
        public static byte[] GetType4DecompressedData(int offset, int datNum, string datName)
        {
            int textureType, width, height, mipMapCount;

            List<byte> decompressedData = new List<byte>();

            offset = OffsetCorrection(datNum, offset);

            var datPath = string.Format(Info.datDir, datName, datNum);

            using (BinaryReader br = new BinaryReader(File.OpenRead(datPath)))
            {
                br.BaseStream.Seek(offset, SeekOrigin.Begin);

                int headerLength = br.ReadInt32();
                int fileType = br.ReadInt32();
                int uncompressedFileSize = br.ReadInt32();
                br.ReadBytes(8);
                mipMapCount = br.ReadInt32();


                int endOfHeader = offset + headerLength;
                int mipMapInfoOffset = offset + 24;

                br.BaseStream.Seek(endOfHeader + 4, SeekOrigin.Begin);

                textureType = br.ReadInt32();
                width = br.ReadInt16();
                height = br.ReadInt16();

                for (int i = 0, j = 0; i < mipMapCount; i++)
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
                                ds.Read(decompressedPartData, 0x00, uncompressedSize);
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
                                        ds.Read(decompressedPartData, 0x00, uncompressedSize);
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
                                    ds.Read(uncompressedData, 0x00, uncompressedSize);
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

            return decompressedData.ToArray();
        }

        /// <summary>
        /// Decompresses the data for files of type 2.
        /// </summary>
        /// <remarks>
        /// Type 2 data varies.
        /// </remarks>
        /// <param name="offset">Offset to the type 2 data.</param>
        /// <param name="datNum">The .dat number to read from.</param>
        /// <returns>The decompressed data.</returns>
        public static byte[] GetType2DecompressedData(int offset, int datNum, string datName)
        {
            offset = OffsetCorrection(datNum, offset);

            var datPath = string.Format(Info.datDir, datName, datNum);

            byte[] decompressedData;
            List<byte> type2Bytes = new List<byte>();

            using(BinaryReader br = new BinaryReader(File.OpenRead(datPath)))
            {
                br.BaseStream.Seek(offset, SeekOrigin.Begin);

                int headerLength = br.ReadInt32();

                br.ReadBytes(16);

                int dataBlockCount = br.ReadInt32();

                for (int i = 0;  i < dataBlockCount; i++)
                {
                    br.BaseStream.Seek(offset + (24 + (8 * i)), SeekOrigin.Begin);

                    int dataBlockOffset = br.ReadInt32();

                    br.BaseStream.Seek(offset + headerLength + dataBlockOffset, SeekOrigin.Begin);

                    br.ReadBytes(8);

                    int compressedSize = br.ReadInt32();
                    int uncompressedSize = br.ReadInt32();

                    if(compressedSize == 32000)
                    {
                        type2Bytes.AddRange(br.ReadBytes(uncompressedSize));
                    }
                    else
                    {
                        byte[] compressedData = br.ReadBytes(compressedSize);

                        decompressedData = new byte[uncompressedSize];

                        using (MemoryStream ms = new MemoryStream(compressedData))
                        {
                            using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
                            {
                                int count = ds.Read(decompressedData, 0, uncompressedSize);
                            }
                        }

                        type2Bytes.AddRange(decompressedData);
                    }
                }
            }
            return type2Bytes.ToArray();
        }

        /// <summary>
        /// Decompressed the data for files of type 3.
        /// </summary>
        /// <remarks>
        /// Type 3 is model data.
        /// </remarks>
        /// <param name="offset">Offset to the type 3 data.</param>
        /// <param name="datNum">The .dat number to read from.</param>
        /// <returns></returns>
        public static Tuple<byte[], int, int> GetType3DecompressedData(int offset, int datNum, string datName)
        {
            offset = OffsetCorrection(datNum, offset);
            var datPath = string.Format(Info.datDir, datName, datNum);

            List<byte> byteList = new List<byte>();
            int meshCount, materialCount;

            using (BinaryReader br = new BinaryReader(File.OpenRead(datPath)))
            {
                br.BaseStream.Seek(offset, SeekOrigin.Begin);

                int headerLength = br.ReadInt32();
                int fileType = br.ReadInt32();
                int decompressedSize = br.ReadInt32();
                br.ReadBytes(8);
                int parts = br.ReadInt16();

                int endOfHeader = offset + headerLength;

                byteList.AddRange(new byte[68]);

                br.BaseStream.Seek(offset + 24, SeekOrigin.Begin);

                int[] chunkUncompSizes = new int[11];
                int[] chunkLengths = new int[11];
                int[] chunkOffsets = new int[11];
                int[] chunkBlockStart = new int[11];
                int[] chunkNumBlocks = new int[11];

                for (int i = 0; i < 11; i++)
                {
                    chunkUncompSizes[i] = br.ReadInt32();
                }
                for (int i = 0; i < 11; i++)
                {
                    chunkLengths[i] = br.ReadInt32();
                }
                for (int i = 0; i < 11; i++)
                {
                    chunkOffsets[i] = br.ReadInt32();
                }
                for (int i = 0; i < 11; i++)
                {
                    chunkBlockStart[i] = br.ReadInt16();
                }
                int totalBlocks = 0;
                for (int i = 0; i < 11; i++)
                {
                    chunkNumBlocks[i] = br.ReadInt16();

                    totalBlocks += chunkNumBlocks[i];
                }

                meshCount = br.ReadInt16();
                materialCount = br.ReadInt16();

                br.ReadBytes(4);

                int[] blockSizes = new int[totalBlocks];

                for (int i = 0; i < totalBlocks; i++)
                {
                    blockSizes[i] = br.ReadInt16();
                }

                br.BaseStream.Seek(offset + headerLength + chunkOffsets[0], SeekOrigin.Begin);

                for (int i = 0; i < totalBlocks; i++)
                {
                    int lastPos = (int)br.BaseStream.Position;

                    br.ReadBytes(8);

                    int partCompSize = br.ReadInt32();
                    int partDecompSize = br.ReadInt32();

                    if (partCompSize == 32000)
                    {
                        byteList.AddRange(br.ReadBytes(partDecompSize));
                    }
                    else
                    {
                        byte[] partDecompBytes = new byte[partDecompSize];
                        using (MemoryStream ms = new MemoryStream(br.ReadBytes(partCompSize)))
                        {
                            using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
                            {
                                ds.Read(partDecompBytes, 0, partDecompSize);
                            }
                        }
                        byteList.AddRange(partDecompBytes);
                    }

                    br.BaseStream.Seek(lastPos + blockSizes[i], SeekOrigin.Begin);
                }
            }

            return new Tuple<byte[], int, int>(byteList.ToArray(), meshCount, materialCount);
        }

        #endregion //Decompressors

        #region Offset Helpers
        /// <summary>
        /// Changes the offset to the correct location based on .dat number.
        /// </summary>
        /// <param name="datNum">The .dat number being used.</param>
        /// <param name="offset">The offset to correct.</param>
        /// <returns>The corrected offset.</returns>
        public static int OffsetCorrection(int datNum, int offset)
        {
            return offset - (16 * datNum);
        }

        /// <summary>
        /// Gets the offset of the item data.
        /// </summary>
        /// <remarks>
        /// Used to obtain the offset of the items IMC, MTRL, MDL, and TEX data
        /// </remarks>
        /// <param name="folderHash">The hash value of the internal folder path.</param>
        /// <param name="fileHash">The hash value of the internal file path.</param>
        /// <returns>The offset in which the items data is located</returns>
        public static int GetDataOffset(int folderHash, int fileHash, string indexName)
        {
            int itemOffset = 0;

            var indexPath = string.Format(Info.indexDir, indexName);

            try
            {
                using (BinaryReader br = new BinaryReader(File.OpenRead(indexPath)))
                {
                    br.BaseStream.Seek(1036, SeekOrigin.Begin);
                    int numOfFiles = br.ReadInt32();

                    br.BaseStream.Seek(2048, SeekOrigin.Begin);
                    for (int i = 0; i < numOfFiles; br.ReadBytes(4), i += 16)
                    {
                        int fileNameHash = br.ReadInt32();

                        if (fileNameHash == fileHash)
                        {
                            int folderPathHash = br.ReadInt32();

                            if (folderPathHash == folderHash)
                            {
                                itemOffset = br.ReadInt32() * 8;
                                break;
                            }
                            else
                            {
                                br.ReadBytes(4);
                            }
                        }
                        else
                        {
                            br.ReadBytes(8);
                        }
                    }
                }
            }
            catch(Exception e)
            {
                MessageBox.Show("[Helper] Error Accessing Index File \n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return itemOffset;
        }

        /// <summary>
        /// Gets the offset of the EXD data.
        /// </summary>
        /// <param name="folderHash">The hash value of the internal folder path.</param>
        /// <param name="fileHash">The hash value of the internal file path.</param>
        /// <returns>The offset in which the EXD data is located.</returns>
        public static int GetEXDOffset(int folderHash, int fileHash)
        {
            int exdOffset = 0;

            var indexPath = string.Format(Info.indexDir, Strings.EXDDat);

            try
            {
                using (BinaryReader br = new BinaryReader(File.OpenRead(indexPath)))
                {
                    br.BaseStream.Seek(1036, SeekOrigin.Begin);
                    int numOfFiles = br.ReadInt32();

                    br.BaseStream.Seek(2048, SeekOrigin.Begin);
                    for (int i = 0; i < numOfFiles; br.ReadBytes(4), i += 16)
                    {
                        int fileNameHash = br.ReadInt32();

                        if (fileNameHash == fileHash)
                        {
                            int folderPathHash = br.ReadInt32();

                            if (folderPathHash == folderHash)
                            {
                                byte[] offset = br.ReadBytes(4);
                                exdOffset = BitConverter.ToInt32(offset, 0) * 8;
                                break;
                            }
                            else
                            {
                                br.ReadBytes(4);
                            }
                        }
                        else
                        {
                            br.ReadBytes(8);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("[Helper] Error Accessing Index A File \n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return exdOffset;
        }

        /// <summary>
        /// Updates the .index files offset for a given item.
        /// </summary>
        /// <param name="offset">The new offset to be used.</param>
        /// <param name="fullPath">The internal path of the file whos offset is to be updated.</param>
        /// <returns>The offset which was replaced.</returns>
        public static int UpdateIndex(long offset, string fullPath, string indexName)
        {
            var folderHash = FFCRC.GetHash(fullPath.Substring(0, fullPath.LastIndexOf("/")));
            var fileHash = FFCRC.GetHash(Path.GetFileName(fullPath));
            int oldOffset = 0;

            var indexPath = string.Format(Info.indexDir, indexName);

            try
            {
                using (var index = File.Open(indexPath, FileMode.Open))
                {
                    using (BinaryReader br = new BinaryReader(index))
                    {
                        using (BinaryWriter bw = new BinaryWriter(index))
                        {
                            br.BaseStream.Seek(1036, SeekOrigin.Begin);
                            int numOfFiles = br.ReadInt32();

                            br.BaseStream.Seek(2048, SeekOrigin.Begin);
                            for (int i = 0; i < numOfFiles; br.ReadBytes(4), i += 16)
                            {
                                int fileNameHash = br.ReadInt32();

                                if (fileNameHash == fileHash)
                                {
                                    int folderPathHash = br.ReadInt32();

                                    if (folderPathHash == folderHash)
                                    {
                                        oldOffset = br.ReadInt32();
                                        bw.BaseStream.Seek(br.BaseStream.Position - 4, SeekOrigin.Begin);
                                        bw.Write(offset / 8);
                                        break;
                                    }
                                    else
                                    {
                                        br.ReadBytes(4);
                                    }
                                }
                                else
                                {
                                    br.ReadBytes(8);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("[Helper] Error Accessing Index File \n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return oldOffset;
        }

        /// <summary>
        /// Updates the .index2 files offset for a given item.
        /// </summary>
        /// <param name="offset">The new offset to be used.</param>
        /// <param name="fullPath">The internal path of the file whos offset is to be updated.</param>
        /// <returns>The offset which was replaced.</returns>
        public static void UpdateIndex2(long offset, string fullPath, string indexName)
        {
            var pathHash = FFCRC.GetHash(fullPath);

            var index2Path = string.Format(Info.index2Dir, indexName);

            try
            {
                using (var index = File.Open(index2Path, FileMode.Open))
                {
                    using (BinaryReader br = new BinaryReader(index))
                    {
                        using (BinaryWriter bw = new BinaryWriter(index))
                        {
                            br.BaseStream.Seek(1036, SeekOrigin.Begin);
                            int numOfFiles = br.ReadInt32();

                            br.BaseStream.Seek(2048, SeekOrigin.Begin);
                            for (int i = 0; i < numOfFiles; i += 8)
                            {
                                int fullPathHash = br.ReadInt32();

                                if (fullPathHash == pathHash)
                                {
                                    bw.BaseStream.Seek(br.BaseStream.Position, SeekOrigin.Begin);
                                    bw.Write((int)(offset / 8));
                                    break;
                                }
                                else
                                {
                                    br.ReadBytes(4);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("[Helper] Error Accessing Index 2 File \n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
        #endregion Offset Helpers

        #region IO
        /// <summary>
        /// Determines whether a given folder path exists
        /// </summary>
        /// <param name="folderHash">The hash value of the internal folder path.</param>
        /// <returns></returns>
        public static bool FolderExists(int folderHash, string indexName)
        {

            var indexPath = string.Format(Info.indexDir, indexName);

            try
            {
                using (BinaryReader br = new BinaryReader(File.OpenRead(indexPath)))
                {
                    br.BaseStream.Seek(1036, SeekOrigin.Begin);
                    int numOfFiles = br.ReadInt32();

                    br.BaseStream.Seek(2048, SeekOrigin.Begin);
                    for (int i = 0; i < numOfFiles; br.ReadBytes(4), i += 16)
                    {
                        br.ReadBytes(4);
                        int folderPathHash = br.ReadInt32();

                        if (folderPathHash == folderHash)
                        {
                            return true;
                        }
                        else
                        {
                            br.ReadBytes(4);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("[Helper] Error Accessing Index File \n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return false;
        }


        /// <summary>
        /// Determines whether a given file name exists
        /// </summary>
        /// <param name="fileHash">The hash value of the internal file path.</param>
        /// <param name="folderHash">The hash value of the internal file name.</param>
        /// <returns></returns>
        public static bool FileExists(int fileHash, int folderHash, string indexName)
        {
            var indexPath = string.Format(Info.indexDir, indexName);

            try
            {
                using (BinaryReader br = new BinaryReader(File.OpenRead(indexPath)))
                {
                    br.BaseStream.Seek(1036, SeekOrigin.Begin);
                    int numOfFiles = br.ReadInt32();

                    br.BaseStream.Seek(2048, SeekOrigin.Begin);
                    for (int i = 0; i < numOfFiles; br.ReadBytes(4), i += 16)
                    {
                        int fileNameHash = br.ReadInt32();

                        if (fileNameHash == fileHash)
                        {
                            int folderPathHash = br.ReadInt32();

                            if (folderPathHash == folderHash)
                            {
                                return true;
                            }
                            else
                            {
                                br.ReadBytes(4);
                            }
                        }
                        else
                        {
                            br.ReadBytes(8);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("[Helper] Error Accessing Index File \n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return false;
        }

        /// <summary>
        /// Determines which folder paths exist given several folder path hashes.
        /// </summary>
        /// <param name="pathPart">A dictionary containing folder path hashes and their associated part number.</param>
        /// <returns>A sorted set of part numbers whos folder path was found.</returns>
        public static SortedSet<ComboBoxInfo> FolderExistsList(Dictionary<int, int> pathPart, string indexName)
        {
            SortedSet<ComboBoxInfo> parts = new SortedSet<ComboBoxInfo>();

            var indexPath = string.Format(Info.indexDir, indexName);

            try
            {
                using (BinaryReader br = new BinaryReader(File.OpenRead(indexPath)))
                {
                    br.BaseStream.Seek(1036, SeekOrigin.Begin);
                    int numOfFiles = br.ReadInt32();

                    br.BaseStream.Seek(2048, SeekOrigin.Begin);
                    for (int i = 0; i < numOfFiles; br.ReadBytes(4), i += 16)
                    {
                        br.ReadBytes(4);
                        int folderPathHash = br.ReadInt32();

                        if (pathPart.Keys.Contains(folderPathHash))
                        {                          
                            parts.Add(new ComboBoxInfo() { Name = pathPart[folderPathHash].ToString(), ID = pathPart[folderPathHash].ToString(), IsNum = true });
                        }
                        br.ReadBytes(4);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("[Helper] Error Accessing Index File \n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return parts;
        }

        /// <summary>
        /// Determines which folder paths exist given several folder path hashes.
        /// </summary>
        /// <param name="pathRace">A dictionary containing folder path hashes and their associated race.</param>
        /// <returns>A sorted set of races whos folder path was found.</returns>
        public static SortedSet<ComboBoxInfo> FolderExistsListRace(Dictionary<int, string> pathRace, string indexName)
        {
            SortedSet<ComboBoxInfo> raceList = new SortedSet<ComboBoxInfo>();

            var indexPath = string.Format(Info.indexDir, indexName);


            try
            {
                using (BinaryReader br = new BinaryReader(File.OpenRead(indexPath)))
                {
                    br.BaseStream.Seek(1036, SeekOrigin.Begin);
                    int numOfFiles = br.ReadInt32();

                    br.BaseStream.Seek(2048, SeekOrigin.Begin);
                    for (int i = 0; i < numOfFiles; br.ReadBytes(4), i += 16)
                    {
                        br.ReadBytes(4);
                        int folderPathHash = br.ReadInt32();

                        if (pathRace.Keys.Contains(folderPathHash))
                        {
                            
                            raceList.Add(new ComboBoxInfo() { Name = Info.IDRace[pathRace[folderPathHash]], ID = pathRace[folderPathHash], IsNum = false });
                        }
                        br.ReadBytes(4);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("[Helper] Error Accessing Index File \n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return raceList;
        }

        /// <summary>
        /// Gets the hashes of all the files contained within a given folder.
        /// </summary>
        /// <param name="folderHash">The hash value of the internal folder path.</param>
        /// <returns>A list of file hashes</returns>
        public static List<int> GetAllFilesInFolder(int folderHash, string indexName)
        {
            List<int> fileOffsetDict = new List<int>();

            var indexPath = string.Format(Info.indexDir, indexName);

            try
            {
                using (BinaryReader br = new BinaryReader(File.OpenRead(indexPath)))
                {
                    br.BaseStream.Seek(1036, SeekOrigin.Begin);
                    int totalFiles = br.ReadInt32();

                    br.BaseStream.Seek(2048, SeekOrigin.Begin);
                    for (int i = 0; i < totalFiles; br.ReadBytes(4), i += 16)
                    {
                        int fileNameHash = br.ReadInt32();

                        int folderPathHash = br.ReadInt32();

                        if (folderPathHash == folderHash)
                        {
                            fileOffsetDict.Add(fileNameHash);
                            br.ReadBytes(4);
                        }
                        else
                        {
                            br.ReadBytes(4);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("[Helper] Error Accessing Index File \n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return fileOffsetDict;
        }

        /// <summary>
        /// Checks the index for the number of dats the game will attempt to read
        /// </summary>
        /// <returns></returns>
        public static bool CheckIndex()
        {
            bool problemFound = false;

            foreach(var indexFile in Info.ModIndexDict)
            {
                var indexPath = string.Format(Info.indexDir, indexFile.Key);
                var index2Path = string.Format(Info.index2Dir, indexFile.Key);

                try
                {
                    using (BinaryReader br = new BinaryReader(File.OpenRead(indexPath)))
                    {
                        br.BaseStream.Seek(1104, SeekOrigin.Begin);

                        var numDats = br.ReadInt16();

                        if (numDats != indexFile.Value)
                        {
                            problemFound = true;
                        }
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("[Helper] Error Accessing Index File \n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                try
                {
                    using (BinaryReader br = new BinaryReader(File.OpenRead(index2Path)))
                    {
                        br.BaseStream.Seek(1104, SeekOrigin.Begin);

                        var numDats = br.ReadInt16();

                        if (numDats != indexFile.Value)
                        {
                            problemFound = true;
                        }
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("[Helper] Error Accessing Index 2 File \n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            return problemFound;
        }

        /// <summary>
        /// Changes the number of dats the game will attempt to read to 5
        /// </summary>
        public static void FixIndex()
        {
            foreach (var indexFile in Info.ModIndexDict)
            {
                var indexPath = string.Format(Info.indexDir, indexFile.Key);
                var index2Path = string.Format(Info.index2Dir, indexFile.Key);

                try
                {
                    using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(indexPath)))
                    {
                        bw.BaseStream.Seek(1104, SeekOrigin.Begin);
                        bw.Write((byte)indexFile.Value);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("[Helper] Error Accessing Index File \n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                try
                {
                    using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(index2Path)))
                    {
                        bw.BaseStream.Seek(1104, SeekOrigin.Begin);
                        bw.Write((byte)indexFile.Value);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("[Helper] Error Accessing Index File \n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        #endregion IO

        #region Misc
        /// <summary>
        /// Gets the type of items contained within a category.
        /// </summary>
        /// <param name="selectedCategory">The currently selected category.</param>
        /// <returns>The type of items contained within a category.</returns>
        public static string GetCategoryType(string selectedCategory)
        {
            string itemType;

            if (selectedCategory.Equals(Strings.Main_Hand) || selectedCategory.Equals(Strings.Off_Hand) || selectedCategory.Equals(Strings.Main_Off) || selectedCategory.Equals(Strings.Two_Handed))
            {
                itemType = "weapon";

            }
            else if (selectedCategory.Equals(Strings.Ears) || selectedCategory.Equals(Strings.Neck) || selectedCategory.Equals(Strings.Wrists) || selectedCategory.Equals(Strings.Rings))
            {
                itemType = "accessory";
            }
            else if (selectedCategory.Equals(Strings.Food))
            {
                itemType = "food";
            }
            else if (selectedCategory.Equals(Strings.Mounts) || selectedCategory.Equals(Strings.Minions) || selectedCategory.Equals(Strings.Pets))
            {
                itemType = "monster";
            }
            else if (selectedCategory.Equals(Strings.Character))
            {
                itemType = "character";
            }
            else
            {
                itemType = "equipment";
            }

            return itemType;
        }

        /// <summary>
        /// Converts a strings case to Title Case.
        /// </summary>
        /// <param name="mString">The string to convert.</param>
        /// <returns>The converted string.</returns>
        public static string ToTitleCase(string mString)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(mString.ToLower());
        }
        #endregion Misc
    }
}
