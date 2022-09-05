using System;
using NUnit.Framework;
using Moq;
using Moq.Protected;
using TrueCraft.Core.Entities;
using TrueCraft.Core.Logic;
using TrueCraft.Core.Networking;
using TrueCraft.Core.Server;
using TrueCraft.Core.World;
using TrueCraft.Core;
using TrueCraft.TerrainGen;
using TrueCraft.Core.Lighting;
using TrueCraft.Test.World;
using TrueCraft.Core.Logic.Items;
using TrueCraft.Core.Logic.Blocks;
using System.Collections.Generic;
using TrueCraft.Core.Inventory;
using TrueCraft.Core.Physics;
using System.IO;
using System.Xml;

namespace TrueCraft.Test.Logic
{
    // TODO: This unit test depends upon the FlatlandGenerator
    [TestFixture]
    public class BlockProviderTest
    {
        private static string xmlCobblestone = @"<block>
  <id>4</id>
  <blastresistance>30</blastresistance>
  <hardness>2</hardness>
  <luminance>0</luminance>
  <opaque>true</opaque>
  <renderopaque>true</renderopaque>
  <lightopacity>255</lightopacity>
  <flammable>false</flammable>
  <blockmetadata>
    <metadata>
      <value>0</value>
      <displayname>Cobblestone</displayname>
      <soundeffect>Stone</soundeffect>
      <drops>
        <drop>
          <tool>
            <kinds>
              <kind>None</kind>
              <kind>Pickaxe</kind>
              <kind>Axe</kind>
              <kind>Shovel</kind>
              <kind>Hoe</kind>
              <kind>Sword</kind>
            </kinds>
            <materials>
              <material>None</material>
              <material>Wood</material>
              <material>Stone</material>
              <material>Iron</material>
              <material>Gold</material>
              <material>Diamond</material>
            </materials>
          </tool>
          <dropitem>
            <itemid>4</itemid>
            <min>1</min>
            <max>1</max>
            <metadata>0</metadata>
          </dropitem>
        </drop>
      </drops>
      <boundingbox>
          <min> 
            <x>0</x><y>0</y><z>0</z>
          </min>
          <max> 
            <x>1</x><y>1</y><z>1</z>
          </max>
      </boundingbox>
      <modelname>Block</modelname>
      <texturefile>terrain.png</texturefile>
      <texture>
        <tc><x>0</x><y>16</y></tc>
        <tc><x>0</x><y>31</y></tc>
        <tc><x>15</x><y>31</y></tc>
        <tc><x>15</x><y>16</y></tc>
      </texture>
    </metadata>
  </blockmetadata>
</block>
";
        private static IBlockProvider BuildCobblestoneBlock()
        {
            XmlDocument doc = new XmlDocument();
            using (StringReader sr = new StringReader(xmlCobblestone))
            using (XmlReader xmlr = XmlReader.Create(sr))
                doc.Load(xmlr);

            return new CobblestoneBlock(doc.FirstChild!);

        }

        [OneTimeSetUp]
        public void SetUp()
        {
            try
            {
                WhoAmI.Answer = IAm.Server;
            }
            catch (InvalidOperationException)
            {
                // Ignore this - it just means we've tried to set WhoAmI
                // multiple times.
            }
        }

