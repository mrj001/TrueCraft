using System;
using System.Xml;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.Networking;
using TrueCraft.Core.Server;
using TrueCraft.Core.World;

namespace TrueCraft.Core.Logic.Items
{
    public class BucketItem : ItemProvider
    {
        public static readonly short ItemID = 0x145;

        public BucketItem(XmlNode node) : base(node)
        {
        }

        protected virtual byte? RelevantBlockType { get { return null; } }

        public override void ItemUsedOnBlock(GlobalVoxelCoordinates coordinates, ItemStack item, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            ServerOnly.Assert();

            coordinates += MathHelper.BlockFaceToCoordinates(face);
            if (item.ID == ItemID) // Empty bucket
            {
                var block = dimension.GetBlockID(coordinates);
                if (block == (byte)BlockIDs.Water || block == (byte)BlockIDs.WaterStationary)
                {
                    var meta = dimension.GetMetadata(coordinates);
                    if (meta == 0) // Is source block?
                    {
                        user.Hotbar[user.SelectedSlot].Item = new ItemStack(WaterBucketItem.ItemID);
                        dimension.SetBlockID(coordinates, 0);
                    }
                }
                else if (block == (byte)BlockIDs.Lava || block == (byte)BlockIDs.LavaStationary)
                {
                    var meta = dimension.GetMetadata(coordinates);
                    if (meta == 0) // Is source block?
                    {
                        user.Hotbar[user.SelectedSlot].Item = new ItemStack(LavaBucketItem.ItemID);
                        dimension.SetBlockID(coordinates, 0);
                    }
                }
            }
            else
            {
                var provider = dimension.BlockRepository.GetBlockProvider(dimension.GetBlockID(coordinates));
                if (!provider.Opaque)
                {
                    if (RelevantBlockType != null)
                    {
                        var blockType = RelevantBlockType.Value;
                        user.Server.BlockUpdatesEnabled = false;
                        dimension.SetBlockID(coordinates, blockType);
                        dimension.SetMetadata(coordinates, 0); // Source block
                        user.Server.BlockUpdatesEnabled = true;
                        var liquidProvider = dimension.BlockRepository.GetBlockProvider(blockType);
                        liquidProvider.BlockPlaced(new BlockDescriptor { Coordinates = coordinates }, face, dimension, user);
                    }
                    user.Hotbar[user.SelectedSlot].Item = new ItemStack(BucketItem.ItemID);
                }
            }
        }
    }

    public class LavaBucketItem : BucketItem, IBurnableItem
    {
        public static readonly new short ItemID = 0x147;

        public LavaBucketItem(XmlNode node) : base(node)
        {
        }

        public TimeSpan BurnTime { get { return TimeSpan.FromSeconds(1000); } }

        protected override byte? RelevantBlockType
        {
            get
            {
                return (byte)BlockIDs.LavaStationary;
            }
        }
    }

    public class MilkItem : BucketItem
    {
        public static readonly new short ItemID = 0x14F;

        public MilkItem(XmlNode node) : base(node)
        {
        }

        protected override byte? RelevantBlockType
        {
            get
            {
                return null;
            }
        }
    }

    public class WaterBucketItem : BucketItem
    {
        public static readonly new short ItemID = 0x146;

        public WaterBucketItem(XmlNode node) : base(node)
        {
        }

        protected override byte? RelevantBlockType
        {
            get
            {
                return (byte)BlockIDs.WaterStationary;
            }
        }
    }
}