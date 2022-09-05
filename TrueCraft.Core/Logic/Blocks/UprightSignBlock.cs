using System;
using System.Xml;
using TrueCraft.Core.Logic.Items;
using fNbt;
using TrueCraft.Core.Networking.Packets;
using TrueCraft.Core.Networking;
using TrueCraft.Core.World;
using TrueCraft.Core.Server;

namespace TrueCraft.Core.Logic.Blocks
{
    public class UprightSignBlock : BlockProvider
    {
        public UprightSignBlock(XmlNode node) : base(node)
        {

        }

        public override void BlockPlaced(BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            double rotation = user.Entity!.Yaw + 180 % 360;  // TODO: possible order of operations problem.
            if (rotation < 0)
                rotation += 360;

            dimension.SetMetadata(descriptor.Coordinates, (byte)(rotation / 22.5));
        }

        public override void BlockMined(BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            ((IDimensionServer)dimension).SetTileEntity(descriptor.Coordinates, null);
            base.BlockMined(descriptor, face, dimension, user);
        }

        public override void TileEntityLoadedForClient(BlockDescriptor descriptor, IDimension dimension, NbtCompound entity, IRemoteClient client)
        {
            client.QueuePacket(new UpdateSignPacket
            {
                X = descriptor.Coordinates.X,
                Y = (short)descriptor.Coordinates.Y,
                Z = descriptor.Coordinates.Z,
                Text = new[]
                {
                    entity["Text1"].StringValue,
                    entity["Text2"].StringValue,
                    entity["Text3"].StringValue,
                    entity["Text4"].StringValue
                }
            });
        }
    }
}
