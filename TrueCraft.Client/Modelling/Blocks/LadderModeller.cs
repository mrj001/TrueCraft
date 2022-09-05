using System;
using Microsoft.Xna.Framework;
using TrueCraft.Core.Logic;
using TrueCraft.Core.Logic.Blocks;

namespace TrueCraft.Client.Modelling.Blocks
{
    public class LadderModeller : BlockModeller
    {
        static LadderModeller()
        {
            RegisterRenderer((byte)BlockIDs.Ladder, new LadderModeller());
            for (int i = 0; i < Texture.Length; i++)
                Texture[i] *= new Vector2(16f / 256f);
        }

        private static Vector2 TextureMap = new Vector2(3, 5);
        private static Vector2[] Texture =
        {
            TextureMap + Vector2.UnitX + Vector2.UnitY,
            TextureMap + Vector2.UnitY,
            TextureMap,
            TextureMap + Vector2.UnitX
        };

        public override VertexPositionNormalColorTexture[] Render(BlockDescriptor descriptor, Vector3 offset,
            VisibleFaces faces, Tuple<int, int> textureMap, int indiciesOffset, out int[] indicies)
        {
            int[] lighting = GetLighting(descriptor);
            VertexPositionNormalColorTexture[] verticies;
            Vector3 correction;
            int faceCorrection = 0;
            switch ((LadderBlock.LadderDirection)descriptor.Metadata)
            {
                case LadderBlock.LadderDirection.North:
                    verticies = CreateQuad(CubeFace.PositiveZ, offset, Texture, 0, indiciesOffset, out indicies, Color.White);
                    correction = Vector3.Forward;
                    faceCorrection = (int)CubeFace.PositiveZ * 4;
                    break;
                case LadderBlock.LadderDirection.South:
                    verticies = CreateQuad(CubeFace.NegativeZ, offset, Texture, 0, indiciesOffset, out indicies, Color.White);
                    correction = Vector3.Backward;
                    faceCorrection = (int)CubeFace.NegativeZ * 4;
                    break;
                case LadderBlock.LadderDirection.East:
                    verticies = CreateQuad(CubeFace.NegativeX, offset, Texture, 0, indiciesOffset, out indicies, Color.White);
                    correction = Vector3.Right;
                    faceCorrection = (int)CubeFace.NegativeX * 4;
                    break;
                case LadderBlock.LadderDirection.West:
                    verticies = CreateQuad(CubeFace.PositiveX, offset, Texture, 0, indiciesOffset, out indicies, Color.White);
                    correction = Vector3.Left;
                    faceCorrection = (int)CubeFace.PositiveX * 4;
                    break;
                default:
                    // Should never happen
                    verticies = CreateUniformCube(offset, Texture, VisibleFaces.All,
                        indiciesOffset, out indicies, Color.White);
                    correction = Vector3.Zero;
                    break;
            }
            for (int i = 0; i < verticies.Length; i++)
                verticies[i].Position += correction;
            for (int i = 0; i < indicies.Length; i++)
                indicies[i] -= faceCorrection;
            return verticies;
        }

        public override VertexPositionNormalColorTexture[] Render(short metadata, Vector3 offset, Vector2[] texture, out int[] indices)
        {
            VertexPositionNormalColorTexture[] vertices;
            Vector3 correction;
            int faceCorrection = 0;
            vertices = CreateQuad(CubeFace.PositiveZ, offset, Texture, 0, 0, out indices, Color.White);
            correction = 0.5f * Vector3.Forward;
            faceCorrection = (int)CubeFace.PositiveZ * 4;

            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Position += correction;
            for (int i = 0; i < indices.Length; i++)
                indices[i] -= faceCorrection;

            return vertices;
        }
    }
}