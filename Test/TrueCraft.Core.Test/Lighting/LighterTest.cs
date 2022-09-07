using System;
using System.Diagnostics;
using Moq;
using NUnit.Framework;
using TrueCraft.Core.Lighting;
using TrueCraft.Core.Logic;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.Server;
using TrueCraft.Core.Test.World;
using TrueCraft.Core.World;

namespace TrueCraft.Core.Test.Lighting
{
    [TestFixture]
    public class LighterTest
    {
        private const int _testSeed = 314159;

        // NOTE: this is the height built by FakeChunk.
        private const int SurfaceHeight = 5;

        private const byte MockAirBlockID = 0;
        private const int MockOpaqueBlockID = 3;
        private const byte MockLeavesBlockID = 18;

        private readonly IBlockRepository _blockRepository;

        private readonly IItemRepository _itemRepository;

        public LighterTest()
        {
            Mock<IBlockProvider> mockAirBlock = new Mock<IBlockProvider>(MockBehavior.Strict);
            mockAirBlock.Setup(x => x.LightOpacity).Returns(0);
            mockAirBlock.Setup(x => x.ID).Returns(MockAirBlockID);

            Mock<IBlockProvider> mockOpaqueBlock = new Mock<IBlockProvider>(MockBehavior.Strict);
            mockOpaqueBlock.Setup(x => x.LightOpacity).Returns(255);
            mockOpaqueBlock.Setup(x => x.ID).Returns(MockOpaqueBlockID);

            Mock<IBlockProvider> mockLeavesBlock = new Mock<IBlockProvider>(MockBehavior.Strict);
            mockLeavesBlock.Setup(x => x.LightOpacity).Returns(2);
            mockLeavesBlock.Setup(x => x.ID).Returns(MockLeavesBlockID);

            Mock<IBlockRepository> mockRepository = new Mock<IBlockRepository>(MockBehavior.Strict);
            mockRepository.Setup(x => x.GetBlockProvider(It.Is<byte>(b => b == MockAirBlockID))).Returns(mockAirBlock.Object);
            mockRepository.Setup(x => x.GetBlockProvider(It.Is<byte>(b => b == MockOpaqueBlockID))).Returns(mockOpaqueBlock.Object);
            mockRepository.Setup(x => x.GetBlockProvider(It.Is<byte>(b => b == MockLeavesBlockID))).Returns(mockLeavesBlock.Object);
            _blockRepository = mockRepository.Object;

            Mock<IItemRepository> mockItemRepository = new Mock<IItemRepository>(MockBehavior.Strict);
            _itemRepository = mockItemRepository.Object;
        }

        private IDimension BuildDimension()
        {
            IDimension rv = new FakeDimension(_blockRepository, _itemRepository);

            for (int x = 0; x < WorldConstants.ChunkWidth; x++)
                for (int z = 0; z < WorldConstants.ChunkDepth; z++)
                    for (int y = 0; y < SurfaceHeight; y++)
                        rv.SetBlockID(new GlobalVoxelCoordinates(x, y, z), MockOpaqueBlockID);

            return rv;
        }

        private IDimension BuildDimension(byte surfaceHeight)
        {
            IDimension rv = new FakeDimension(_blockRepository, _itemRepository, surfaceHeight);

            for (int x = 0; x < WorldConstants.ChunkWidth; x++)
                for (int z = 0; z < WorldConstants.ChunkDepth; z++)
                    for (int y = 0; y < SurfaceHeight; y++)
                        rv.SetBlockID(new GlobalVoxelCoordinates(x, y, z), MockOpaqueBlockID);

            return rv;
        }

        [Test]
        public void TestBasicLighting()
        {
            IDimension dimension = BuildDimension();
            ILighter lighter = new OverWorldLighter(dimension, null!);
            IChunk chunk = dimension.GetChunk(GlobalChunkCoordinates.Zero)!;
            int ground = chunk.GetHeight(0, 0);

            lighter.DoLightingOperation(new LightingOperation(GlobalVoxelCoordinates.Zero, LightingOperationMode.Add, LightingOperationKind.Initial, 15));

            for (int y = ground + 5; y >= ground; y--)
            {
                Console.Write("Y: {0} ", y);
                Console.Write(dimension.GetBlockID(new GlobalVoxelCoordinates(0, y, 0)));
                Console.Write(" -> ");
                Console.WriteLine(dimension.GetSkyLight(new GlobalVoxelCoordinates(0, y, 0)));
            }

            // Validate behavior
            for (int y = 0; y < WorldConstants.Height; y++)
            {
                for (int x = 0; x < WorldConstants.ChunkWidth; x++)
                {
                    for (int z = 0; z < WorldConstants.ChunkDepth; z++)
                    {
                        GlobalVoxelCoordinates coords = new GlobalVoxelCoordinates(x, y, z);
                        int sky = dimension.GetSkyLight(coords);
                        if (y < SurfaceHeight)
                            Assert.AreEqual(0, sky, coords.ToString());
                        else
                            Assert.AreEqual(15, sky, coords.ToString());
                    }
                }
            }
        }