        [Test]
        public void TestBlockMined()
        {
            //
            // Set up
            //
            bool generateDropEntityCalled = false;
            BlockDescriptor? generateDescriptor = null;
            IDimension? generateDimension = null;
            IMultiplayerServer? generateServer = null;
            ItemStack? generateHeldItem = null;
            Mock<BlockProvider> blockProvider = new Mock<BlockProvider>(MockBehavior.Strict);
            blockProvider.Setup(x => x.BlockMined(It.IsAny<BlockDescriptor>(),
                It.IsAny<BlockFace>(), It.IsAny<IDimension>(), It.IsAny<IRemoteClient>())).CallBase();
            blockProvider.Setup(x => x.GenerateDropEntity(It.IsAny<BlockDescriptor>(),
                It.IsAny<IDimension>(), It.IsAny<IMultiplayerServer>(), It.IsAny<ItemStack>()))
                .Callback<BlockDescriptor, IDimension, IMultiplayerServer, ItemStack>(
                (block, dimension, server, heldItem) =>
                {
                    generateDropEntityCalled = true;
                    generateDescriptor = block;
                    generateDimension = dimension;
                    generateServer = server;
                    generateHeldItem = heldItem;
                });

            // NOTE: dependency upon BlockDescriptor and GlobalVoxelCoordinates.
            GlobalVoxelCoordinates blockCoordinates = new GlobalVoxelCoordinates(5, 15, 10);
            var descriptor = new BlockDescriptor
            {
                ID = 10,
                Coordinates = blockCoordinates
            };

            Mock<IEntityManager> mockEntityManager = new Mock<IEntityManager>(MockBehavior.Strict);

            Mock<IItemRepository> mockItemRepository = new Mock<IItemRepository>(MockBehavior.Strict);

            GlobalVoxelCoordinates blockSetAt = GlobalVoxelCoordinates.Zero;
            byte newBlockID = 1;
            Mock<IDimensionServer> mockDimension = new Mock<IDimensionServer>(MockBehavior.Strict);
            mockDimension.Setup(x => x.EntityManager).Returns(mockEntityManager.Object);
            mockDimension.Setup(x => x.ItemRepository).Returns(mockItemRepository.Object);
            mockDimension.Setup(x => x.SetBlockID(It.IsAny<GlobalVoxelCoordinates>(), It.IsAny<byte>()))
                .Callback<GlobalVoxelCoordinates, byte>((coord, id) => { blockSetAt = coord; newBlockID = id; });

            Mock<IMultiplayerServer> mockServer = new Mock<IMultiplayerServer>(MockBehavior.Strict);

            Mock<IRemoteClient> mockRemoteClient = new Mock<IRemoteClient>(MockBehavior.Strict);
            mockRemoteClient.Setup(x => x.Server).Returns(mockServer.Object);
            // NOTE: dependent upon ItemStack class.
            mockRemoteClient.Setup(x => x.SelectedItem).Returns(ItemStack.EmptyStack);

            //
            // Act
            //
            blockProvider.Object.BlockMined(descriptor, BlockFace.PositiveY, mockDimension.Object, mockRemoteClient.Object);

            //
            // Assert
            //
            Assert.AreEqual(blockCoordinates, blockSetAt);
            Assert.AreEqual(0, newBlockID);
            Assert.True(generateDropEntityCalled);
            Assert.IsNotNull(generateDescriptor);
            Assert.AreEqual(10, generateDescriptor?.ID);
            Assert.AreEqual(blockCoordinates, generateDescriptor?.Coordinates);
            Assert.True(object.ReferenceEquals(mockDimension.Object, generateDimension));
            Assert.AreEqual(ItemStack.EmptyStack, generateHeldItem);
            Assert.True(object.ReferenceEquals(mockServer.Object, generateServer));
        }

