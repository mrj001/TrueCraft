using System;
using TrueCraft.Core;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.World;

namespace TrueCraft.TerrainGen.Biomes
{
    public class TundraBiome : BiomeProvider
    {
        public override byte ID
        {
            get { return (byte)Biome.Tundra; }
        }

        public override double Temperature
        {
            get { return 0.1f; }
        }

        public override double Rainfall
        {
            get { return 0.7f; }
        }

        public override TreeSpecies[] Trees
        {
            get
            {
                return new[] { TreeSpecies.Spruce };
            }
        }

        public override PlantSpecies[] Plants
        {
            get
            {
                return new PlantSpecies[0];
            }
        }

        public override double TreeDensity
        {
            get
            {
                return 50;
            }
        }

        public override byte SurfaceBlock
        {
            get
            {
                return (byte)BlockIDs.Grass;
            }
        }

        public override byte FillerBlock
        {
            get
            {
                return (byte)BlockIDs.Dirt;
            }
        }
    }
}
