using System;
using System.Xml;

namespace TrueCraft.Core.Logic.Blocks
{
    public class FenceBlock : BlockProvider, IBurnableItem
    {
        public FenceBlock(XmlNode node) : base(node)
        {
        }

        public TimeSpan BurnTime { get { return TimeSpan.FromSeconds(15); } }
    }
}