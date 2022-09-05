using System;
using System.Xml;
using TrueCraft.Core.Logic.Items;
using TrueCraft.Core.World;

namespace TrueCraft.Core.Logic.Blocks
{
    public class RedstoneDustBlock : BlockProvider
    {
        public RedstoneDustBlock(XmlNode node) : base(node)
        {

        }

        public override Vector3i GetSupportDirection(BlockDescriptor descriptor)
        {
            return Vector3i.Down;
        }
    }
}