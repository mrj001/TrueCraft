using System;
using TrueCraft.Core;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.World;

namespace TrueCraft.TerrainGen.Biomes
{
    public class DesertBiome : BiomeProvider
    {
        public override byte ID
        {
            get { return (byte)Biome.Desert; }
        }

        public override double Temperature
        {
            get { return 2.0f; }
        }

        public override double Rainfall
        {
            get { return 0.0f; }
        }
        
        public override bool Spawn
        {
            get { return false; }
        }

        public override TreeSpecies[] Trees
        {
            get
            {
                return new TreeSpecies[0];
            }
        }

        public override PlantSpecies[] Plants
        {
            get
            {
                return new[] { PlantSpecies.Deadbush, PlantSpecies.Cactus };
            }
        }

        public override byte SurfaceBlock
        {
            get
            {
                return (byte)BlockIDs.Sand;
            }
        }

        public override byte FillerBlock
        {
            get
            {
                return (byte)BlockIDs.Sandstone;
            }
        }

        public override int SurfaceDepth
        {
            get
            {
                return 4;
            }
        }
    }
}
