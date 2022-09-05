using System;
using System.Xml;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.Networking;
using TrueCraft.Core.Server;
using TrueCraft.Core.World;

namespace TrueCraft.Core.Logic.Items
{
    public class RedstoneItem : ItemProvider
    {
        public static readonly short ItemID = 0x14B;

        public RedstoneItem(XmlNode node) : base(node)
        {
        }

        public override void ItemUsedOnBlock(GlobalVoxelCoordinates coordinates, ItemStack item, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            ServerOnly.Assert();

            coordinates += MathHelper.BlockFaceToCoordinates(face);
            IBlockProvider supportingBlock = dimension.BlockRepository.GetBlockProvider(dimension.GetBlockID(coordinates + Vector3i.Down));

            if (supportingBlock.Opaque)
            {
                dimension.SetBlockID(coordinates, (byte)BlockIDs.RedstoneDust);
                item.Count--;
                user.Hotbar[user.SelectedSlot].Item = item;
            }
        }
    }
}