        [Test]
        public void TestSupport()
        {
            //
            // Setup
            //
            Mock<IBlockProvider> supportive = new Mock<IBlockProvider>(MockBehavior.Strict);
            supportive.SetupGet(p => p.Opaque).Returns(true);
            supportive.SetupGet<byte>(p => p.ID).Returns(1);
            Mock<IBlockProvider> needsSupport = new Mock<IBlockProvider>(MockBehavior.Strict);
            needsSupport.SetupGet<byte>(p => p.ID).Returns(2);
            Mock<IBlockProvider> unsupportive = new Mock<IBlockProvider>(MockBehavior.Strict);
            unsupportive.SetupGet(p => p.Opaque).Returns(false);
            unsupportive.SetupGet<byte>(p => p.ID).Returns(3);

            Mock<IBlockRepository> mockBlockRepository = new Mock<IBlockRepository>(MockBehavior.Strict);
            mockBlockRepository.Setup(r => r.GetBlockProvider(It.Is<byte>(b => b == supportive.Object.ID))).Returns(supportive.Object);
            mockBlockRepository.Setup(r => r.GetBlockProvider(It.Is<byte>(b => b == unsupportive.Object.ID))).Returns(unsupportive.Object);

            Mock<IItemRepository> mockItemRepository = new Mock<IItemRepository>(MockBehavior.Strict);
            mockItemRepository.Setup(x => x.GetItemProvider(It.Is<short>(x => x == -1))).Returns<IItemProvider>(null);

            Mock<IMultiplayerServer> mockServer = new Mock<IMultiplayerServer>(MockBehavior.Strict);

            Mock<IServiceLocator> mockServiceLocator = new Mock<IServiceLocator>(MockBehavior.Strict);
            mockServiceLocator.Setup(x => x.BlockRepository).Returns(mockBlockRepository.Object);
            mockServiceLocator.Setup(x => x.ItemRepository).Returns(mockItemRepository.Object);

            Mock<IEntityManager> mockEntityManager = new Mock<IEntityManager>(MockBehavior.Strict);
            IEntity? spawnedEntity = null;
            mockEntityManager.Setup(x => x.SpawnEntity(It.IsAny<IEntity>())).Callback<IEntity>(
                (entity) =>
                {
                    spawnedEntity = entity;
                });

            IDimension fakeDimension = new FakeDimension(mockBlockRepository.Object,
                mockItemRepository.Object, mockEntityManager.Object);

            fakeDimension.SetBlockID(GlobalVoxelCoordinates.Zero, supportive.Object.ID);
            GlobalVoxelCoordinates oneY = new GlobalVoxelCoordinates(0, 1, 0);
            fakeDimension.SetBlockID(oneY, needsSupport.Object.ID);

            // Note: It would be preferable to have this be a Strict Mock rather
            //  than a loose one, but the setup of GetDrop is not working.
            Mock<BlockProvider> testBlockProvider = new Mock<BlockProvider>(MockBehavior.Loose);
            testBlockProvider.CallBase = true;
            testBlockProvider.SetupGet<byte>(x => x.ID).Returns(2);
            testBlockProvider.SetupGet<ToolType>(x => x.EffectiveTools).Returns(ToolType.All);
            testBlockProvider.SetupGet<ToolMaterial>(x => x.EffectiveToolMaterials).Returns(ToolMaterial.All);
            testBlockProvider.Setup(b => b.GetSupportDirection(It.IsAny<BlockDescriptor>())).Returns(Vector3i.Down);
            testBlockProvider.Setup(x => x.BlockUpdate(It.IsAny<BlockDescriptor>(),
                It.IsAny<BlockDescriptor>(), It.IsAny<IMultiplayerServer>(),
                It.IsAny<IDimension>())).CallBase();
            testBlockProvider.Setup(x => x.IsSupported(It.IsAny<IDimension>(),
                It.IsAny<BlockDescriptor>())).CallBase();
            testBlockProvider.Setup(x => x.GenerateDropEntity(It.IsAny<BlockDescriptor>(),
                It.IsAny<IDimension>(), It.IsAny<IMultiplayerServer>(), It.IsAny<ItemStack>()))
                .CallBase();
            // Note: This could not be made to work.  It was throwing an exception
            // stating that
            // "all invocations on the Mock must have a corresponding setup"
            // Changing the passed in signature produced an error as expected so
            // the setup was able to find the method, but execution of the Mock
            // was not.
            //testBlockProvider.Protected().Setup<ItemStack[]>("GetDrop", It.IsAny<BlockDescriptor>(), It.IsAny<ItemStack>()).CallBase();

            BlockDescriptor updated = new BlockDescriptor { ID = needsSupport.Object.ID, Coordinates = oneY };
            BlockDescriptor source = new BlockDescriptor { ID = needsSupport.Object.ID, Coordinates = new GlobalVoxelCoordinates(1, 0, 0) };


            //
            // Act / Assert
            //

            // Send the block an update from the side
            testBlockProvider.Object.BlockUpdate(updated, source, mockServer.Object, fakeDimension);
            // Assert that the block needing support is still there.
            Assert.AreEqual(needsSupport.Object.ID, fakeDimension.GetBlockID(oneY));

            // Switch the block underneath to one that does not provide support.
            fakeDimension.SetBlockID(GlobalVoxelCoordinates.Zero, unsupportive.Object.ID);
            // Send the supported block an update from below
            source = new BlockDescriptor { ID = unsupportive.Object.ID, Coordinates = GlobalVoxelCoordinates.Zero };
            testBlockProvider.Object.BlockUpdate(updated, source, mockServer.Object, fakeDimension);
            // Assert that the block requiring support has been replace with an Air Block
            Assert.AreEqual(0, fakeDimension.GetBlockID(oneY));
            // Assert that an Item Entity with ID == 2 was spawned.
            Assert.IsNotNull(spawnedEntity);
            ItemEntity? itemEntity = spawnedEntity as ItemEntity;
            Assert.IsNotNull(itemEntity);
            Assert.AreEqual(needsSupport.Object.ID, itemEntity?.Item.ID);
        }

