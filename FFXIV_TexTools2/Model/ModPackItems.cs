using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIV_TexTools2.Model
{
    public class ModPackItems : IComparable<ModPackItems>
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string Race { get; set; }
        public string Part { get; set; }
        public string Num { get; set; }
        public string Map { get; set; }
        public bool Active { get; set; }
        public JsonEntry Entry { get; set; }
        public ModPackJson mEntry { get; set; }

        public int CompareTo(ModPackItems obj)
        {
            return Name.CompareTo(obj.Name);
        }
    }
}
