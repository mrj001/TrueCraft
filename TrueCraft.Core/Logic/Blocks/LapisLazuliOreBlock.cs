using System;
using System.Xml;
using TrueCraft.Core.Logic.Items;

namespace TrueCraft.Core.Logic.Blocks
{
    public class LapisLazuliOreBlock : BlockProvider
    {
        public LapisLazuliOreBlock(XmlNode node) : base(node)
        {

        }

        // TODO: Can you smelt Lapis Lazuli Ore?
        //public ItemStack SmeltingOutput { get { return new ItemStack(); } } // TODO: Metadata
    }
}