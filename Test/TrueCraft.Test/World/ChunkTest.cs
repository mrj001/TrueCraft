using System;
using NUnit.Framework;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.World;
using TrueCraft.World;

namespace TrueCraft.Test.World
{
    [TestFixture]
    public class ChunkTest
    {
        [Test]
        public void TestGetBlockID()
        {
            var chunk = new Chunk(GlobalChunkCoordinates.Zero);
            chunk.SetBlockID(LocalVoxelCoordinates.Zero, 12);
            Assert.AreEqual(12, chunk.GetBlockID(LocalVoxelCoordinates.Zero));
        }

        [Test]
        public void TestGetBlockLight()
        {
            var chunk = new Chunk(GlobalChunkCoordinates.Zero);
            chunk.SetBlockLight(LocalVoxelCoordinates.Zero, 5);
            Assert.AreEqual(5, chunk.GetBlockLight(LocalVoxelCoordinates.Zero));
        }

        [Test]
        public void TestGetSkyLight()
        {
            var chunk = new Chunk(GlobalChunkCoordinates.Zero);
            chunk.SetSkyLight(LocalVoxelCoordinates.Zero, 5);
            Assert.AreEqual(5, chunk.GetSkyLight(LocalVoxelCoordinates.Zero));
        }

        [Test]
        public void TestGetMetadata()
        {
            var chunk = new Chunk(GlobalChunkCoordinates.Zero);
            chunk.SetMetadata(LocalVoxelCoordinates.Zero, 5);
            Assert.AreEqual(5, chunk.GetMetadata(LocalVoxelCoordinates.Zero));
        }

        [Test]
        public void TestHeightMap()
        {
            var chunk = new Chunk(GlobalChunkCoordinates.Zero);
            for (int x = 0; x < Chunk.Width; ++x)
            for (int z = 0; z < Chunk.Width; ++z)
                chunk.SetBlockID(new LocalVoxelCoordinates(x, 20, z), (byte)BlockIDs.Stone);
            chunk.UpdateHeightMap();
            Assert.AreEqual(20, chunk.GetHeight(0, 0));
            Assert.AreEqual(20, chunk.GetHeight(1, 0));
            chunk.SetBlockID(new LocalVoxelCoordinates(1, 80, 0), 1);
            Assert.AreEqual(80, chunk.GetHeight(1, 0));
        }

        [Test]
        public void TestConsistency()
        {
            var chunk = new Chunk(GlobalChunkCoordinates.Zero);
            byte val = 0;
            for (int y = 0; y < Chunk.Height; y++)
            for (int x = 0; x < Chunk.Width; x++)
            for (int z = 0; z < Chunk.Depth; z++)
            {
                LocalVoxelCoordinates coords = new LocalVoxelCoordinates(x, y, z);
                chunk.SetBlockID(coords, val);
                chunk.SetMetadata(coords, (byte)(val % 16));
                chunk.SetBlockLight(coords, (byte)(val % 16));
                chunk.SetSkyLight(coords, (byte)(val % 16));
                val++;
            }
            val = 0;
            for (int y = 0; y < Chunk.Height; y++)
            for (int x = 0; x < Chunk.Width; x++)
            for (int z = 0; z < Chunk.Depth; z++)
            {
                LocalVoxelCoordinates coords = new LocalVoxelCoordinates(x, y, z);
                Assert.AreEqual(val, chunk.GetBlockID(coords));
                Assert.AreEqual((byte)(val % 16), chunk.GetMetadata(coords));
                Assert.AreEqual((byte)(val % 16), chunk.GetBlockLight(coords));
                Assert.AreEqual((byte)(val % 16), chunk.GetSkyLight(coords));
                val++;
            }
        }
    }
}