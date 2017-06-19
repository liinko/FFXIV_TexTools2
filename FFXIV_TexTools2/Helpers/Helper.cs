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
        /// Decompresses the byte array 
        /// </summary>
        /// <param name="offset">Offset of the byte list to decompress</param>
        /// <returns></returns>
        public static byte[] GetDecompressedBytes(int offset)
        {
            List<byte> byteList = new List<byte>();

            using (BinaryReader br = new BinaryReader(File.OpenRead(Info.aDatDir)))
            {
                br.BaseStream.Seek(offset, SeekOrigin.Begin);

                int headerLength = br.ReadInt32();
                int type = br.ReadInt32();
                int decompressedSize = br.ReadInt32();
                br.ReadBytes(8);
                int parts = br.ReadInt32();

                int endOfHeader = offset + headerLength;
                int partStart = offset + 24;

                for (int f = 0, g = 0; f < parts; f++)
                {
                    //read the current parts info
                    br.BaseStream.Seek(partStart + g, SeekOrigin.Begin);
                    int fromHeaderEnd = br.ReadInt32();
                    int partLength = br.ReadInt16();
                    int partSize = br.ReadInt16();
                    int partLocation = endOfHeader + fromHeaderEnd;

                    //go to part data and read its info
                    br.BaseStream.Seek(partLocation, SeekOrigin.Begin);
                    br.ReadBytes(8);
                    int partCompressedSize = br.ReadInt32();
                    int partDecompressedSize = br.ReadInt32();

                    //if data is already uncompressed add to list if not decompress and add to list
                    if (partCompressedSize == 32000)
                    {
                        byte[] forlist = br.ReadBytes(partDecompressedSize);
                        byteList.AddRange(forlist);
                    }
                    else
                    {
                        byte[] forlist = br.ReadBytes(partCompressedSize);
                        byte[] partDecompressedBytes = new byte[partDecompressedSize];

                        using (MemoryStream ms = new MemoryStream(forlist))
                        {
                            using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
                            {
                                int count = ds.Read(partDecompressedBytes, 0x00, partDecompressedSize);
                            }
                        }
                        byteList.AddRange(partDecompressedBytes);
                    }
                    g += 8;
                }

                if (byteList.Count < decompressedSize)
                {
                    int difference = decompressedSize - byteList.Count;
                    byte[] padd = new byte[difference];
                    Array.Clear(padd, 0, difference);
                    byteList.AddRange(padd);
                }
            }

            return byteList.ToArray();
        }

        public static byte[] GetDecompressedBytes(int offset, int datNum)
        {
            List<byte> byteList = new List<byte>();

            offset = OffsetCorrection(datNum, offset);

            using (BinaryReader br = new BinaryReader(File.OpenRead(Info.datDir + datNum)))
            {
                br.BaseStream.Seek(offset, SeekOrigin.Begin);

                int headerLength = br.ReadInt32();
                int type = br.ReadInt32();
                int decompressedSize = br.ReadInt32();
                br.ReadBytes(8);
                int parts = br.ReadInt32();

                int endOfHeader = offset + headerLength;
                int partStart = offset + 24;

                for (int f = 0, g = 0; f < parts; f++)
                {
                    //read the current parts info
                    br.BaseStream.Seek(partStart + g, SeekOrigin.Begin);
                    int fromHeaderEnd = br.ReadInt32();
                    int partLength = br.ReadInt16();
                    int partSize = br.ReadInt16();
                    int partLocation = endOfHeader + fromHeaderEnd;

                    //go to part data and read its info
                    br.BaseStream.Seek(partLocation, SeekOrigin.Begin);
                    br.ReadBytes(8);
                    int partCompressedSize = br.ReadInt32();
                    int partDecompressedSize = br.ReadInt32();

                    //if data is already uncompressed add to list if not decompress and add to list
                    if (partCompressedSize == 32000)
                    {
                        byte[] forlist = br.ReadBytes(partDecompressedSize);
                        byteList.AddRange(forlist);
                    }
                    else
                    {
                        byte[] forlist = br.ReadBytes(partCompressedSize);
                        byte[] partDecompressedBytes = new byte[partDecompressedSize];

                        using (MemoryStream ms = new MemoryStream(forlist))
                        {
                            using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
                            {
                                int count = ds.Read(partDecompressedBytes, 0x00, partDecompressedSize);
                            }
                        }
                        byteList.AddRange(partDecompressedBytes);
                    }
                    g += 8;
                }

                if (byteList.Count < decompressedSize)
                {
                    int difference = decompressedSize - byteList.Count;
                    byte[] padd = new byte[difference];
                    Array.Clear(padd, 0, difference);
                    byteList.AddRange(padd);
                }
            }

            return byteList.ToArray();
        }

        public static byte[] GetDecompressedIMCBytes(int offset, int datNum)
        {
            offset = OffsetCorrection(datNum, offset);
            byte[] decompBytes;

            using(BinaryReader b = new BinaryReader(File.OpenRead(Info.datDir + datNum)))
            {
                b.BaseStream.Seek(offset, SeekOrigin.Begin);

                int headerLength = b.ReadInt32();

                b.ReadBytes(headerLength + 4);

                int compressedSize = b.ReadInt32();
                int uncompressedSize = b.ReadInt32();

                byte[] data = b.ReadBytes(compressedSize);
                decompBytes = new byte[uncompressedSize];

                using (MemoryStream ms = new MemoryStream(data))
                {
                    using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
                    {
                        int count = ds.Read(decompBytes, 0, uncompressedSize);
                    }
                }
            }

            return decompBytes;
        }
        #endregion //Decompressors

