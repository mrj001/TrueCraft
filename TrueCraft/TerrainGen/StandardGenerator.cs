using System;
using System.Collections.Generic;
using TrueCraft.Core.World;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.TerrainGen.Noise;
using TrueCraft.TerrainGen.Decorators;
using TrueCraft.World;
using TrueCraft.Core;

namespace TrueCraft.TerrainGen
{
    /// <summary>
    /// This terrain generator is still under heavy development. Use at your own risk.
    /// </summary>
    public class StandardGenerator : Generator
    {
        BiomeRepository Biomes = new BiomeRepository();
        Perlin HighNoise;
        Perlin LowNoise;
        Perlin BottomNoise;
        Perlin CaveNoise;
        ClampNoise HighClamp;
        ClampNoise LowClamp;
        ClampNoise BottomClamp;
        ModifyNoise FinalNoise;
        bool EnableCaves;
        private const int GroundLevel = 50;

        private readonly IBiomeMap _biomeMap;

        public StandardGenerator(int seed) : base(seed)
        {
            EnableCaves = true;

            _biomeMap = new BiomeMap(seed);

            ChunkDecorators.Add(new LiquidDecorator());
            ChunkDecorators.Add(new OreDecorator());
            ChunkDecorators.Add(new PlantDecorator());
            ChunkDecorators.Add(new TreeDecorator());
            ChunkDecorators.Add(new FreezeDecorator());
            ChunkDecorators.Add(new CactusDecorator());
            ChunkDecorators.Add(new SugarCaneDecorator());
            ChunkDecorators.Add(new DungeonDecorator(GroundLevel));

            HighNoise = new Perlin(seed);
            LowNoise = new Perlin(seed);
            BottomNoise = new Perlin(seed);
            CaveNoise = new Perlin(seed);
            
            CaveNoise.Octaves = 3;
            CaveNoise.Amplitude = 0.05;
            CaveNoise.Persistance = 2;
            CaveNoise.Frequency = 0.05;
            CaveNoise.Lacunarity = 2;

            HighNoise.Persistance = 1;
            HighNoise.Frequency = 0.013;
            HighNoise.Amplitude = 10;
            HighNoise.Octaves = 2;
            HighNoise.Lacunarity = 2;

            LowNoise.Persistance = 1;
            LowNoise.Frequency = 0.004;
            LowNoise.Amplitude = 35;
            LowNoise.Octaves = 2;
            LowNoise.Lacunarity = 2.5;

            BottomNoise.Persistance = 0.5;
            BottomNoise.Frequency = 0.013;
            BottomNoise.Amplitude = 5;
            BottomNoise.Octaves = 2;
            BottomNoise.Lacunarity = 1.5;

            HighClamp = new ClampNoise(HighNoise);
            HighClamp.MinValue = -30;
            HighClamp.MaxValue = 50;

            LowClamp = new ClampNoise(LowNoise);
            LowClamp.MinValue = -30;
            LowClamp.MaxValue = 30;

            BottomClamp = new ClampNoise(BottomNoise);
            BottomClamp.MinValue = -20;
            BottomClamp.MaxValue = 5;

            FinalNoise = new ModifyNoise(HighClamp, LowClamp, NoiseModifier.Add);
        }

        public Vector3 SpawnPoint { get; private set; }
        public bool SingleBiome { get; private set; }
        public byte GenerationBiome { get; private set; }

