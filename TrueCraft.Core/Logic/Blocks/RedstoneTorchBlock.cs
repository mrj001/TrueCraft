using System;
using System.Xml;

namespace TrueCraft.Core.Logic.Blocks
{
    public class RedstoneTorchBlock : TorchBlock
    {
        public RedstoneTorchBlock(XmlNode node) : base(node)
        {

        }

        // TODO: Redstone behaviour
    }

    public class InactiveRedstoneTorchBlock : RedstoneTorchBlock
    {
        public InactiveRedstoneTorchBlock(XmlNode node) : base(node)
        {

        }

        // TODO: Redstone behaviour
    }
}