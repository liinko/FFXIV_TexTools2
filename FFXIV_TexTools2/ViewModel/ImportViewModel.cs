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
using FFXIV_TexTools2.Material;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Input;

namespace FFXIV_TexTools2.ViewModel
{
    public class ImportViewModel
    {
        private ICommand importCommand;

        private bool canExecute = true;
        TexInfo texInfo;
        MTRLInfo mtrlInfo;
        string category, item, path, fPath;
        int newHeight, newWidth, newMipCount;

        public bool CanExecute
        {
            get
            {
                return this.canExecute;
            }

            set
            {
                if (this.canExecute == value)
                {
                    return;
                }

                this.canExecute = value;
            }
        }

        public ICommand ImportCommand
        {
            get
            {
                return importCommand;
            }
        }

        public ImportViewModel(TexInfo info, string category, string item, string map, string fullPath)
        {
            this.category = category;
            this.item = item;
            texInfo = info;
            fPath = fullPath;

            string dxPath = Path.GetFileNameWithoutExtension(fullPath);

            path = Properties.Settings.Default.Save_Directory + "/" + category + "/" + item + "/" + dxPath + ".dds";

            importCommand = new RelayCommand(Import);
        }

        public ImportViewModel(MTRLInfo info, string category, string item)
        {
            this.category = category;
            this.item = item;
            mtrlInfo = info;
            fPath = info.MTRLPath;
            path = Properties.Settings.Default.Save_Directory + "/" + category + "/" + item + "/" + Path.GetFileNameWithoutExtension(info.MTRLPath) + ".dds";

            importCommand = new RelayCommand(ImportColor);
        }


