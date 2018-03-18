using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIV_TexTools2.Model
{
    public class ImportSettings
    {
        public string path;
        public bool Fix;
        public bool Disable;
        public Dictionary<int, int> PartDictionary;
    }
}
