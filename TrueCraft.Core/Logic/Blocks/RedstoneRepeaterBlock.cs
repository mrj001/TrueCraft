using System;
using System.Xml;

namespace TrueCraft.Core.Logic.Blocks
{
    public class RedstoneRepeaterBlock : BlockProvider
    {
        public RedstoneRepeaterBlock(XmlNode node) : base(node)
        {

        }

        // TODO: Redstone Behaviour
    }

    public class ActiveRedstoneRepeaterBlock : RedstoneRepeaterBlock
    {
        public ActiveRedstoneRepeaterBlock(XmlNode node) : base(node)
        {

        }
    }
}