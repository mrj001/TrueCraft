using System;
using System.Xml;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.Networking;
using TrueCraft.Core.World;

namespace TrueCraft.Core.Logic.Items
{
    public class HoeItem : ToolItem
    {
        public HoeItem(XmlNode node) : base(node)
        {
        }

        public override void ItemUsedOnBlock(GlobalVoxelCoordinates coordinates, ItemStack item, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            var id = dimension.GetBlockID(coordinates);
            if (id == (byte)BlockIDs.Dirt || id == (byte)BlockIDs.Grass)
            {
                dimension.SetBlockID(coordinates, (byte)BlockIDs.Farmland);
                dimension.BlockRepository.GetBlockProvider((byte)BlockIDs.Farmland).BlockPlaced(
                    new BlockDescriptor { Coordinates = coordinates }, face, dimension, user);
            }
        }
    }
}
