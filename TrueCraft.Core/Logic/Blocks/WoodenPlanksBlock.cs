using System;
using System.Xml;

namespace TrueCraft.Core.Logic.Blocks
{
    public class WoodenPlanksBlock : BlockProvider, IBurnableItem
    {
        public WoodenPlanksBlock(XmlNode node) : base(node)
        {

        }

        public TimeSpan BurnTime { get { return TimeSpan.FromSeconds(15); } }
    }
}
