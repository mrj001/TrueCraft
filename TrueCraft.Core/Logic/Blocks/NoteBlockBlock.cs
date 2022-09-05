using System;
using System.Xml;

namespace TrueCraft.Core.Logic.Blocks
{
    public class NoteBlockBlock : BlockProvider, IBurnableItem
    {
        public NoteBlockBlock(XmlNode node) : base(node)
        {

        }

        public TimeSpan BurnTime { get { return TimeSpan.FromSeconds(15); } }

        // TODO: Redstone behaviour.
    }
}