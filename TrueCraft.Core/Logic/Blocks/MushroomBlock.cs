using System;
using System.Xml;

namespace TrueCraft.Core.Logic.Blocks
{

    public class MushroomBlock : BlockProvider
    {
        public MushroomBlock(XmlNode node) : base(node)
        {

        }

        // TODO: Spread
        // TODO: food item
        // TODO: block update: mushroom pops based on light level
    }
}