        [Test]
        public void TestShortPropegation()
        {
            IDimension dimension = BuildDimension();
            IChunk chunk = dimension.GetChunk(GlobalChunkCoordinates.Zero)!;
            //var lighter = new Core.Lighting.Lighting(dimension, repository);
            ILighter lighter = new OverWorldLighter(dimension, null!);
            int xHole = 5;
            int zHole = 5;
            int yGround = chunk.GetHeight(xHole, zHole);

            dimension.SetBlockID(new GlobalVoxelCoordinates(xHole, yGround, zHole), 0);         // Create area that looks like so:
            dimension.SetBlockID(new GlobalVoxelCoordinates(xHole, yGround - 1, zHole), 0);     // x x  Light goes like so: |
            dimension.SetBlockID(new GlobalVoxelCoordinates(xHole, yGround - 2, zHole), 0);     // x x                      |
            dimension.SetBlockID(new GlobalVoxelCoordinates(xHole - 1, yGround - 2, zHole), 0); //   x                     -/

            lighter.DoLightingOperation(new LightingOperation(GlobalVoxelCoordinates.Zero, LightingOperationMode.Add, LightingOperationKind.Initial, 15));
            lighter.DoLightingOperation(new LightingOperation(new GlobalVoxelCoordinates(xHole, yGround - 2, zHole), LightingOperationMode.Add, LightingOperationKind.Sky, 15));

            Console.WriteLine("Testing {0}", new GlobalVoxelCoordinates(xHole, yGround, zHole));
            Assert.AreEqual(15, dimension.GetSkyLight(new GlobalVoxelCoordinates(xHole, yGround, zHole)));
            Console.WriteLine("Testing {0}", new GlobalVoxelCoordinates(xHole, yGround - 1, zHole));
            Assert.AreEqual(15, dimension.GetSkyLight(new GlobalVoxelCoordinates(xHole, yGround - 1, zHole)));
            Console.WriteLine("Testing {0}", new GlobalVoxelCoordinates(xHole, yGround - 2, zHole));
            Assert.AreEqual(15, dimension.GetSkyLight(new GlobalVoxelCoordinates(5, yGround - 2, zHole)));
            Console.WriteLine("Testing {0}", new GlobalVoxelCoordinates(xHole - 1, yGround - 2, zHole));
            Assert.AreEqual(14, dimension.GetSkyLight(new GlobalVoxelCoordinates(xHole - 1, yGround - 2, zHole)));
        }

