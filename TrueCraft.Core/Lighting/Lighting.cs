using System;
using TrueCraft.Core.World;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.Logic;
using System.Collections.Generic;
using TrueCraft.Profiling;
using System.Collections.Concurrent;

namespace TrueCraft.Core.Lighting
{
    // https://github.com/SirCmpwn/TrueCraft/wiki/Lighting

    // Note: Speed-critical code
    public class Lighting
    {
        private struct LightingOperation
        {
            public BoundingBox Box { get; set; }
            public bool SkyLight { get; set; }
            public bool Initial { get; set; }
        }

        private readonly IBlockRepository _blockRepository;
        public IDimension Dimension { get; }

        private ConcurrentQueue<LightingOperation> PendingOperations { get; set; }

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<GlobalChunkCoordinates, byte[,]> HeightMaps { get; set; }

        public Lighting(IDimension dimension, IBlockRepository blockRepository)
        {
            _blockRepository = blockRepository;
            Dimension = dimension;
            PendingOperations = new ConcurrentQueue<LightingOperation>();
            HeightMaps = new Dictionary<GlobalChunkCoordinates, byte[,]>();

            // TODO restore maintenance of lighting Height Maps
            //dimension.ChunkGenerated += (sender, e) => GenerateHeightMap(e.Chunk);
            //dimension.ChunkLoaded += (sender, e) => GenerateHeightMap(e.Chunk);
            //dimension.BlockChanged += (sender, e) =>
            //{
            //    if (e.NewBlock.ID != e.OldBlock.ID)
            //        UpdateHeightMap(e.Position);
            //};

            //foreach (var chunk in dimension)
            //    GenerateHeightMap(chunk);
        }

        private void GenerateHeightMap(IChunk chunk)
        {
            LocalVoxelCoordinates coords;
            var map = new byte[WorldConstants.ChunkWidth, WorldConstants.ChunkDepth];
            for (byte x = 0; x < WorldConstants.ChunkWidth; x++)
            {
                for (byte z = 0; z < WorldConstants.ChunkDepth; z++)
                {
                    for (byte y = (byte)(chunk.GetHeight(x, z) + 2); y > 0; y--)
                    {
                        if (y >= WorldConstants.Height)
                            continue;
                        coords = new LocalVoxelCoordinates(x, y - 1, z);
                        var id = chunk.GetBlockID(coords);
                        if (id == (byte)BlockIDs.Air)
                            continue;
                        var provider = _blockRepository.GetBlockProvider(id);
                        if (provider == null || provider.LightOpacity != 0)
                        {
                            map[x, z] = y;
                            break;
                        }
                    }
                }
            }
            HeightMaps[chunk.Coordinates] = map;
        }

        private void UpdateHeightMap(GlobalVoxelCoordinates coords)
        {
            IChunk? chunk;
            LocalVoxelCoordinates adjusted = Dimension.FindBlockPosition(coords, out chunk);

            if (!HeightMaps.ContainsKey(chunk!.Coordinates))
                return;

            var map = HeightMaps[chunk.Coordinates];
            byte x = (byte)adjusted.X; byte z = (byte)adjusted.Z;
            LocalVoxelCoordinates localCoords;
            for (byte y = (byte)(Math.Min(WorldConstants.Height, chunk.GetHeight(x, z) + 2)); y > 0; y--)
            {
                localCoords = new LocalVoxelCoordinates(x, y - 1, z);
                var id = chunk.GetBlockID(localCoords);
                if (id == 0)
                    continue;
                var provider = _blockRepository.GetBlockProvider(id);
                if (provider.LightOpacity != 0)
                {
                    map[x, z] = y;
                    break;
                }
            }
        }

