using System;
using System.Collections.Generic;
using System.ComponentModel;
using Moq;
using NUnit.Framework;
using TrueCraft.Core.Entities;
using TrueCraft.Core.Lighting;
using TrueCraft.Core.Logic;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.Networking;
using TrueCraft.Core.Physics;
using TrueCraft.Core.Server;
using TrueCraft.Core.World;
using TrueCraft.TerrainGen;
using TrueCraft.Test.World;
using TrueCraft.World;
using static TrueCraft.Core.Logic.Blocks.StairsBlock;

namespace TrueCraft.Core.Test.Physics
{
    [TestFixture]
    public class PhysicsEngineTest
    {
        /// <summary>
        /// A constant to check if two doubles are "close enough" to equal
        /// </summary>
        /// <remarks>This is much larger than double.Epsilon.</remarks>
        private const double Epsilon = 1e-8;

        private const byte StoneBlockID = 1;

        private const int SurfaceHeight = 1;

        private readonly IBlockRepository _blockRepository;
        private readonly IItemRepository _itemRepository;
        private readonly IEntityManager _entityManager;

        public PhysicsEngineTest()
        {
            Mock<IBlockProvider> mockStoneBlock = new Mock<IBlockProvider>(MockBehavior.Strict);
            mockStoneBlock.Setup(x => x.ID).Returns(StoneBlockID);
            mockStoneBlock.Setup(x => x.GetCollisionBox(0)).Returns(new BoundingBox(Vector3.Zero, Vector3.One));

            Mock<IBlockRepository> mockBlockRepository = new Mock<IBlockRepository>(MockBehavior.Strict);
            mockBlockRepository.Setup(x => x.GetBlockProvider(It.Is<byte>(b => b == StoneBlockID))).Returns(mockStoneBlock.Object);
            _blockRepository = mockBlockRepository.Object;

            Mock<IItemRepository> mockItemRepository = new Mock<IItemRepository>(MockBehavior.Strict);
            _itemRepository = mockItemRepository.Object;

            Mock<IEntityManager> mockEntityManager = new Mock<IEntityManager>(MockBehavior.Strict);
            _entityManager = mockEntityManager.Object;
        }

        private IDimension BuildDimension()
        {
            IDimensionServer rv = new FakeDimension(_blockRepository,
                _itemRepository, _entityManager);

            // Generate a chunk
            IChunk? chunk = rv.GetChunk(new GlobalChunkCoordinates(0, 0), LoadEffort.Generate);
            for (int x = 0; x < Chunk.Width; x++)
                for (int z = 0; z < Chunk.Depth; z++)
                    chunk?.SetBlockID(new LocalVoxelCoordinates(x, 0, z), StoneBlockID);

            return rv;
        }

        private class TestEntity : IEntity
        {

            public event PropertyChangedEventHandler? PropertyChanged;

            public TestEntity()
            {
                TerminalVelocity = 10;
                Size = new Size(1);
                CollisionOccured = false;
            }

            public bool BeginUpdate()
            {
                return true;
            }

            /// <inheritdoc />
            public void EndUpdate(Vector3 newPosition, Vector3 newVelocity)
            {
                Position = newPosition;
                Velocity = newVelocity;
            }

            public Vector3 Position { get; set; }
            public Vector3 Velocity { get; set; }
            public float AccelerationDueToGravity { get; set; }
            public float Drag { get; set; }
            public float TerminalVelocity { get; set; }
            public Vector3 CollisionPoint { get; set; }
            public bool CollisionOccured { get; set; }

            public void TerrainCollision(Vector3 collisionPoint, Vector3 collisionDirection)
            {
                CollisionPoint = collisionPoint;
                CollisionOccured = true;
            }

            public BoundingBox BoundingBox
            {
                get
                {
                    double hw = Size.Width * 0.5;
                    double hd = Size.Depth * 0.5;
                    Vector3 min = new Vector3(Position.X - hw, Position.Y, Position.Z - hd);
                    Vector3 max = new Vector3(Position.X + hw, Position.Y + Size.Height, Position.Z + hd);
                    return new BoundingBox(min, max);
                }
            }

            public Size Size { get; set; }

            public void Update(IEntityManager entityManager)
            {
                throw new NotImplementedException();
            }

            public IPacket SpawnPacket => throw new NotImplementedException();

