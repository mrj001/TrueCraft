using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;

namespace TrueCraft.Core.Logic
{
    public class Discover : IDiscover
    {
        private readonly XmlDocument _doc;

        public Discover()
        {
            _doc = new XmlDocument();

            Assembly thisAssembly = this.GetType().Assembly;
            using (Stream xsd = thisAssembly.GetManifestResourceStream("TrueCraft.Core.Assets.TrueCraft.xsd")!)
                _doc.Schemas.Add(XmlSchema.Read(xsd, null)!);

            using (Stream sz = thisAssembly.GetManifestResourceStream("TrueCraft.Core.Assets.TrueCraft.xml.gz")!)
            using (Stream s = new GZipStream(sz, CompressionMode.Decompress))
            using (XmlReader xmlr = XmlReader.Create(s))
            {
                _doc.Load(xmlr);
                _doc.Validate(null);
            }

        }

        public static IServiceLocator DoDiscovery(IDiscover discoverer)
        {
            IBlockRepository blockRepository = BlockRepository.Init(discoverer);
            IItemRepository itemRepository = ItemRepository.Init(discoverer);
            ICraftingRepository craftingRepository = CraftingRepository.Init(discoverer);

            return new ServiceLocator(blockRepository, itemRepository, craftingRepository);
        }

        public virtual void DiscoverBlockProviders(IRegisterBlockProvider repository)
        {
            List<IBlockProvider> providers = GetBlockProviders();
            foreach (IBlockProvider provider in providers)
                repository.RegisterBlockProvider(provider);
        }

        private List<IBlockProvider> GetBlockProviders()
        {
            List<IBlockProvider> rv = new List<IBlockProvider>(256);
            Assembly thisAssembly = this.GetType().Assembly;
            Type typeBlockProvider = typeof(BlockProvider);
            XmlNode truecraft = _doc["truecraft"]!;
            XmlNode blocks = truecraft["blockrepository"]!;
            foreach (XmlNode blockNode in blocks.ChildNodes)
            {
                XmlNode? behavior = blockNode["behavior"];
                IBlockProvider? instance = null;
                if (behavior is null)
                {
                    instance = (IBlockProvider?)Activator.CreateInstance(typeBlockProvider, new object[] { blockNode });
                }
                else
                {
                    string typeName = behavior.InnerText;
                    // TODO: Find Assembly (which will need to be specified in XML).
                    // TODO: Change First to FirstOrDefault and handle null case.
                    Type typeBehavior = thisAssembly.ExportedTypes.Where(t => t.FullName == typeName).First();
                    instance = (IBlockProvider?)Activator.CreateInstance(typeBehavior, new object[] { blockNode });
                }
                // TODO: If instance is null, it means the developer has done
                //    something incorrectly.  Log a warning.
                if (instance is not null)
                    rv.Add(instance);
            }

            return rv;
        }

        public virtual void DiscoverItemProviders(IRegisterItemProvider repository)
        {
            // Register Block Item Providers
            List<IBlockProvider> blockProviders = GetBlockProviders();
            blockProviders.ForEach(bp =>
            {
                IItemProvider? itemProvider = bp as IItemProvider;
                if (itemProvider is not null)
                    repository.RegisterItemProvider(itemProvider);
            });

            // TODO: add enumeration of other xml files in the same folder as
            //       this Assembly to discover additional items.
            Assembly thisAssembly = this.GetType().Assembly;
            Type typeItemProvider = typeof(ItemProvider);
            XmlNode truecraft = _doc["truecraft"]!;
            XmlNode items = truecraft["itemrepository"]!;
            foreach(XmlNode item in items.ChildNodes)
            {
                XmlNode? behavior = item["behavior"];
                IItemProvider? instance = null;
                if (behavior is null)
                {
                    instance = (IItemProvider?)Activator.CreateInstance(typeItemProvider, new object[] { item });
                }
                else
                {
                    string typeName = behavior.InnerText;
                    // TODO: Find Assembly (which will need to be specified in XML).
                    // TODO: Change First to FirstOrDefault and handle null case.
                    Type typeBehavior = thisAssembly.ExportedTypes.Where(t => t.FullName == typeName).First();
                    instance = (IItemProvider?)Activator.CreateInstance(typeBehavior, new object[] { item });
                }
                // TODO: If instance is null, it means the developer has done
                //    something incorrectly.  Log a warning.
                if (instance is not null)
                    repository.RegisterItemProvider(instance);
            }
        }


        public virtual void DiscoverRecipes(IRegisterRecipe repository)
        {
            // TODO: add enumeration of other xml files in the same folder as
            //       this Assembly to discover additional Crafting Recipes.
            XmlNode truecraft = _doc.ChildNodes.OfType<XmlNode>().Where<XmlNode>(n => n.LocalName == "truecraft").First<XmlNode>();
            XmlNode recipes = truecraft.ChildNodes.OfType<XmlNode>().Where<XmlNode>(n => n.LocalName == "recipes").First<XmlNode>();
            foreach (XmlNode recipe in recipes.ChildNodes)
                repository.RegisterRecipe(new CraftingRecipe(recipe));
        }


    }
}
