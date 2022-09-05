using System;
using TrueCraft.World;
using TrueCraft.Core.Logic;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.World;

namespace TrueCraft.TerrainGen.Decorators
{
    public class LiquidDecorator : IChunkDecorator
    {
        public static readonly int WaterLevel = 40;

        public void Decorate(int seed, IChunk chunk, IBlockRepository _, IBiomeRepository biomes)
        {
            for (int x = 0; x < Chunk.Width; x++)
            {
                for (int z = 0; z < Chunk.Depth; z++)
                {
                    IBiomeProvider biome = biomes.GetBiome(chunk.GetBiome(x, z));
                    int height = chunk.GetHeight(x, z);
                    for (int y = height; y <= WaterLevel; y++)
                    {
                        LocalVoxelCoordinates blockLocation = new LocalVoxelCoordinates(x, y, z);
                        int blockId = chunk.GetBlockID(blockLocation);
                        if (blockId.Equals((byte)BlockIDs.Air))
                        {
                            chunk.SetBlockID(blockLocation, biome.WaterBlock);
                            var below = new LocalVoxelCoordinates(blockLocation.X, blockLocation.Y - 1, blockLocation.Z);
                            if (!chunk.GetBlockID(below).Equals((byte)BlockIDs.Air) && !chunk.GetBlockID(below).Equals(biome.WaterBlock))
                            {
                                if (!biome.WaterBlock.Equals((byte)BlockIDs.Lava) && !biome.WaterBlock.Equals((byte)BlockIDs.LavaStationary))
                                {
                                    var random = new Random(seed);
                                    if (random.Next(100) < 40)
                                    {
                                        chunk.SetBlockID(below, (byte)BlockIDs.Clay);
                                    }
                                    else
                                    {
                                        chunk.SetBlockID(below, (byte)BlockIDs.Sand);
                                    }
                                }
                            }
                        }
                    }
                    for (int y = 4; y < height / 8; y++)
                    {
                        LocalVoxelCoordinates blockLocation = new LocalVoxelCoordinates(x, y, z);
                        int blockId = chunk.GetBlockID(blockLocation);
                        if (blockId.Equals((byte)BlockIDs.Air))
                        {
                            chunk.SetBlockID(blockLocation, (byte)BlockIDs.LavaStationary);
                        }
                    }
                }
            }
        }
    }
}
