using System;
using System.Xml;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.Networking;
using TrueCraft.Core.Server;
using TrueCraft.Core.World;

namespace TrueCraft.Core.Logic.Items
{
    public class SugarCanesItem : ItemProvider
    {
        public static readonly short ItemID = 0x152;

        public SugarCanesItem(XmlNode node) : base(node)
        {
        }

        public override void ItemUsedOnBlock(GlobalVoxelCoordinates coordinates, ItemStack item, BlockFace face, IDimension dimension   , IRemoteClient user)
        {
            ServerOnly.Assert();

            coordinates += MathHelper.BlockFaceToCoordinates(face);
            if (SugarcaneBlock.ValidPlacement(new BlockDescriptor { Coordinates = coordinates }, dimension))
            {
                dimension.SetBlockID(coordinates, (byte)BlockIDs.SugarCane);
                item.Count--;
                user.Hotbar[user.SelectedSlot].Item = item;
                dimension.BlockRepository.GetBlockProvider((byte)BlockIDs.SugarCane).BlockPlaced(
                    new BlockDescriptor { Coordinates = coordinates }, face, dimension, user);
            }
        }
    }
}