using System;
using Microsoft.Xna.Framework;
using TrueCraft.Core.Logic.Blocks;

namespace TrueCraft.Client.Modelling.Blocks
{
    public class CobwebModeller : FlatQuadModeller
    {
        static CobwebModeller()
        {
            RegisterRenderer((byte)BlockIDs.Cobweb, new CobwebModeller());
        }

        protected override Vector2 TextureMap { get { return new Vector2(11, 0); } }
    }
}
