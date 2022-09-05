using System;
using System.Xml;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.Networking;
using TrueCraft.Core.World;

namespace TrueCraft.Core.Logic.Items
{
    public class SeedsItem : ItemProvider
    {
        public static readonly short ItemID = 0x127;

        public SeedsItem(XmlNode node) : base(node)
        {
        }

        public override void ItemUsedOnBlock(GlobalVoxelCoordinates coordinates, ItemStack item, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            if (dimension.GetBlockID(coordinates) == (byte)BlockIDs.Farmland)
            {
                dimension.SetBlockID(coordinates + MathHelper.BlockFaceToCoordinates(face), (byte)BlockIDs.Crops);
                dimension.BlockRepository.GetBlockProvider((byte)BlockIDs.Crops).BlockPlaced(
                    new BlockDescriptor { Coordinates = coordinates }, face, dimension, user);
            }
        }
    }
}