using System;
using System.Xml;
using TrueCraft.Core.Logic.Items;

namespace TrueCraft.Core.Logic.Blocks
{
    public class IronOreBlock : BlockProvider, ISmeltableItem
    {
        public IronOreBlock(XmlNode node) : base(node)
        {

        }

        public ItemStack SmeltingOutput { get { return new ItemStack((short)ItemIDs.IronIngot); } }
    }
}