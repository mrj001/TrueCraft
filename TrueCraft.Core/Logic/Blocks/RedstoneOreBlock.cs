using System;
using System.Xml;
using TrueCraft.Core.Logic.Items;

namespace TrueCraft.Core.Logic.Blocks
{
    public class RedstoneOreBlock : BlockProvider, ISmeltableItem
    {
        public RedstoneOreBlock(XmlNode node) : base(node)
        {

        }

        public ItemStack SmeltingOutput { get { return new ItemStack(RedstoneItem.ItemID); } }

        // TODO: behaviour (when walked upon).
    }

    public class GlowingRedstoneOreBlock : RedstoneOreBlock
    {
        public GlowingRedstoneOreBlock(XmlNode node) : base(node)
        {

        }

        // TODO: Behaviour (reverts to non-glowing after some delay)
    }
}