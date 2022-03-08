using System;

namespace TrueCraft.Core.Logic.Items
{
    public class GoldIngotItem : ItemProvider
    {
        public static readonly short ItemID = 0x10A;

        public override short ID { get { return 0x10A; } }

        public override Tuple<int, int> GetIconTexture(byte metadata)
        {
            return new Tuple<int, int>(7, 2);
        }

        public override string GetDisplayName(short metadata)
        {
            return "Gold Ingot";
        }
    }
}