        [Test]
        public void TestFarPropegation()
        {
            IDimension dimension = BuildDimension();
            IChunk chunk = dimension.GetChunk(GlobalChunkCoordinates.Zero)!;
            int xHole = 5;
            int zHole = 5;
            int yGround = chunk.GetHeight(xHole, zHole);
            ILighter lighter = new OverWorldLighter(dimension, null!);

            //lighter.InitialLighting(dimension.GetChunk(GlobalChunkCoordinates.Zero));

            dimension.SetBlockID(new GlobalVoxelCoordinates(xHole, yGround, zHole), 0);         // Create area that looks like so:
            dimension.SetBlockID(new GlobalVoxelCoordinates(xHole, yGround - 1, zHole), 0);     // x x  Light goes like so: |
            dimension.SetBlockID(new GlobalVoxelCoordinates(xHole, yGround - 2, zHole), 0);     // x x                      |
            dimension.SetBlockID(new GlobalVoxelCoordinates(xHole - 1, yGround - 2, zHole), 0); //   x                     -/

            for (int x = 0; x < 4; x++)
            {
                dimension.SetBlockID(new GlobalVoxelCoordinates(x, yGround - 2, zHole), 0); // Dig a tunnel
                // xxxxx x ish
                // x     x
                // xxxxxxx
            }

            lighter.DoLightingOperation(new LightingOperation(GlobalVoxelCoordinates.Zero, LightingOperationMode.Add, LightingOperationKind.Initial, 15));
            lighter.DoLightingOperation(new LightingOperation(new GlobalVoxelCoordinates(xHole, yGround - 2, zHole),
                LightingOperationMode.Add, LightingOperationKind.Sky, 15));

            //
            // output lighting for command-line testing
            //
            Console.WriteLine("Block IDS:");
            Console.WriteLine("y");
            for (int y = SurfaceHeight; y >= 1; y--)
            {
                Console.Write($"{y} ");
                for (int x = 0; x <= xHole; x++)
                    Console.Write(dimension.GetBlockID(new GlobalVoxelCoordinates(x, y, zHole)).ToString("D2") + " ");
                Console.WriteLine();
            }
            Console.Write("x:");
            for (int x = 0; x <= xHole; x++)
                Console.Write($"{x:D2} ");
            Console.WriteLine();

            Console.WriteLine("Sky Light levels:");
            Console.WriteLine("y");
            for (int y = SurfaceHeight; y >= 1; y--)
            {
                Console.Write($"{y} ");
                for (int x = 0; x <= xHole; x++)
                    Console.Write(dimension.GetSkyLight(new GlobalVoxelCoordinates(x, y, zHole)).ToString("D2") + " ");
                Console.WriteLine();
            }

            //
            // Assertions:
            //
            Console.WriteLine("Testing {0}", new GlobalVoxelCoordinates(xHole, yGround, zHole));
            Assert.AreEqual(15, dimension.GetSkyLight(new GlobalVoxelCoordinates(xHole, yGround, zHole)));
            Console.WriteLine("Testing {0}", new GlobalVoxelCoordinates(xHole, yGround - 1, zHole));
            Assert.AreEqual(15, dimension.GetSkyLight(new GlobalVoxelCoordinates(xHole, yGround - 1, zHole)));

            byte expected = 15;
            for (int x = xHole; x >= 0; x--)
            {
                GlobalVoxelCoordinates coords = new GlobalVoxelCoordinates(x, yGround - 2, zHole);
                Console.WriteLine("Testing {0}", coords);
                Assert.AreEqual(expected, dimension.GetSkyLight(coords),
                    "At {0}; expected {1}, but found {2}",
                    coords, expected, dimension.GetSkyLight(coords));
                expected--;
            }
        }

