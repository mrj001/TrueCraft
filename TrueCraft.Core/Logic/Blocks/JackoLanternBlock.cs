using System;
using System.Xml;
using TrueCraft.Core.World;
using TrueCraft.Core.Networking;

namespace TrueCraft.Core.Logic.Blocks
{
    public class JackoLanternBlock : BlockProvider
    {
        public JackoLanternBlock(XmlNode node) : base(node)
        {

        }

        // TODO: behaviour: can't be place on glass or ice.
        public override void BlockPlaced(BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            dimension.SetMetadata(descriptor.Coordinates, (byte)MathHelper.DirectionByRotationFlat(user.Entity!.Yaw, true));
        }
    }
}