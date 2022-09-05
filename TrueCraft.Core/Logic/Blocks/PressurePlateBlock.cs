using System;
using System.Xml;

namespace TrueCraft.Core.Logic.Blocks
{
    public abstract class PressurePlateBlock : BlockProvider
    {
        protected PressurePlateBlock(XmlNode node) : base(node)
        {

        }
    }

    public class WoodenPressurePlateBlock : PressurePlateBlock
    {
        public WoodenPressurePlateBlock(XmlNode node) : base(node)
        {

        }

        // TODO: Redstone behaviour
    }

    public class StonePressurePlateBlock : PressurePlateBlock
    {
        public StonePressurePlateBlock(XmlNode node) : base(node)
        {

        }

        // TODO: Redstone behaviour
    }
}