        /// <summary>
        /// Test GenerateDropEntity using a tool which is effective and of the correct material
        /// </summary>
        [Test]
        public void GenerateDropEntity_Effective()
        {
            //
            // Setup
            //
            GlobalVoxelCoordinates coordinates = new GlobalVoxelCoordinates(5, 7, 13);
            byte blockID = 47;
            BlockDescriptor block = new BlockDescriptor()
            {
                ID = blockID,
                Coordinates = coordinates
            };

            Mock<BlockProvider> testBlockProvider = new Mock<BlockProvider>(MockBehavior.Loose);
            testBlockProvider.CallBase = true;
            testBlockProvider.Setup(x => x.EffectiveToolMaterials).CallBase();
            testBlockProvider.Setup(x => x.EffectiveTools).CallBase();
            testBlockProvider.Setup(x => x.GenerateDropEntity(It.IsAny<BlockDescriptor>(),
                It.IsAny<IDimension>(), It.IsAny<IMultiplayerServer>(), It.IsAny<ItemStack>()))
                .CallBase();

            Mock<IMultiplayerServer> mockServer = new Mock<IMultiplayerServer>(MockBehavior.Strict);

            short toolItemID = 12;
            ItemStack heldItem = new ItemStack(toolItemID);

            Mock<ToolItem> mockTool = new Mock<ToolItem>(MockBehavior.Strict);
            mockTool.Setup(x => x.Material).Returns(ToolMaterial.Stone);
            mockTool.Setup(x => x.ToolType).Returns(ToolType.Hoe);

            Mock<IItemRepository> mockItemRepository = new Mock<IItemRepository>(MockBehavior.Strict);
            mockItemRepository.Setup(x => x.GetItemProvider(It.Is<short>(y => y == toolItemID))).Returns(mockTool.Object);

            Mock<IEntityManager> mockEntityManager = new Mock<IEntityManager>(MockBehavior.Strict);
            int spawnCount = 0;
            IEntity? spawnedEntity = null;
            mockEntityManager.Setup(x => x.SpawnEntity(It.IsAny<IEntity>())).Callback<IEntity>((entity) =>
            {
                spawnCount++;
                spawnedEntity = entity;
            });

            Mock<IDimensionServer> mockDimension = new Mock<IDimensionServer>(MockBehavior.Strict);
            mockDimension.Setup(x => x.ItemRepository).Returns(mockItemRepository.Object);
            mockDimension.Setup(x => x.EntityManager).Returns(mockEntityManager.Object);

            //
            // Act
            //
            testBlockProvider.Object.GenerateDropEntity(block, mockDimension.Object,
                mockServer.Object, heldItem);

            //
            // Assert
            //
            Assert.AreEqual(1, spawnCount);
            ItemEntity? spawnedItem = spawnedEntity as ItemEntity;
            Assert.IsNotNull(spawnedItem);
            Assert.AreEqual(blockID, spawnedItem?.Item.ID);
            Assert.AreEqual(1, spawnedItem?.Item.Count);
            Assert.AreEqual(coordinates.X, Math.Floor(spawnedEntity?.Position.X ?? double.MinValue));
            Assert.AreEqual(coordinates.Y, Math.Floor(spawnedEntity?.Position.Y ?? double.MinValue));
            Assert.AreEqual(coordinates.Z, Math.Floor(spawnedEntity?.Position.Z ?? double.MinValue));
        }

        /// <summary>
        /// Test GenerateDropEntity using a tool which is ineffective but of the correct material
        /// </summary>
        [Test]
        public void GenerateDropEntity_IneffectiveTool()
        {
            //
            // Setup
            //
            GlobalVoxelCoordinates coordinates = new GlobalVoxelCoordinates(5, 7, 13);
            byte blockID = 47;
            BlockDescriptor block = new BlockDescriptor()
            {
                ID = blockID,
                Coordinates = coordinates
            };

            Mock<BlockProvider> testBlockProvider = new Mock<BlockProvider>(MockBehavior.Loose);
            testBlockProvider.CallBase = true;
            testBlockProvider.Setup(x => x.EffectiveToolMaterials).Returns(ToolMaterial.Diamond);
            testBlockProvider.Setup(x => x.EffectiveTools).Returns(ToolType.Pickaxe);
            testBlockProvider.Setup(x => x.GenerateDropEntity(It.IsAny<BlockDescriptor>(),
                It.IsAny<IDimension>(), It.IsAny<IMultiplayerServer>(), It.IsAny<ItemStack>()))
                .CallBase();

            Mock<IMultiplayerServer> mockServer = new Mock<IMultiplayerServer>(MockBehavior.Strict);

            short toolItemID = 12;
            ItemStack heldItem = new ItemStack(toolItemID);

            Mock<ToolItem> mockTool = new Mock<ToolItem>(MockBehavior.Strict);
            mockTool.Setup(x => x.Material).Returns(ToolMaterial.Diamond);
            mockTool.Setup(x => x.ToolType).Returns(ToolType.Hoe);

            Mock<IItemRepository> mockItemRepository = new Mock<IItemRepository>(MockBehavior.Strict);
            mockItemRepository.Setup(x => x.GetItemProvider(It.Is<short>(y => y == toolItemID))).Returns(mockTool.Object);

            Mock<IEntityManager> mockEntityManager = new Mock<IEntityManager>(MockBehavior.Strict);
            int spawnCount = 0;
            IEntity? spawnedEntity = null;
            mockEntityManager.Setup(x => x.SpawnEntity(It.IsAny<IEntity>())).Callback<IEntity>((entity) =>
            {
                spawnCount++;
                spawnedEntity = entity;
            });

            Mock<IDimensionServer> mockDimension = new Mock<IDimensionServer>(MockBehavior.Strict);
            mockDimension.Setup(x => x.ItemRepository).Returns(mockItemRepository.Object);
            mockDimension.Setup(x => x.EntityManager).Returns(mockEntityManager.Object);

            //
            // Act
            //
            testBlockProvider.Object.GenerateDropEntity(block, mockDimension.Object,
                mockServer.Object, heldItem);

            //
            // Assert
            //
            Assert.AreEqual(0, spawnCount);
            Assert.IsNull(spawnedEntity);
        }

