﻿using System;

namespace TrueCraft.Core.Logic.Items
{
    public abstract class BootsItem : ArmorItem
    {
        public override sbyte MaximumStack { get { return 1; } }
    }

    public class LeatherBootsItem : BootsItem
    {
        public static readonly short ItemID = 0x12D;

        public override short ID { get { return 0x12D; } }

        public override Tuple<int, int> GetIconTexture(byte metadata)
        {
            return new Tuple<int, int>(0, 3);
        }

        public override ArmorMaterial Material { get { return ArmorMaterial.Leather; } }

        public override short BaseDurability { get { return 40; } }

        public override float BaseArmor { get { return 1.5f; } }

        public override string GetDisplayName(short metadata)
        {
            return "Leather Boots";
        }
    }

    public class IronBootsItem : BootsItem
    {
        public static readonly short ItemID = 0x135;

        public override short ID { get { return 0x135; } }

        public override Tuple<int, int> GetIconTexture(byte metadata)
        {
            return new Tuple<int, int>(2, 3);
        }

        public override ArmorMaterial Material { get { return ArmorMaterial.Iron; } }

        public override short BaseDurability { get { return 160; } }

        public override float BaseArmor { get { return 1.5f; } }

        public override string GetDisplayName(short metadata)
        {
            return "Iron Boots";
        }
    }

    public class GoldenBootsItem : BootsItem
    {
        public static readonly short ItemID = 0x13D;

        public override short ID { get { return 0x13D; } }

        public override Tuple<int, int> GetIconTexture(byte metadata)
        {
            return new Tuple<int, int>(4, 3);
        }

        public override ArmorMaterial Material { get { return ArmorMaterial.Gold; } }

        public override short BaseDurability { get { return 80; } }

        public override float BaseArmor { get { return 1.5f; } }

        public override string GetDisplayName(short metadata)
        {
            return "Golden Boots";

        }
    }

    public class DiamondBootsItem : BootsItem
    {
        public static readonly short ItemID = 0x139;

        public override short ID { get { return 0x139; } }

        public override Tuple<int, int> GetIconTexture(byte metadata)
        {
            return new Tuple<int, int>(3, 3);
        }

        public override ArmorMaterial Material { get { return ArmorMaterial.Diamond; } }

        public override short BaseDurability { get { return 320; } }

        public override float BaseArmor { get { return 1.5f; } }

        public override string GetDisplayName(short metadata)
        {
            return "Diamond Boots";

        }
    }

    public class ChainBootsItem : ArmorItem // Not HelmentItem because it can't inherit the recipe
    {
        public static readonly short ItemID = 0x131;

        public override short ID { get { return 0x131; } }

        public override Tuple<int, int> GetIconTexture(byte metadata)
        {
            return new Tuple<int, int>(1, 3);
        }

        public override ArmorMaterial Material { get { return ArmorMaterial.Chain; } }

        public override short BaseDurability { get { return 79; } }

        public override float BaseArmor { get { return 1.5f; } }

        public override string GetDisplayName(short metadata)
        {
            return "Chain Boots";
        }
    }
}