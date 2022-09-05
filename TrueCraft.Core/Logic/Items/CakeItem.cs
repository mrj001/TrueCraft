using System;
using System.Xml;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.Networking;
using TrueCraft.Core.Server;
using TrueCraft.Core.World;

namespace TrueCraft.Core.Logic.Items
{
    public class CakeItem : FoodItem // TODO: This is a special sort of FoodItem that isn't fully consumed.
    {
        public static readonly short ItemID = 0x162;

        public CakeItem(XmlNode node) : base(node)
        {
        }

        public override void ItemUsedOnBlock(GlobalVoxelCoordinates coordinates, ItemStack item, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            ServerOnly.Assert();

            coordinates += MathHelper.BlockFaceToCoordinates(face);
            var old = dimension.BlockRepository.GetBlockProvider(dimension.GetBlockID(coordinates));
            if (old.Hardness == 0)
            {
                dimension.SetBlockID(coordinates, (byte)BlockIDs.Cake);
                item.Count--;
                user.Hotbar[user.SelectedSlot].Item = item;
            }
        }
    }
}