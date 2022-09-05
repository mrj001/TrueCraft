using System;
using System.Xml;
using TrueCraft.Core.Logic.Items;

namespace TrueCraft.Core.Logic.Blocks
{
    public class GoldOreBlock : BlockProvider, ISmeltableItem
    {
        public GoldOreBlock(XmlNode node) : base(node)
        {

        }

        public ItemStack SmeltingOutput { get { return new ItemStack((short)ItemIDs.GoldIngot); } }
    }
}