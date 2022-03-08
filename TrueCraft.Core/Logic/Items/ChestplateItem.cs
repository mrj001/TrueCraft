﻿using System;

namespace TrueCraft.Core.Logic.Items
{
    public abstract class ChestplateItem : ArmorItem
    {
        public override sbyte MaximumStack { get { return 1; } }
    }

    public class LeatherTunicItem : ChestplateItem
    {
        public static readonly short ItemID = 0x12B;

        public override short ID { get { return 0x12B; } }

        public override Tuple<int, int> GetIconTexture(byte metadata)
        {
            return new Tuple<int, int>(0, 1);
        }

        public override ArmorMaterial Material { get { return ArmorMaterial.Leather; } }

        public override short BaseDurability { get { return 49; } }

        public override float BaseArmor { get { return 4; } }

        public override string GetDisplayName(short metadata)
        {
            return "Leather Tunic";
        }
    }

    public class IronChestplateItem : ChestplateItem
    {
        public static readonly short ItemID = 0x133;

        public override short ID { get { return 0x133; } }

        public override Tuple<int, int> GetIconTexture(byte metadata)
        {
            return new Tuple<int, int>(2, 1);
        }

        public override ArmorMaterial Material { get { return ArmorMaterial.Iron; } }

        public override short BaseDurability { get { return 192; } }

        public override float BaseArmor { get { return 4; } }

        public override string GetDisplayName(short metadata)
        {
            return "Iron Chestplate";
        }
    }

    public class GoldenChestplateItem : ChestplateItem
    {
        public static readonly short ItemID = 0x13B;

        public override short ID { get { return 0x13B; } }

        public override Tuple<int, int> GetIconTexture(byte metadata)
        {
            return new Tuple<int, int>(4, 1);
        }

        public override ArmorMaterial Material { get { return ArmorMaterial.Gold; } }

        public override short BaseDurability { get { return 96; } }

        public override float BaseArmor { get { return 4; } }

        public override string GetDisplayName(short metadata)
        {
            return "Golden Chestplate";
        }
    }

    public class DiamondChestplateItem : ChestplateItem
    {
        public static readonly short ItemID = 0x137;

        public override short ID { get { return 0x137; } }

        public override Tuple<int, int> GetIconTexture(byte metadata)
        {
            return new Tuple<int, int>(3, 1);
        }

        public override ArmorMaterial Material { get { return ArmorMaterial.Diamond; } }

        public override short BaseDurability { get { return 384; } }

        public override float BaseArmor { get { return 4; } }

        public override string GetDisplayName(short metadata)
        {
            return "Diamond Chestplate";
        }
    }

    public class ChainChestplateItem : ArmorItem // Not HelmentItem because it can't inherit the recipe
    {
        public static readonly short ItemID = 0x12F;

        public override short ID { get { return 0x12F; } }

        public override Tuple<int, int> GetIconTexture(byte metadata)
        {
            return new Tuple<int, int>(1, 1);
        }

        public override ArmorMaterial Material { get { return ArmorMaterial.Chain; } }

        public override short BaseDurability { get { return 96; } }

        public override float BaseArmor { get { return 4; } }

        public override string GetDisplayName(short metadata)
        {
            return "Chain Chestplate";
        }
    }
}