            public int EntityID { get => 1; set => throw new NotImplementedException(); }
            public float Yaw { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public float Pitch { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public bool Despawned { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public DateTime SpawnTime { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public MetadataDictionary Metadata => throw new NotImplementedException();

            public IEntityManager EntityManager => throw new NotImplementedException();

            public IDimension Dimension => throw new NotImplementedException();

            public bool SendMetadataToClients => throw new NotImplementedException();
        }

        [Test]
        public void TestGravity()
        {
            IDimension dimension = BuildDimension();
            IPhysicsEngine physics = new PhysicsEngine(dimension);
            TestEntity entity = new TestEntity();
            entity.Position = new Vector3(0, 100, 0);
            entity.Velocity = Vector3.Zero;
            entity.AccelerationDueToGravity = 1;
            entity.Drag = 0;
            physics.AddEntity(entity);

            // Test
            physics.Update(TimeSpan.FromSeconds(1));

            Assert.AreEqual(0, entity.Position.X);
            Assert.AreEqual(99, entity.Position.Y);
            Assert.AreEqual(0, entity.Position.Z);
            Assert.AreEqual(0, entity.Velocity.X);
            Assert.AreEqual(-1, entity.Velocity.Y);
            Assert.AreEqual(0, entity.Velocity.Z);

            physics.Update(TimeSpan.FromSeconds(1));

            Assert.AreEqual(0, entity.Position.X);
            Assert.AreEqual(97, entity.Position.Y);
            Assert.AreEqual(0, entity.Position.Z);
            Assert.AreEqual(0, entity.Velocity.X);
            Assert.AreEqual(-2, entity.Velocity.Y);
            Assert.AreEqual(0, entity.Velocity.Z);
        }

        [Test]
        public void TestDrag()
        {
            IDimension dimension = BuildDimension();
            IPhysicsEngine physics = new PhysicsEngine(dimension);
            TestEntity entity = new TestEntity();
            entity.Position = new Vector3(0, 100, 0);
            entity.AccelerationDueToGravity = 0;
            entity.Drag = 0.5f;
            entity.Velocity = Vector3.Down * 2;
            physics.AddEntity(entity);

            // Test
            physics.Update(TimeSpan.FromSeconds(1));

            Assert.AreEqual(0, entity.Position.X);
            Assert.AreEqual(99, entity.Position.Y);
            Assert.AreEqual(0, entity.Position.Z);
            Assert.AreEqual(0, entity.Velocity.X);
            Assert.AreEqual(-1, entity.Velocity.Y);
            Assert.AreEqual(0, entity.Velocity.Z);
        }

        // Tests that the Terrain supports the Entity
        [Test]
        public void TestTerrainCollision()
        {
            IDimension dimension = BuildDimension();
            IPhysicsEngine physics = new PhysicsEngine(dimension);
            TestEntity entity = new TestEntity();
            entity.Size = new Size(0.6, 1.8, 0.6);
            double xPos = 10.9, zPos = 10.9;
            double yPos = SurfaceHeight;
            entity.Position = new Vector3(xPos, yPos, zPos);
            entity.Velocity = Vector3.Zero;
            entity.AccelerationDueToGravity = 1;
            physics.AddEntity(entity);

            // Test
            physics.Update(TimeSpan.FromSeconds(1));

            Assert.AreEqual(xPos, entity.Position.X);
            Assert.AreEqual(yPos, entity.Position.Y);
            Assert.AreEqual(zPos, entity.Position.Z);
            Assert.True(Math.Abs(entity.Velocity.X) < Epsilon);
            Assert.True(Math.Abs(entity.Velocity.Y) < Epsilon);
            Assert.True(Math.Abs(entity.Velocity.Z) < Epsilon);

            physics.Update(TimeSpan.FromSeconds(5));

            Assert.AreEqual(xPos, entity.Position.X);
            Assert.AreEqual(yPos, entity.Position.Y);
            Assert.AreEqual(zPos, entity.Position.Z);
            Assert.True(Math.Abs(entity.Velocity.X) < Epsilon);
            Assert.True(Math.Abs(entity.Velocity.Y) < Epsilon);
            Assert.True(Math.Abs(entity.Velocity.Z) < Epsilon);
        }

        // If the player's feet are somehow inside a block, the player
        // is still supported.
        [Test]
        public void TestInTerrainSupport()
        {
            IDimension dimension = BuildDimension();
            IPhysicsEngine physics = new PhysicsEngine(dimension);
            double feetHeight = 11.9;
            for (int x = 0; x < WorldConstants.ChunkWidth; x ++)
                for (int z = 0; z < WorldConstants.ChunkDepth; z ++)
                {
                    GlobalVoxelCoordinates coords = new(x, (int)Math.Floor(feetHeight), z);
                    dimension.SetBlockID(coords, StoneBlockID);
                }

            TestEntity entity = new TestEntity();
            entity.Size = new Size(0.6, 1.8, 0.6);
            double xPos = 10.9, zPos = 10.9;
            double yPos = feetHeight;
            entity.Position = new Vector3(xPos, yPos, zPos);
            entity.Velocity = Vector3.Zero;
            entity.AccelerationDueToGravity = 1;
            physics.AddEntity(entity);

            // Test
            physics.Update(TimeSpan.FromSeconds(1));

            Assert.AreEqual(xPos, entity.Position.X);
            Assert.AreEqual(yPos, entity.Position.Y);
            Assert.AreEqual(zPos, entity.Position.Z);
            Assert.True(Math.Abs(entity.Velocity.X) < Epsilon);
            Assert.True(Math.Abs(entity.Velocity.Y) < Epsilon);
            Assert.True(Math.Abs(entity.Velocity.Z) < Epsilon);
        }

        [Test]
        public void TestExtremeTerrainCollision()
        {
            IDimension dimension = BuildDimension();
            IPhysicsEngine physics = new PhysicsEngine(dimension);
            TestEntity entity = new TestEntity();
            double origHeightAboveSurface = 5;
            entity.Position = new Vector3(0, SurfaceHeight + origHeightAboveSurface, 0);
            entity.Velocity = Vector3.Zero;
            entity.AccelerationDueToGravity = 10;
            physics.AddEntity(entity);

            // Test
            physics.Update(TimeSpan.FromSeconds(1));

            Assert.AreEqual(0, entity.Position.X);
            Assert.AreEqual(SurfaceHeight, entity.Position.Y);
            Assert.AreEqual(0, entity.Position.Z);

            // The entity's velocity is the velocity required to go from its
            // previous position to its current position.  Not the original velocity,
            // but not yet zero either.
            Assert.AreEqual(new Vector3(0, -origHeightAboveSurface, 0), entity.Velocity);
        }

        [Test]
        public void TestAdjacentFall()
        {
            // Tests an entity that falls alongside a wall
            IDimension dimension = BuildDimension();
            IPhysicsEngine physics = new PhysicsEngine(dimension);
            TestEntity entity = new TestEntity();
            entity.Position = new Vector3(0, 10, 0);
            entity.AccelerationDueToGravity = 1;
            physics.AddEntity(entity);

            // Create a pillar
            for (int y = 0; y < 12; y++)
                dimension.SetBlockID(new GlobalVoxelCoordinates(1, y, 0), StoneBlockID);

            // Test
            physics.Update(TimeSpan.FromSeconds(1));

            Assert.AreEqual(0, entity.Position.X);
            Assert.AreEqual(9, entity.Position.Y);
            Assert.AreEqual(0, entity.Position.Z);
            Assert.IsFalse(entity.CollisionOccured);
        }

        [Test]
        public void TestCornerFall()
        {
            // Tests that an entity that falls when in a corder
            IDimension dimension = BuildDimension();
            IPhysicsEngine physics = new PhysicsEngine(dimension);
            TestEntity entity = new TestEntity();
            entity.AccelerationDueToGravity = 1;
            physics.AddEntity(entity);

            GlobalVoxelCoordinates bc1 = new GlobalVoxelCoordinates(5, SurfaceHeight, 5);
            GlobalVoxelCoordinates bc2 = new GlobalVoxelCoordinates(6, SurfaceHeight, 6);

            dimension.SetBlockID(bc1, StoneBlockID);
            dimension.SetBlockID(bc2, StoneBlockID);

            Vector3 positionBefore = new Vector3(bc1.X + 1 + entity.Size.Width * 0.5, SurfaceHeight + 0.9, bc2.Z - entity.Size.Depth * 0.5);
            entity.Position = positionBefore;

            // Test
            physics.Update(TimeSpan.FromSeconds(1));

            //
            // Assert that the entity has moved vertically.
            //
            Assert.AreEqual(positionBefore.X, entity.Position.X);
            Assert.AreEqual(positionBefore.Z, entity.Position.Z);
            Assert.True(entity.Position.Y < positionBefore.Y);
        }

        [Test]
        public void TestCollisionPoint()
        {
            IDimension dimension = BuildDimension();
            IPhysicsEngine physics = new PhysicsEngine(dimension);
            TestEntity entity = new TestEntity();
            int yBlock = 4;
            entity.Position = new Vector3(0, yBlock + 1.5, 0);
            entity.AccelerationDueToGravity = 1;
            entity.Drag = 0;
            physics.AddEntity(entity);

            dimension.SetBlockID(new GlobalVoxelCoordinates(0, yBlock, 0), StoneBlockID);

            // Test
            physics.Update(TimeSpan.FromSeconds(1));

            Assert.True(entity.CollisionOccured);
            Assert.AreEqual(new Vector3(0, yBlock, 0), entity.CollisionPoint);
        }

        [Test]
        public void TestHorizontalCollision()
        {
            IDimension dimension = BuildDimension();
            IPhysicsEngine physics = new PhysicsEngine(dimension);
            TestEntity entity = new TestEntity();
            entity.Position = new Vector3(0, 5, 0);
            entity.AccelerationDueToGravity = 0;
            entity.Drag = 0;
            entity.Velocity = new Vector3(1.5, 0, 0);
            physics.AddEntity(entity);
            dimension.SetBlockID(new GlobalVoxelCoordinates(1, 5, 0), StoneBlockID);

            // Test
            physics.Update(TimeSpan.FromSeconds(1));

            //
            // Assertions
            //

            // The centre of the entity should be half the entity's width from
            // the near edge of the block at x==1.
            Assert.AreEqual(1 - entity.Size.Width / 2, entity.Position.X);

            // The Entity's y and z positions should remain unchanged
            Assert.AreEqual(5, entity.Position.Y);
            Assert.AreEqual(0, entity.Position.Z);

            // The Entity's x velocity should be that which was required to move
            // from it's centre at x == 0 to being in contact with the block at x == 1.
            Assert.AreEqual(1 - entity.Size.Width / 2, entity.Velocity.X);

            // The Entity's y and z velocity components should remain unchanged.
            Assert.AreEqual(0, entity.Velocity.Y);
            Assert.AreEqual(0, entity.Velocity.Z);
        }

        /// <summary>
        /// Tests a very odd condition involving a surprise launch into the air.
        /// </summary>
        /// <remarks>
        /// <para>
        /// In my test world, I found a particular block that when I walk into the
        /// side of it, I get launched up into the air.  This test reproduces
        /// that with actual numbers and locations taken from the actual
        /// observed bug.
        /// </para>
        /// </remarks>
        [Test]
        public void CrazyJump()
        {
            //
            // Set up
            //
            IDimensionServer dimension = (IDimensionServer)BuildDimension();
            IPhysicsEngine physics = new PhysicsEngine(dimension);

            // Generate chunks at chunk coordinates (-1, 12) and (-1, 13)
            IChunk chunk12 = dimension.GetChunk(new GlobalChunkCoordinates(-1, 12), LoadEffort.Generate)!;
            IChunk chunk13 = dimension.GetChunk(new GlobalChunkCoordinates(-1, 13), LoadEffort.Generate)!;

            // Create a layer of dirt at y == 62.
            // This will place the entity's feet at y == 63.
            for (int x = 0; x < 16; x ++)
                for (int z = 0; z < 16; z ++)
                {
                    LocalVoxelCoordinates localCoords = new(x, 62, z);
                    chunk12.SetBlockID(localCoords, StoneBlockID);
                    chunk13.SetBlockID(localCoords, StoneBlockID);
                }

            // Place blocks at z == 192, x == -5 && x == -3
            dimension.SetBlockID(new GlobalVoxelCoordinates(-4, 63, 192), StoneBlockID);
            dimension.SetBlockID(new GlobalVoxelCoordinates(-6, 63, 192), StoneBlockID);

            Size entitySize = new Size(0.6, 1.8, 0.6);   // same size as Player Entity
            double xPos = -4.34298322997483;
            double yPos = 63;
            double zPos = 192.92777590726;
            Vector3 entityStartPos = new Vector3(xPos, yPos, zPos);
            double xVel = 0.0502763610985;
            double yVel = 0;
            double zVel = -0.0520632516009488;
            Vector3 entityStartVel = new Vector3(xVel, yVel,  zVel);

            TestEntity entity = new TestEntity();
            entity.Position = entityStartPos;
            entity.Velocity = entityStartVel;
            entity.Size = entitySize;
            entity.AccelerationDueToGravity = 1.6f;
            entity.Drag = 0.0f;  // In the scenario being modelled, the W was held down, so Drag could not slow the Player.
            entity.TerminalVelocity = 78.4f;
            physics.AddEntity(entity);

            //
            // Act
            //
            Console.Error.WriteLine($"Before: {entity.Position}");
            physics.Update(TimeSpan.FromSeconds(1));
            Console.Error.WriteLine($"After: {entity.Position}");

            //
            // Assertions
            //
            // Collision occurred
            Assert.True(entity.CollisionOccured);
            // Collided with the block at -4, 63, 192
            Assert.AreEqual(new Vector3(-4, 63, 192), entity.CollisionPoint);

            // x Position should put the entity in contact with the block
            Assert.AreEqual(-4 - entity.Size.Width / 2, entity.Position.X);
            // y position should be unchanged.
            Assert.AreEqual(yPos, entity.Position.Y);
            // z position should be advanced by one time unit's velocity
            Assert.AreEqual(zPos + zVel, entity.Position.Z);

            // x-velocity should be what was required to make contact in one unit
            // of time
            Assert.AreEqual(-4 - xPos - entity.Size.Width / 2, entity.Velocity.X);
            // y-velocity should be unchanged.
            Assert.AreEqual(yVel, entity.Velocity.Y);
            // z-velocity should be unchanged.
            Assert.AreEqual(zVel, entity.Velocity.Z);

            // A collision should have been recordd.
            Assert.True(entity.CollisionOccured);

            //
            // Act again.
            //
            physics.Update(TimeSpan.FromSeconds(1));

            //
            // More assertions
            //
            // x position should still be in contact with the block
            Assert.AreEqual(-4 - entity.Size.Width / 2, entity.Position.X);
            // y position should remain unchanged.
            Assert.AreEqual(yPos, entity.Position.Y);
            // z Position should have advanced by 2 units of time
            Assert.True(Math.Abs(zPos + 2 * zVel - entity.Position.Z) < GameConstants.Epsilon,
                "Expected: {0};  Actual: {0}", zPos + 2 * zVel, entity.Position.Z);

            // X velocity should now be zero
            Assert.True(Math.Abs(entity.Velocity.X) < GameConstants.Epsilon, "Expected 0; Actual: {0}", entity.Velocity.X  );
            // Y velocity should remain unchanged
            Assert.AreEqual(yVel, entity.Velocity.Y);
            // Z velocity should remain unchanged.
            Assert.AreEqual(zVel, entity.Velocity.Z);
        }

        // Testing that when PhysicsEngine.Update is called with
        // different amounts of time, different distances are travelled.
        [TestCase(0.05)]
        [TestCase(0.10)]
        [TestCase(1.00)]
        public void TestVelocity(double seconds)
        {
            double xPos = 8, yPos = 64, zPos = 8;
            double xVel = 1, yVel = 2, zVel = 3;

            TestEntity entity = new();
            entity.Position = new Vector3(xPos, yPos, zPos);
            entity.Velocity = new Vector3(xVel, yVel, zVel);
            entity.AccelerationDueToGravity = 0;
            entity.Drag = 0;

            IDimension dimension = BuildDimension();
            IPhysicsEngine physics = new PhysicsEngine(dimension);

            physics.AddEntity(entity);

            //
            // Act
            //
            physics.Update(TimeSpan.FromSeconds(seconds));

            //
            // Assertions
            //
            // Velocity should be unchanged.
            Assert.True(Math.Abs(xVel - entity.Velocity.X) < GameConstants.Epsilon);
            Assert.True(Math.Abs(yVel - entity.Velocity.Y) < GameConstants.Epsilon);
            Assert.True(Math.Abs(zVel - entity.Velocity.Z) < GameConstants.Epsilon);
            // Position should be updated per units of time given
            Vector3 expectedPosition = new Vector3(xPos + seconds * xVel,
                yPos + seconds * yVel, zPos + seconds * zVel);
            Assert.AreEqual(expectedPosition, entity.Position);
        }

        public static IEnumerable<object[]> WalkIntoCornerTestData()
        {
            Vector3 expectedPos, expectedDir;
            Vector3 startPos, startDir;

            // In this scenario, the player is trying to move diagonally, is
            // sliding along the block at (-7,63,200), and is about to contact
            // the block at (-8,63,201).  The player should be stopped at the
            // corner.
            // Recorded values:
            // CollisionBlock: < -7,63,200 >; Face: NegativeZ; Position: < -7.3,63,200.67823697928787 >
            // Before: direction: < 0.04405987998940461,0,0.05741969386000391 >
            // After: direction: < 0.04405987998940461,0,-0.978236979287874 >
            startPos = new Vector3(-7.3, 63, 200.67823697928787);
            startDir = new Vector3(0.04405987998940461, 0, 0.05741969386000391);
            expectedPos = new Vector3(startPos.X, startPos.Y, 200.7);
            expectedDir = new Vector3(0, 0, expectedPos.Z - startPos.Z);

            yield return new object[] { expectedPos, expectedDir, startPos, startDir };
        }

        // Testing where the movement vector intersects more than one block in a
        // single Tick.
        [TestCaseSource(nameof(WalkIntoCornerTestData))]
        public void WalkIntoCorner(Vector3 expectedPosition, Vector3 expectedDirection,
            Vector3 startPosition, Vector3 startDirection)
        {
            //
            // Set up
            //
            IDimensionServer dimension = (IDimensionServer)BuildDimension();
            IPhysicsEngine physics = new PhysicsEngine(dimension);

            // Generate chunks at chunk coordinates (-1, 12) and (-1, 13)
            IChunk chunk12 = dimension.GetChunk(new GlobalChunkCoordinates(-1, 12), LoadEffort.Generate)!;

            // Create a layer of stone at y == 62.
            // This will place the entity's feet at y == 63.
            for (int x = 0; x < 16; x++)
                for (int z = 0; z < 16; z++)
                {
                    LocalVoxelCoordinates localCoords = new(x, 62, z);
                    chunk12.SetBlockID(localCoords, StoneBlockID);
                }

            // Create the corner that we will walk into
            for (int z = 201 % 16; z <= 207 % 16; z ++)
                for (int x = 0; x < 16; x ++)
                {
                    LocalVoxelCoordinates localCoords = new(x, 63, z);
                    chunk12.SetBlockID(localCoords, StoneBlockID);
                }
            for(int x = 16-7; x <= 16-1; x ++)
            {
                LocalVoxelCoordinates localCoords = new(x, 63, 200 % 16);
                chunk12.SetBlockID(localCoords, StoneBlockID);
            }

            // Create the Player Entity
            Size entitySize = new Size(0.6, 1.8, 0.6);   // same size as Player Entity
            TestEntity entity = new TestEntity();
            entity.Position = startPosition;
            entity.Velocity = startDirection;
            entity.Size = entitySize;
            entity.AccelerationDueToGravity = (float)GameConstants.AccelerationDueToGravity;
            entity.Drag = 0.0f;  // In the scenario being modelled, the W was held down, so Drag could not slow the Player.
            entity.TerminalVelocity = (float)GameConstants.TerminalVelocity;
            physics.AddEntity(entity);

            //
            // Act
            //
            physics.Update(TimeSpan.FromSeconds(1));

            //
            // Assertions
            //
            Assert.True(Math.Abs(expectedPosition.X - entity.Position.X) < GameConstants.Epsilon,
                $"X Position: Expected: {expectedPosition.X}; Actual: {entity.Position.X}");
            Assert.True(Math.Abs(expectedPosition.Y - entity.Position.Y) < GameConstants.Epsilon,
                $"Y Position: Expected: {expectedPosition.Y}; Actual: {entity.Position.Y}");
            Assert.True(Math.Abs(expectedPosition.Z - entity.Position.Z) < GameConstants.Epsilon,
                $"Z Position: Expected: {expectedPosition.Z}; Actual: {entity.Position.Z}");
            Assert.True(Math.Abs(expectedDirection.X - entity.Velocity.X) < GameConstants.Epsilon,
                $"X Direction: Expected: {expectedDirection.X}; Actual: {entity.Velocity.X}");
            Assert.True(Math.Abs(expectedDirection.Y - entity.Velocity.Y) < GameConstants.Epsilon,
                $"Y Direction: Expected: {expectedDirection.Y}; Actual: {entity.Velocity.Y}");
            Assert.True(Math.Abs(expectedDirection.Z - entity.Velocity.Z) < GameConstants.Epsilon,
                $"Z Direction: Expected: {expectedDirection.Z}; Actual: {entity.Velocity.Z}");
        }

        [Test]
        public void WalkIntoCorner2()
        {
            //
            // Set up
            //
            IDimensionServer dimension = (IDimensionServer)BuildDimension();
            IPhysicsEngine physics = new PhysicsEngine(dimension);

            // Build cliff
            IChunk chunk = dimension.GetChunk(new GlobalChunkCoordinates(0, 0), LoadEffort.Generate)!;
            for (int x = 7; x < WorldConstants.ChunkWidth; x++)
                for (int z = 0; z < WorldConstants.ChunkDepth; z++)
                    for (int y = 1; y < 7; y++)
                        chunk.SetBlockID(new LocalVoxelCoordinates(x, y, z), StoneBlockID);

            // Dig stairway into cliff
            int zs = 7;
            for (int x = 7; x < 10; x++)
                for (int y = 1; y < 4; y++)
                    chunk.SetBlockID(new LocalVoxelCoordinates(x, y + x - 7, zs), (byte)BlockIDs.Air);

            // Create the Player Entity
            Size entitySize = new Size(0.6, 1.8, 0.6);   // same size as Player Entity
            TestEntity entity = new TestEntity();
            Vector3 startPosition = new Vector3(9.658, 3, 7.3);
            Vector3 startDirection = new Vector3(3.064, 0, -3.118);
            entity.Position = startPosition;
            entity.Velocity = startDirection;
            entity.Size = entitySize;
            entity.AccelerationDueToGravity = (float)GameConstants.AccelerationDueToGravity;
            entity.Drag = 0.0f;  // In the scenario being modelled, the W was held down, so Drag could not slow the Player.
            entity.TerminalVelocity = (float)GameConstants.TerminalVelocity;
            physics.AddEntity(entity);

            //
            // Act
            //
            physics.Update(TimeSpan.FromMilliseconds(50));

            //
            // Assert
            //
            Assert.AreEqual(9.7, entity.Position.X);
            Assert.AreEqual(startPosition.Y, entity.Position.Y);
            Assert.AreEqual(startPosition.Z, entity.Position.Z);
        }

        #region Testing IsGrounded
        public static IEnumerable<object[]> IsGroundedTestData()
        {
            // Entity is in contact with the ground
            yield return new object[]
            {
                1, true, 8, 1.0, 9
            };

            // Entity is above the ground
            yield return new object[]
            {
                2, false, 7, 1.1, 12
            };

            // Entity is way above the ground
            yield return new object[]
            {
                3, false, 7, 110, 12
            };

            // The Entity's feet are in the ground.
            yield return new object[]
            {
                4, true, 6, 0.97, 4
            };
        }

        [TestCaseSource(nameof(IsGroundedTestData))]
        public void IsGrounded(int serial, bool expected, double x, double y, double z)
        {
            IDimension dimension = BuildDimension();
            IPhysicsEngine physics = new PhysicsEngine(dimension);

            TestEntity entity = new();
            entity.Size = new Size(0.6, 1.6, 0.6);
            entity.Position = new Vector3(x, y, z);
            physics.AddEntity(entity);

            bool actual = physics.IsGrounded(entity);

            Assert.AreEqual(expected, actual);
        }

        // A test where the Entity's bounding box just overlaps (or not) the block below.
        [Test]
        public void IsGrounded_Edge()
        {
            IDimension dimension = BuildDimension();
            IPhysicsEngine physics = new PhysicsEngine(dimension);
            GlobalVoxelCoordinates bc = new GlobalVoxelCoordinates(5, 64, 7);

            dimension.SetBlockID(bc, StoneBlockID);

            TestEntity entity = new();
            entity.Size = new Size(0.6, 1.6, 0.6);
            physics.AddEntity(entity);

            //
            // Test negative-X edge of block
            //

            // The Entity will be standing on the Block, just hanging on in the x-direction,
            // but centred in the z-direction.
            entity.Position = new Vector3(bc.X - entity.Size.Width * 0.5 + GameConstants.Epsilon, bc.Y + 1, bc.Z + 0.5);
            Assert.True(physics.IsGrounded(entity));

            // Move the Entity to just past the edge.
            entity.Position = new Vector3(bc.X - entity.Size.Width * 0.5 - GameConstants.Epsilon, bc.Y + 1, bc.Z + 0.5);
            Assert.False(physics.IsGrounded(entity));

            // Put the entity just within the positive-X edge of the block
            entity.Position = new Vector3(bc.X + 1 + entity.Size.Width * 0.5 - GameConstants.Epsilon, bc.Y + 1, bc.Z + 0.5);
            Assert.True(physics.IsGrounded(entity));

            // Move the Entity just past the edge
            entity.Position = new Vector3(bc.X + 1 + entity.Size.Width * 0.5 + GameConstants.Epsilon, bc.Y + 1, bc.Z + 0.5);
            Assert.False(physics.IsGrounded(entity));

            // Place the Entity just on the negative-Z edge
            entity.Position = new Vector3(bc.X + 0.5, bc.Y + 1, bc.Z - entity.Size.Depth * 0.5 + GameConstants.Epsilon);
            Assert.True(physics.IsGrounded(entity));

            // Move the Entity just off the negative-Z edge
            entity.Position = new Vector3(bc.X + 0.5, bc.Y + 1, bc.Z - entity.Size.Depth * 0.5 - GameConstants.Epsilon);
            Assert.False(physics.IsGrounded(entity));

            // Place the Entity just on the positive-Z edge
            entity.Position = new Vector3(bc.X + 0.5, bc.Y + 1, bc.Z + 1 + entity.Size.Depth * 0.5 - GameConstants.Epsilon);
            Assert.True(physics.IsGrounded(entity));

            // Move the Entity just off the negative-Z edge
            entity.Position = new Vector3(bc.X + 0.5, bc.Y + 1, bc.Z + 1 + entity.Size.Depth * 0.5 + GameConstants.Epsilon);
            Assert.False(physics.IsGrounded(entity));
        }

        /// <summary>
        /// Testing the boundary condition where the entity bounding box edge is
        /// equal to the edge of the block.  This should NOT be grounded.
        /// </summary>
        [Test]
        public void IsGrounded_Edge2()
        {
            IDimension dimension = BuildDimension();
            IPhysicsEngine physics = new PhysicsEngine(dimension);
            GlobalVoxelCoordinates bc = new GlobalVoxelCoordinates(5, 64, 7);

            dimension.SetBlockID(bc, StoneBlockID);

            TestEntity entity = new();
            entity.Size = new Size(0.6, 1.6, 0.6);
            physics.AddEntity(entity);


            // Test North edge of block
            entity.Position = new Vector3(bc.X + 0.5, bc.Y + 0.9, bc.Z - entity.Size.Depth * 0.5);
            Assert.False(physics.IsGrounded(entity));

            // Test East edge of block
            entity.Position = new Vector3(bc.X + 1 + entity.Size.Width * 0.5, bc.Y + 0.9, bc.Z + 0.5);
            Assert.False(physics.IsGrounded(entity));

            // Test South edge of block
            entity.Position = new Vector3(bc.X + 0.5, bc.Y + 0.9, bc.Z + 1 + entity.Size.Depth * 0.5);
            Assert.False(physics.IsGrounded(entity));

            // Test West edge of block
            entity.Position = new Vector3(bc.X - entity.Size.Width * 0.5, bc.Y + 0.9, bc.Z + 0.5);
            Assert.False(physics.IsGrounded(entity));
        }

        [Test]
        public void IsGrounded_Corner()
        {
            IDimension dimension = BuildDimension();
            IPhysicsEngine physics = new PhysicsEngine(dimension);
            GlobalVoxelCoordinates bc = new(5, SurfaceHeight + 1, 5);

            for (int x = bc.X - 1; x <= bc.X + 1; x++)
                dimension.SetBlockID(new GlobalVoxelCoordinates(x, bc.Y, bc.Z), StoneBlockID);

            for (int z = bc.Z - 1; z <= bc.Z + 1; z ++)
                dimension.SetBlockID(new GlobalVoxelCoordinates(bc.X, bc.Y, z), StoneBlockID);

            TestEntity entity = new();
            entity.Size = new Size(0.6, 1.6, 0.6);
            double hw = entity.Size.Width * 0.5;
            double hd = entity.Size.Depth * 0.5;
            physics.AddEntity(entity);

            // Test corner is to the south-west of the entity
            entity.Position = new Vector3(bc.X + 1 + hw, bc.Y + 1, bc.Z - hd);
            Assert.False(physics.IsGrounded(entity));

            // Test corner is to the north-west of the entity
            entity.Position = new Vector3(bc.X + 1 + hw, bc.Y + 1, bc.Z + 1 + hd);
            Assert.False(physics.IsGrounded(entity));

            // Test corner is to the north-east of the entity
            entity.Position = new Vector3(bc.X - hw, bc.Y + 1, bc.Z + 1 + hd);
            Assert.False(physics.IsGrounded(entity));

            // Test corner is to the south-east of the entity
            entity.Position = new Vector3(bc.X - hw, bc.Y + 1, bc.Z - hd);
            Assert.False(physics.IsGrounded(entity));
        }

        #endregion

        [Test]
        public void JumpUpOneBlock()
        {
            IDimensionServer dimension = (IDimensionServer)BuildDimension();
            IPhysicsEngine physics = new PhysicsEngine(dimension);

            GlobalChunkCoordinates chunkCoordinates = new(-1, 12);
            IChunk chunk = dimension.GetChunk(chunkCoordinates, LoadEffort.Generate)!;

            int yLower = 62;
            int yUpper = yLower + 1;
            for (int x = 0; x < WorldConstants.ChunkWidth; x++)
                for (int z = 0; z < WorldConstants.ChunkDepth; z++)
                    chunk.SetBlockID(new LocalVoxelCoordinates(x, yLower, z), StoneBlockID);

            for (int x = 0; x < WorldConstants.ChunkWidth; x ++)
                for (int z = 9; z < WorldConstants.ChunkDepth; z ++)  // 9 == 201 mod 16
                    chunk.SetBlockID(new LocalVoxelCoordinates(x, yUpper, z), StoneBlockID);

            // Note that the y-component indicates the Entity's feet are above the upper level of
            //   blocks and the Z-component indicates the feet are past the edge of the upper level
            //   of blocks.  Thus, the jump up should ALEADY be successful.  However, we're observing
            //   that the Player is knocked back to being in contact with z == 201, and y == 64 (the top
            //   of the lower level of blocks).
            Vector3 initialPosition = new Vector3(-8.401511844263727, 64.05635210306419, 201.06527613705063);

            TestEntity entity = new();
            entity.Size = new Size(0.6, 1.6, 0.6);
            entity.Position = initialPosition;
            entity.Velocity = new Vector3(0.15257002413272858, -2.1800181849375067, 4.369036674499512);
            entity.AccelerationDueToGravity = (float)GameConstants.AccelerationDueToGravity;
            entity.Drag = 0.4f;
            entity.TerminalVelocity = (float)GameConstants.TerminalVelocity;

            double hw = entity.Size.Width * 0.5;
            double hd = entity.Size.Depth * 0.5;
            physics.AddEntity(entity);

            //
            // Act
            //
            physics.Update(TimeSpan.FromMilliseconds(50));

            //
            // Asssert
            //
            Assert.True(entity.Position.Z > initialPosition.Z);
        }

        [Test]
        public void EmbeddedJump()
        {
            IDimensionServer dimension = (IDimensionServer)BuildDimension();
            IPhysicsEngine physics = new PhysicsEngine(dimension);
            IChunk chunk = dimension.GetChunk(new GlobalChunkCoordinates(0, 0), LoadEffort.Generate)!;

            // Create a one-block hole into which we can place an entity
            for (int x = 0; x < WorldConstants.ChunkWidth; x ++)
                for (int z = 0; z < WorldConstants.ChunkDepth; z ++)
                    chunk.SetBlockID(new LocalVoxelCoordinates(x, 1, z), StoneBlockID);
            LocalVoxelCoordinates bc = new LocalVoxelCoordinates(5, 1, 5);
            chunk.SetBlockID(bc, (byte)BlockIDs.Air);

            TestEntity entity = new();
            entity.Size = new Size(0.6, 1.6, 0.6);
            double hw = entity.Size.Width * 0.5;
            double hd = entity.Size.Depth * 0.5;
            // The initial position is slightly embedded in one side of the hole, and
            // partway up the side.
            Vector3 initialPosition = new(bc.X + hw, bc.Y + 0.9, bc.Z - hd + 0.001);
            entity.Position = initialPosition;
            entity.Velocity = new Vector3(0, GameConstants.JumpVelocity, 0);
            entity.AccelerationDueToGravity = (float)GameConstants.AccelerationDueToGravity;
            entity.Drag = 0.4f;
            entity.TerminalVelocity = (float)GameConstants.TerminalVelocity;
            physics.AddEntity(entity);

            //
            // Act
            //
            physics.Update(TimeSpan.FromMilliseconds(50));

            //
            // Assert
            //
            Assert.True(entity.Position.Y > initialPosition.Y);
        }
    }
}
