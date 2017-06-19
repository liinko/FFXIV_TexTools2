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
using FFXIV_TexTools2.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace FFXIV_TexTools2.ViewModel
{
    public class SaveViewModel
    {
        private ICommand savePNGCommand;

        private ICommand saveDDSCommand;

        private bool canExecute = true;
        string selecteditem, selectedCategory, texMap, name;
        BitmapSource selectedBitmap;
        TexInfo texInfo;
        MTRLInfo mtrlInfo;

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

        public ICommand SaveDDSCommand
        {
            get
            {
                return saveDDSCommand;
            }
        }

        public ICommand SavePNGCommand
        {
            get
            {
                return savePNGCommand;
            }
        }

        public SaveViewModel(string item, string category, string map, BitmapSource bmp, string path)
        {
            selecteditem = item;
            selectedCategory = category;
            selectedBitmap = bmp;
            texMap = map;
            name = path;

            savePNGCommand = new RelayCommand(SavePNG);
        }

        public SaveViewModel(string item, string category, string map, TexInfo info, string path)
        {
            selecteditem = item;
            selectedCategory = category;
            texMap = map;
            texInfo = info;
            name = path;

            saveDDSCommand = new RelayCommand(SaveDDS);
        }

        public SaveViewModel(string item, string category, string map, MTRLInfo info)
        {
            selecteditem = item;
            selectedCategory = category;
            texMap = map;
            mtrlInfo = info;
            name = info.MTRLPath;

            saveDDSCommand = new RelayCommand(SaveDDS);
        }


        public void SavePNG(object obj)
        {
            string saveDir;
            string directory = Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selecteditem;
            Directory.CreateDirectory(directory);

            saveDir = Path.Combine(directory, (Path.GetFileNameWithoutExtension(name) + ".png"));

            using (var fileStream = new FileStream(saveDir, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(selectedBitmap));
                encoder.Save(fileStream);
            }
        }

        public void SaveDDS(object obj)
        {
            string saveDir;
            string directory = Properties.Settings.Default.Save_Directory + "/" + selectedCategory + "/" + selecteditem;
            Directory.CreateDirectory(directory);

            saveDir = Path.Combine(directory, (Path.GetFileNameWithoutExtension(name) + ".dds"));

            List<byte> DDS = new List<byte>();

            if (texMap.Equals(Strings.ColorSet))
            {
                if(mtrlInfo.ColorFlags != null)
                {
                    var colorFlagsDir = Path.Combine(directory, (Path.GetFileNameWithoutExtension(name) + ".dat"));
                    File.WriteAllBytes(colorFlagsDir, mtrlInfo.ColorFlags);
                }

                DDS.AddRange(CreateColorDDSHeader());
                DDS.AddRange(mtrlInfo.ColorData);
            }
            else
            {
                DDS.AddRange(CreateDDSHeader());
                DDS.AddRange(texInfo.RawTexData);
            }


            File.WriteAllBytes(saveDir, DDS.ToArray());


        }

        public byte[] CreateDDSHeader()
        {
            uint m_linearsize;
            uint m_pflags;
            List<byte> header = new List<byte>();
            //DDS
            uint m_magic = 0x20534444;
            header.AddRange(BitConverter.GetBytes(m_magic));
            //header size
            uint m_size = 124;
            header.AddRange(BitConverter.GetBytes(m_size));
            //Flags (DDSD_CAPS, DDSD_HEIGHT, DDSD_WIDTH, DDSD_PIXELFORMAT, DDSD_LINEARSIZE);
            uint m_flags = 528391;
            header.AddRange(BitConverter.GetBytes(m_flags));
            //height
            uint m_height = (uint)texInfo.Height;
            header.AddRange(BitConverter.GetBytes(m_height));
            //width
            uint m_width = (uint)texInfo.Width;
            header.AddRange(BitConverter.GetBytes(m_width));
            //Linearsize
            if (texInfo.Type == 9312)
            {
                m_linearsize = 512;
            }
            else if (texInfo.Type == 5200)
            {
                m_linearsize = (uint)((m_height * m_width) * 4);
            }
            else if(texInfo.Type == 13344)
            {
                m_linearsize = (uint)((m_height * m_width) / 2);
            }
            else
            {
                m_linearsize = (uint)(m_height * m_width);
            }
            header.AddRange(BitConverter.GetBytes(m_linearsize));
            //depth
            uint m_depth = 0;
            header.AddRange(BitConverter.GetBytes(m_depth));
            //mipmap count
            uint m_mipmap = (uint)texInfo.MipCount;
            header.AddRange(BitConverter.GetBytes(m_mipmap));
            //blank
            byte[] blank = new byte[44];
            Array.Clear(blank, 0, 44);
            header.AddRange(blank);
            //pixelformat size
            uint m_psize = 32;
            header.AddRange(BitConverter.GetBytes(m_psize));
            //pixelformat flags (DDPF_FOURCC)
            if (texInfo.Type == 5200)
            {
                m_pflags = 65;
            }
            else
            {
                m_pflags = 4;
            }

            header.AddRange(BitConverter.GetBytes(m_pflags));
            //pixelformat dwFourCC
            if (texInfo.Type == 13344)
            {
                uint m_filetype = 0x31545844;
                header.AddRange(BitConverter.GetBytes(m_filetype));
            }
            else if (texInfo.Type == 13361)
            {
                uint m_filetype = 0x35545844;
                header.AddRange(BitConverter.GetBytes(m_filetype));
            }
            else if (texInfo.Type == 13360)
            {
                uint m_filetype = 0x33545844;
                header.AddRange(BitConverter.GetBytes(m_filetype));
            }
            else if (texInfo.Type == 9312)
            {
                uint m_filetype = 0x71;
                header.AddRange(BitConverter.GetBytes(m_filetype));
            }
            else if (texInfo.Type == 5200)
            {
                uint m_filetype = 0;
                header.AddRange(BitConverter.GetBytes(m_filetype));
            }
            else
            {
                return null;
            }

            if (texInfo.Type == 5200)
            {
                uint m_bpp = 32;
                header.AddRange(BitConverter.GetBytes(m_bpp));
                uint m_red = 16711680;
                header.AddRange(BitConverter.GetBytes(m_red));
                uint m_green = 65280;
                header.AddRange(BitConverter.GetBytes(m_green));
                uint m_blue = 255;
                header.AddRange(BitConverter.GetBytes(m_blue));
                uint m_alpha = 4278190080;
                header.AddRange(BitConverter.GetBytes(m_alpha));
                uint m_dwCaps = 4096;
                header.AddRange(BitConverter.GetBytes(m_dwCaps));

                byte[] blank1 = new byte[16];
                Array.Clear(blank, 0, 16);
                header.AddRange(blank1);

            }
            else
            {
                //blank1
                byte[] blank1 = new byte[40];
                Array.Clear(blank, 0, 40);
                header.AddRange(blank1);
            }

            return header.ToArray();
        }

        public byte[] CreateColorDDSHeader()
        {
            uint m_linearsize;
            List<byte> header = new List<byte>();
            //DDS
            uint m_magic = 0x20534444;
            header.AddRange(BitConverter.GetBytes(m_magic));
            //header size
            uint m_size = 124;
            header.AddRange(BitConverter.GetBytes(m_size));
            //Flags (DDSD_CAPS, DDSD_HEIGHT, DDSD_WIDTH, DDSD_PIXELFORMAT, DDSD_LINEARSIZE);
            uint m_flags = 528399;
            header.AddRange(BitConverter.GetBytes(m_flags));
            //height
            uint m_height = 16;
            header.AddRange(BitConverter.GetBytes(m_height));
            //width
            uint m_width = 4;
            header.AddRange(BitConverter.GetBytes(m_width));
            //Linearsize
            m_linearsize = 512;
            header.AddRange(BitConverter.GetBytes(m_linearsize));
            //depth
            uint m_depth = 0;
            header.AddRange(BitConverter.GetBytes(m_depth));
            //mipmap count
            uint m_mipmap = 0;
            header.AddRange(BitConverter.GetBytes(m_mipmap));
            //blank
            byte[] blank = new byte[44];
            Array.Clear(blank, 0, 44);
            header.AddRange(blank);
            //pixelformat size
            uint m_psize = 32;
            header.AddRange(BitConverter.GetBytes(m_psize));
            //pixelformat flags (DDPF_FOURCC)
            uint m_pflags = 4;
            header.AddRange(BitConverter.GetBytes(m_pflags));
            //pixelformat dwFourCC
            uint m_filetype = 0x71;
            header.AddRange(BitConverter.GetBytes(m_filetype));
            //blank1
            byte[] blank1 = new byte[40];
            Array.Clear(blank, 0, 40);
            header.AddRange(blank1);

            return header.ToArray();
        }
    }
}
