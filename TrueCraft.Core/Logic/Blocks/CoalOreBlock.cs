using System;
using System.Xml;
using TrueCraft.Core.Logic.Items;

namespace TrueCraft.Core.Logic.Blocks
{
    public class CoalOreBlock : BlockProvider, ISmeltableItem
    {
        public CoalOreBlock(XmlNode node) : base(node)
        {

        }

        // TODO: Is Coal Ore really smeltable?  Can it even be obtained?
        public ItemStack SmeltingOutput { get { return new ItemStack(CoalItem.ItemID); } }
    }
}