        /// <summary>
        /// Test GenerateDropEntity using a tool which is effective but of the incorrect material
        /// </summary>
        [Test]
        public void GenerateDropEntity_IneffectiveMaterial()
        {
            //
            // Setup
            //
            GlobalVoxelCoordinates coordinates = new GlobalVoxelCoordinates(5, 7, 13);
            byte blockID = 47;
            BlockDescriptor block = new BlockDescriptor()
            {
                ID = blockID,
                Coordinates = coordinates
            };

            Mock<BlockProvider> testBlockProvider = new Mock<BlockProvider>(MockBehavior.Loose);
            testBlockProvider.CallBase = true;
            testBlockProvider.Setup(x => x.EffectiveToolMaterials).Returns(ToolMaterial.Diamond);
            testBlockProvider.Setup(x => x.EffectiveTools).Returns(ToolType.Pickaxe);
            testBlockProvider.Setup(x => x.GenerateDropEntity(It.IsAny<BlockDescriptor>(),
                It.IsAny<IDimension>(), It.IsAny<IMultiplayerServer>(), It.IsAny<ItemStack>()))
                .CallBase();

            Mock<IMultiplayerServer> mockServer = new Mock<IMultiplayerServer>(MockBehavior.Strict);

            short toolItemID = 12;
            ItemStack heldItem = new ItemStack(toolItemID);

            Mock<ToolItem> mockTool = new Mock<ToolItem>(MockBehavior.Strict);
            mockTool.Setup(x => x.Material).Returns(ToolMaterial.Stone);
            mockTool.Setup(x => x.ToolType).Returns(ToolType.Pickaxe);

            Mock<IItemRepository> mockItemRepository = new Mock<IItemRepository>(MockBehavior.Strict);
            mockItemRepository.Setup(x => x.GetItemProvider(It.Is<short>(y => y == toolItemID))).Returns(mockTool.Object);

            Mock<IEntityManager> mockEntityManager = new Mock<IEntityManager>(MockBehavior.Strict);
            int spawnCount = 0;
            IEntity? spawnedEntity = null;
            mockEntityManager.Setup(x => x.SpawnEntity(It.IsAny<IEntity>())).Callback<IEntity>((entity) =>
            {
                spawnCount++;
                spawnedEntity = entity;
            });

            Mock<IDimensionServer> mockDimension = new Mock<IDimensionServer>(MockBehavior.Strict);
            mockDimension.Setup(x => x.ItemRepository).Returns(mockItemRepository.Object);
            mockDimension.Setup(x => x.EntityManager).Returns(mockEntityManager.Object);

            //
            // Act
            //
            testBlockProvider.Object.GenerateDropEntity(block, mockDimension.Object,
                mockServer.Object, heldItem);

            //
            // Assert
            //
            Assert.AreEqual(0, spawnCount);
            Assert.IsNull(spawnedEntity);
        }

        /// <summary>
        /// Tests that a Block will not be placed in a voxel occupied by an Air Block.
        /// </summary>
        [Test]
        public void ItemUsedOnBlock_Space()
        {
            //
            // Setup
            //
            Mock<IBlockRepository> mockBlockRepository = new Mock<IBlockRepository>(MockBehavior.Strict);
            mockBlockRepository.Setup(x => x.GetBlockProvider((byte)BlockIDs.Cobblestone)).Returns(BuildCobblestoneBlock());

            Mock<IItemRepository> mockItemRepository = new Mock<IItemRepository>(MockBehavior.Strict);

            Mock<IEntityManager> mockEntityManager = new Mock<IEntityManager>(MockBehavior.Strict);
            mockEntityManager.Setup(x => x.EntitiesInRange(It.IsAny<Vector3>(), It.IsAny<float>())).Returns(new List<IEntity>(1));

            GlobalVoxelCoordinates coordinates = new GlobalVoxelCoordinates(3, 5, 7);
            FakeDimension dimension = new FakeDimension(mockBlockRepository.Object,
                mockItemRepository.Object, mockEntityManager.Object);
            dimension.SetBlockID(coordinates, (byte)BlockIDs.Cobblestone);
            dimension.ResetCounts();

            int blockPlacedCallCount = 0;
            Mock<BlockProvider> testBlockProvider = new Mock<BlockProvider>(MockBehavior.Strict);
            testBlockProvider.Setup(x => x.ItemUsedOnBlock(It.IsAny<GlobalVoxelCoordinates>(),
                It.IsAny<ItemStack>(), It.IsAny<BlockFace>(), It.IsAny<IDimension>(), It.IsAny<IRemoteClient>()))
                .CallBase();
            testBlockProvider.Setup(x => x.GetCollisionBox(0)).CallBase();
            testBlockProvider.Setup(x => x.ID).Returns((byte)BlockIDs.Cobblestone);
            testBlockProvider.Setup(x => x.BlockPlaced(It.IsAny<BlockDescriptor>(),
                It.IsAny<BlockFace>(), It.IsAny<IDimension>(), It.IsAny<IRemoteClient>()))
                .Callback(() => blockPlacedCallCount++)
                .CallBase();
            testBlockProvider.Setup(x => x.IsSupported(It.IsAny<IDimension>(), It.IsAny<BlockDescriptor>()))
                .CallBase();
            testBlockProvider.Setup(x => x.GetSupportDirection(It.IsAny<BlockDescriptor>()))
                .CallBase();

            BlockFace face = BlockFace.PositiveY;
            sbyte heldItemCount = 32;
            short selectedSlot = 6;   // index into Hotbar.
            ItemStack heldItem = new ItemStack((byte)BlockIDs.Cobblestone, heldItemCount);
            ItemStack updatedHeldItem = heldItem;

            Mock<IServerSlot> mockSlot = new Mock<IServerSlot>(MockBehavior.Strict);
            mockSlot.SetupGet(x => x.Item).Returns(updatedHeldItem);
            mockSlot.SetupSet(x => x.Item = heldItem.GetReducedStack(1)).Verifiable();

            Mock<ISlots<IServerSlot>> mockInventory = new Mock<ISlots<IServerSlot>>(MockBehavior.Strict);
            mockInventory.SetupGet(x => x[It.Is<int>(i => i == selectedSlot)]).Returns(mockSlot.Object);

            Mock<IRemoteClient> mockUser = new Mock<IRemoteClient>(MockBehavior.Strict);
            mockUser.Setup(x => x.Inventory).Returns(mockInventory.Object);
            mockUser.Setup(x => x.SelectedSlot).Returns(selectedSlot);

            //
            // Act
            //
            testBlockProvider.Object.ItemUsedOnBlock(coordinates, heldItem, face, dimension, mockUser.Object);

            //
            // Assertions
            //
            Assert.AreEqual(1, dimension.SetBlockIDCount);
            Assert.AreEqual(1, blockPlacedCallCount);
            Assert.AreEqual((byte)BlockIDs.Cobblestone, dimension.GetBlockID(coordinates));
            Assert.AreEqual((byte)BlockIDs.Cobblestone, dimension.GetBlockID(coordinates + Vector3i.Up));
            mockSlot.Verify();    // Asserts that the held item count has been reduced by one.
        }

