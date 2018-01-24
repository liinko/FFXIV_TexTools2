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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FFXIV_TexTools2.IO
{
    public class ImportRaw
    {
        public static void ImportType2(string filePath, ItemData item, string internalFilePath, string category)
        {
            var ext = Path.GetExtension(filePath);
            var cat = ext.Substring(1, ext.Length - 1);

            JsonEntry modEntry = null;
            int lineNum = 0;
            int newOffset = 0;
            bool inModList = false;


            List<byte> newData = new List<byte>();
            List<byte> headerData = new List<byte>();
            List<byte> dataBlocks = new List<byte>();

            try
            {
                using (StreamReader sr = new StreamReader(Properties.Settings.Default.Modlist_Directory))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        modEntry = JsonConvert.DeserializeObject<JsonEntry>(line);
                        if (modEntry.fullPath.Equals(filePath))
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

            var rawBytes = File.ReadAllBytes(filePath);


            headerData.AddRange(BitConverter.GetBytes(128));
            headerData.AddRange(BitConverter.GetBytes(2));
            headerData.AddRange(BitConverter.GetBytes(rawBytes.Length));

            int dataOffset = 0;
            int totalCompSize = 0;
            int uncompressedLength = rawBytes.Length;

            int partCount = (int)Math.Ceiling(uncompressedLength / 16000f);

            headerData.AddRange(BitConverter.GetBytes(partCount));

            int remainder = uncompressedLength;

            using (BinaryReader br = new BinaryReader(new MemoryStream(rawBytes)))
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

            var headerSize = 128;

            if(headerData.Count > 128)
            {
                headerData.RemoveRange(0, 4);
                headerData.InsertRange(0, BitConverter.GetBytes(256));
                headerSize = 256;
            }
            int headerPadding = headerSize - headerData.Count;

            headerData.AddRange(new byte[headerPadding]);

            newData.AddRange(headerData);
            newData.AddRange(dataBlocks);

            newOffset = WriteToDat(newData, modEntry, inModList, internalFilePath, category, item.ItemName, lineNum, Strings.ItemsDat);

            if(newOffset == 0)
            {
                MessageBox.Show("Import Failed. \n\n" +
                    "internalFilePath: " + internalFilePath +
                    "\n\ncat: " + cat, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            }
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
            if (internalFilePath.Equals(""))
            {
                return 0;
            }

            int offset = 0;
            bool dataOverwritten = false;

            string datNum = Info.ModDatDict[datName];

            var modDatPath = string.Format(Info.datDir, datName, datNum);

            var datOffsetAmount = 16 * int.Parse(datNum);


            if (inModList)
            {
                if (modEntry.modOffset == 0)
                {
                    MessageBox.Show("TexTools detected a Mod List entry with a Mod Offset of 0.\n\n" +
                        "Please submit a bug report along with your modlist file.", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return 0;
                }
                else if (modEntry.originalOffset == 0)
                {
                    MessageBox.Show("TexTools detected a Mod List entry with an Original Offset of 0.\n\n" +
                        "Please submit a bug report along with your modlist file.", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return 0;
                }
            }

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
                        if (modEntry.modOffset != 0)
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
                    }
                    else
                    {
                        int emptyLength = 0;
                        int emptyLine = 0;

                        /* 
                         * If there is an empty entry in the modlist and the compressed data being imported is smaller or equal to the available space
                        *  write the compressed data in the existing space.
                        */
                        try
                        {
                            foreach (string line in File.ReadAllLines(Properties.Settings.Default.Modlist_Directory))
                            {
                                JsonEntry emptyEntry = JsonConvert.DeserializeObject<JsonEntry>(line);

                                if (emptyEntry.fullPath.Equals(""))
                                {
                                    if (emptyEntry.modOffset != 0)
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

                                            if (inModList)
                                            {
                                                originalOffset = modEntry.originalOffset;

                                                JsonEntry replaceOriginalEntry = new JsonEntry()
                                                {
                                                    category = String.Empty,
                                                    name = "Empty Replacement",
                                                    fullPath = String.Empty,
                                                    originalOffset = 0,
                                                    modOffset = modEntry.modOffset,
                                                    modSize = modEntry.modSize,
                                                    datFile = Strings.ItemsDat
                                                };

                                                string[] oLines = File.ReadAllLines(Properties.Settings.Default.Modlist_Directory);
                                                oLines[lineNum] = JsonConvert.SerializeObject(replaceOriginalEntry);
                                                File.WriteAllLines(Properties.Settings.Default.Modlist_Directory, oLines);
                                            }


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

                                            string[] lines = File.ReadAllLines(Properties.Settings.Default.Modlist_Directory);
                                            lines[emptyLine] = JsonConvert.SerializeObject(replaceEntry);
                                            File.WriteAllLines(Properties.Settings.Default.Modlist_Directory, lines);

                                            offset = emptyEntry.modOffset;

                                            dataOverwritten = true;
                                            break;
                                        }
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

                            if (offset != 0)
                            {
                                bw.Write(data.ToArray());
                            }
                            else
                            {
                                MessageBox.Show("[Import] There was an issue obtaining the .dat4 offset to write data to, try importing again. " +
                                    "\n\n If the problem persists, please submit a bug report.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }

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
                if (offset != 0)
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

                        string[] lines = File.ReadAllLines(Properties.Settings.Default.Modlist_Directory);
                        lines[lineNum] = JsonConvert.SerializeObject(replaceEntry);
                        File.WriteAllLines(Properties.Settings.Default.Modlist_Directory, lines);
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
                        using (StreamWriter modFile = new StreamWriter(Properties.Settings.Default.Modlist_Directory, true))
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
            }

            return offset;
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