        public void Import(object obj)
        {
            int type, modOffset = 0, modSize = 0;
            int linenum = 0;
            bool inModList = false;
            bool overWrite = false;
            JsonEntry modEntry  = null;

            //If the file requesting to be imported exists
            if (File.Exists(path))
            {
                //check to see if the item has been modified before i.e. full path of item already exists in mod list 
                try
                {
                    using (StreamReader sr = new StreamReader(Info.modListDir))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            modEntry = JsonConvert.DeserializeObject<JsonEntry>(line);
                            if (modEntry.fullPath.Equals(fPath))
                            {
                                modOffset = modEntry.modOffset;
                                modSize = modEntry.modSize;
                                inModList = true;
                                break;
                            }
                            linenum++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("[Main] Error Accessing .modlist File \n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                //read file to be imported
                using (BinaryReader br = new BinaryReader(File.OpenRead(path)))
                {

                    br.BaseStream.Seek(12, SeekOrigin.Begin);
                    newHeight = br.ReadInt32();
                    newWidth = br.ReadInt32();
                    br.ReadBytes(8);
                    newMipCount = br.ReadInt32();

                    br.BaseStream.Seek(84, SeekOrigin.Begin);

                    type = br.ReadInt32();

                    //check for type equality
                    if (type == Info.DDSType[texInfo.Type])
                    {
                        List<byte> fullImport = new List<byte>();
                        long offset = 0;

                        int uncompLength = (int)new FileInfo(path).Length - 128;

                        var DDSInfo = ReadDDS(br);

                        fullImport.AddRange(MakeHeader(DDSInfo.Item2, DDSInfo.Item3, uncompLength));
                        fullImport.AddRange(MakeSecondHeader());
                        fullImport.AddRange(DDSInfo.Item1);

                        //open dat4 for writing 
                        try
                        {
                            using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(Info.modDatDir)))
                            {
                                //If the item has been previously modified and the compressed data being imported is smaller or equal to the exisiting data
                                //replace the existing data with new data.
                                if (inModList && fullImport.Count <= modSize)
                                {
                                    int sizeDiff = modSize - fullImport.Count;

                                    bw.BaseStream.Seek(modOffset - 64, SeekOrigin.Begin);

                                    bw.Write(fullImport.ToArray());

                                    bw.Write(new byte[sizeDiff]);

                                    Helper.UpdateIndex(modEntry.modOffset, fPath);
                                    Helper.UpdateIndex2(modEntry.modOffset, fPath);

                                    overWrite = true;
                                }
                                else
                                {
                                    int emptyLength = 0;
                                    int emptyLine = 0;

                                    //check for an empty entry in the modlist in which the data to be imported may fit.
                                    try
                                    {
                                        foreach (string line in File.ReadAllLines(Info.modListDir))
                                        {
                                            JsonEntry emptyEntry = JsonConvert.DeserializeObject<JsonEntry>(line);

                                            if (emptyEntry.fullPath.Equals(""))
                                            {
                                                emptyLength = emptyEntry.modSize;

                                                if (emptyLength > fullImport.Count)
                                                {
                                                    int sizeDiff = emptyLength - fullImport.Count;

                                                    bw.BaseStream.Seek(emptyEntry.modOffset - 64, SeekOrigin.Begin);

                                                    bw.Write(fullImport.ToArray());

                                                    bw.Write(new byte[sizeDiff]);

                                                    int oldOffset = Helper.UpdateIndex(emptyEntry.modOffset, fPath) * 8;
                                                    Helper.UpdateIndex2(emptyEntry.modOffset, fPath);

                                                    JsonEntry replaceEntry = new JsonEntry(category, item, fPath, oldOffset, emptyEntry.modOffset, emptyEntry.modSize);
                                                    string[] lines = File.ReadAllLines(Info.modListDir);
                                                    lines[emptyLine] = JsonConvert.SerializeObject(replaceEntry);
                                                    File.WriteAllLines(Info.modListDir, lines);

                                                    overWrite = true;
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

                                    if (!overWrite)
                                    {
                                        bw.BaseStream.Seek(0, SeekOrigin.End);

                                        if ((bw.BaseStream.Position & 0xff) != 0)
                                        {
                                            bw.Write((byte)0);
                                        }

                                        int eof = (int)bw.BaseStream.Position + fullImport.Count;

                                        while ((eof & 0xFF) != 0)
                                        {
                                            fullImport.AddRange(new byte[16]);
                                            eof = eof + 16;
                                        }

                                        offset = bw.BaseStream.Position + 64;

                                        bw.Write(fullImport.ToArray());
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("[Import] Error Accessing .dat4 File \n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }


                        if (!overWrite)
                        {
                            //If the item has been previously modifed, but the new compressed data to be imported is larger than the existing data
                            //remove the data from the modlist, leaving the offset and size intact for future use
                            if (inModList && fullImport.Count > modSize && modEntry != null)
                            {
                                JsonEntry replaceEntry = new JsonEntry(String.Empty, String.Empty, String.Empty, 0, modEntry.modOffset, modEntry.modSize);
                                string[] lines = File.ReadAllLines(Info.modListDir);
                                lines[linenum] = JsonConvert.SerializeObject(replaceEntry);
                                File.WriteAllLines(Info.modListDir, lines);
                            }

                            int oldOffset = Helper.UpdateIndex(offset, fPath) * 8;
                            Helper.UpdateIndex2(offset, fPath);

                            JsonEntry entry = new JsonEntry(category, item, fPath, oldOffset, (int)offset, fullImport.Count);

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
                    }
                    else
                    {
                        MessageBox.Show("Incorrect file type \nExpected: " + texInfo.Type + " Given: " + type, "Texture format error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Could not find file \n" + path, "File read Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ImportColor(Object obj)
        {
            if (File.Exists(path))
            {
                List<byte> mtrlBytes = new List<byte>();
                List<byte> complete = new List<byte>();

                int offset = mtrlInfo.MTRLOffset;
                long newOffset = 0;
                short fileSize;
                JsonEntry modEntry;
                int modOffset = 0;
                int modSize = 0;
                int lineNum = 0;
                bool inModList = false;
                bool overWrite = false;

                int datNum = ((offset / 8) & 0x000f) / 2;

                try
                {
                    using (StreamReader sr = new StreamReader(Info.modListDir))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            modEntry = JsonConvert.DeserializeObject<JsonEntry>(line);
                            if (modEntry.fullPath.Equals(fPath))
                            {
                                modOffset = modEntry.modOffset;
                                modSize = modEntry.modSize;
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

                using (BinaryReader br = new BinaryReader(new MemoryStream(Helper.GetDecompressedIMCBytes(offset, datNum))))
                {
                    br.BaseStream.Seek(4, SeekOrigin.Begin);
                    fileSize = br.ReadInt16();
                    short clrSize = br.ReadInt16();
                    short texNameSize = br.ReadInt16();
                    br.ReadBytes(2);
                    byte texNum = br.ReadByte();
                    byte mapNum = br.ReadByte();
                    byte clrNum = br.ReadByte();
                    byte unkNum = br.ReadByte();

                    int headerEnd = 16 + ((texNum + mapNum + clrNum) * 4);

                    br.BaseStream.Seek(0, SeekOrigin.Begin);

                    mtrlBytes.AddRange(br.ReadBytes(headerEnd));
                    mtrlBytes.AddRange(br.ReadBytes(texNameSize + 4));
                    br.ReadBytes(clrSize);

                    if(clrSize == 544)
                    {
                        using (BinaryReader br1 = new BinaryReader(File.OpenRead(path)))
                        {
                            br1.BaseStream.Seek(128, SeekOrigin.Begin);

                            mtrlBytes.AddRange(br1.ReadBytes(clrSize - 32));
                        }

                        string flagsPath = Path.Combine(Path.GetDirectoryName(path), (Path.GetFileNameWithoutExtension(path) + ".dat"));

                        using (BinaryReader br1 = new BinaryReader(File.OpenRead(flagsPath)))
                        {
                            br1.BaseStream.Seek(0, SeekOrigin.Begin);

                            mtrlBytes.AddRange(br1.ReadBytes(32));
                        }
                    }
                    else
                    {
                        using (BinaryReader br1 = new BinaryReader(File.OpenRead(path)))
                        {
                            br1.BaseStream.Seek(128, SeekOrigin.Begin);

                            mtrlBytes.AddRange(br1.ReadBytes(clrSize));
                        }
                    }

                    mtrlBytes.AddRange(br.ReadBytes(fileSize - (int)br.BaseStream.Position));
                }

                var compressed = Compressor(mtrlBytes.ToArray());
                int padding = 128 - (compressed.Length % 128);

                complete.AddRange(MakeMTRLHeader(fileSize, compressed.Length + padding));
                complete.AddRange(BitConverter.GetBytes(16));
                complete.AddRange(BitConverter.GetBytes(0));
                complete.AddRange(BitConverter.GetBytes(compressed.Length));
                complete.AddRange(BitConverter.GetBytes((int)fileSize));
                complete.AddRange(compressed);
                complete.AddRange(new byte[padding]);

                try
                {
                    using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(Info.modDatDir)))
                    {

                        if (inModList)
                        {
                            int sizeDiff = modSize - complete.Count;

                            bw.BaseStream.Seek(modOffset - 64, SeekOrigin.Begin);

                            bw.Write(complete.ToArray());

                            bw.Write(new byte[sizeDiff]);

                            overWrite = true;
                        }
                        else
                        {
                            int emptyLength = 0;
                            int emptyLine = 0;

                            //check for an empty entry in the modlist in which the data to be imported may fit.
                            try
                            {
                                foreach (string line in File.ReadAllLines(Info.modListDir))
                                {
                                    JsonEntry emptyEntry = JsonConvert.DeserializeObject<JsonEntry>(line);

                                    if (emptyEntry.fullPath.Equals(""))
                                    {
                                        emptyLength = emptyEntry.modSize;

                                        if (emptyLength > complete.Count)
                                        {
                                            int sizeDiff = emptyLength - complete.Count;

                                            bw.BaseStream.Seek(emptyEntry.modOffset - 64, SeekOrigin.Begin);

                                            bw.Write(complete.ToArray());

                                            bw.Write(new byte[sizeDiff]);

                                            int oldOffset = Helper.UpdateIndex(emptyEntry.modOffset, fPath) * 8;
                                            Helper.UpdateIndex2(emptyEntry.modOffset, fPath);

                                            JsonEntry replaceEntry = new JsonEntry(category, item, fPath, oldOffset, emptyEntry.modOffset, emptyEntry.modSize);
                                            string[] lines = File.ReadAllLines(Info.modListDir);
                                            lines[emptyLine] = JsonConvert.SerializeObject(replaceEntry);
                                            File.WriteAllLines(Info.modListDir, lines);

                                            overWrite = true;
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


                            if (!overWrite)
                            {
                                bw.BaseStream.Seek(0, SeekOrigin.End);

                                if ((bw.BaseStream.Position & 0xff) != 0)
                                {
                                    bw.Write((byte)0);
                                }

                                int eof = (int)bw.BaseStream.Position + complete.Count;

                                while ((eof & 0xFF) != 0)
                                {
                                    complete.AddRange(new byte[16]);
                                    eof = eof + 16;
                                }

                                newOffset = bw.BaseStream.Position + 64;

                                bw.Write(complete.ToArray());
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("[Import] Error Accessing .dat4 File \n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }


                if (!overWrite)
                {
                    int oldOffset = Helper.UpdateIndex(newOffset, fPath) * 8;
                    Helper.UpdateIndex2(newOffset, fPath);

                    JsonEntry entry = new JsonEntry(category, item, fPath, oldOffset, (int)newOffset, complete.Count);

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

            }
            else
            {
                MessageBox.Show("Could not find file \n" + path, "File read Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Tuple<List<byte>, List<short>, List<short>> ReadDDS(BinaryReader br)
        {
            List<byte> compressedDDS = new List<byte>();
            List<short> mipPartList = new List<short>();
            List<short> mipPartCount = new List<short>();

            int mipLength;
            if(texInfo.Type == 13344)
            {
                mipLength = (newWidth * newHeight) / 2;
            }
            else if(texInfo.Type == 13361)
            {
                mipLength = newWidth * newHeight;

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

                        if (j == mipParts - 1)
                        {
                            uncompLength = mipLength % 16000;
                        }
                        else
                        {
                            uncompLength = 16000;
                        }

                        byte[] compressed;
                        if(uncompLength > 128)
                        {
                            compressed = Compressor(br.ReadBytes(uncompLength));
                        }
                        else
                        {
                            compressed = br.ReadBytes(uncompLength);
                        }



                        compressedDDS.AddRange(BitConverter.GetBytes(16));
                        compressedDDS.AddRange(BitConverter.GetBytes(0));
                        if(uncompLength < 128)
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

                        mipPartList.Add((short)(compressed.Length + padding + 16));
                    }
                }
                else
                {
                    int uncompLength;

                    if (mipLength != 16000)
                    {
                        uncompLength = mipLength % 16000;
                    }
                    else
                    {
                        uncompLength = 16000;
                    }

                    byte[] compressed = Compressor(br.ReadBytes(uncompLength));

                    compressedDDS.AddRange(BitConverter.GetBytes(16));
                    compressedDDS.AddRange(BitConverter.GetBytes(0));
                    compressedDDS.AddRange(BitConverter.GetBytes(compressed.Length));
                    compressedDDS.AddRange(BitConverter.GetBytes(uncompLength));
                    compressedDDS.AddRange(compressed);

                    int padding = 128 - (compressed.Length % 128);

                    compressedDDS.AddRange(new byte[padding]);

                    mipPartList.Add((short)(compressed.Length + padding + 16));
                }

                if (mipLength > 16)
                {
                    mipLength = mipLength / 4;
                }
                else
                {
                    mipLength = 16;
                }
            }

            return new Tuple<List<byte>, List<short>, List<short>>(compressedDDS, mipPartList, mipPartCount);
        }

        private List<byte> MakeHeader(List<short> mipPartList, List<short> mipPartCounts, int uncompLength)
        {
            List<byte> header = new List<byte>();

            int headerSize = 24 + (newMipCount * 20) + (mipPartList.Count * 2);
            int headerPadding = 128 - (headerSize % 128);

            header.AddRange(BitConverter.GetBytes(headerSize + headerPadding));
            header.AddRange(BitConverter.GetBytes(4));
            header.AddRange(BitConverter.GetBytes(uncompLength));
            header.AddRange(BitConverter.GetBytes(0));
            header.AddRange(BitConverter.GetBytes(0));
            header.AddRange(BitConverter.GetBytes(newMipCount));


            int partIndex = 0;
            int mipOffsetIndex = 80;
            int uncompMipSize = newHeight * newWidth;
            if (texInfo.Type == 13344)
            {
                uncompMipSize = (newWidth * newHeight) / 2;
            }
            else if (texInfo.Type == 13361)
            {
                uncompMipSize = newWidth * newHeight;

            }
            else
            {
                uncompMipSize = (newWidth * newHeight) * 4;

            }

            for (int i = 0; i < newMipCount; i++)
            {
                header.AddRange(BitConverter.GetBytes(mipOffsetIndex));

                int paddedSize = 0;
                
                for(int j = 0; j < mipPartCounts[i]; j++)
                {
                    paddedSize = paddedSize + mipPartList[j + partIndex];
                }

                header.AddRange(BitConverter.GetBytes(paddedSize));

                if(uncompMipSize > 16)
                {
                    header.AddRange(BitConverter.GetBytes(uncompMipSize));
                }
                else
                {
                    header.AddRange(BitConverter.GetBytes(16));
                }
                uncompMipSize = uncompMipSize / 4;

                header.AddRange(BitConverter.GetBytes(partIndex));
                header.AddRange(BitConverter.GetBytes((int)mipPartCounts[i]));

                partIndex = partIndex + mipPartCounts[i];
                mipOffsetIndex = mipOffsetIndex + paddedSize;
            }

            foreach(short part in mipPartList)
            {
                header.AddRange(BitConverter.GetBytes(part));
            }

            header.AddRange(new byte[headerPadding]);

            return header;
        }

        private List<byte> MakeSecondHeader()
        {
            List<byte> header = new List<byte>();

            header.AddRange(BitConverter.GetBytes((short)0));
            header.AddRange(BitConverter.GetBytes((short)128));
            header.AddRange(BitConverter.GetBytes((short)texInfo.Type));
            header.AddRange(BitConverter.GetBytes((short)0));
            header.AddRange(BitConverter.GetBytes((short)newWidth));
            header.AddRange(BitConverter.GetBytes((short)newHeight));
            header.AddRange(BitConverter.GetBytes((short)1));
            header.AddRange(BitConverter.GetBytes((short)newMipCount));


            header.AddRange(BitConverter.GetBytes(0));
            header.AddRange(BitConverter.GetBytes(1));
            header.AddRange(BitConverter.GetBytes(2));

            int mipLength;
            if (texInfo.Type == 13344)
            {
                mipLength = (newWidth * newHeight) / 2;
            }
            else if (texInfo.Type == 13361)
            {
                mipLength = newWidth * newHeight;

            }
            else
            {
                mipLength = (newWidth * newHeight) * 4;

            }


            int combindedLength = 80;

            for (int i = 0; i < newMipCount; i++)
            {
                header.AddRange(BitConverter.GetBytes(combindedLength));
                combindedLength = combindedLength + mipLength;

                if(mipLength > 16)
                {
                    mipLength = mipLength / 4;
                }
                else
                {
                    mipLength = 16;
                }
            }

            int padding = 80 - header.Count;

            header.AddRange(new byte[padding]);

            return header;
        }

        private List<byte> MakeMTRLHeader(short fileSize, int paddedSize)
        {
            List<byte> header = new List<byte>();

            header.AddRange(BitConverter.GetBytes(128));
            header.AddRange(BitConverter.GetBytes(2));
            header.AddRange(BitConverter.GetBytes((int)fileSize));
            header.AddRange(BitConverter.GetBytes(4));
            header.AddRange(BitConverter.GetBytes(3));
            header.AddRange(BitConverter.GetBytes(1));
            header.AddRange(BitConverter.GetBytes(0));
            header.AddRange(BitConverter.GetBytes((short)paddedSize));
            header.AddRange(BitConverter.GetBytes(fileSize));
            header.AddRange(new byte[96]);

            return header;
        }

        private byte[] Compressor(byte[] uncomp)
        {
            using (MemoryStream ms = new MemoryStream(uncomp))
            {
                byte[] compbytes = null;
                using (var cs = new MemoryStream())
                {
                    using (var ds = new DeflateStream(cs, CompressionMode.Compress))
                    {
                        ms.CopyTo(ds);
                        ds.Close();
                        compbytes = cs.ToArray();
                    }
                }
                return compbytes;
            }
        }
    }
}