        /// <summary>
        /// Tests that a Block will not be placed in a voxel occupied by a non-overwritable block.
        /// </summary>
        [Test]
        public void ItemUsedOnBlock_NoSpace()
        {
            //
            // Setup
            //
            Mock<IBlockRepository> mockBlockRepository = new Mock<IBlockRepository>(MockBehavior.Strict);
            mockBlockRepository.Setup(x => x.GetBlockProvider((byte)BlockIDs.Cobblestone)).Returns(BuildCobblestoneBlock());

            Mock<IItemRepository> mockItemRepository = new Mock<IItemRepository>(MockBehavior.Strict);

            Mock<IEntityManager> mockEntityManager = new Mock<IEntityManager>(MockBehavior.Strict);

            GlobalVoxelCoordinates coordinates = new GlobalVoxelCoordinates(3, 5, 7);
            FakeDimension dimension = new FakeDimension(mockBlockRepository.Object,
                mockItemRepository.Object, mockEntityManager.Object);
            dimension.SetBlockID(coordinates, (byte)BlockIDs.Cobblestone);
            dimension.SetBlockID(coordinates + Vector3i.Up, (byte)BlockIDs.Cobblestone);
            dimension.ResetCounts();

            Mock<BlockProvider> testBlockProvider = new Mock<BlockProvider>(MockBehavior.Strict);
            testBlockProvider.Setup(x => x.ItemUsedOnBlock(It.IsAny<GlobalVoxelCoordinates>(),
                It.IsAny<ItemStack>(), It.IsAny<BlockFace>(), It.IsAny<IDimension>(), It.IsAny<IRemoteClient>()))
                .CallBase();

            BlockFace face = BlockFace.PositiveY;
            sbyte heldItemCount = 32;
            ItemStack heldItem = new ItemStack((byte)BlockIDs.Cobblestone, heldItemCount);

            Mock<IRemoteClient> mockUser = new Mock<IRemoteClient>(MockBehavior.Strict);

            //
            // Act
            //
            testBlockProvider.Object.ItemUsedOnBlock(coordinates, heldItem, face, dimension, mockUser.Object);

            //
            // Assertions
            //
            Assert.AreEqual(0, dimension.SetBlockIDCount);
            Assert.AreEqual((byte)BlockIDs.Cobblestone, dimension.GetBlockID(coordinates));
            Assert.AreEqual((byte)BlockIDs.Cobblestone, dimension.GetBlockID(coordinates + Vector3i.Up));
        }

