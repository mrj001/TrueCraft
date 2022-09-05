using System;
using System.Xml;

namespace TrueCraft.Core.Logic.Blocks
{
    public class CobblestoneBlock : BlockProvider, ISmeltableItem
    {
        public CobblestoneBlock(XmlNode node) : base(node)
        {

        }

        public ItemStack SmeltingOutput { get => new ItemStack((byte)BlockIDs.Stone, 1); }
    }
}