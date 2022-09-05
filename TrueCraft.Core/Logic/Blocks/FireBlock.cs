using System;
using System.Xml;
using TrueCraft.Core.Networking;
using TrueCraft.Core.World;
using TrueCraft.Core.Server;

namespace TrueCraft.Core.Logic.Blocks
{
    public class FireBlock : BlockProvider
    {
        public static readonly int MinSpreadTime = 1;
        public static readonly int MaxSpreadTime = 5;

        public FireBlock(XmlNode node) : base(node)
        {

        }

        protected override ItemStack[] GetDrop(BlockDescriptor descriptor, ItemStack item)
        {
            return new ItemStack[0];
        }

        private static readonly Vector3i[] SpreadableBlocks =
        {
            Vector3i.Down,
            Vector3i.West,
            Vector3i.East,
            Vector3i.North,
            Vector3i.South,
            Vector3i.Up * 1,
            Vector3i.Up * 2,
            Vector3i.Up * 3,
            Vector3i.Up * 4
        };

        private static readonly Vector3i[] AdjacentBlocks =
        {
            Vector3i.Up,
            Vector3i.Down,
            Vector3i.West,
            Vector3i.East,
            Vector3i.North,
            Vector3i.South
        };

        public void DoUpdate(IMultiplayerServer server, IDimension dimension, BlockDescriptor descriptor)
        {
            IChunk? chunk = dimension.GetChunk(descriptor.Coordinates);
            if (chunk is null)
                return;

            var down = descriptor.Coordinates + Vector3i.Down;

            var current = dimension.GetBlockID(descriptor.Coordinates);
            if (current != (byte)BlockIDs.Fire && current != (byte)BlockIDs.Lava && current != (byte)BlockIDs.LavaStationary)
                return;

            // Decay
            var meta = dimension.GetMetadata(descriptor.Coordinates);
            meta++;
            if (meta == 0xE)
            {
                if (!dimension.IsValidPosition(down) || dimension.GetBlockID(down) != (byte)BlockIDs.Netherrack)
                {
                    dimension.SetBlockID(descriptor.Coordinates, (byte)BlockIDs.Air);
                    return;
                }
            }
            dimension.SetMetadata(descriptor.Coordinates, meta);

            if (meta > 9)
            {
                var pick = AdjacentBlocks[meta % AdjacentBlocks.Length];
                IBlockProvider provider = dimension.BlockRepository
                    .GetBlockProvider(dimension.GetBlockID(pick + descriptor.Coordinates));
                if (provider.Flammable)
                    dimension.SetBlockID(pick + descriptor.Coordinates, (byte)BlockIDs.Air);
            }

            // Spread
            DoSpread(server, dimension, descriptor);

            // Schedule next event
            ScheduleUpdate(server, dimension, descriptor);
        }

        public void DoSpread(IMultiplayerServer server, IDimension dimension, BlockDescriptor descriptor)
        {
            foreach (var coord in SpreadableBlocks)
            {
                var check = descriptor.Coordinates + coord;
                if (dimension.GetBlockID(check) == (byte)BlockIDs.Air)
                {
                    // Check if this is adjacent to a flammable block
                    foreach (var adj in AdjacentBlocks)
                    {
                        IBlockProvider provider = dimension.BlockRepository.GetBlockProvider(
                           dimension.GetBlockID(check + adj));
                        if (provider.Flammable)
                        {
                            if (provider.Hardness == 0)
                                check = check + adj;

                            // Spread to this block
                            dimension.SetBlockID(check, (byte)BlockIDs.Fire);
                            ScheduleUpdate(server, dimension, dimension.GetBlockData(check));
                            break;
                        }
                    }
                }
            }
        }

        public override void BlockPlaced(BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            ScheduleUpdate(user.Server, dimension, descriptor);
        }

        public void ScheduleUpdate(IMultiplayerServer server, IDimension dimension, BlockDescriptor descriptor)
        {
            IChunk? chunk = dimension.GetChunk(descriptor.Coordinates);
            if (chunk is null)
                return;

            server.Scheduler.ScheduleEvent("fire.spread", chunk,
                TimeSpan.FromSeconds(MathHelper.Random.Next(MinSpreadTime, MaxSpreadTime)),
                s => DoUpdate(s, dimension, descriptor));
        }
    }
}