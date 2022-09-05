using System;
using System.Xml;

namespace TrueCraft.Core.Logic.Blocks
{
    public class LockedChestBlock : BlockProvider, IBurnableItem
    {
        public LockedChestBlock(XmlNode node) : base(node)
        {

        }

        // TODO: behaviour analogous to Chest.

        public TimeSpan BurnTime { get { return TimeSpan.FromSeconds(15); } }
    }
}