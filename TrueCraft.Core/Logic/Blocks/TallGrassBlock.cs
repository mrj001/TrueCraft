using System;
using System.Xml;
using TrueCraft.Core.Logic.Items;
using TrueCraft.Core.World;

namespace TrueCraft.Core.Logic.Blocks
{
    public class TallGrassBlock : BlockProvider
    {
        public enum TallGrassType
        {
            DeadBush = 0,
            TallGrass = 1,
            Fern = 2
        }

        public TallGrassBlock(XmlNode node) : base(node)
        {

        }

        public override Vector3i GetSupportDirection(BlockDescriptor descriptor)
        {
            return Vector3i.Down;
        }
        
        protected override ItemStack[] GetDrop(BlockDescriptor descriptor, ItemStack item)
        {
            if (MathHelper.Random.Next (1, 24) == 1)
                return new[] { new ItemStack (SeedsItem.ItemID, 1) };
            else
                return new[] { ItemStack.EmptyStack };
        }
    }
}
