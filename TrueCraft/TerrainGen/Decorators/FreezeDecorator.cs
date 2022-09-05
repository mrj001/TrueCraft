using System;
using System.Linq;
using TrueCraft.Core.Logic;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.World;
using TrueCraft.World;

namespace TrueCraft.TerrainGen.Decorators
{
    class FreezeDecorator : IChunkDecorator
    {
        public void Decorate(int seed, IChunk chunk, IBlockRepository _, IBiomeRepository biomes)
        {
            for (int x = 0; x < Chunk.Width; x++)
            {
                for (int z = 0; z < Chunk.Depth; z++)
                {
                    IBiomeProvider biome = biomes.GetBiome(chunk.GetBiome(x, z));
                    if (biome.Temperature < 0.15)
                    {
                        int height = chunk.GetHeight(x, z);
                        for (int y = height; y < Chunk.Height; y++)
                        {
                            var location = new LocalVoxelCoordinates(x, y, z);
                            if (chunk.GetBlockID(location).Equals((byte)BlockIDs.WaterStationary) || chunk.GetBlockID(location).Equals((byte)BlockIDs.Water))
                                chunk.SetBlockID(location, (byte)BlockIDs.Ice);
                            else
                            {
                                var below = chunk.GetBlockID(location);
                                byte[] whitelist =
                                {
                                    (byte)BlockIDs.Dirt,
                                    (byte)BlockIDs.Grass,
                                    (byte)BlockIDs.Ice,
                                    (byte)BlockIDs.Leaves
                                };
                                if (y == height && whitelist.Any(w => w == below))
                                {
                                    if (chunk.GetBlockID(location).Equals((byte)BlockIDs.Ice) && CoverIce(chunk, biomes, location))
                                        chunk.SetBlockID(new LocalVoxelCoordinates(location.X, location.Y + 1, location.Z), (byte)BlockIDs.Snow);
                                    else if (!chunk.GetBlockID(location).Equals((byte)BlockIDs.Snow) && !chunk.GetBlockID(location).Equals((byte)BlockIDs.Air))
                                        chunk.SetBlockID(new LocalVoxelCoordinates(location.X, location.Y + 1, location.Z), (byte)BlockIDs.Air);
                                }
                            }
                        }
                    }
                }
            }
        }

        bool CoverIce(IChunk chunk, IBiomeRepository biomes, LocalVoxelCoordinates location)
        {
            const int maxDistance = 4;
            Vector3i[] nearby = new Vector3i[]
            {
                maxDistance * Vector3i.West,
                maxDistance * Vector3i.East,
                maxDistance * Vector3i.South,
                maxDistance * Vector3i.North
            };
            for (int i = 0; i < nearby.Length; i++)
            {
                int checkX = location.X + nearby[i].X;
                int checkZ = location.Z + nearby[i].Z;
                // TODO: does the order of the nearby array produce peculiar direction-dependent variations
                //       in snow cover near chunk boundaries?
                if (checkX < 0 || checkX >= Chunk.Width || checkZ < 0 || checkZ >= Chunk.Depth)
                    return false;
                LocalVoxelCoordinates check = new LocalVoxelCoordinates(checkX, location.Y, checkZ);
                IBiomeProvider biome = biomes.GetBiome(chunk.GetBiome(checkX, checkZ));
                if (chunk.GetBlockID(check).Equals(biome.SurfaceBlock) || chunk.GetBlockID(check).Equals(biome.FillerBlock))
                    return true;
            }
            return false;
        }
    }
}
