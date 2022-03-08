using System;

namespace TrueCraft.Core.Logic.Items
{
    public class CoalItem : ItemProvider, IBurnableItem
    {
        public enum MetaData : short
        {
            Coal = 0,
            Charcoal = 1
        }

        public static readonly short ItemID = 0x107;

        public override short ID { get { return 0x107; } }

        public override Tuple<int, int> GetIconTexture(byte metadata)
        {
            return new Tuple<int, int>(7, 0);
        }

        public override string GetDisplayName(short metadata)
        {
            if ((MetaData)metadata == MetaData.Charcoal)
                return "Charcoal";

            return "Coal";
        }

        public TimeSpan BurnTime { get { return TimeSpan.FromSeconds(80); } }
    }
}