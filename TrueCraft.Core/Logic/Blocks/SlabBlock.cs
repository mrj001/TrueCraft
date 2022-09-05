using System;
using System.Xml;

namespace TrueCraft.Core.Logic.Blocks
{
    public class SlabBlock : BlockProvider
    {
        public enum SlabMaterial
        {
            Stone       = 0x0,
            Standstone  = 0x1,
            Wooden      = 0x2,
            Cobblestone = 0x3
        }

        public SlabBlock(XmlNode node) : base(node)
        {
        }
    }

    public class DoubleSlabBlock : BlockProvider
    {
        public DoubleSlabBlock(XmlNode node) : base(node)
        {
        }
    }
}