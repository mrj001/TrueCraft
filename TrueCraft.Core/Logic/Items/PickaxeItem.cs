﻿using System;

namespace TrueCraft.Core.Logic.Items
{
    public abstract class PickaxeItem : ToolItem
    {
        public override ToolType ToolType
        {
            get
            {
                return ToolType.Pickaxe;
            }
        }
    }

    public class WoodenPickaxeItem : PickaxeItem
    {
        public static readonly short ItemID = 0x10E;

        public override short ID { get { return 0x10E; } }

        public override Tuple<int, int> GetIconTexture(byte metadata)
        {
            return new Tuple<int, int>(0, 6);
        }

        public override ToolMaterial Material { get { return ToolMaterial.Wood; } }

        public override short BaseDurability { get { return 60; } }

        public override string GetDisplayName(short metadata)
        {
            return "Wooden Pickaxe";
        }
    }

    public class StonePickaxeItem : PickaxeItem
    {
        public static readonly short ItemID = 0x112;

        public override short ID { get { return 0x112; } }

        public override Tuple<int, int> GetIconTexture(byte metadata)
        {
            return new Tuple<int, int>(1, 6);
        }

        public override ToolMaterial Material { get { return ToolMaterial.Stone; } }

        public override short BaseDurability { get { return 132; } }

        public override string GetDisplayName(short metadata)
        {
            return "Stone Pickaxe";
        }
    }

    public class IronPickaxeItem : PickaxeItem
    {
        public static readonly short ItemID = 0x101;

        public override short ID { get { return 0x101; } }

        public override Tuple<int, int> GetIconTexture(byte metadata)
        {
            return new Tuple<int, int>(2, 6);
        }

        public override ToolMaterial Material { get { return ToolMaterial.Iron; } }

        public override short BaseDurability { get { return 251; } }

        public override string GetDisplayName(short metadata)
        {
            return "Iron Pickaxe";
        }
    }

    public class GoldenPickaxeItem : PickaxeItem
    {
        public static readonly short ItemID = 0x11D;

        public override short ID { get { return 0x11D; } }

        public override Tuple<int, int> GetIconTexture(byte metadata)
        {
            return new Tuple<int, int>(4, 6);
        }

        public override ToolMaterial Material { get { return ToolMaterial.Gold; } }

        public override short BaseDurability { get { return 33; } }

        public override string GetDisplayName(short metadata)
        {
            return "Golden Pickaxe";
        }
    }

    public class DiamondPickaxeItem : PickaxeItem
    {
        public static readonly short ItemID = 0x116;

        public override short ID { get { return 0x116; } }

        public override Tuple<int, int> GetIconTexture(byte metadata)
        {
            return new Tuple<int, int>(3, 6);
        }

        public override ToolMaterial Material { get { return ToolMaterial.Diamond; } }

        public override short BaseDurability { get { return 1562; } }

        public override string GetDisplayName(short metadata)
        {
            return "Diamond Pickaxe";
        }
    }
}