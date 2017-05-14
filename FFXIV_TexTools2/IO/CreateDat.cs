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

using System.IO;
using System.Security.Cryptography;
using FFXIV_TexTools2.Helpers;

namespace FFXIV_TexTools2
{
    public static class CreateDat
    {
        public static void MakeDat()
        {
            using(FileStream fs = File.Create(Info.datDir + "3")){
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    bw.BaseStream.Seek(0, SeekOrigin.Begin);

                    WriteSqPackHeader(bw);
                    WriteDatHeader(bw);
                }
            }

            using(BinaryWriter bw = new BinaryWriter(File.OpenWrite(Info.indexDir))){
                bw.BaseStream.Seek(1104, SeekOrigin.Begin);
                bw.Write((byte)4);
            }

            using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(Info.index2Dir)))
            {
                bw.BaseStream.Seek(1104, SeekOrigin.Begin);
                bw.Write((byte)4);
            }

        }

        public static void WriteSqPackHeader(BinaryWriter bw){
            byte[] header = new byte[1024];

            using (BinaryWriter hw = new BinaryWriter(new MemoryStream(header)))
            {
                hw.BaseStream.Seek(0, SeekOrigin.Begin);

                SHA1Managed shaM = new SHA1Managed();

                hw.Write(1632661843);
                hw.Write(27491);
                hw.Write(0);
                hw.Write(1024);
                hw.Write(1);
                hw.Write(1);
                hw.Seek(8, SeekOrigin.Current);
                hw.Write(-1);
                hw.Seek(960, SeekOrigin.Begin);

                hw.Write(shaM.ComputeHash(header, 0, 959));

                bw.Write(header);
            }
        }

        public static void WriteDatHeader(BinaryWriter bw)
        {
            byte[] header = new byte[1024];

            using (BinaryWriter hw = new BinaryWriter(new MemoryStream(header)))
            {
                hw.BaseStream.Seek(0, SeekOrigin.Begin);

                SHA1Managed shaM = new SHA1Managed();

                hw.Write(header.Length);
                hw.Write(0);
                hw.Write(16);
                hw.Write(2048);
                hw.Write(2);
                hw.Write(0);
                hw.Write(2000000000);
                hw.Write(0);
                hw.Seek(960, SeekOrigin.Begin);

                hw.Write(shaM.ComputeHash(header, 0, 959));

                bw.BaseStream.Seek(1024, SeekOrigin.Begin);
                bw.Write(header);
            }
        }

        public static void CreateModList()
        {
            File.Create(Info.modListDir);
        }
    }
}
