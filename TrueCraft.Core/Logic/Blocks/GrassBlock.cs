using System;
using System.Xml;
using TrueCraft.Core.World;
using TrueCraft.Core.Server;
using TrueCraft.Core.Networking;

namespace TrueCraft.Core.Logic.Blocks
{
    public class GrassBlock : BlockProvider
    {
        public static readonly int MinGrowthTime = 60 * 5;
        public static readonly int MaxGrowthTime = 60 * 10;

        static GrassBlock()
        {
            GrowthCandidates = new Vector3i[3 * 3 * 5];
            int i = 0;
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    for (int y = -3; y <= 1; y++)
                    {
                        GrowthCandidates[i++] = new Vector3i(x, y, z);
                    }
                }
            }
        }

        private static readonly Vector3i[] GrowthCandidates;

        public static readonly int MaxDecayTime = 60 * 10;
        public static readonly int MinDecayTime = 60 * 2;

        public GrassBlock(XmlNode node) : base(node)
        {

        }

        private void ScheduledUpdate(IDimension dimension, GlobalVoxelCoordinates coords)
        {
            if (dimension.IsValidPosition(coords + Vector3i.Up))
            {
                var id = dimension.GetBlockID(coords + Vector3i.Up);
                var provider = dimension.BlockRepository.GetBlockProvider(id);
                if (provider.Opaque)
                    dimension.SetBlockID(coords, (byte)BlockIDs.Dirt);
            }
        }

        public override void BlockUpdate(BlockDescriptor descriptor, BlockDescriptor source, IMultiplayerServer server, IDimension dimension)
        {
            if (source.Coordinates == descriptor.Coordinates + Vector3i.Up)
            {
                var provider = dimension.BlockRepository.GetBlockProvider(source.ID);
                if (provider.Opaque)
                {
                    IChunk? chunk = dimension.GetChunk(descriptor.Coordinates);
                    server.Scheduler.ScheduleEvent("grass",
                        chunk!,     // won't be null as a block update was just done in this chunk.
                    TimeSpan.FromSeconds(MathHelper.Random.Next(MinDecayTime, MaxDecayTime)), s =>
                    {
                        ScheduledUpdate(dimension, descriptor.Coordinates);
                    });
                }
            }
        }

        private void TrySpread(IMultiplayerServer server, IDimension dimension, GlobalVoxelCoordinates coords)
        {
            if (!dimension.IsValidPosition(coords + Vector3i.Up))
                return;
            var sky = dimension.GetSkyLight(coords + Vector3i.Up);
            var block = dimension.GetBlockLight(coords + Vector3i.Up);
            if (sky < 9 && block < 9)
                return;
            for (int i = 0, j = MathHelper.Random.Next(GrowthCandidates.Length); i < GrowthCandidates.Length; i++, j++)
            {
                var candidate = GrowthCandidates[j % GrowthCandidates.Length] + coords;
                if (!dimension.IsValidPosition(candidate) || !dimension.IsValidPosition(candidate + Vector3i.Up))
                    continue;
                var id = dimension.GetBlockID(candidate);
                if (id == (byte)BlockIDs.Dirt)
                {
                    var _sky = dimension.GetSkyLight(candidate + Vector3i.Up);
                    var _block = dimension.GetBlockLight(candidate + Vector3i.Up);
                    if (_sky < 4 && _block < 4)
                        continue;
                    IChunk? chunk;
                    LocalVoxelCoordinates _candidate = dimension.FindBlockPosition(candidate, out chunk);
                    if (chunk is null)
                        // Don't try to spread into unloaded Chunks.
                        continue;

                    bool grow = true;
                    for (int y = candidate.Y; y < chunk.GetHeight((byte)_candidate.X, (byte)_candidate.Z); y++)
                    {
                        var b = dimension.GetBlockID(new GlobalVoxelCoordinates(candidate.X, y, candidate.Z));
                        var p = dimension.BlockRepository.GetBlockProvider(b);
                        if (p.LightOpacity >= 2)
                        {
                            grow = false;
                            break;
                        }
                    }
                    if (grow)
                    {
                        dimension.SetBlockID(candidate, (byte)BlockIDs.Grass);
                        server.Scheduler.ScheduleEvent("grass", chunk,
                            TimeSpan.FromSeconds(MathHelper.Random.Next(MinGrowthTime, MaxGrowthTime)),
                            s => TrySpread(server, dimension, candidate));
                    }
                    break;
                }
            }
        }

        public override void BlockPlaced(BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            // TODO: Investigate inconsistency - some methods change this coordinate per BlockFace.  Why not here?
            IChunk? chunk = dimension.GetChunk(descriptor.Coordinates);
            user.Server.Scheduler.ScheduleEvent("grass",
                chunk!,    // very unlikely to be null as the block can't be placed in an unloaded Chunk.
                TimeSpan.FromSeconds(MathHelper.Random.Next(MinGrowthTime, MaxGrowthTime)),
                s => TrySpread(user.Server, dimension, descriptor.Coordinates));
        }

        public override void BlockLoadedFromChunk(IMultiplayerServer server, IDimension dimension, GlobalVoxelCoordinates coords)
        {
            IChunk chunk = dimension.GetChunk(coords)!;
            server.Scheduler.ScheduleEvent("grass",
                chunk,    // Chunk loading caused this method to be called.
                TimeSpan.FromSeconds(MathHelper.Random.Next(MinGrowthTime, MaxGrowthTime)),
                s => TrySpread(server, dimension, coords));
        }
    }
}
