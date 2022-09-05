using System;
using System.IO;
using System.Xml;
using Moq;
using NUnit.Framework;
using TrueCraft.Core.Logic;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.Logic.Items;

namespace TrueCraft.Core.Test
{
    [SetUpFixture]
    public class CoreSetup
    {
        // The test framework will make calls to this class that will
        // initialize these members prior to running tests.  So we can get
        // away with "faking" them as non-nullable.
        private static IBlockRepository _blockRepository = null!;
        private static IItemRepository _itemRepository = null!;
        private static ICraftingRepository _craftingRepository = null!;

        public CoreSetup()
        {
        }

        #region Mock Blocks

        private static IBlockProvider MockBlock(byte id)
        {
            Mock<IBlockProvider> rv = new(MockBehavior.Strict);
            rv.Setup(x => x.ID).Returns(id);

            return rv.Object;
        }

        public static IBlockProvider MockAirBlock
        {
            get
            {
                return MockBlock((byte)BlockIDs.Air);
            }
        }

        public static IBlockProvider MockStoneBlock
        {
            get
            {
                return MockBlock((byte)BlockIDs.Stone);
            }
        }

        public static IBlockProvider MockGrassBlock
        {
            get
            {
                return MockBlock((byte)BlockIDs.Grass);
            }
        }

        public static IBlockProvider MockDirtBlock
        {
            get
            {
                return MockBlock((byte)BlockIDs.Dirt);
            }
        }

        public static IBlockProvider MockCobbleStoneBlock
        {
            get
            {
                return MockBlock((byte)BlockIDs.Cobblestone);
            }
        }

        public static IBlockProvider MockBedrockBlock
        {
            get
            {
                return MockBlock((byte)BlockIDs.Bedrock);
            }
        }

        public static IBlockProvider MockLeavesBlock
        {
            get
            {
                return MockBlock((byte)BlockIDs.Leaves);
            }
        }
        #endregion

        #region Mock Items
        public static IItemProvider MockStoneItem
        {
            get
            {
                Mock<IItemProvider> rv = new(MockBehavior.Strict);

                rv.Setup(x => x.ID).Returns((short)BlockIDs.Stone);

                return rv.Object;
            }
        }

        public static IItemProvider MockGrassItem
        {
            get
            {
                Mock<IItemProvider> rv = new(MockBehavior.Strict);

                rv.Setup(x => x.ID).Returns((short)BlockIDs.Grass);

                return rv.Object;
            }
        }

        public static IItemProvider MockDirtItem
        {
            get
            {
                Mock<IItemProvider> rv = new(MockBehavior.Strict);

                rv.Setup(x => x.ID).Returns((short)BlockIDs.Dirt);

                return rv.Object;
            }
        }

        public static IItemProvider MockCobblestoneItem
        {
            get
            {
                Mock<IItemProvider> rv = new(MockBehavior.Strict);

                rv.Setup(x => x.ID).Returns((short)BlockIDs.Cobblestone);

                return rv.Object;
            }
        }

        public static IItemProvider MockLavaItem
        {
            get
            {
                Mock<IItemProvider> rv = new(MockBehavior.Strict);

                rv.Setup(x => x.ID).Returns((short)BlockIDs.LavaStationary);

                return rv.Object;
            }
        }

        public static IItemProvider MockSandItem
        {
            get
            {
                Mock<IItemProvider> rv = new(MockBehavior.Strict);

                rv.Setup(x => x.ID).Returns((short)BlockIDs.Sand);

                return rv.Object;
            }
        }
        #endregion

        // BlockProviderTest, WorldLighterTest, PhysicsEngineTest, and CraftingAreaTest depend upon
        // having some blocks and items available in their repositories.
        private class MockDiscover : IDiscover
        {
            public void DiscoverBlockProviders(IRegisterBlockProvider repository)
            {
                repository.RegisterBlockProvider(MockGrassBlock);
                repository.RegisterBlockProvider(MockDirtBlock);
                repository.RegisterBlockProvider(MockStoneBlock);
                repository.RegisterBlockProvider(MockAirBlock);
                repository.RegisterBlockProvider(MockBedrockBlock);
                repository.RegisterBlockProvider(MockLeavesBlock);
                repository.RegisterBlockProvider(MockCobbleStoneBlock);
            }

