using System;
using System.Xml;
using TrueCraft.Core.Logic.Items;

namespace TrueCraft.Core.Logic.Blocks
{
    public class LeavesBlock : BlockProvider
    {
        public LeavesBlock(XmlNode node) : base(node)
        {

        }

        // TODO: Leaves decay when not attached to a tree.

        protected override ItemStack[] GetDrop(BlockDescriptor descriptor, ItemStack item)
        {
            var provider = ItemRepository.Get().GetItemProvider(item.ID);
            if (provider is ShearsItem)
                return base.GetDrop(descriptor, item);
            else
            {
                if (MathHelper.Random.Next(20) == 0) // 5% chance
                return new[] { new ItemStack((short)BlockIDs.Sapling, 1, descriptor.Metadata) };
                else
                    return new ItemStack[0];
            }
        }
    }
}
