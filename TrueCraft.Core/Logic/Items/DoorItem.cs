using System;
using System.Xml;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.Networking;
using TrueCraft.Core.Server;
using TrueCraft.Core.World;

namespace TrueCraft.Core.Logic.Items
{
    public abstract class DoorItem : ItemProvider
    {
        [Flags]
        public enum DoorFlags
        {
            Northeast = 0x0,
            Southeast = 0x1,
            Southwest = 0x2,
            Northwest = 0x3,
            Lower = 0x0,
            Upper = 0x8,
            Closed = 0x0,
            Open = 0x4
        }

        public DoorItem(XmlNode node) : base(node)
        {
        }

        protected abstract byte BlockID { get; }

        public override void ItemUsedOnBlock(GlobalVoxelCoordinates coordinates, ItemStack item, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            ServerOnly.Assert();

            var bottom = coordinates + MathHelper.BlockFaceToCoordinates(face);
            var top = bottom + Vector3i.Up;
            if (dimension.GetBlockID(top) != 0 || dimension.GetBlockID(bottom) != 0)
                return;
            DoorFlags direction;
            switch (MathHelper.DirectionByRotationFlat(user.Entity!.Yaw))
            {
                case Direction.North:
                    direction = DoorFlags.Northwest;
                    break;
                case Direction.South:
                    direction = DoorFlags.Southeast;
                    break;
                case Direction.East:
                    direction = DoorFlags.Northeast;
                    break;
                default: // Direction.West:
                    direction = DoorFlags.Southwest;
                    break;
            }
            user.Server.BlockUpdatesEnabled = false;
            dimension.SetBlockID(bottom, BlockID);
            dimension.SetMetadata(bottom, (byte)direction);
            dimension.SetBlockID(top, BlockID);
            dimension.SetMetadata(top, (byte)(direction | DoorFlags.Upper));
            user.Server.BlockUpdatesEnabled = true;
            item.Count--;
            user.Hotbar[user.SelectedSlot].Item = item;
        }
    }

    public class IronDoorItem : DoorItem
    {
        public static readonly short ItemID = 0x14A;

        public IronDoorItem(XmlNode node) : base(node)
        {
        }

        protected override byte BlockID { get { return (byte)BlockIDs.IronDoor; } }
    }

    public class WoodenDoorItem : DoorItem
    {
        public static readonly short ItemID = 0x144;

        public WoodenDoorItem(XmlNode node) : base(node)
        {
        }

        protected override byte BlockID { get { return (byte)BlockIDs.WoodenDoor; } }
    }
}