        private void LightBox(LightingOperation op)
        {
            IChunk? chunk = Dimension.GetChunk((GlobalVoxelCoordinates)op.Box.Center);
            if (chunk is null || !chunk.TerrainPopulated)
                return;
            Profiler.Start("lighting.box");
            for (int x = (int)op.Box.Min.X; x < (int)op.Box.Max.X; x++)
            for (int z = (int)op.Box.Min.Z; z < (int)op.Box.Max.Z; z++)
            for (int y = (int)op.Box.Max.Y - 1; y >= (int)op.Box.Min.Y; y--)
            {
                LightVoxel(x, y, z, op);
            }
            Profiler.Done();
        }

        /// <summary>
        /// Propegates a lighting change to an adjacent voxel (if neccesary)
        /// </summary>
        private void PropegateLightEvent(int x, int y, int z, byte value, LightingOperation op)
        {
            var coords = new GlobalVoxelCoordinates(x, y, z);
            if (!Dimension.IsValidPosition(coords))
                return;
            IChunk? chunk;
            var adjustedCoords = Dimension.FindBlockPosition(coords, out chunk);
            if (chunk is null || !chunk.TerrainPopulated)
                return;
            byte current = op.SkyLight ? Dimension.GetSkyLight(coords) : Dimension.GetBlockLight(coords);
            if (value == current)
                return;
            var provider = _blockRepository.GetBlockProvider(Dimension.GetBlockID(coords));
            if (op.Initial)
            {
                byte emissiveness = provider.Luminance;
                if (chunk.GetHeight((byte)adjustedCoords.X, (byte)adjustedCoords.Z) <= y)
                    emissiveness = 15;
                if (emissiveness >= current)
                    return;
            }
            EnqueueOperation(new BoundingBox(new Vector3(x, y, z), new Vector3(x, y, z) + 1), op.SkyLight, op.Initial);
        }

        /// <summary>
        /// Computes the correct lighting value for a given voxel.
        /// </summary>
        private void LightVoxel(int x, int y, int z, LightingOperation op)
        {
            GlobalVoxelCoordinates coords = new GlobalVoxelCoordinates(x, y, z);

            IChunk? chunk;
            var adjustedCoords = Dimension.FindBlockPosition(coords, out chunk);

            if (chunk is null || !chunk.TerrainPopulated) // Move on if this chunk is empty
                return;

            Profiler.Start("lighting.voxel");

            var id = Dimension.GetBlockID(coords);
            var provider = _blockRepository.GetBlockProvider(id);

            // The opacity of the block determines the amount of light it receives from
            // neighboring blocks. This is subtracted from the max of the neighboring
            // block values. We must subtract at least 1.
            byte opacity = Math.Max(provider.LightOpacity, (byte)1);

            byte current = op.SkyLight ? Dimension.GetSkyLight(coords) : Dimension.GetBlockLight(coords);
            byte final = 0;

            // Calculate emissiveness
            byte emissiveness = provider.Luminance;
            if (op.SkyLight)
            {
                byte[,]? map;
                if (!HeightMaps.TryGetValue(chunk.Coordinates, out map))
                {
                    GenerateHeightMap(chunk);
                    map = HeightMaps[chunk.Coordinates];
                }
                var height = map[adjustedCoords.X, adjustedCoords.Z];
                // For skylight, the emissiveness is 15 if y >= height
                if (y >= height)
                    emissiveness = 15;
                else
                {
                    if (provider.LightOpacity >= 15)
                        emissiveness = 0;
                }
            }
            
            if (opacity < 15 || emissiveness != 0)
            {
                // Compute the light based on the max of the neighbors
                byte max = 0;
                for (int i = 0; i < Vector3i.Neighbors6.Length; i++)
                {
                    GlobalVoxelCoordinates neighbor = coords + Vector3i.Neighbors6[i];
                    // NOTE: If the neighbor is in a different chunk, which is already generated,
                    //    that chunk will be loaded and lit as well.  If a sufficient number of nearby chunks
                    //    have been previously generated, this recursion will cause a stack overflow.
                    //    We must neither load nor generate neighbouring chunks that are not
                    //    already loaded.
                    if (Dimension.IsValidPosition(neighbor) && Dimension.IsChunkLoaded(neighbor))
                    {
                        IChunk? c;
                        var adjusted = Dimension.FindBlockPosition(neighbor, out c);
                        if (c is not null) // We don't want to generate new chunks just to light this voxel
                        {
                            byte val;
                            if (op.SkyLight)
                                val = c.GetSkyLight(adjusted);
                            else
                                val = c.GetBlockLight(adjusted);
                            max = Math.Max(max, val);
                        }
                    }
                }
                // final = MAX(max - opacity, emissiveness, 0)
                final = (byte)Math.Max(max - opacity, emissiveness);
                if (final < 0)
                    final = 0;
            }

            if (final != current)
            {
                // Apply changes
                if (op.SkyLight)
                    chunk.SetSkyLight(adjustedCoords, final);
                else
                    chunk.SetBlockLight(adjustedCoords, final);
                
                byte propegated = (byte)Math.Max(final - 1, 0);

                // Propegate lighting change to neighboring blocks
                PropegateLightEvent(x - 1, y, z, propegated, op);
                PropegateLightEvent(x, y - 1, z, propegated, op);
                PropegateLightEvent(x, y, z - 1, propegated, op);
                if (x + 1 >= op.Box.Max.X)
                    PropegateLightEvent(x + 1, y, z, propegated, op);
                if (y + 1 >= op.Box.Max.Y)
                    PropegateLightEvent(x,  y + 1, z, propegated, op);
                if (z + 1 >= op.Box.Max.Z)
                    PropegateLightEvent(x, y, z + 1, propegated, op);
            }
            Profiler.Done();
        }

