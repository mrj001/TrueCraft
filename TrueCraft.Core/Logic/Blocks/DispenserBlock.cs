using System;
using System.Xml;
using TrueCraft.Core.Networking;
using TrueCraft.Core.World;

namespace TrueCraft.Core.Logic.Blocks
{
    public class DispenserBlock : BlockProvider
    {
        public DispenserBlock(XmlNode node) : base(node)
        {

        }

        public override void BlockPlaced(BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            dimension.SetMetadata(descriptor.Coordinates, (byte)MathHelper.DirectionByRotationFlat(user.Entity!.Yaw, true));
        }
    }
}