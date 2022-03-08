using System;

namespace TrueCraft.Core.Logic.Items
{
    public class RawPorkchopItem : FoodItem
    {
        public static readonly short ItemID = 0x13F;

        public override short ID { get { return 0x13F; } }

        public override Tuple<int, int> GetIconTexture(byte metadata)
        {
            return new Tuple<int, int>(7, 5);
        }

        public override float Restores { get { return 1.5f; } }

        public override string GetDisplayName(short metadata)
        {
            return "Raw Porkchop";
        }
    }
}