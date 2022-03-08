using System;

namespace TrueCraft.Core.Logic.Items
{
    public class BoneItem : ItemProvider
    {
        public static readonly short ItemID = 0x160;

        public override short ID { get { return 0x160; } }

        public override Tuple<int, int> GetIconTexture(byte metadata)
        {
            return new Tuple<int, int>(12, 1);
        }

        public override string GetDisplayName(short metadata)
        {
            return "Bone";
        }
    }
}