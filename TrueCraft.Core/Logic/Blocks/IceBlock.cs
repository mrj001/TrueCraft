using System;
using TrueCraft.Core.World;
using TrueCraft.Core.Networking;
using System.Xml;

namespace TrueCraft.Core.Logic.Blocks
{
    public class IceBlock : BlockProvider
    {
        public IceBlock(XmlNode node) : base(node)
        {

        }

        public override void BlockMined(BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            dimension.SetBlockID(descriptor.Coordinates, (byte)BlockIDs.Water);
            dimension.BlockRepository.GetBlockProvider((byte)BlockIDs.Water).BlockPlaced(descriptor, face, dimension, user);
        }
    }
}