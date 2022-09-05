using System;
using System.Xml;

namespace TrueCraft.Core.Logic.Blocks
{
    public class JukeboxBlock : BlockProvider, IBurnableItem
    {
        public JukeboxBlock(XmlNode node) : base(node)
        {

        }

        public TimeSpan BurnTime { get { return TimeSpan.FromSeconds(15); } }

        // TODO: behaviour: playing sound.
    }
}