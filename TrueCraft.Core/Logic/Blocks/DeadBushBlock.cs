using System;
using System.Xml;
using TrueCraft.Core.World;

namespace TrueCraft.Core.Logic.Blocks
{
    public class DeadBushBlock : BlockProvider
    {
        public DeadBushBlock(XmlNode node) : base(node)
        {

        }

        // TODO: shouldn't this be in the TrueCraft.xml too?
        public override Vector3i GetSupportDirection(BlockDescriptor descriptor)
        {
            return Vector3i.Down;
        }
    }
}