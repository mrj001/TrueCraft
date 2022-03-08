using System;

namespace TrueCraft.Core.Logic.Items
{
    public class LeatherItem : ItemProvider
    {
        public static readonly short ItemID = 0x14E;

        public override short ID { get { return 0x14E; } }

        public override Tuple<int, int> GetIconTexture(byte metadata)
        {
            return new Tuple<int, int>(7, 6);
        }

        public override string GetDisplayName(short metadata)
        {
            return "Leather";
        }
    }
}