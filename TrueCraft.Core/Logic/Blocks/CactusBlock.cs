using System;
using System.Xml;
using TrueCraft.Core.World;
using TrueCraft.Core.Entities;
using TrueCraft.Core.Server;
using TrueCraft.Core.Networking;

namespace TrueCraft.Core.Logic.Blocks
{
    public class CactusBlock : BlockProvider
    {
        public static readonly int MinGrowthSeconds = 30;
        public static readonly int MaxGrowthSeconds = 60;
        public static readonly int MaxGrowHeight = 3;

        public CactusBlock(XmlNode node) : base(node)
        {
        }

        public bool ValidCactusPosition(BlockDescriptor descriptor, IBlockRepository repository, IDimension dimension, bool checkNeighbor = true, bool checkSupport = true)
        {
            if (checkNeighbor)
            {
                GlobalVoxelCoordinates coords = descriptor.Coordinates;
                foreach (Vector3i neighbor in Vector3i.Neighbors4)
                    if (dimension.GetBlockID(coords + neighbor) != (byte)BlockIDs.Air)
                        return false;
            }

            if (checkSupport)
            {
                var supportingBlock = repository.GetBlockProvider(dimension.GetBlockID(descriptor.Coordinates + Vector3i.Down));
                if (((BlockIDs)supportingBlock.ID != BlockIDs.Cactus) && (supportingBlock.ID != (byte)BlockIDs.Sand))
                    return false;
            }

            return true;
        }

        private void TryGrowth(IMultiplayerServer server, IChunk chunk, LocalVoxelCoordinates coords)
        {
            if ((BlockIDs)chunk.GetBlockID(coords) != BlockIDs.Cactus)
                return;

            // Find current height of stalk
            int height = 0;
            for (int y = -MaxGrowHeight; y <= MaxGrowHeight; y++)
            {
                if ((BlockIDs)chunk.GetBlockID(coords + (Vector3i.Down * y)) == BlockIDs.Cactus)
                    height++;
            }
            if (height < MaxGrowHeight)
            {
                var meta = chunk.GetMetadata(coords);
                meta++;
                chunk.SetMetadata(coords, meta);
                if (meta == 15)
                {
                    if (chunk.GetBlockID(coords + Vector3i.Up) == 0)
                    {
                        chunk.SetBlockID(coords + Vector3i.Up, (byte)BlockIDs.Cactus);
                        server.Scheduler.ScheduleEvent("cactus", chunk,
                            TimeSpan.FromSeconds(MathHelper.Random.Next(MinGrowthSeconds, MaxGrowthSeconds)),
                            (_server) => TryGrowth(_server, chunk, coords + Vector3i.Up));
                    }
                }
                else
                {
                    server.Scheduler.ScheduleEvent("cactus", chunk,
                        TimeSpan.FromSeconds(MathHelper.Random.Next(MinGrowthSeconds, MaxGrowthSeconds)),
                        (_server) => TryGrowth(_server, chunk, coords));
                }
            }
        }

        public void DestroyCactus(BlockDescriptor descriptor, IMultiplayerServer server, IDimension dimension)
        {
            ServerOnly.Assert();

            var toDrop = 0;

            // Search upwards
            for (int y = descriptor.Coordinates.Y; y < 127; y++)
            {
                var coordinates = new GlobalVoxelCoordinates(descriptor.Coordinates.X, y, descriptor.Coordinates.Z);
                if ((BlockIDs)dimension.GetBlockID(coordinates) == BlockIDs.Cactus)
                {
                    dimension.SetBlockID(coordinates, (byte)BlockIDs.Air);
                    toDrop++;
                }
            }

            // Search downwards.
            for (int y = descriptor.Coordinates.Y - 1; y > 0; y--)
            {
                var coordinates = new GlobalVoxelCoordinates(descriptor.Coordinates.X, y, descriptor.Coordinates.Z);
                if ((BlockIDs)dimension.GetBlockID(coordinates) == BlockIDs.Cactus)
                {
                    dimension.SetBlockID(coordinates, (byte)BlockIDs.Air);
                    toDrop++;
                }
            }

            IEntityManager manager = ((IDimensionServer)dimension).EntityManager;
            manager.SpawnEntity(
                new ItemEntity(dimension, manager, (Vector3)(descriptor.Coordinates + Vector3i.Up),
                    new ItemStack((byte)BlockIDs.Cactus, (sbyte)toDrop)));
        }

        public override void BlockPlaced(BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            ServerOnly.Assert();

            if (ValidCactusPosition(descriptor, dimension.BlockRepository, dimension))
                base.BlockPlaced(descriptor, face, dimension, user);
            else
            {
                dimension.SetBlockID(descriptor.Coordinates, (byte)BlockIDs.Air);

                IEntityManager manager = ((IDimensionServer)dimension).EntityManager;
                manager.SpawnEntity(
                    new ItemEntity(dimension, manager, (Vector3)(descriptor.Coordinates + Vector3i.Up),
                        new ItemStack((byte)BlockIDs.Cactus, (sbyte)1)));
                // user.Inventory.PickUpStack() wasn't working?
            }

            IChunk chunk = dimension.GetChunk(descriptor.Coordinates)!;
            user.Server.Scheduler.ScheduleEvent("cactus", chunk,
                TimeSpan.FromSeconds(MathHelper.Random.Next(MinGrowthSeconds, MaxGrowthSeconds)),
                (server) => TryGrowth(server, chunk, (LocalVoxelCoordinates)descriptor.Coordinates));
        }

        public override void BlockUpdate(BlockDescriptor descriptor, BlockDescriptor source, IMultiplayerServer server, IDimension dimension)
        {
            if (!ValidCactusPosition(descriptor, dimension.BlockRepository, dimension))
                DestroyCactus(descriptor, server, dimension);
            base.BlockUpdate(descriptor, source, server, dimension);
        }

        public override void BlockLoadedFromChunk(IMultiplayerServer server, IDimension dimension, GlobalVoxelCoordinates coordinates)
        {
            IChunk chunk = dimension.GetChunk(coordinates)!;
            server.Scheduler.ScheduleEvent("cactus", chunk,
                TimeSpan.FromSeconds(MathHelper.Random.Next(MinGrowthSeconds, MaxGrowthSeconds)),
                s => TryGrowth(s, chunk, (LocalVoxelCoordinates)coordinates));
        }
    }
}