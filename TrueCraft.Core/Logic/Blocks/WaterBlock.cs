using System;
using System.Xml;

namespace TrueCraft.Core.Logic.Blocks
{
    public class WaterBlock : FluidBlock
    {
        public WaterBlock(XmlNode node) : base(node)
        {

        }

        protected override double SecondsBetweenUpdates { get { return 0.25; } }

        protected override byte MaximumFluidDepletion { get { return 7; } }

        protected override byte FlowingID { get { return (byte)BlockIDs.Water; } }

        protected override byte StillID { get { return (byte)BlockIDs.WaterStationary; } }
    }

    public class StationaryWaterBlock : WaterBlock
    {
        public StationaryWaterBlock(XmlNode node) : base(node)
        {

        }
    }
}