using System;

namespace TrueCraft.Core.Logic.Items
{
    public class AppleItem : FoodItem
    {
        public static readonly short ItemID = 0x104;

        public override short ID { get { return 0x104; } }

        public override Tuple<int, int> GetIconTexture(byte metadata)
        {
            return new Tuple<int, int>(10, 0);
        }

        public override float Restores { get { return 2; } }

        public override string GetDisplayName(short metadata)
        {
            return "Apple";
        }
    }
}