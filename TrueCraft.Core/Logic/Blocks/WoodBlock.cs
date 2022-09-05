using System;
using System.Collections.Generic;
using System.Xml;
using TrueCraft.Core.Logic.Items;

namespace TrueCraft.Core.Logic.Blocks
{
    public class WoodBlock : BlockProvider, IBurnableItem, ISmeltableItem
    {
        public enum WoodType
        {
            Oak = 0,
            Spruce = 1,
            Birch = 2
        }

        public WoodBlock(XmlNode node) : base(node)
        {

        }

        public TimeSpan BurnTime { get { return TimeSpan.FromSeconds(15); } }

        public ItemStack SmeltingOutput { get => new ItemStack(CoalItem.ItemID, 1, (short)CoalItem.MetaData.Charcoal); }

        public override IEnumerable<short> VisibleMetadata
        {
            get
            {
                yield return (short)WoodType.Oak;
                yield return (short)WoodType.Spruce;
                yield return (short)WoodType.Birch;
            }
        }
    }
}
