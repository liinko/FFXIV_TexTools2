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
using System;
using System.IO;
using System.Security.Cryptography;
using System.Windows;

namespace FFXIV_TexTools2
{
    public static class CreateDat
    {
        /// <summary>
        /// Creates a .Dat file in which to store modified data
        /// </summary>
        public static void MakeDat()
        {
            try
            {
                foreach(var modFile in Info.ModDatDict)
                {
                    var modDatPath = string.Format(Info.datDir, modFile.Key, modFile.Value);

                    using (FileStream fs = File.Create(modDatPath))
                    {
                        using (BinaryWriter bw = new BinaryWriter(fs))
                        {
                            bw.BaseStream.Seek(0, SeekOrigin.Begin);

                            WriteSqPackHeader(bw);
                            WriteDatHeader(bw);
                        }
                    }
                }

            }
            catch (Exception e)
            {
                MessageBox.Show("[Create] Error Creating .Dat4 File \n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Changes the amount of dat files the game is to read upon loading 
        /// </summary>
        public static void ChangeDatAmounts()
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
                    MessageBox.Show("[Create] Error Accessing Index File \n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    MessageBox.Show("[Create] Error Accessing Index 2 File \n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Writes the SqPack header into the dat file
        /// </summary>
        /// <param name="bw"></param>
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

        /// <summary>
        /// Writes the default dat header into the dat file
        /// </summary>
        /// <param name="bw"></param>
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

        /// <summary>
        /// Creates the ModList which stores data on which items have been modded
        /// </summary>
        public static void CreateModList()
        {
            try
            {
                File.Create(Info.modListDir);
            }
            catch (Exception e)
            {
                MessageBox.Show("[Create] Error Creating .modlist File \n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
    }
}
