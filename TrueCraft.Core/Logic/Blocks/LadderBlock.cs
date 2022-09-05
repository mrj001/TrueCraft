using System;
using System.Xml;
using TrueCraft.Core.World;
using TrueCraft.Core.Networking;
using TrueCraft.Core.Server;

namespace TrueCraft.Core.Logic.Blocks
{
    public class LadderBlock : BlockProvider
    {
        /// <summary>
        /// The side of the block that this ladder is attached to (i.e. "the north side")
        /// </summary>
        public enum LadderDirection
        {
            East = 0x04,
            West = 0x05,
            North = 0x03,
            South = 0x02
        }

        public LadderBlock(XmlNode node) : base(node)
        {

        }

        public override Vector3i GetSupportDirection(BlockDescriptor descriptor)
        {
            switch ((LadderDirection)descriptor.Metadata)
            {
                case LadderDirection.East:
                    return Vector3i.East;
                case LadderDirection.West:
                    return Vector3i.West;
                case LadderDirection.North:
                    return Vector3i.North;
                case LadderDirection.South:
                    return Vector3i.South;
                default:
                    return Vector3i.Zero;
            }
        }

        public override void ItemUsedOnBlock(GlobalVoxelCoordinates coordinates, ItemStack item, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            ServerOnly.Assert();

            coordinates += MathHelper.BlockFaceToCoordinates(face);
            var descriptor = dimension.GetBlockData(coordinates);
            LadderDirection direction;
            switch (MathHelper.DirectionByRotationFlat(user.Entity!.Yaw))
            {
                case Direction.North:
                    direction = LadderDirection.North;
                    break;
                case Direction.South:
                    direction = LadderDirection.South;
                    break;
                case Direction.East:
                    direction = LadderDirection.East;
                    break;
                default:
                    direction = LadderDirection.West;
                    break;
            }
            descriptor.Metadata = (byte)direction;
            if (IsSupported(dimension, descriptor))
            {
                dimension.SetBlockID(descriptor.Coordinates, (byte)BlockIDs.Ladder);
                dimension.SetMetadata(descriptor.Coordinates, (byte)direction);
                item.Count--;
                user.Hotbar[user.SelectedSlot].Item = item;
            }
        }
    }
}