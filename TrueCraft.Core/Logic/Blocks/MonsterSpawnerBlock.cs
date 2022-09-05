using System;
using System.Xml;

namespace TrueCraft.Core.Logic.Blocks
{
    public class MonsterSpawnerBlock : BlockProvider
    {
        public MonsterSpawnerBlock(XmlNode node) : base(node)
        {

        }

        protected override ItemStack[] GetDrop(BlockDescriptor descriptor, ItemStack item)
        {
            return new ItemStack[0];
        }
    }
}