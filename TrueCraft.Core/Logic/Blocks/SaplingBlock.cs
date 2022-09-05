using System;
using System.Xml;

namespace TrueCraft.Core.Logic.Blocks
{
    public class SaplingBlock : BlockProvider, IBurnableItem
    {
        public enum SaplingType
        {
            Oak = 0,
            Spruce = 1,
            Birch = 2
        }

        public SaplingBlock(XmlNode node) : base(node)
        {
        }

        public TimeSpan BurnTime { get { return TimeSpan.FromSeconds(5); } }
    }
}
