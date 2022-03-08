using System;

namespace TrueCraft.Core.Logic.Items
{
    public class EggItem : ItemProvider
    {
        public static readonly short ItemID = 0x158;

        public override short ID { get { return 0x158; } }

        public override Tuple<int, int> GetIconTexture(byte metadata)
        {
            return new Tuple<int, int>(12, 0);
        }

        public override sbyte MaximumStack { get { return 16; } }

        public override string GetDisplayName(short metadata)
        {
            return "Egg";
        }
    }
}