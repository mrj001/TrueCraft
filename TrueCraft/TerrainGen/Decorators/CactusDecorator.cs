using System;
using System.Linq;
using TrueCraft.Core;
using TrueCraft.Core.Logic;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.TerrainGen.Decorations;
using TrueCraft.TerrainGen.Noise;
using TrueCraft.Core.World;
using TrueCraft.World;

namespace TrueCraft.TerrainGen.Decorators
{
    public class CactusDecorator : IChunkDecorator
    {
        public void Decorate(int seed, IChunk chunk, IBlockRepository _, IBiomeRepository biomes)
        {
            var noise = new Perlin(seed);
            var chanceNoise = new ClampNoise(noise);
            chanceNoise.MaxValue = 2;
            for (int x = 0; x < Chunk.Width; x++)
            {
                for (int z = 0; z < Chunk.Depth; z++)
                {
                    IBiomeProvider biome = biomes.GetBiome(chunk.GetBiome(x, z));
                    var blockX = MathHelper.ChunkToBlockX(x, chunk.Coordinates.X);
                    var blockZ = MathHelper.ChunkToBlockZ(z, chunk.Coordinates.Z);

                    int height = chunk.GetHeight(x, z);
                    if (biome.Plants.Contains(PlantSpecies.Cactus) && chanceNoise.Value2D(blockX, blockZ) > 1.7)
                    {
                        var blockLocation = new LocalVoxelCoordinates(x, height, z);
                        var cactiPosition = new LocalVoxelCoordinates(blockLocation.X, blockLocation.Y + 1, blockLocation.Z);
                        if (chunk.GetBlockID(blockLocation).Equals((byte)BlockIDs.Sand))
                        {
                            var HeightChance = chanceNoise.Value2D(blockX, blockZ);
                            var CactusHeight = (HeightChance < 1.4) ? 2 : 3;
                            Decoration.GenerateColumn(chunk, cactiPosition, CactusHeight, (byte)BlockIDs.Cactus);
                        }
                    }
                }
            }
        }
    }
}
