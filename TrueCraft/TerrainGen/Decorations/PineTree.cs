using System;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.World;

namespace TrueCraft.TerrainGen.Decorations
{
    public class PineTree : Decoration
    {
        const int LeafRadius = 2;

        public override bool ValidLocation(LocalVoxelCoordinates location)
        {
            if (location.X - LeafRadius < 0
                || location.X + LeafRadius >= WorldConstants.ChunkWidth
                || location.Z - LeafRadius < 0
                || location.Z + LeafRadius >= WorldConstants.ChunkDepth)
                return false;
            return true;
        }

        public override bool GenerateAt(int seed, IChunk chunk, LocalVoxelCoordinates location)
        {
            if (!ValidLocation(location))
                return false;

            Random random = new Random(seed);
            int height = random.Next(7, 8);
            GenerateColumn(chunk, location, height, (byte)BlockIDs.Wood, 0x1);
            for (int y = 1; y < height; y++)
            {
                if (y % 2 == 0)
                {
                    GenerateVanillaCircle(chunk, new LocalVoxelCoordinates(location.X, location.Y + y + 1, location.Z), LeafRadius - 1, (byte)BlockIDs.Leaves, 0x1);
                    continue;
                }
                GenerateVanillaCircle(chunk, new LocalVoxelCoordinates(location.X, location.Y + y + 1, location.Z), LeafRadius, (byte)BlockIDs.Leaves, 0x1);
            }

            GenerateTopper(chunk, new LocalVoxelCoordinates(location.X, location.Y + height, location.Z), 0x1);
            return true;
        }

        /*
         * Generates the top of the pine/conifer trees.
         * Type:
         * 0x0 - two level topper
         * 0x1 - three level topper
         */
        protected void GenerateTopper(IChunk chunk, LocalVoxelCoordinates location, byte type = 0x0)
        {
            const int sectionRadius = 1;
            GenerateCircle(chunk, location, sectionRadius, (byte)BlockIDs.Leaves, 0x1);
            LocalVoxelCoordinates top = new LocalVoxelCoordinates(location.X, location.Y + 1, location.Z);
            chunk.SetBlockID(top, (byte)BlockIDs.Leaves);
            chunk.SetMetadata(top, 0x1);
            if (type == 0x1 && top.Y + 1 < WorldConstants.Height)
            {
                top = new LocalVoxelCoordinates(top.X, top.Y + 1, top.Z);
                GenerateVanillaCircle(chunk, top, sectionRadius, (byte)BlockIDs.Leaves, 0x1); 
            }
        }
    }
}
