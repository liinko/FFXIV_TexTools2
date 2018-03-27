using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIV_TexTools2.Model
{
    public class ModPackJson
    {
        public string Name { get; set; }

        public string Category { get; set; }

        public string FullPath { get; set; }

        public long ModOffset { get; set; }

        public int ModSize { get; set; }

        public string DatFile { get; set; }
    }
}
