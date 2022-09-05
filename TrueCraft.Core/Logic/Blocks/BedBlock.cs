using System;
using System.Xml;
using TrueCraft.Core.Logic.Items;
using TrueCraft.Core.Server;
using TrueCraft.Core.World;

namespace TrueCraft.Core.Logic.Blocks
{
    public class BedBlock : BlockProvider
    {
        [Flags]
        public enum BedDirection : byte
        {
            South =  0x0,
            West = 0x1,
            North =  0x2,
            East = 0x3,
        }

        [Flags]
        public enum BedType : byte
        {
            Foot = 0x0,
            Head = 0x8,
        }

        public BedBlock(XmlNode node) : base(node)
        {

        }
            
        public bool ValidBedPosition(BlockDescriptor descriptor, IBlockRepository repository, IDimension dimension, bool checkNeighbor = true, bool checkSupport = false)
        {
            if (checkNeighbor)
            {
                var other = Vector3i.Zero;
                switch ((BedDirection)(descriptor.Metadata & 0x3))
                {
                    case BedDirection.East:
                        other = Vector3i.East;
                        break;
                    case BedDirection.West:
                        other = Vector3i.West;
                        break;
                    case BedDirection.North:
                        other = Vector3i.North;
                        break;
                    case BedDirection.South:
                        other = Vector3i.South;
                        break;
                }
                if ((descriptor.Metadata & (byte)BedType.Head) == (byte)BedType.Head)
                    other = -other;
                if (dimension.GetBlockID(descriptor.Coordinates + other) != (byte)BlockIDs.Bed)
                    return false;
            }
            if (checkSupport)
            {
                var supportingBlock = repository.GetBlockProvider(dimension.GetBlockID(descriptor.Coordinates + Vector3i.Down));
                if (!supportingBlock.Opaque)
                    return false;
            }
            return true;
        }

        public override void BlockUpdate(BlockDescriptor descriptor, BlockDescriptor source, IMultiplayerServer server, IDimension dimension)
        {
            if (!ValidBedPosition(descriptor, dimension.BlockRepository, dimension))
                dimension.SetBlockID(descriptor.Coordinates, 0);
            base.BlockUpdate(descriptor, source, server, dimension);
        }
    }
}