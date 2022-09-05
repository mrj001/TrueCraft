using System;
using System.Xml;

namespace TrueCraft.Core.Logic.Blocks
{
    public class LavaBlock : FluidBlock
    {
        public LavaBlock(XmlNode node) : this(node, false)
        {
        }

        public LavaBlock(XmlNode node, bool nether) : base(node)
        {
            // TODO: need a better way of setting this.
            if (nether)
                _MaximumFluidDepletion = 7;
            else
                _MaximumFluidDepletion = 3;
        }

        protected override bool AllowSourceCreation { get { return false; } }

        protected override double SecondsBetweenUpdates { get { return 2; } }

        private byte _MaximumFluidDepletion { get; set; }
        protected override byte MaximumFluidDepletion { get { return _MaximumFluidDepletion; } }

        protected override byte FlowingID { get { return (byte)BlockIDs.Lava; } }

        protected override byte StillID { get { return (byte)BlockIDs.LavaStationary; } }
    }

    public class StationaryLavaBlock : LavaBlock
    {
        public StationaryLavaBlock(XmlNode node) : base(node)
        {

        }
    }
}