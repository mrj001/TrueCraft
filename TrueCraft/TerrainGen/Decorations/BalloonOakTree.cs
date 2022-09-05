using System;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.World;

namespace TrueCraft.TerrainGen.Decorations
{
    public class BalloonOakTree : Decoration
    {
        const int LeafRadius = 2;

        public override bool ValidLocation(LocalVoxelCoordinates location)
        {
            if (location.X - LeafRadius < 0
                || location.X + LeafRadius >= WorldConstants.ChunkWidth
                || location.Z - LeafRadius < 0
                || location.Z + LeafRadius >= WorldConstants.ChunkDepth
                || location.Y + LeafRadius >= WorldConstants.Height)
            {
                return false;
            }
            return true;
        }

        public override bool GenerateAt(int seed, IChunk chunk, LocalVoxelCoordinates location)
        {
            if (!ValidLocation(location))
                return false;

            Random random = new Random(seed);
            int height = random.Next(4, 5);
            GenerateColumn(chunk, location, height, (byte)BlockIDs.Wood, 0x0);
            LocalVoxelCoordinates leafLocation = new LocalVoxelCoordinates(location.X, location.Y + height, location.Z);
            GenerateSphere(chunk, leafLocation, LeafRadius, (byte)BlockIDs.Leaves, 0x0);
            return true;
        }
    }
}
