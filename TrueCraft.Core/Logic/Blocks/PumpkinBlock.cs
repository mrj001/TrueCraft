using System;
using System.Xml;
using TrueCraft.Core.World;
using TrueCraft.Core.Networking;

namespace TrueCraft.Core.Logic.Blocks
{
    public class PumpkinBlock : BlockProvider
    {
        public PumpkinBlock(XmlNode node) : base(node)
        {

        }

        public override void BlockPlaced(BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            dimension.SetMetadata(descriptor.Coordinates, (byte)MathHelper.DirectionByRotationFlat(user.Entity!.Yaw, true));
        }
    }
}