        /// <summary>
        /// Tests that a Block will not be placed in a voxel occupied by a mob.
        /// </summary>
        [Test]
        public void ItemUsedOnBlock_MobOccupied()
        {
            //
            // Setup
            //
            Mock<IBlockRepository> mockBlockRepository = new Mock<IBlockRepository>(MockBehavior.Strict);
            mockBlockRepository.Setup(x => x.GetBlockProvider((byte)BlockIDs.Cobblestone)).Returns(BuildCobblestoneBlock());

            Mock<IItemRepository> mockItemRepository = new Mock<IItemRepository>(MockBehavior.Strict);

            Mock<IMobEntity> mockMob = new Mock<IMobEntity>(MockBehavior.Strict);
            mockMob.Setup(x => x.BoundingBox).Returns(new BoundingBox(new Vector3(3, 6, 7), new Vector3(4, 8, 8)));

            Mock<IEntityManager> mockEntityManager = new Mock<IEntityManager>(MockBehavior.Strict);
            mockEntityManager.Setup(x => x.EntitiesInRange(It.IsAny<Vector3>(), It.IsAny<float>()))
                .Returns(new List<IEntity>() { mockMob.Object });

            GlobalVoxelCoordinates coordinates = new GlobalVoxelCoordinates(3, 5, 7);
            FakeDimension dimension = new FakeDimension(mockBlockRepository.Object,
                mockItemRepository.Object, mockEntityManager.Object);
            dimension.SetBlockID(coordinates, (byte)BlockIDs.Cobblestone);
            dimension.ResetCounts();

            Mock<BlockProvider> testBlockProvider = new Mock<BlockProvider>(MockBehavior.Strict);
            testBlockProvider.Setup(x => x.ItemUsedOnBlock(It.IsAny<GlobalVoxelCoordinates>(),
                It.IsAny<ItemStack>(), It.IsAny<BlockFace>(), It.IsAny<IDimension>(), It.IsAny<IRemoteClient>()))
                .CallBase();
            testBlockProvider.Setup(x => x.GetCollisionBox(0)).CallBase();

            BlockFace face = BlockFace.PositiveY;
            sbyte heldItemCount = 32;
            ItemStack heldItem = new ItemStack((byte)BlockIDs.Cobblestone, heldItemCount);

            Mock<IRemoteClient> mockUser = new Mock<IRemoteClient>(MockBehavior.Strict);

            //
            // Act
            //
            testBlockProvider.Object.ItemUsedOnBlock(coordinates, heldItem, face, dimension, mockUser.Object);

            //
            // Assertions
            //
            Assert.AreEqual(0, dimension.SetBlockIDCount);
            Assert.AreEqual((byte)BlockIDs.Cobblestone, dimension.GetBlockID(coordinates));
            Assert.AreEqual((byte)BlockIDs.Air, dimension.GetBlockID(coordinates + Vector3i.Up));
        }

        /// <summary>
        /// Tests that a Block can be placed in a voxel occupied by an Item.
        /// </summary>
        /// <remarks>
        /// NOTE: this test depends upon the ItemEntity implementation.
        /// </remarks>
        [Test]
        public void ItemUsedOnBlock_ItemOccupied()
        {
            //
            // Setup
            //
            Mock<IBlockRepository> mockBlockRepository = new Mock<IBlockRepository>(MockBehavior.Strict);
            mockBlockRepository.Setup(x => x.GetBlockProvider((byte)BlockIDs.Cobblestone)).Returns(BuildCobblestoneBlock());

            Mock<IItemRepository> mockItemRepository = new Mock<IItemRepository>(MockBehavior.Strict);

            Mock<IEntityManager> mockEntityManager = new Mock<IEntityManager>(MockBehavior.Strict);

            GlobalVoxelCoordinates coordinates = new GlobalVoxelCoordinates(3, 5, 7);
            FakeDimension dimension = new FakeDimension(mockBlockRepository.Object,
                mockItemRepository.Object, mockEntityManager.Object);
            dimension.SetBlockID(coordinates, (byte)BlockIDs.Cobblestone);
            dimension.ResetCounts();

            ItemEntity itemEntity = new ItemEntity(dimension, mockEntityManager.Object,
                new Vector3(3.375, 6, 7.375), new ItemStack((byte)BlockIDs.Cobblestone, 2));
            mockEntityManager.Setup(x => x.EntitiesInRange(It.IsAny<Vector3>(), It.IsAny<float>()))
                .Returns(new List<IEntity>() { itemEntity });

            int blockPlacedCallCount = 0;
            Mock<BlockProvider> testBlockProvider = new Mock<BlockProvider>(MockBehavior.Strict);
            testBlockProvider.Setup(x => x.ItemUsedOnBlock(It.IsAny<GlobalVoxelCoordinates>(),
                It.IsAny<ItemStack>(), It.IsAny<BlockFace>(), It.IsAny<IDimension>(), It.IsAny<IRemoteClient>()))
                .CallBase();
            testBlockProvider.Setup(x => x.GetCollisionBox(0)).CallBase();
            testBlockProvider.SetupGet(x => x.ID).Returns((byte)BlockIDs.Cobblestone);
            testBlockProvider.Setup(x => x.BlockPlaced(It.IsAny<BlockDescriptor>(),
                It.IsAny<BlockFace>(), It.IsAny<IDimension>(), It.IsAny<IRemoteClient>()))
                .Callback(() => blockPlacedCallCount++)
                .CallBase();
            testBlockProvider.Setup(x => x.IsSupported(It.IsAny<IDimension>(), It.IsAny<BlockDescriptor>()))
                .CallBase();
            testBlockProvider.Setup(x => x.GetSupportDirection(It.IsAny<BlockDescriptor>()))
                .CallBase();

            BlockFace face = BlockFace.PositiveY;
            sbyte heldItemCount = 32;
            short selectedSlot = 6;   // index into Hotbar.
            ItemStack heldItem = new ItemStack((byte)BlockIDs.Cobblestone, heldItemCount);
            ItemStack updatedHeldItem = heldItem;

            Mock<IServerSlot> mockSlot = new Mock<IServerSlot>(MockBehavior.Strict);
            mockSlot.SetupGet(x => x.Item).Returns(updatedHeldItem);
            mockSlot.SetupSet(x => x.Item = heldItem.GetReducedStack(1)).Verifiable();

            Mock<ISlots<IServerSlot>> mockInventory = new Mock<ISlots<IServerSlot>>(MockBehavior.Strict);
            mockInventory.SetupGet(x => x[It.Is<int>(i => i == selectedSlot)]).Returns(mockSlot.Object);

            Mock<IRemoteClient> mockUser = new Mock<IRemoteClient>(MockBehavior.Strict);
            mockUser.Setup(x => x.Inventory).Returns(mockInventory.Object);
            mockUser.Setup(x => x.SelectedSlot).Returns(selectedSlot);

            //
            // Act
            //
            testBlockProvider.Object.ItemUsedOnBlock(coordinates, heldItem, face, dimension, mockUser.Object);

            //
            // Assertions
            //
            Assert.AreEqual(1, dimension.SetBlockIDCount);
            Assert.AreEqual(1, blockPlacedCallCount);
            Assert.AreEqual((byte)BlockIDs.Cobblestone, dimension.GetBlockID(coordinates));
            Assert.AreEqual((byte)BlockIDs.Cobblestone, dimension.GetBlockID(coordinates + Vector3i.Up));
            mockSlot.Verify();    // Asserts that the held item count has been reduced by one.
        }

