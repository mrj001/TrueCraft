using System;

namespace TrueCraft.Core.Logic.Items
{
    public class BrickItem : ItemProvider
    {
        public static readonly short ItemID = 0x150;

        public override Tuple<int, int> GetIconTexture(byte metadata)
        {
            return new Tuple<int, int>(6, 1);
        }

        public override short ID { get { return 0x150; } }

        public override string GetDisplayName(short metadata)
        {
            return "Brick";
        }
    }
}