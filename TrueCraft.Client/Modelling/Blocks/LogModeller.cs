using System;
using Microsoft.Xna.Framework;
using TrueCraft.Core.Logic;
using TrueCraft.Core.Logic.Blocks;

namespace TrueCraft.Client.Modelling.Blocks
{
    public class LogModeller : BlockModeller
    {
        static LogModeller()
        {
            RegisterRenderer((byte)BlockIDs.Wood, new LogModeller());
            for (int i = 0; i < BaseTexture.Length; i++)
            {
                BaseTexture[i] *= new Vector2(16f / 256f);
                SpruceTexture[i] *= new Vector2(16f / 256f);
                BirchTexture[i] *= new Vector2(16f / 256f);
            }
        }

        private static Vector2 BaseEndsTexture = new Vector2(5, 1);
        private static Vector2 BaseSidesTexture = new Vector2(4, 1);
        private static Vector2 SpruceSidesTexture = new Vector2(4, 7);
        private static Vector2 BirchSidesTexture = new Vector2(5, 7);
        private static Vector2[] BaseTexture =
        {
            // Positive Z
            BaseSidesTexture + Vector2.UnitX + Vector2.UnitY,
            BaseSidesTexture + Vector2.UnitY,
            BaseSidesTexture,
            BaseSidesTexture + Vector2.UnitX,
            // Negative Z
            BaseSidesTexture + Vector2.UnitX + Vector2.UnitY,
            BaseSidesTexture + Vector2.UnitY,
            BaseSidesTexture,
            BaseSidesTexture + Vector2.UnitX,
            // Positive X
            BaseSidesTexture + Vector2.UnitX + Vector2.UnitY,
            BaseSidesTexture + Vector2.UnitY,
            BaseSidesTexture,
            BaseSidesTexture + Vector2.UnitX,
            // Negative X
            BaseSidesTexture + Vector2.UnitX + Vector2.UnitY,
            BaseSidesTexture + Vector2.UnitY,
            BaseSidesTexture,
            BaseSidesTexture + Vector2.UnitX,
            // Positive Y
            BaseEndsTexture + Vector2.UnitX + Vector2.UnitY,
            BaseEndsTexture + Vector2.UnitY,
            BaseEndsTexture,
            BaseEndsTexture + Vector2.UnitX,
            // Negative Y
            BaseEndsTexture + Vector2.UnitX + Vector2.UnitY,
            BaseEndsTexture + Vector2.UnitY,
            BaseEndsTexture,
            BaseEndsTexture + Vector2.UnitX,
        };
        private static Vector2[] SpruceTexture =
        {
            // Positive Z
            SpruceSidesTexture + Vector2.UnitX + Vector2.UnitY,
            SpruceSidesTexture + Vector2.UnitY,
            SpruceSidesTexture,
            SpruceSidesTexture + Vector2.UnitX,
            // Negative Z
            SpruceSidesTexture + Vector2.UnitX + Vector2.UnitY,
            SpruceSidesTexture + Vector2.UnitY,
            SpruceSidesTexture,
            SpruceSidesTexture + Vector2.UnitX,
            // Positive X
            SpruceSidesTexture + Vector2.UnitX + Vector2.UnitY,
            SpruceSidesTexture + Vector2.UnitY,
            SpruceSidesTexture,
            SpruceSidesTexture + Vector2.UnitX,
            // Negative X
            SpruceSidesTexture + Vector2.UnitX + Vector2.UnitY,
            SpruceSidesTexture + Vector2.UnitY,
            SpruceSidesTexture,
            SpruceSidesTexture + Vector2.UnitX,
            // Positive Y
            BaseEndsTexture + Vector2.UnitX + Vector2.UnitY,
            BaseEndsTexture + Vector2.UnitY,
            BaseEndsTexture,
            BaseEndsTexture + Vector2.UnitX,
            // Negative Y
            BaseEndsTexture + Vector2.UnitX + Vector2.UnitY,
            BaseEndsTexture + Vector2.UnitY,
            BaseEndsTexture,
            BaseEndsTexture + Vector2.UnitX,
        };
        private static Vector2[] BirchTexture =
        {
            // Positive Z
            BirchSidesTexture + Vector2.UnitX + Vector2.UnitY,
            BirchSidesTexture + Vector2.UnitY,
            BirchSidesTexture,
            BirchSidesTexture + Vector2.UnitX,
            // Negative Z
            BirchSidesTexture + Vector2.UnitX + Vector2.UnitY,
            BirchSidesTexture + Vector2.UnitY,
            BirchSidesTexture,
            BirchSidesTexture + Vector2.UnitX,
            // Positive X
            BirchSidesTexture + Vector2.UnitX + Vector2.UnitY,
            BirchSidesTexture + Vector2.UnitY,
            BirchSidesTexture,
            BirchSidesTexture + Vector2.UnitX,
            // Negative X
            BirchSidesTexture + Vector2.UnitX + Vector2.UnitY,
            BirchSidesTexture + Vector2.UnitY,
            BirchSidesTexture,
            BirchSidesTexture + Vector2.UnitX,
            // Positive Y
            BaseEndsTexture + Vector2.UnitX + Vector2.UnitY,
            BaseEndsTexture + Vector2.UnitY,
            BaseEndsTexture,
            BaseEndsTexture + Vector2.UnitX,
            // Negative Y
            BaseEndsTexture + Vector2.UnitX + Vector2.UnitY,
            BaseEndsTexture + Vector2.UnitY,
            BaseEndsTexture,
            BaseEndsTexture + Vector2.UnitX,
        };

        public override VertexPositionNormalColorTexture[] Render(BlockDescriptor descriptor, Vector3 offset,
            VisibleFaces faces, Tuple<int, int> textureMap, int indiciesOffset, out int[] indicies)
        {
            int[] lighting = GetLighting(descriptor);

            switch ((WoodBlock.WoodType)descriptor.Metadata)
            {
                case WoodBlock.WoodType.Spruce:
                    return CreateUniformCube(offset, SpruceTexture, faces, indiciesOffset, out indicies, Color.White, lighting);
                case WoodBlock.WoodType.Birch:
                    return CreateUniformCube(offset, BirchTexture, faces, indiciesOffset, out indicies, Color.White, lighting);
                case WoodBlock.WoodType.Oak:
                default:
                    return CreateUniformCube(offset, BaseTexture, faces, indiciesOffset, out indicies, Color.White, lighting);
            }
        }

        public override VertexPositionNormalColorTexture[] Render(short metadata, Vector3 offset, Vector2[] texture, out int[] indices)
        {
            switch ((WoodBlock.WoodType)metadata)
            {
                case WoodBlock.WoodType.Spruce:
                    return base.Render(metadata, offset, SpruceTexture, out indices);
                case WoodBlock.WoodType.Birch:
                    return base.Render(metadata, offset, BirchTexture, out indices);
                case WoodBlock.WoodType.Oak:
                default:
                    return base.Render(metadata, offset, BaseTexture, out indices);
            }
        }
    }
}