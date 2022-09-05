using System;
using TrueCraft.Core.Logic.Items;
using TrueCraft.Core.Networking.Packets;
using fNbt;
using TrueCraft.Core.Networking;
using TrueCraft.Core.World;
using TrueCraft.Core.Server;
using System.Xml;

namespace TrueCraft.Core.Logic.Blocks
{
    public class WallSignBlock : BlockProvider
    {
        public WallSignBlock(XmlNode node) : base(node)
        {

        }

        public override void BlockPlaced(BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            dimension.SetMetadata(descriptor.Coordinates, (byte)MathHelper.DirectionByRotationFlat(user.Entity!.Yaw, true));
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