        public bool TryLightNext()
        {
            LightingOperation op;
            if (PendingOperations.Count == 0)
                return false;
            // TODO: Maybe a timeout or something?
            bool dequeued = false;
            while (!(dequeued = PendingOperations.TryDequeue(out op)) && PendingOperations.Count > 0) ;
            if (dequeued)
                LightBox(op);
            return dequeued;
        }

        public void EnqueueOperation(BoundingBox box, bool skyLight, bool initial = false)
        {
            // Try to merge with existing operation
            /*
            for (int i = PendingOperations.Count - 1; i > PendingOperations.Count - 5 && i > 0; i--)
            {
                var op = PendingOperations[i];
                if (op.Box.Intersects(box))
                {
                    op.Box = new BoundingBox(Vector3.Min(op.Box.Min, box.Min), Vector3.Max(op.Box.Max, box.Max));
                    return;
                }
            }
            */
            PendingOperations.Enqueue(new LightingOperation { SkyLight = skyLight, Box = box, Initial = initial });
        }

        /// <summary>
        /// Sets the skylight of all voxels above any non-air blocks to maximum
        /// </summary>
        /// <param name="chunk">The Chunk to operate on.</param>
        private void SetUpperVoxels(IChunk chunk)
        {
            for (int x = 0; x < WorldConstants.ChunkWidth; x++)
            for (int z = 0; z < WorldConstants.ChunkDepth; z++)
            for (int y = chunk.MaxHeight + 1; y < WorldConstants.Height; y++)
                chunk.SetSkyLight(new LocalVoxelCoordinates(x, y, z), 15);
        }

        /// <summary>
        /// Queues the initial lighting pass for a newly generated chunk.
        /// </summary>
        public void InitialLighting(IChunk chunk, bool flush = true)
        {
            // Set voxels above max height to 0xFF
            SetUpperVoxels(chunk);
            GlobalVoxelCoordinates coords = (GlobalVoxelCoordinates)chunk.Coordinates;
            EnqueueOperation(new BoundingBox(new Vector3(coords.X, 0, coords.Z),
                new Vector3(coords.X + WorldConstants.ChunkWidth, chunk.MaxHeight + 2, coords.Z + WorldConstants.ChunkDepth)),
                true, true);
            TryLightNext();
            while (flush && TryLightNext())
            {
            }
        }
    }
}