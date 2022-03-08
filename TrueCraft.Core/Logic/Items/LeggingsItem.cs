﻿using System;

namespace TrueCraft.Core.Logic.Items
{
    public abstract class LeggingsItem : ArmorItem
    {
        public override sbyte MaximumStack { get { return 1; } }
    }

    public class LeatherPantsItem : LeggingsItem
    {
        public static readonly short ItemID = 0x12C;

        public override short ID { get { return 0x12C; } }

        public override Tuple<int, int> GetIconTexture(byte metadata)
        {
            return new Tuple<int, int>(0, 2);
        }

        public override ArmorMaterial Material { get { return ArmorMaterial.Leather; } }

        public override short BaseDurability { get { return 46; } }

        public override float BaseArmor { get { return 3; } }

        public override string GetDisplayName(short metadata)
        {
            return "Leather Pants";
        }
    }

    public class IronLeggingsItem : LeggingsItem
    {
        public static readonly short ItemID = 0x134;

        public override short ID { get { return 0x134; } }

        public override Tuple<int, int> GetIconTexture(byte metadata)
        {
            return new Tuple<int, int>(2, 2);
        }

        public override ArmorMaterial Material { get { return ArmorMaterial.Iron; } }

        public override short BaseDurability { get { return 184; } }

        public override float BaseArmor { get { return 3; } }

        public override string GetDisplayName(short metadata)
        {
            return "Iron Leggings";
        }
    }

    public class GoldenLeggingsItem : LeggingsItem
    {
        public static readonly short ItemID = 0x13C;

        public override short ID { get { return 0x13C; } }

        public override Tuple<int, int> GetIconTexture(byte metadata)
        {
            return new Tuple<int, int>(4, 2);
        }

        public override ArmorMaterial Material { get { return ArmorMaterial.Gold; } }

        public override short BaseDurability { get { return 92; } }

        public override float BaseArmor { get { return 3; } }

        public override string GetDisplayName(short metadata)
        {
            return "Golden Leggings";
        }
    }

    public class DiamondLeggingsItem : LeggingsItem
    {
        public static readonly short ItemID = 0x138;

        public override short ID { get { return 0x138; } }

        public override Tuple<int, int> GetIconTexture(byte metadata)
        {
            return new Tuple<int, int>(3, 2);
        }

        public override ArmorMaterial Material { get { return ArmorMaterial.Diamond; } }

        public override short BaseDurability { get { return 368; } }

        public override float BaseArmor { get { return 3; } }

        public override string GetDisplayName(short metadata)
        {
            return "Diamond Leggings";
        }
    }

    public class ChainLeggingsItem : ArmorItem // Not HelmentItem because it can't inherit the recipe
    {
        public static readonly short ItemID = 0x130;

        public override short ID { get { return 0x130; } }

        public override Tuple<int, int> GetIconTexture(byte metadata)
        {
            return new Tuple<int, int>(1, 2);
        }

        public override ArmorMaterial Material { get { return ArmorMaterial.Chain; } }

        public override short BaseDurability { get { return 92; } }

        public override float BaseArmor { get { return 3; } }

        public override string GetDisplayName(short metadata)
        {
            return "Chain Leggings";
        }
    }
}