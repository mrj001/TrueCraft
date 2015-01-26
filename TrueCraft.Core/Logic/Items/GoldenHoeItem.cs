using System;
using TrueCraft.API.Logic;
using TrueCraft.API;

namespace TrueCraft.Core.Logic.Items
{
    public class GoldenHoeItem : ToolItem
    {
        public static readonly short ItemID = 0x126;

        public override short ID { get { return 0x126; } }

        public override ToolMaterial Material { get { return ToolMaterial.Gold; } }

        public override string DisplayName { get { return "Golden Hoe"; } }
    }
}