        [Test]
        public void TestFarPropegationx2()
        {
            IDimension dimension = BuildDimension();
            IChunk chunk = dimension.GetChunk(GlobalChunkCoordinates.Zero)!;
            ILighter lighter = new OverWorldLighter(dimension, null!);
            int xHole = 5;
            int zHole = 5;
            int groundY = chunk.GetHeight(xHole, zHole);
            int tunnelLength = 4;

            // Test this layout:
            // xxx x    y=3
            // x   x    y=2
            // x   x    y=1
            // xxxxx    y=0
            //
            //    ^ x,z = 5

            // Dig a hole 3 blocks deep at xHole,zHole.
            for (int y = groundY - 2; y <= groundY; y++)
                dimension.SetBlockID(new GlobalVoxelCoordinates(xHole, y, zHole), MockAirBlockID);

            // Dig a 1-wide, 2-high tunnel sideways for 4 blocks.
            for (int x = 1; x <= tunnelLength; x++) // Dig outwards
            {
                dimension.SetBlockID(new GlobalVoxelCoordinates(x + xHole, groundY - 1, zHole), MockAirBlockID);
                dimension.SetBlockID(new GlobalVoxelCoordinates(x + xHole, groundY - 2, zHole), MockAirBlockID);
            }

            var watch = new Stopwatch();
            watch.Start();

            lighter.DoLightingOperation(new LightingOperation(GlobalVoxelCoordinates.Zero, LightingOperationMode.Add, LightingOperationKind.Initial, 15));

            lighter.DoLightingOperation(new LightingOperation(new GlobalVoxelCoordinates(xHole, groundY - 3, zHole), LightingOperationMode.Add, LightingOperationKind.Sky, 15));

            watch.Stop();

            // Output lighting
            Console.WriteLine("Block IDS:");
            Console.WriteLine("y");
            for (int y = groundY; y >= 0; y--)
            {
                Console.Write($"{y} ");
                for (int x = 0; x <= 5; x++)
                {
                    Console.Write(dimension.GetBlockID(new GlobalVoxelCoordinates(x + xHole, y, zHole)).ToString("D2") + " ");
                }
                Console.WriteLine();
            }
            Console.Write("x:");
            for (int x = 0; x <= tunnelLength + 1; x++)
                Console.Write($"{x + xHole:D2} ");
            Console.WriteLine();

            Console.WriteLine("Sky Light levels:");
            Console.WriteLine("y");
            for (int y = groundY; y >= 0; y--)
            {
                Console.Write($"{y} ");
                for (int x = 0; x <= tunnelLength + 1; x++)
                {
                    Console.Write(dimension.GetSkyLight(new GlobalVoxelCoordinates(x + xHole, y, zHole)).ToString("D2") + " ");
                }
                Console.WriteLine();
            }

            Console.WriteLine("Testing {0}", new GlobalVoxelCoordinates(xHole, groundY, zHole));
            Assert.AreEqual(15, dimension.GetSkyLight(new GlobalVoxelCoordinates(xHole, groundY, zHole)));
            Assert.AreEqual(15, dimension.GetSkyLight(new GlobalVoxelCoordinates(xHole, groundY - 1, zHole)));
            Assert.AreEqual(15, dimension.GetSkyLight(new GlobalVoxelCoordinates(xHole, groundY - 2, zHole)));

            byte expected = 14;
            for (int x = 1; x <= tunnelLength; x++)
            {
                GlobalVoxelCoordinates coords = new GlobalVoxelCoordinates(x + xHole, groundY - 1, zHole);
                Console.WriteLine("Testing {0}", coords);
                Assert.AreEqual(expected, dimension.GetSkyLight(coords),
                    "At {0}; expected {1}, but found {2}",
                    coords, expected, dimension.GetSkyLight(coords));

                coords = new GlobalVoxelCoordinates(x + xHole, groundY - 2, zHole);
                Console.WriteLine("Testing {0}", coords);
                Assert.AreEqual(expected, dimension.GetSkyLight(coords),
                    "At {0}; expected {1}, but found {2}",
                    coords, expected, dimension.GetSkyLight(coords));

                expected--;
            }

            Console.WriteLine("{0}ms", watch.ElapsedMilliseconds);
        }

        /// <summary>
        /// Tests initial lighting with Sky Light 
        /// </summary>
        [Test]
        public void TestLeavesAndEtc()
        {
            IDimension dimension = BuildDimension();
            IChunk chunk = dimension.GetChunk(GlobalChunkCoordinates.Zero)!;
            ILighter lighter = new OverWorldLighter(dimension, null!);
            int x = 5;
            int z = 5;
            int ground = chunk.GetHeight(x, z);

            for (int y = ground + 1; y <= ground + 16; y++)
            {
                GlobalVoxelCoordinates coords = new GlobalVoxelCoordinates(x, y, z);
                dimension.SetBlockID(coords, 0);
                dimension.SetBlockID(coords + Vector3i.East, (byte)BlockIDs.Dirt);
                dimension.SetBlockID(coords + Vector3i.West, (byte)BlockIDs.Dirt);
                dimension.SetBlockID(coords + Vector3i.North, (byte)BlockIDs.Dirt);
                dimension.SetBlockID(coords + Vector3i.South, (byte)BlockIDs.Dirt);
            }

            // Test this layout:
            // xox      o == leaves
            // x x      x == dirt
            // xox
            // x x
            // xox ...

            for (int y = ground + 1; y <= ground + 16; y++)
                if (y % 2 == 1)
                    dimension.SetBlockID(new GlobalVoxelCoordinates(x, y, z), (byte)BlockIDs.Leaves);

            lighter.DoLightingOperation(new LightingOperation(GlobalVoxelCoordinates.Zero, LightingOperationMode.Add, LightingOperationKind.Initial, 15));

            // Output lighting
            for (int y = ground + 16; y >= ground; y--)
            {
                Console.Write(dimension.GetBlockID(new GlobalVoxelCoordinates(x, y, z)).ToString("D2"));
                Console.Write(" " + dimension.GetSkyLight(new GlobalVoxelCoordinates(x, y, z)).ToString("D2"));
                Console.WriteLine("   Y={0}", y);
            }                

            var expected = new byte[]
            {
                15, // air
                13, // leaves
                13, // air
                11, // leaves
                11, // air
                9,  // leaves
                9,  // air
                7,  // leaves
                7,  // air
                5,  // leaves
                5,  // air
                3,  // leaves
                3,  // air
                1,  // leaves
                1,  // air
                0,  // leaves
                0,  // dirt
            };

            for (int y = ground + 16, i = 0; y >= ground; y--, i++)
            {
                byte ex;
                if (i < expected.Length)
                    ex = expected[i];
                else
                    ex = 0;
                Assert.AreEqual(ex, dimension.GetSkyLight(new GlobalVoxelCoordinates(x, y, z)));
            }
        }

