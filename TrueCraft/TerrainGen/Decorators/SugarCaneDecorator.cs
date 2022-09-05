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
    public class SugarCaneDecorator : IChunkDecorator
    {
        public void Decorate(int seed, IChunk chunk, IBlockRepository _, IBiomeRepository biomes)
        {
            var noise = new Perlin(seed);
            var chanceNoise = new ClampNoise(noise);
            chanceNoise.MaxValue = 1;
            for (int x = 0; x < Chunk.Width; x++)
            {
                for (int z = 0; z < Chunk.Depth; z++)
                {
                    IBiomeProvider biome = biomes.GetBiome(chunk.GetBiome(x, z));
                    int height = chunk.GetHeight(x, z);
                    int blockX = MathHelper.ChunkToBlockX(x, chunk.Coordinates.X);
                    int blockZ = MathHelper.ChunkToBlockZ(z, chunk.Coordinates.Z);
                    if (biome.Plants.Contains(PlantSpecies.SugarCane))
                    {
                        if (noise.Value2D(blockX, blockZ) > 0.65)
                        {
                            LocalVoxelCoordinates blockLocation = new LocalVoxelCoordinates(x, height, z);
                            LocalVoxelCoordinates sugarCaneLocation = new LocalVoxelCoordinates(x, height + 1, z);
                            var neighborsWater = Decoration.NeighboursBlock(chunk, blockLocation, (byte)BlockIDs.Water) || Decoration.NeighboursBlock(chunk, blockLocation, (byte)BlockIDs.WaterStationary);
                            if (chunk.GetBlockID(blockLocation).Equals((byte)BlockIDs.Grass) && neighborsWater || chunk.GetBlockID(blockLocation).Equals((byte)BlockIDs.Sand) && neighborsWater)
                            {
                                var random = new Random(seed);
                                double heightChance = random.NextDouble();
                                int caneHeight = 3;
                                if (heightChance < 0.05)
                                    caneHeight = 4;
                                else if (heightChance > 0.1 && height < 0.25)
                                    caneHeight = 2;
                                Decoration.GenerateColumn(chunk, sugarCaneLocation, caneHeight, (byte)BlockIDs.SugarCane);
                            }
                        }
                    }
                }
            }
        }
    }
}
