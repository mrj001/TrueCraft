using System;
using System.Xml;
using TrueCraft.Core.Networking;
using TrueCraft.Core.World;

namespace TrueCraft.Core.Logic.Blocks
{
    public class PistonBlock : BlockProvider
    {
        public PistonBlock(XmlNode node) : base(node)
        {

        }

        public override void BlockPlaced(BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            dimension.SetMetadata(descriptor.Coordinates,
                (byte)MathHelper.DirectionByRotation(user.Entity!.Position, user.Entity.Yaw,
                descriptor.Coordinates, true));
        }

        // TODO: Redstone behaviour
    }

    public class StickyPistonBlock : BlockProvider
    {
        public StickyPistonBlock(XmlNode node) : base(node)
        {

        }

        public override void BlockPlaced(BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            dimension.SetMetadata(descriptor.Coordinates,
                (byte)MathHelper.DirectionByRotation(user.Entity!.Position, user.Entity.Yaw,
                descriptor.Coordinates, true));
        }

        // TODO: Redstone behaviour
    }
}
