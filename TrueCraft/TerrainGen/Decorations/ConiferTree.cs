using System;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.World;

namespace TrueCraft.TerrainGen.Decorations
{
    public class ConiferTree : PineTree
    {
        const int LeafRadius = 2;

        public override bool GenerateAt(int seed, IChunk chunk, LocalVoxelCoordinates location)
        {
            if (!ValidLocation(location))
                return false;

            Random random = new Random(seed);
            int height = random.Next(7, 8);
            GenerateColumn(chunk, location, height, (byte)BlockIDs.Wood, 0x1);
            LocalVoxelCoordinates centre = new LocalVoxelCoordinates(location.X, location.Y + height - 2, location.Z);
            GenerateCircle(chunk, centre, LeafRadius - 1, (byte)BlockIDs.Leaves, 0x1);
            centre = new LocalVoxelCoordinates(location.X, location.Y + height - 1, location.Z);
            GenerateCircle(chunk, centre, LeafRadius, (byte)BlockIDs.Leaves, 0x1);
            centre = new LocalVoxelCoordinates(location.X, location.Y + height, location.Z);
            GenerateCircle(chunk, centre, LeafRadius, (byte)BlockIDs.Leaves, 0x1);
            centre = new LocalVoxelCoordinates(location.X, location.Y + height + 1, location.Z);
            GenerateTopper(chunk, centre, 0x0);
            return true;
        }
    }
}
