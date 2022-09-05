using System;
using System.Xml;
using TrueCraft.Core.Server;
using TrueCraft.Core.World;
using TrueCraft.Core.Logic.Items;
using TrueCraft.Core.Networking;

namespace TrueCraft.Core.Logic.Blocks
{
    public abstract class DoorBlock : BlockProvider
    {
        public DoorBlock(XmlNode node) : base(node)
        {

        }

        public override void BlockUpdate(BlockDescriptor descriptor, BlockDescriptor source, IMultiplayerServer server, IDimension dimension)
        {
            bool upper = ((DoorItem.DoorFlags)descriptor.Metadata & DoorItem.DoorFlags.Upper) == DoorItem.DoorFlags.Upper;
            var other = upper ? Vector3i.Down : Vector3i.Up;
            if (dimension.GetBlockID(descriptor.Coordinates + other) != ID)
                dimension.SetBlockID(descriptor.Coordinates, 0);
        }

        // TODO Redstone
    }

    public class WoodenDoorBlock : DoorBlock
    {
        public WoodenDoorBlock(XmlNode node) : base(node)
        {

        }

        public override void BlockLeftClicked(IServiceLocator serviceLocator,
            BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            BlockRightClicked(serviceLocator, descriptor, face, dimension, user);
        }

        public override bool BlockRightClicked(IServiceLocator serviceLocator,
            BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            bool upper = ((DoorItem.DoorFlags)descriptor.Metadata & DoorItem.DoorFlags.Upper) == DoorItem.DoorFlags.Upper;
            var other = upper ? Vector3i.Down : Vector3i.Up;
            var otherMeta = dimension.GetMetadata(descriptor.Coordinates + other);
            dimension.SetMetadata(descriptor.Coordinates, (byte)(descriptor.Metadata ^ (byte)DoorItem.DoorFlags.Open));
            dimension.SetMetadata(descriptor.Coordinates + other, (byte)(otherMeta ^ (byte)DoorItem.DoorFlags.Open));
            return false;
        }
    }

    public class IronDoorBlock : DoorBlock
    {
        public IronDoorBlock(XmlNode node) : base(node)
        {

        }
    }
}