        /// <summary>
        /// Testing propagation of reduced skylight into caves.
        /// </summary>
        [Test]
        public void TestLeavesAndCaves()
        {
            IDimension dimension = BuildDimension(20);
            IChunk chunk = dimension.GetChunk(GlobalChunkCoordinates.Zero)!;
            ILighter lighter = new OverWorldLighter(dimension, null!);
            int xHole = 5;
            int zHole = 5;
            int groundY = chunk.GetHeight(xHole, zHole);

            // drill a hole into the ground with leaves on even levels
            for (int y = 0; y < 16; y ++)
            {
                if ((y % 2) == 1)
                    dimension.SetBlockID(new GlobalVoxelCoordinates(xHole, groundY - y, zHole), MockLeavesBlockID);
                else
                    for (int j = 0; j < 3; j ++)
                        dimension.SetBlockID(new GlobalVoxelCoordinates(xHole + j, groundY - y, zHole), MockAirBlockID);
            }

            //
            // Action
            //

            // Initial lighting.
            lighter.DoLightingOperation(new LightingOperation(GlobalVoxelCoordinates.Zero, LightingOperationMode.Add, LightingOperationKind.Initial, 15));
            // debugging
            for (int y = groundY; y >= 0; y--)
                Console.WriteLine($"{y,2}: {dimension.GetSkyLight(new GlobalVoxelCoordinates(xHole, y, zHole))}");
            // end debugging

            // Final lighting of vertical shaft
            for (int y = 0; y < 1; y ++) // TODO upper bound should be 16
            {
                GlobalVoxelCoordinates coords = new GlobalVoxelCoordinates(xHole, groundY - y, zHole);
                byte lightLevel = dimension.GetSkyLight(coords);
                lighter.DoLightingOperation(new LightingOperation(coords,
                    LightingOperationMode.Add, LightingOperationKind.Sky, lightLevel));
            }

            // Output lighting
            Console.WriteLine("Block IDS:");
            Console.WriteLine("y");
            for (int y = groundY; y >= 0; y--)
            {
                Console.Write($"{y:D2} ");
                for (int x = 0; x <= 5; x++)
                    Console.Write(dimension.GetBlockID(new GlobalVoxelCoordinates(x + xHole, y, zHole)).ToString("D2") + " ");
                Console.WriteLine();
            }
            Console.Write("x:");
            for (int x = 0; x <= 5; x++)
                Console.Write($" {x + xHole:D2}");
            Console.WriteLine();

            Console.WriteLine("Sky Light levels:");
            Console.WriteLine("y");
            for (int y = groundY; y >= 0; y--)
            {
                Console.Write($"{y:D2} ");
                for (int x = 0; x <= 5; x++)
                    Console.Write(dimension.GetSkyLight(new GlobalVoxelCoordinates(x + xHole, y, zHole)).ToString("D2") + " ");
                Console.WriteLine();
            }

            byte[] expected = new byte[]
            {
                15, // air
                13, // leaves
                13, // air
                11, // leaves
                11, // air
                9,  // leaves
                9,  // air
                7,  // leaves
                7,  // air
                5,  // leaves
                5,  // air
                3,  // leaves
                3,  // air
                1,  // leaves
                1,  // air
                0,  // leaves
                0,  // dirt
            };

            for (int y = groundY, i = 0; y >= 0; y--, i++)
            {
                byte ex;
                if (i < expected.Length)
                    ex = expected[i];
                else
                    ex = 0;
                Assert.AreEqual(ex, dimension.GetSkyLight(new GlobalVoxelCoordinates(xHole, y, zHole)));
            }
        }
    }
}

