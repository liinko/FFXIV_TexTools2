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
        string path, fPath;

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
            texInfo = info;
            fPath = fullPath;
            path = Properties.Settings.Default.Save_Directory + "/" + category + "/" + item + "/" + Path.GetFileNameWithoutExtension(fullPath) + ".dds";

            importCommand = new RelayCommand(Import);
        }

        public ImportViewModel(MTRLInfo info, string category, string item)
        {
            mtrlInfo = info;
            fPath = info.MTRLPath;
            path = Properties.Settings.Default.Save_Directory + "/" + category + "/" + item + "/" + Path.GetFileNameWithoutExtension(info.MTRLPath) + ".dds";

            importCommand = new RelayCommand(ImportColor);
        }


        public void Import(object obj)
        {
            int type;

            if (File.Exists(path))
            {
                using (BinaryReader br = new BinaryReader(File.OpenRead(path)))
                {
                    br.BaseStream.Seek(84, SeekOrigin.Begin);
                    type = br.ReadInt32();

                    if (type == Info.DDSType[texInfo.Type])
                    {
                        List<byte> fullImport = new List<byte>();
                        long offset;

                        int uncompLength = (int)new FileInfo(path).Length - 128;

                        var DDSInfo = ReadDDS(br);

                        fullImport.AddRange(MakeHeader(DDSInfo.Item2, DDSInfo.Item3, uncompLength));
                        fullImport.AddRange(makeSecondHeader());
                        fullImport.AddRange(DDSInfo.Item1);

                        using(BinaryWriter bw = new BinaryWriter(File.OpenWrite(Properties.Settings.Default.FFXIV_Directory + "/040000.win32.dat3")))
                        {
                            bw.BaseStream.Seek(0, SeekOrigin.End);

                            if ((bw.BaseStream.Position & 0xff) != 0)
                            {
                                bw.Write((byte)0);
                            }

                            int eof = (int)bw.BaseStream.Position + fullImport.Count;

                            while((eof & 0xFF) != 0)
                            {
                                fullImport.AddRange(new byte[16]);
                                eof = eof + 16;
                            }

                            offset = bw.BaseStream.Position + 48;

                            bw.Write(fullImport.ToArray());
                        }

                        UpdateIndex(offset);
                        UpdateIndex2(offset);
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
                long newOffset;
                short fileSize;

                int datNum = ((offset / 8) & 0x000f) / 2;

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

                using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(Properties.Settings.Default.FFXIV_Directory + "/040000.win32.dat3")))
                {
                    bw.BaseStream.Seek(0, SeekOrigin.End);

                    if((bw.BaseStream.Position & 0xff) != 0)
                    {
                        bw.Write((byte)0);
                    }

                    int eof = (int)bw.BaseStream.Position + complete.Count;

                    while ((eof & 0xFF) != 0)
                    {
                        complete.AddRange(new byte[16]);
                        eof = eof + 16;
                    }

                    newOffset = bw.BaseStream.Position + 48;

                    bw.Write(complete.ToArray());
                }

                UpdateIndex(newOffset);
                UpdateIndex2(newOffset);
            }
            else
            {

            }
        }

        private Tuple<List<byte>, List<short>, List<short>> ReadDDS(BinaryReader br)
        {
            List<byte> compressedDDS = new List<byte>();
            List<short> mipPartList = new List<short>();
            List<short> mipPartCount = new List<short>();

            int mipLength = texInfo.Width * texInfo.Height;

            br.BaseStream.Seek(128, SeekOrigin.Begin);

            for (int i = 0; i < texInfo.MipCount; i++)
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

            int headerSize = 24 + (texInfo.MipCount * 20) + (mipPartList.Count * 2);
            int headerPadding = 128 - (headerSize % 128);

            header.AddRange(BitConverter.GetBytes(headerSize + headerPadding));
            header.AddRange(BitConverter.GetBytes(4));
            header.AddRange(BitConverter.GetBytes(uncompLength));
            header.AddRange(BitConverter.GetBytes(0));
            header.AddRange(BitConverter.GetBytes(0));
            header.AddRange(BitConverter.GetBytes(texInfo.MipCount));

            int partIndex = 0;
            int mipOffsetIndex = 80;
            int uncompMipSize = texInfo.Height * texInfo.Width;

            for(int i = 0; i < texInfo.MipCount; i++)
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

        private List<byte> makeSecondHeader()
        {
            List<byte> header = new List<byte>();

            header.AddRange(BitConverter.GetBytes((short)0));
            header.AddRange(BitConverter.GetBytes((short)128));
            header.AddRange(BitConverter.GetBytes((short)texInfo.Type));
            header.AddRange(BitConverter.GetBytes((short)0));
            header.AddRange(BitConverter.GetBytes((short)texInfo.Width));
            header.AddRange(BitConverter.GetBytes((short)texInfo.Height));
            header.AddRange(BitConverter.GetBytes((short)1));
            header.AddRange(BitConverter.GetBytes((short)texInfo.MipCount));

            header.AddRange(BitConverter.GetBytes(0));
            header.AddRange(BitConverter.GetBytes(1));
            header.AddRange(BitConverter.GetBytes(2));

            int mipLength = texInfo.Height * texInfo.Width;
            int combindedLength = 80;

            for (int i = 0; i < texInfo.MipCount; i++)
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

        private void UpdateIndex(long offset)
        {
            var index = File.Open(Info.indexDir, FileMode.Open);
            var folderHash = FFCRC.GetHash(fPath.Substring(0, fPath.LastIndexOf("/")));
            var fileHash = FFCRC.GetHash(Path.GetFileName(fPath));

            using(BinaryReader br = new BinaryReader(index))
            {
                using(BinaryWriter bw = new BinaryWriter(index))
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
                                bw.BaseStream.Seek(br.BaseStream.Position, SeekOrigin.Begin);
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

        private void UpdateIndex2(long offset)
        {
            var index = File.Open(Info.index2Dir, FileMode.Open);
            var pathHash = FFCRC.GetHash(fPath);

            using (BinaryReader br = new BinaryReader(index))
            {
                using (BinaryWriter bw = new BinaryWriter(index))
                {
                    br.BaseStream.Seek(1036, SeekOrigin.Begin);
                    int totalFiles = br.ReadInt32();

                    br.BaseStream.Seek(2056, SeekOrigin.Begin);
                    for (int i = 0; i < totalFiles; br.ReadBytes(4), i += 16)
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
                            br.ReadBytes(8);
                        }
                    }
                }
            }
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
