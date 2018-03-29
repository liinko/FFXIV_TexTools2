using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FFXIV_TexTools2.Helpers;
using FFXIV_TexTools2.Model;
using FFXIV_TexTools2.Resources;
using Newtonsoft.Json;

namespace FFXIV_TexTools2.IO
{
    public class ChangeMTRL
    {
        /// <summary>
        /// Imports the items color set into a dat file
        /// </summary>
        /// <param name="mtrlData">MTRL data for the currently displayed color set</param>
        /// <param name="category">The items category</param>
        /// <param name="itemName">The items name</param>
        /// <param name="toggle">Toggle value for translucency</param>
        public static int TranslucencyToggle(MTRLData mtrlData, string category, string itemName, bool toggle)
        {
            int newOffset = 0;

            List<byte> mtrlBytes = new List<byte>();
            List<byte> newMTRL = new List<byte>();

            JsonEntry modEntry = null;
            int offset = mtrlData.MTRLOffset;
            int lineNum = 0;
            short fileSize;
            bool inModList = false;

            int datNum = ((offset / 8) & 0x000f) / 2;

            try
            {
                using (StreamReader sr = new StreamReader(Properties.Settings.Default.Modlist_Directory))
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
                FlexibleMessageBox.Show("Error Accessing .modlist File \n" + ex.Message, "ImportTex Error " + Info.appVersion, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            using (BinaryReader br = new BinaryReader(new MemoryStream(Helper.GetType2DecompressedData(offset, datNum, Strings.ItemsDat))))
            {
                bool enableAlpha = false;
                br.BaseStream.Seek(4, SeekOrigin.Begin);
                fileSize = br.ReadInt16();
                short colorDataSize = br.ReadInt16();
                short textureNameSize = br.ReadInt16();
                short toSHPK = br.ReadInt16();
                byte numOfTextures = br.ReadByte();
                byte numOfMaps = br.ReadByte();
                byte numOfColorSets = br.ReadByte();
                byte unknown = br.ReadByte();

                int endOfHeader = 16 + ((numOfTextures + numOfMaps + numOfColorSets) * 4);

                br.BaseStream.Seek(0, SeekOrigin.Begin);

                mtrlBytes.AddRange(br.ReadBytes(endOfHeader));
                mtrlBytes.AddRange(br.ReadBytes(textureNameSize + 4));
                mtrlBytes.AddRange(br.ReadBytes(colorDataSize));

                mtrlBytes.AddRange(br.ReadBytes(8));
                br.ReadByte();
                mtrlBytes.Add(toggle ? (byte) 0x1D : (byte) 0x0D);

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

            return ImportTex.WriteToDat(newMTRL, modEntry, inModList, mtrlData.MTRLPath, category, itemName, lineNum, Strings.ItemsDat);
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
    }
}