#region offset
        public static int OffsetCorrection(int datNum, int offset)
        {
            return offset - (16 * datNum);
        }


        public static int GetOffset(int folderHash, int fileHash)
        {
            int fileOffset = 0;

            try
            {
                using (BinaryReader br = new BinaryReader(File.OpenRead(Info.indexDir)))
                {
                    br.BaseStream.Seek(1036, SeekOrigin.Begin);
                    int totalFiles = br.ReadInt32();

                    br.BaseStream.Seek(2048, SeekOrigin.Begin);
                    for (int i = 0; i < totalFiles; br.ReadBytes(4), i += 16)
                    {
                        int tempOffset = br.ReadInt32();

                        if (tempOffset == fileHash)
                        {
                            int fTempOffset = br.ReadInt32();

                            if (fTempOffset == folderHash)
                            {
                                fileOffset = br.ReadInt32() * 8;
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
            return fileOffset;
        }

        public static int GetAOffset(int folderHash, int fileHash)
        {
            int fileOffset = 0;

            try
            {
                using (BinaryReader br = new BinaryReader(File.OpenRead(Info.aIndexDir)))
                {
                    br.BaseStream.Seek(1036, SeekOrigin.Begin);
                    int totalFiles = br.ReadInt32();

                    br.BaseStream.Seek(2048, SeekOrigin.Begin);
                    for (int i = 0; i < totalFiles; br.ReadBytes(4), i += 16)
                    {
                        int tempOffset = br.ReadInt32();

                        if (tempOffset == fileHash)
                        {
                            int fTempOffset = br.ReadInt32();

                            if (fTempOffset == folderHash)
                            {
                                byte[] offset = br.ReadBytes(4);
                                fileOffset = BitConverter.ToInt32(offset, 0) * 8;
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

            return fileOffset;
        }


        public static int GetOffset(int fileHash)
        {
            int fileOffset = 0;

            try
            {
                using (BinaryReader br = new BinaryReader(File.OpenRead(Info.indexDir)))
                {
                    br.BaseStream.Seek(1036, SeekOrigin.Begin);
                    int totalFiles = br.ReadInt32();

                    br.BaseStream.Seek(2048, SeekOrigin.Begin);
                    for (int i = 0; i < totalFiles; br.ReadBytes(4), i += 16)
                    {
                        int tempFileOffset = br.ReadInt32();

                        if (tempFileOffset == fileHash)
                        {
                            br.ReadBytes(4);
                            byte[] offset = br.ReadBytes(4);
                            fileOffset = BitConverter.ToInt32(offset, 0) * 8;
                            break;
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

            return fileOffset;
        }

        public static int UpdateIndex(long offset, string fullPath)
        {
            var folderHash = FFCRC.GetHash(fullPath.Substring(0, fullPath.LastIndexOf("/")));
            var fileHash = FFCRC.GetHash(Path.GetFileName(fullPath));
            int oldOffset = 0;

            try
            {
                using (var index = File.Open(Info.indexDir, FileMode.Open))
                {
                    using (BinaryReader br = new BinaryReader(index))
                    {
                        using (BinaryWriter bw = new BinaryWriter(index))
                        {
                            br.BaseStream.Seek(1036, SeekOrigin.Begin);
                            int totalFiles = br.ReadInt32();

                            br.BaseStream.Seek(2048, SeekOrigin.Begin);
                            for (int i = 0; i < totalFiles; br.ReadBytes(4), i += 16)
                            {
                                int tempOffset = br.ReadInt32();

                                if (tempOffset == fileHash)
                                {
                                    int fTempOffset = br.ReadInt32();

                                    if (fTempOffset == folderHash)
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

        public static void UpdateIndex2(long offset, string fullPath)
        {
            var pathHash = FFCRC.GetHash(fullPath);

            try
            {
                using (var index = File.Open(Info.index2Dir, FileMode.Open))
                {
                    using (BinaryReader br = new BinaryReader(index))
                    {
                        using (BinaryWriter bw = new BinaryWriter(index))
                        {
                            br.BaseStream.Seek(1036, SeekOrigin.Begin);
                            int totalFiles = br.ReadInt32();

                            br.BaseStream.Seek(2048, SeekOrigin.Begin);
                            for (int i = 0; i < totalFiles; i += 8)
                            {
                                int tempOffset = br.ReadInt32();

                                if (tempOffset == pathHash)
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
#endregion offset

#region IO
        public static bool FolderExists(int folderHash)
        {
            try
            {
                using (BinaryReader br = new BinaryReader(File.OpenRead(Info.indexDir)))
                {
                    br.BaseStream.Seek(1036, SeekOrigin.Begin);
                    int totalFiles = br.ReadInt32();

                    br.BaseStream.Seek(2048, SeekOrigin.Begin);
                    for (int i = 0; i < totalFiles; br.ReadBytes(4), i += 16)
                    {
                        br.ReadBytes(4);
                        int tempFolderOffset = br.ReadInt32();

                        if (tempFolderOffset == folderHash)
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

        public static bool FileExists(int fileHash, int folderHash)
        {
            try
            {
                using (BinaryReader br = new BinaryReader(File.OpenRead(Info.indexDir)))
                {
                    br.BaseStream.Seek(1036, SeekOrigin.Begin);
                    int totalFiles = br.ReadInt32();

                    br.BaseStream.Seek(2048, SeekOrigin.Begin);
                    for (int i = 0; i < totalFiles; br.ReadBytes(4), i += 16)
                    {
                        int tempFileOffset = br.ReadInt32();

                        if (tempFileOffset == fileHash)
                        {
                            int tempFolderOffset = br.ReadInt32();

                            if (tempFolderOffset == folderHash)
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

        public static SortedSet<ComboBoxInfo> FolderExistsList(Dictionary<int, int> mtrl)
        {
            SortedSet<ComboBoxInfo> parts = new SortedSet<ComboBoxInfo>();

            try
            {
                using (BinaryReader br = new BinaryReader(File.OpenRead(Info.indexDir)))
                {
                    br.BaseStream.Seek(1036, SeekOrigin.Begin);
                    int totalFiles = br.ReadInt32();

                    br.BaseStream.Seek(2048, SeekOrigin.Begin);
                    for (int i = 0; i < totalFiles; br.ReadBytes(4), i += 16)
                    {
                        br.ReadBytes(4);
                        int tempFolderOffset = br.ReadInt32();

                        if (mtrl.Keys.Contains(tempFolderOffset))
                        {
                            parts.Add(new ComboBoxInfo(mtrl[tempFolderOffset].ToString(), mtrl[tempFolderOffset].ToString()));
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

        public static SortedSet<ComboBoxInfo> FolderExistsListRace(Dictionary<int, string> races)
        {
            SortedSet<ComboBoxInfo> raceList = new SortedSet<ComboBoxInfo>();

            try
            {
                using (BinaryReader br = new BinaryReader(File.OpenRead(Info.indexDir)))
                {
                    br.BaseStream.Seek(1036, SeekOrigin.Begin);
                    int totalFiles = br.ReadInt32();

                    br.BaseStream.Seek(2048, SeekOrigin.Begin);
                    for (int i = 0; i < totalFiles; br.ReadBytes(4), i += 16)
                    {
                        br.ReadBytes(4);
                        int tempFolderOffset = br.ReadInt32();

                        if (races.Keys.Contains(tempFolderOffset))
                        {
                            raceList.Add(new ComboBoxInfo(Info.IDRace[races[tempFolderOffset]], races[tempFolderOffset]));
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

        public static Dictionary<int, int> GetAllFilesInFolder(int folderHash)
        {
            Dictionary<int, int> fileOffsetDict = new Dictionary<int, int>();

            try
            {
                using (BinaryReader br = new BinaryReader(File.OpenRead(Info.indexDir)))
                {
                    br.BaseStream.Seek(1036, SeekOrigin.Begin);
                    int totalFiles = br.ReadInt32();

                    br.BaseStream.Seek(2048, SeekOrigin.Begin);
                    for (int i = 0; i < totalFiles; br.ReadBytes(4), i += 16)
                    {
                        int tempFileHash = br.ReadInt32();

                        int tempFolderHash = br.ReadInt32();

                        if (tempFolderHash == folderHash)
                        {
                            fileOffsetDict.Add(tempFileHash, BitConverter.ToInt32(br.ReadBytes(4), 0) * 8);
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

        public static bool CheckIndex()
        {
            bool problem = false;

            try
            {
                using (BinaryReader br = new BinaryReader(File.OpenRead(Info.indexDir)))
                {
                    br.BaseStream.Seek(1104, SeekOrigin.Begin);

                    var numDats = br.ReadInt16();

                    if (numDats != 4)
                    {
                        problem = true;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("[Helper] Error Accessing Index File \n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            try
            {
                using (BinaryReader br = new BinaryReader(File.OpenRead(Info.index2Dir)))
                {
                    br.BaseStream.Seek(1104, SeekOrigin.Begin);

                    var numDats = br.ReadInt16();

                    if (numDats != 4)
                    {
                        problem = true;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("[Helper] Error Accessing Index 2 File \n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }


            return problem;
        }

        public static void FixIndex()
        {
            try
            {
                using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(Info.indexDir)))
                {
                    bw.BaseStream.Seek(1104, SeekOrigin.Begin);
                    bw.Write((byte)4);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("[Helper] Error Accessing Index File \n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            try
            {
                using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(Info.index2Dir)))
                {
                    bw.BaseStream.Seek(1104, SeekOrigin.Begin);
                    bw.Write((byte)4);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("[Helper] Error Accessing Index File \n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
#endregion

        #region Misc
        public static string GetItemType(string selectedParent)
        {
            string itemType;

            if (selectedParent.Equals(Strings.Main_Hand) || selectedParent.Equals(Strings.Off_Hand) || selectedParent.Equals(Strings.Main_Off) || selectedParent.Equals(Strings.Two_Handed))
            {
                itemType = "weapon";

            }
            else if (selectedParent.Equals(Strings.Ears) || selectedParent.Equals(Strings.Neck) || selectedParent.Equals(Strings.Wrists) || selectedParent.Equals(Strings.Rings))
            {
                itemType = "accessory";
            }
            else if (selectedParent.Equals(Strings.Food))
            {
                itemType = "food";
            }
            else if (selectedParent.Equals(Strings.Mounts) || selectedParent.Equals(Strings.Minions) || selectedParent.Equals(Strings.Pets))
            {
                itemType = "monster";
            }
            else if (selectedParent.Equals(Strings.Character))
            {
                itemType = "character";
            }
            else
            {
                itemType = "equipment";
            }

            return itemType;
        }

        public static string ToTitleCase(string s)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s.ToLower());
        }
#endregion
    }
}
