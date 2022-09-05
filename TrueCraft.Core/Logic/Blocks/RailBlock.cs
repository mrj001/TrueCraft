using System;
using System.Xml;

namespace TrueCraft.Core.Logic.Blocks
{
    public class RailBlock : BlockProvider
    {
        public RailBlock(XmlNode node) : base(node)
        {

        }

        // TODO Behaviour
    }

    public class PoweredRailBlock : RailBlock
    {
        public PoweredRailBlock(XmlNode node) : base(node)
        {

        }
    }

    public class DetectorRailBlock : RailBlock
    {
        public DetectorRailBlock(XmlNode node) : base(node)
        {

        }
    }
}