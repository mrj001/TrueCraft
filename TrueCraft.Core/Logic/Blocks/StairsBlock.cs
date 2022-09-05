using System;
using System.Xml;
using TrueCraft.Core.Networking;
using TrueCraft.Core.World;

namespace TrueCraft.Core.Logic.Blocks
{
    public abstract class StairsBlock : BlockProvider
    {
        public enum StairDirection
        {
            East = 0,
            West = 1,
            South = 2,
            North = 3
        }

        protected StairsBlock(XmlNode node) : base(node)
        {

        }

        public override void BlockPlaced(BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            byte meta = 0;
            switch (MathHelper.DirectionByRotationFlat(user.Entity!.Yaw))
            {
                case Direction.East:
                    meta = (byte)StairDirection.East;
                    break;
                case Direction.West:
                    meta = (byte)StairDirection.West;
                    break;
                case Direction.North:
                    meta = (byte)StairDirection.North;
                    break;
                case Direction.South:
                    meta = (byte)StairDirection.South;
                    break;
                default:
                    meta = 0; // Should never happen
                    break;
            }
            dimension.SetMetadata(descriptor.Coordinates, meta);
        }
    }

    public class WoodenStairsBlock : StairsBlock, IBurnableItem
    {
        public WoodenStairsBlock(XmlNode node) : base(node)
        {

        }

        public TimeSpan BurnTime { get { return TimeSpan.FromSeconds(15); } }
    }

    public class StoneStairsBlock : StairsBlock
    {
        public StoneStairsBlock(XmlNode node) : base(node)
        {

        }
    }
}