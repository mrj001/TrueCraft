using System;
using Microsoft.Xna.Framework;
using TrueCraft.Core.Logic;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.World;

namespace TrueCraft.Client.Modelling.Blocks
{
    public class GrassModeller : BlockModeller
    {
        static GrassModeller()
        {
            RegisterRenderer((byte)BlockIDs.Grass, new GrassModeller());
            for (int i = 0; i < Texture.Length; i++)
                Texture[i] *= new Vector2(16f / 256f);
            for (int i = 0; i < Texture.Length; i++)
                SnowTexture[i] *= new Vector2(16f / 256f);
        }

        private static Vector2 TextureMap = new Vector2(0, 0);
        private static Vector2 EndsTexture = new Vector2(2, 0);
        private static Vector2 SideTexture = new Vector2(3, 0);
        private static Vector2 SideTextureSnow = new Vector2(4, 4);
        private static Vector2[] Texture =
        {
            // Positive Z
            SideTexture + Vector2.UnitX + Vector2.UnitY,
            SideTexture + Vector2.UnitY,
            SideTexture,
            SideTexture + Vector2.UnitX,
            // Negative Z
            SideTexture + Vector2.UnitX + Vector2.UnitY,
            SideTexture + Vector2.UnitY,
            SideTexture,
            SideTexture + Vector2.UnitX,
            // Positive X
            SideTexture + Vector2.UnitX + Vector2.UnitY,
            SideTexture + Vector2.UnitY,
            SideTexture,
            SideTexture + Vector2.UnitX,
            // Negative X
            SideTexture + Vector2.UnitX + Vector2.UnitY,
            SideTexture + Vector2.UnitY,
            SideTexture,
            SideTexture + Vector2.UnitX,
            // Positive Y
            TextureMap + Vector2.UnitX + Vector2.UnitY,
            TextureMap + Vector2.UnitY,
            TextureMap,
            TextureMap + Vector2.UnitX,
            // Negative Y
            EndsTexture + Vector2.UnitX + Vector2.UnitY,
            EndsTexture + Vector2.UnitY,
            EndsTexture,
            EndsTexture + Vector2.UnitX,
        };
        private static Vector2[] SnowTexture =
        {
            // Positive Z
            SideTextureSnow + Vector2.UnitX + Vector2.UnitY,
            SideTextureSnow + Vector2.UnitY,
            SideTextureSnow,
            SideTextureSnow + Vector2.UnitX,
            // Negative Z
            SideTextureSnow + Vector2.UnitX + Vector2.UnitY,
            SideTextureSnow + Vector2.UnitY,
            SideTextureSnow,
            SideTextureSnow + Vector2.UnitX,
            // Positive X
            SideTextureSnow + Vector2.UnitX + Vector2.UnitY,
            SideTextureSnow + Vector2.UnitY,
            SideTextureSnow,
            SideTextureSnow + Vector2.UnitX,
            // Negative X
            SideTextureSnow + Vector2.UnitX + Vector2.UnitY,
            SideTextureSnow + Vector2.UnitY,
            SideTextureSnow,
            SideTextureSnow + Vector2.UnitX,
            // Positive Y
            TextureMap + Vector2.UnitX + Vector2.UnitY,
            TextureMap + Vector2.UnitY,
            TextureMap,
            TextureMap + Vector2.UnitX,
            // Negative Y
            EndsTexture + Vector2.UnitX + Vector2.UnitY,
            EndsTexture + Vector2.UnitY,
            EndsTexture,
            EndsTexture + Vector2.UnitX,
        };

        public static readonly Color BiomeColor = new Color(105, 169, 63);

        public override VertexPositionNormalColorTexture[] Render(BlockDescriptor descriptor, Vector3 offset,
            VisibleFaces faces, Tuple<int, int> textureMap, int indiciesOffset, out int[] indicies)
        {
            var texture = Texture;
            if (descriptor.Coordinates.Y < WorldConstants.Height && descriptor.Chunk != null &&
                    descriptor.Chunk.GetBlockID((LocalVoxelCoordinates)(descriptor.Coordinates + Vector3i.Up)) == (byte)BlockIDs.Snow)
                texture = SnowTexture;

            int[] lighting = GetLighting(descriptor);

            var cube = CreateUniformCube(offset, texture, faces, indiciesOffset, out indicies, Color.White, lighting);
            // Apply biome colors to top of cube
            for (int i = (int)CubeFace.PositiveY * 4; i < (int)CubeFace.PositiveY * 4 + 4; i++)
            {
                cube[i].Color = new Color(cube[i].Color.ToVector3() * BiomeColor.ToVector3()); // TODO: Take this from biome
            }
            return cube;
        }
    }
}