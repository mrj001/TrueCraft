using System;
using System.Linq;
using TrueCraft.Core;
using TrueCraft.Core.Logic;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.World;
using TrueCraft.TerrainGen.Decorations;
using TrueCraft.TerrainGen.Noise;
using TrueCraft.World;

namespace TrueCraft.TerrainGen.Decorators
{
    public class TreeDecorator : IChunkDecorator
    {
        public void Decorate(int seed, IChunk chunk, IBlockRepository blockRepository, IBiomeRepository biomes)
        {
            INoise noise = new Perlin(seed);
            ClampNoise chanceNoise = new ClampNoise(noise);
            chanceNoise.MaxValue = 2;
            LocalColumnCoordinates? lastTree = null;
            for (int x = 0; x < Chunk.Width; x++)
            {
                for (int z = 0; z < Chunk.Depth; z++)
                {
                    IBiomeProvider biome = biomes.GetBiome(chunk.GetBiome(x, z));
                    var blockX = MathHelper.ChunkToBlockX(x, chunk.Coordinates.X);
                    var blockZ = MathHelper.ChunkToBlockZ(z, chunk.Coordinates.Z);
                    int height = chunk.GetHeight(x, z);

                    if (lastTree is not null && lastTree.DistanceTo(new LocalColumnCoordinates(x, z)) < biome.TreeDensity)
                        continue;

                    if (noise.Value2D(blockX, blockZ) > 0.3)
                    {
                        LocalVoxelCoordinates location = new LocalVoxelCoordinates(x, height, z);
                        byte id = chunk.GetBlockID(location);
                        byte metadata = chunk.GetMetadata(location);
                        IBlockProvider provider = blockRepository.GetBlockProvider(id);
                        if (id == (byte)BlockIDs.Dirt || id == (byte)BlockIDs.Grass || id == (byte)BlockIDs.SnowBlock
                            || (id != (byte)BlockIDs.WaterStationary && id != (byte)BlockIDs.Water
                                && id != (byte)BlockIDs.Lava && id != (byte)BlockIDs.LavaStationary
                                && provider.GetCollisionBox(metadata).HasValue))
                        {
                            if (provider.GetCollisionBox(metadata).HasValue)
                                location = new LocalVoxelCoordinates(location.X, location.Y - 1, location.Z);
                            var oakNoise = chanceNoise.Value2D(blockX * 0.6, blockZ * 0.6);
                            var birchNoise = chanceNoise.Value2D(blockX * 0.2, blockZ * 0.2);
                            var spruceNoise = chanceNoise.Value2D(blockX * 0.35, blockZ * 0.35);

                            LocalVoxelCoordinates baseCoordinates = new LocalVoxelCoordinates(location.X, location.Y + 1, location.Z);
                            if (biome.Trees.Contains(TreeSpecies.Oak) && oakNoise > 1.01 && oakNoise < 1.25)
                            {
                                var oak = new OakTree().GenerateAt(seed, chunk, baseCoordinates);
                                if (oak)
                                {
                                    lastTree = new LocalColumnCoordinates(x, z);
                                    continue;
                                }
                            }
                            if (biome.Trees.Contains(TreeSpecies.Birch) && birchNoise > 0.3 && birchNoise < 0.95)
                            {
                                var birch = new BirchTree().GenerateAt(seed, chunk, baseCoordinates);
                                if (birch)
                                {
                                    lastTree = new LocalColumnCoordinates(x, z);
                                    continue;
                                }
                            }
                            if (biome.Trees.Contains(TreeSpecies.Spruce) && spruceNoise < 0.75)
                            {
                                var random = new Random(seed);
                                var type = random.Next(1, 2);
                                var generated = false;
                                if (type.Equals(1))
                                    generated = new PineTree().GenerateAt(seed, chunk, baseCoordinates);
                                else
                                    generated = new ConiferTree().GenerateAt(seed, chunk, baseCoordinates);

                                if (generated)
                                {
                                    lastTree = new LocalColumnCoordinates(x, z);
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
