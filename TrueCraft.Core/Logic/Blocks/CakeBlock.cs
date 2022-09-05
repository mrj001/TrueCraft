using System;
using System.Xml;
using TrueCraft.Core.World;
using TrueCraft.Core.Networking;

namespace TrueCraft.Core.Logic.Blocks
{
    public class CakeBlock : BlockProvider
    {
        public CakeBlock(XmlNode node) : base(node)
        {
        }

        public override bool BlockRightClicked(IServiceLocator serviceLocator,
            BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            if (descriptor.Metadata == 5)
                dimension.SetBlockID(descriptor.Coordinates, (byte)BlockIDs.Air);
            else
                dimension.SetMetadata(descriptor.Coordinates, (byte)(descriptor.Metadata + 1));
            return false;
        }
    }
}