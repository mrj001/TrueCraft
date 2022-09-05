using System;
using System.Xml;
using TrueCraft.Core.Logic.Items;
using TrueCraft.Core.World;
using TrueCraft.Core.Server;
using TrueCraft.Core.Networking;

namespace TrueCraft.Core.Logic.Blocks
{
    public class SugarcaneBlock : BlockProvider
    {
        public static readonly int MinGrowthSeconds = 30;
        public static readonly int MaxGrowthSeconds = 120;
        public static readonly int MaxGrowHeight = 3;

        public SugarcaneBlock(XmlNode node) : base(node)
        {

        }
        protected override ItemStack[] GetDrop(BlockDescriptor descriptor, ItemStack item)
        {
            return new[] { new ItemStack(SugarCanesItem.ItemID) };
        }

        public static bool ValidPlacement(BlockDescriptor descriptor, IDimension dimension)
        {
            var below = dimension.GetBlockID(descriptor.Coordinates + Vector3i.Down);
            if (below != (byte)BlockIDs.SugarCane && below != (byte)BlockIDs.Grass && below != (byte)BlockIDs.Dirt)
                return false;
            var toCheck = new[]
            {
                Vector3i.Down + Vector3i.West,
                Vector3i.Down + Vector3i.East,
                Vector3i.Down + Vector3i.North,
                Vector3i.Down + Vector3i.South
            };
            if (below != (byte)BlockIDs.SugarCane)
            {
                bool foundWater = false;
                for (int i = 0; i < toCheck.Length; i++)
                {
                    var id = dimension.GetBlockID(descriptor.Coordinates + toCheck[i]);
                    if (id == (byte)BlockIDs.WaterStationary || id == (byte)BlockIDs.Water)
                    {
                        foundWater = true;
                        break;
                    }
                }
                return foundWater;
            }
            return true;
        }

        public override void BlockUpdate(BlockDescriptor descriptor, BlockDescriptor source, IMultiplayerServer server, IDimension dimension)
        {
            if (!ValidPlacement(descriptor, dimension))
            {
                // Destroy self
                dimension.SetBlockID(descriptor.Coordinates, 0);
                GenerateDropEntity(descriptor, dimension, server, ItemStack.EmptyStack);
            }
        }

        private void TryGrowth(IMultiplayerServer server, IDimension dimension, GlobalVoxelCoordinates coords)
        {
            IChunk? chunk = dimension.GetChunk(coords);
            if (chunk is null || dimension.GetBlockID(coords) != (byte)BlockIDs.SugarCane)
                return;

            // Find current height of stalk
            int height = 0;
            for (int y = -MaxGrowHeight; y <= MaxGrowHeight; y++)
            {
                if (dimension.GetBlockID(coords + (Vector3i.Down * y)) == (byte)BlockIDs.SugarCane)
                    height++;
            }
            if (height < MaxGrowHeight)
            {
                var meta = dimension.GetMetadata(coords);
                meta++;
                dimension.SetMetadata(coords, meta);
                if (meta == 15)
                {
                    if (dimension.GetBlockID(coords + Vector3i.Up) == 0)
                    {
                        dimension.SetBlockID(coords + Vector3i.Up, (byte)BlockIDs.SugarCane);
                        server.Scheduler.ScheduleEvent("sugarcane", chunk,
                            TimeSpan.FromSeconds(MathHelper.Random.Next(MinGrowthSeconds, MaxGrowthSeconds)),
                            (_server) => TryGrowth(_server, dimension, coords + Vector3i.Up));
                    }
                }
                else
                {
                    server.Scheduler.ScheduleEvent("sugarcane", chunk,
                        TimeSpan.FromSeconds(MathHelper.Random.Next(MinGrowthSeconds, MaxGrowthSeconds)),
                        (_server) => TryGrowth(_server, dimension, coords));
                }
            }
        }

        public override void BlockPlaced(BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            IChunk chunk = dimension.GetChunk(descriptor.Coordinates)!;
            user.Server.Scheduler.ScheduleEvent("sugarcane", chunk,
                TimeSpan.FromSeconds(MathHelper.Random.Next(MinGrowthSeconds, MaxGrowthSeconds)),
                (server) => TryGrowth(server, dimension, descriptor.Coordinates));
        }

        public override void BlockLoadedFromChunk(IMultiplayerServer server, IDimension dimension, GlobalVoxelCoordinates coords)
        {
            IChunk chunk = dimension.GetChunk(coords)!;
            server.Scheduler.ScheduleEvent("sugarcane", chunk,
                TimeSpan.FromSeconds(MathHelper.Random.Next(MinGrowthSeconds, MaxGrowthSeconds)),
                s => TryGrowth(s, dimension, coords));
        }
    }
}