        public override IChunk GenerateChunk(GlobalChunkCoordinates coordinates)
        {
            const int featurePointDistance = 400;

            // TODO: Create a terrain generator initializer function that gets passed the seed etc
            var worley = new CellNoise(_seed);
            HighNoise.Seed = _seed;
            LowNoise.Seed = _seed;
            CaveNoise.Seed = _seed;

            var chunk = new Chunk(coordinates);

            for (int x = 0; x < Chunk.Width; x++)
            {
                for (int z = 0; z < Chunk.Depth; z++)
                {
                    var blockX = MathHelper.ChunkToBlockX(x, coordinates.X);
                    var blockZ = MathHelper.ChunkToBlockZ(z, coordinates.Z);

                    const double lowClampRange = 5;
                    double lowClampMid = LowClamp.MaxValue - ((LowClamp.MaxValue + LowClamp.MinValue) / 2);
                    double lowClampValue = LowClamp.Value2D(blockX, blockZ);

                    if (lowClampValue > lowClampMid - lowClampRange && lowClampValue < lowClampMid + lowClampRange)
                    {
                        InvertNoise NewPrimary = new InvertNoise(HighClamp);
                        FinalNoise.PrimaryNoise = NewPrimary;
                    }
                    else
                    {
                        //reset it after modifying the values
                        FinalNoise = new ModifyNoise(HighClamp, LowClamp, NoiseModifier.Add);
                    }
                    FinalNoise = new ModifyNoise(FinalNoise, BottomClamp, NoiseModifier.Subtract);

                    var cellValue = worley.Value2D(blockX, blockZ);
                    GlobalColumnCoordinates location = new GlobalColumnCoordinates(blockX, blockZ);
                    if (_biomeMap.BiomeCells.Count < 1
                        || cellValue.Equals(1)
                        && _biomeMap.ClosestCellPoint(location) >= featurePointDistance)
                    {
                        byte id = (SingleBiome) ? GenerationBiome
                            : _biomeMap.GenerateBiome(_seed, Biomes, location,
                                IsSpawnCoordinate(location.X, location.Z));
                        var cell = new BiomeCell(id, location);
                        _biomeMap.AddCell(cell);
                    }

                    Biome biomeId = (Biome)GetBiome(location);
                    IBiomeProvider biome = Biomes.GetBiome(biomeId);
                    chunk.SetBiome(x, z, biomeId);

                    var height = GetHeight(blockX, blockZ);
                    var surfaceHeight = height - biome.SurfaceDepth;

                    // TODO: Do not overwrite blocks if they are already set from adjacent chunks
                    for (int y = 0; y <= height; y++)
                    {
                        double cave = 0;
                        if (!EnableCaves)
                            cave = double.MaxValue;
                        else
                            cave = CaveNoise.Value3D((blockX + x) / 2, y / 2, (blockZ + z) / 2);
                        double threshold = 0.05;
                        if (y < 4)
                            threshold = double.MaxValue;
                        else
                        {
                            if (y > height - 8)
                                threshold = 8;
                        }
                        if (cave < threshold)
                        {
                            if (y == 0)
                                chunk.SetBlockID(new LocalVoxelCoordinates(x, y, z), (byte)BlockIDs.Bedrock);
                            else
                            {
                                if (y.Equals(height) || y < height && y > surfaceHeight)
                                    chunk.SetBlockID(new LocalVoxelCoordinates(x, y, z), biome.SurfaceBlock);
                                else
                                {
                                    if (y > surfaceHeight - biome.FillerDepth)
                                        chunk.SetBlockID(new LocalVoxelCoordinates(x, y, z), biome.FillerBlock);
                                    else
                                        chunk.SetBlockID(new LocalVoxelCoordinates(x, y, z), (byte)BlockIDs.Stone);
                                }
                            }
                        }
                    }
                }
            }
            foreach (var decorator in ChunkDecorators)
                // TODO: pass the BlockRepository in as a parameter.
                decorator.Decorate(_seed, chunk, Core.Logic.BlockRepository.Get(), Biomes);
            chunk.TerrainPopulated = true;
            chunk.UpdateHeightMap();
            return chunk;
        }

        public override GlobalVoxelCoordinates GetSpawn(IDimension dimension)
        {
            var chunk = GenerateChunk(new GlobalChunkCoordinates(0, 0));
            int spawnPointHeight = chunk.GetHeight(0, 0);
            return new GlobalVoxelCoordinates(0, spawnPointHeight + 1, 0);
        }

        private byte GetBiome(GlobalColumnCoordinates location)
        {
            if (SingleBiome)
                return GenerationBiome;
            return _biomeMap.GetBiome(location);
        }

        // TODO:  for the following values of (x,z), this will return true:
        //     (-2000, 0) (2000, 0) (0, -2000) (0, 2000)
        //   Is this really desired behaviour????
        bool IsSpawnCoordinate(int x, int z)
        {
            return x > -1000 && x < 1000 || z > -1000 && z < 1000;
        }

        int GetHeight(int x, int z)
        {
            var value = FinalNoise.Value2D(x, z) + GroundLevel;
            double distanceFromOrigin = Math.Sqrt(x * x + z * z);
            double distance = IsSpawnCoordinate(x, z) ? distanceFromOrigin : 1000;
            if (distance < 1000) // Avoids deep water within 1km sq of spawn
                value += (1 - distance / 1000f) * 18;
            if (value < 0)
                value = GroundLevel;
            if (value > Chunk.Height)
                value = Chunk.Height - 1;
            return (int)value;
        }
    }
}
