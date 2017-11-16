using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIV_TexTools2.Model
{
    public class TreeNode : IComparable
    {
        public List<TreeNode> _subNode = new List<TreeNode>();
        public IList<TreeNode> SubNode
        {
            get { return _subNode; }
        }

        public ItemData ItemData { get; set; }

        public string Name { get; set; }

        public int CompareTo(object obj)
        {
            return Name.CompareTo(((TreeNode)obj).Name);
        }
    }
}