            public void DiscoverItemProviders(IRegisterItemProvider repository)
            {
                repository.RegisterItemProvider(MockLavaItem);  // Item ID 10
                repository.RegisterItemProvider(MockSandItem);  // Item ID 12
                repository.RegisterItemProvider(MockStoneItem); // Item ID 1
                repository.RegisterItemProvider(MockGrassItem); // Item ID 2
                repository.RegisterItemProvider(MockDirtItem);  // Item ID 3
                repository.RegisterItemProvider(MockCobblestoneItem);  // Item ID 4

                string xmlSnowBall = @"    <item>
      <id>332</id>
      <maximumstack>16</maximumstack>
      <visiblemetadata>
        <metadata>
          <value>0</value>
        <displayname>Snowball</displayname>
        <icontexture>
          <x>14</x>
          <y>0</y>
        </icontexture>
        </metadata>
      </visiblemetadata>
    </item>
";
                repository.RegisterItemProvider(new SnowballItem(GetTopNode(xmlSnowBall)));
            }

            private static XmlNode GetTopNode(string xml)
            {
                XmlDocument doc = new XmlDocument();
                using (StringReader sr = new StringReader(xml))
                using (XmlReader xmlr = XmlReader.Create(sr))
                    doc.Load(xmlr);

                return doc.FirstChild!;
            }

            public void DiscoverRecipes(IRegisterRecipe repository)
            {
                string[] recipeXML = new string[]
                {
                    // Sticks
                    @"<recipe>
      <pattern>
        <r>
          <c>
            <id>5</id>
            <count>1</count>
          </c>
        </r>
        <r>
          <c>
            <id>5</id>
            <count>1</count>
          </c>
        </r>
      </pattern>
      <output>
        <id>280</id>
        <count>4</count>
      </output>
    </recipe>
",
                    // Stone shovel
                    @"<recipe>
      <pattern>
        <r>
          <c>
            <id>4</id>
            <count>1</count>
          </c>
        </r>
        <r>
          <c>
            <id>280</id>
            <count>1</count>
          </c>
        </r>
        <r>
          <c>
            <id>280</id>
            <count>1</count>
          </c>
        </r>
      </pattern>
      <output>
        <id>273</id>
        <count>1</count>
      </output>
    </recipe>
",
                    // Stone Hoe
                    @"<recipe>
      <pattern>
        <r>
          <c>
            <id>4</id>
            <count>1</count>
          </c>
          <c>
            <id>4</id>
            <count>1</count>
          </c>
        </r>
        <r>
          <c>
            <id>-1</id>
            <count>1</count>
          </c>
          <c>
            <id>280</id>
            <count>1</count>
          </c>
        </r>
        <r>
          <c>
            <id>-1</id>
            <count>1</count>
          </c>
          <c>
            <id>280</id>
            <count>1</count>
          </c>
        </r>
      </pattern>
      <output>
        <id>291</id>
        <count>1</count>
      </output>
    </recipe>
",
                    // Stone PickAxe
                    @"<recipe>
      <pattern>
        <r>
          <c>
            <id>4</id>
            <count>1</count>
          </c>
          <c>
            <id>4</id>
            <count>1</count>
          </c>
          <c>
            <id>4</id>
            <count>1</count>
          </c>
        </r>
        <r>
          <c>
            <id>-1</id>
            <count>1</count>
          </c>
          <c>
            <id>280</id>
            <count>1</count>
          </c>
          <c>
            <id>-1</id>
            <count>1</count>
          </c>
        </r>
        <r>
          <c>
            <id>-1</id>
            <count>1</count>
          </c>
          <c>
            <id>280</id>
            <count>1</count>
          </c>
          <c>
            <id>-1</id>
            <count>1</count>
          </c>
        </r>
      </pattern>
      <output>
        <id>274</id>
        <count>1</count>
      </output>
    </recipe>"
                };

                for (int j = 0; j < recipeXML.Length; j ++)
                {
                    XmlDocument doc= new XmlDocument();
                    doc.LoadXml(recipeXML[j]);
                    XmlNode item = doc.DocumentElement!;

                    repository.RegisterRecipe(new CraftingRecipe(item));
                }
            }
        }

        public static IBlockRepository BlockRepository { get => _blockRepository; }
        public static IItemRepository ItemRepository { get => _itemRepository; }
        public static ICraftingRepository CraftingRepository { get => _craftingRepository; }

        [OneTimeSetUp]
        public void SetupRepositories()
        {
            IDiscover discover = new MockDiscover();
            _blockRepository = TrueCraft.Core.Logic.BlockRepository.Init(discover);
            _itemRepository = TrueCraft.Core.Logic.ItemRepository.Init(discover);
            _craftingRepository =  TrueCraft.Core.Logic.CraftingRepository.Init(discover);
        }
    }
}
