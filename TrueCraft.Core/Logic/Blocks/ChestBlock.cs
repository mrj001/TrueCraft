using System;
using TrueCraft.Core.World;
using fNbt;
using TrueCraft.Core.Windows;
using TrueCraft.Core.Entities;
using TrueCraft.Core.Networking;
using TrueCraft.Core.Server;
using TrueCraft.Core.Inventory;
using System.Xml;

namespace TrueCraft.Core.Logic.Blocks
{
    public class ChestBlock : BlockProvider, IBurnableItem
    {
        private const int ChestLength = 27;

        private static readonly Vector3i[] AdjacentBlocks =
        {
            Vector3i.North,
            Vector3i.South,
            Vector3i.West,
            Vector3i.East
        };

        public ChestBlock(XmlNode node) : base(node)
        {
        }

        public TimeSpan BurnTime { get { return TimeSpan.FromSeconds(15); } }

        public override void ItemUsedOnBlock(GlobalVoxelCoordinates coordinates, ItemStack item, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            int adjacent = 0;
            GlobalVoxelCoordinates coords = coordinates + MathHelper.BlockFaceToCoordinates(face);
            GlobalVoxelCoordinates? t = null;
            // Check for adjacent chests. We can only allow one adjacent check block.
            for (int i = 0; i < AdjacentBlocks.Length; i++)
            {
                if (dimension.GetBlockID(coords + AdjacentBlocks[i]) == (byte)BlockIDs.Chest)
                {
                    t = coords + AdjacentBlocks[i];
                    adjacent++;
                }
            }
            if (adjacent <= 1)
            {
                if (t is not null)
                {
                    // Confirm that adjacent chest is not a double chest
                    for (int i = 0; i < AdjacentBlocks.Length; i++)
                    {
                        if (dimension.GetBlockID(t + AdjacentBlocks[i]) == (byte)BlockIDs.Chest)
                            adjacent++;
                    }
                }
                if (adjacent <= 1)
                    base.ItemUsedOnBlock(coordinates, item, face, dimension, user);
            }
        }

        public override void BlockPlaced(BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            dimension.SetMetadata(descriptor.Coordinates, (byte)MathHelper.DirectionByRotationFlat(user.Entity!.Yaw, true));
        }

        public override bool BlockRightClicked(IServiceLocator serviceLocator,
            BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            ServerOnly.Assert();

            GlobalVoxelCoordinates? adjacent = null; // No adjacent chest
            GlobalVoxelCoordinates self = descriptor.Coordinates;
            for (int i = 0; i < AdjacentBlocks.Length; i++)
            {
                var test = self + AdjacentBlocks[i];
                if (dimension.GetBlockID(test) == (byte)BlockIDs.Chest)
                {
                    adjacent = test;
                    var up = dimension.BlockRepository.GetBlockProvider(dimension.GetBlockID(test + Vector3i.Up));
                    if (up.Opaque && !(up is WallSignBlock)) // Wall sign blocks are an exception
                        return false; // Obstructed
                    break;
                }
            }
            var upSelf = dimension.BlockRepository.GetBlockProvider(dimension.GetBlockID(self + Vector3i.Up));
            if (upSelf.Opaque && !(upSelf is WallSignBlock))
                return false; // Obstructed

            if (adjacent is not null)
            {
                // TODO LATER: this assumes that chests cannot be placed next to each other.
                // Ensure that chests are always opened in the same arrangement
                if (adjacent.X < self.X ||
                    adjacent.Z < self.Z)
                {
                    var _ = adjacent;
                    adjacent = self;
                    self = _; // Swap
                }
            }

            IInventoryFactory<IServerSlot> factory = new InventoryFactory<IServerSlot>();
            ISlotFactory<IServerSlot> slotFactory = SlotFactory<IServerSlot>.Get();
            sbyte windowID = WindowIDs.GetWindowID();
            IChestWindow<IServerSlot> window = factory.NewChestWindow(serviceLocator.ItemRepository,
                slotFactory, windowID, user.Inventory, user.Hotbar,
                dimension, descriptor.Coordinates, adjacent);

            user.OpenWindow(window);
            return false;
        }

        public override void BlockMined(BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            ServerOnly.Assert();

            IDimensionServer dimensionServer = (IDimensionServer)dimension;
            GlobalVoxelCoordinates self = descriptor.Coordinates;
            NbtCompound? entity = dimensionServer.GetTileEntity(self);
            IEntityManager manager = ((IDimensionServer)dimension).EntityManager;
            if (entity is not null)
            {
                foreach (var item in (NbtList)entity["Items"])
                {
                    var slot = ItemStack.FromNbt((NbtCompound)item);
                    manager.SpawnEntity(new ItemEntity(dimension, manager,
                        new Vector3(descriptor.Coordinates.X + 0.5, descriptor.Coordinates.Y + 0.5, descriptor.Coordinates.Z + 0.5), slot));
                }
            }
            dimensionServer.SetTileEntity(self, null);
            base.BlockMined(descriptor, face, dimension, user);
        }
    }
}