        /// <summary>
        /// Tests that a Block will not be placed in a voxel occupied by a Player.
        /// </summary>
        /// <remarks>
        /// NOTE: this test depends upon the implementation of the Player class.
        /// </remarks>
        [Test]
        public void ItemUsedOnBlock_PlayerOccupied()
        {
            //
            // Setup
            //
            Mock<IBlockRepository> mockBlockRepository = new Mock<IBlockRepository>(MockBehavior.Strict);
            mockBlockRepository.Setup(x => x.GetBlockProvider((byte)BlockIDs.Cobblestone)).Returns(BuildCobblestoneBlock());

            Mock<IItemRepository> mockItemRepository = new Mock<IItemRepository>(MockBehavior.Strict);

            Mock<IEntityManager> mockEntityManager = new Mock<IEntityManager>(MockBehavior.Strict);

            GlobalVoxelCoordinates coordinates = new GlobalVoxelCoordinates(3, 5, 7);
            FakeDimension dimension = new FakeDimension(mockBlockRepository.Object,
                mockItemRepository.Object, mockEntityManager.Object);
            dimension.SetBlockID(coordinates, (byte)BlockIDs.Cobblestone);
            dimension.ResetCounts();

            PlayerEntity player = new PlayerEntity(dimension, mockEntityManager.Object, "Fred");
            player.Position = new Vector3(3, 6, 7);
            mockEntityManager.Setup(x => x.EntitiesInRange(It.IsAny<Vector3>(), It.IsAny<float>()))
                .Returns(new List<IEntity>() { player });

            Mock<BlockProvider> testBlockProvider = new Mock<BlockProvider>(MockBehavior.Strict);
            testBlockProvider.Setup(x => x.ItemUsedOnBlock(It.IsAny<GlobalVoxelCoordinates>(),
                It.IsAny<ItemStack>(), It.IsAny<BlockFace>(), It.IsAny<IDimension>(), It.IsAny<IRemoteClient>()))
                .CallBase();
            testBlockProvider.Setup(x => x.GetCollisionBox(0)).CallBase();

            BlockFace face = BlockFace.PositiveY;
            sbyte heldItemCount = 32;
            ItemStack heldItem = new ItemStack((byte)BlockIDs.Cobblestone, heldItemCount);

            Mock<IRemoteClient> mockUser = new Mock<IRemoteClient>(MockBehavior.Strict);

            //
            // Act
            //
            testBlockProvider.Object.ItemUsedOnBlock(coordinates, heldItem, face, dimension, mockUser.Object);

            //
            // Assertions
            //
            Assert.AreEqual(0, dimension.SetBlockIDCount);
            Assert.AreEqual((byte)BlockIDs.Cobblestone, dimension.GetBlockID(coordinates));
            Assert.AreEqual((byte)BlockIDs.Air, dimension.GetBlockID(coordinates + Vector3i.Up));
        }
    }
}