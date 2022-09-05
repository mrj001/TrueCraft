using System;
using System.Collections.Generic;
using TrueCraft.Core.World;
using TrueCraft.Core.Networking;
using TrueCraft.Core.Server;
using fNbt;

namespace TrueCraft.Core.Logic
{
    public interface IBlockProvider
    {
        byte ID { get; }
        double BlastResistance { get; }
        double Hardness { get; }
        byte Luminance { get; }
        bool Opaque { get; }
        bool RenderOpaque { get; }
        byte LightOpacity { get; }
        bool Flammable { get; }
        SoundEffectClass SoundEffect { get; }
        ToolMaterial EffectiveToolMaterials { get; }
        ToolType EffectiveTools { get; }

        /// <summary>
        /// Gets the Bounding Box(es) for purposes of colliding with the Block.
        /// </summary>
        /// <param name="metadata"></param>
        /// <returns>
        /// An IEnumerable of all the parts with which Entities may collide.  This can
        /// be null for blocks with no collision (eg. Flowers).
        /// </returns>
        /// <remarks>
        /// <para>
        /// While most blocks will only require a single Bounding Box, Blocks
        /// such as stairs require multiple Bounding Boxes in order to work correctly.
        /// </para>
        /// </remarks>
        IEnumerable<BoundingBox>? GetCollisionBoxes(byte metadata);

        /// <summary>
        /// Gets the union of all the Collision Boxes.
        /// </summary>
        /// <param name="metadata"></param>
        /// <returns></returns>
        BoundingBox? GetCollisionBox(byte metadata);

        /// <summary>
        /// Bounding Box for purposes of interacting with the Block.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the bounding box that the Player's hand/tool has to hit to break
        /// the Block.  It's also the one drawn around the Block when Bounding
        /// Boxes are being drawn.
        /// </para>
        /// </remarks>
        BoundingBox? InteractiveBoundingBox { get; } // NOTE: Will this eventually need to be metadata-aware?

        Tuple<int, int>? GetTextureMap(byte metadata);

        /// <summary>
        /// Gets an enumerable over any metadata which affects rendering.
        /// </summary>
        IEnumerable<short> VisibleMetadata { get; }

        void GenerateDropEntity(BlockDescriptor descriptor, IDimension world, IMultiplayerServer server, ItemStack heldItem);
        void BlockLeftClicked(IServiceLocator serviceLocator, BlockDescriptor descriptor, BlockFace face, IDimension world, IRemoteClient user);
        bool BlockRightClicked(IServiceLocator serviceLocator, BlockDescriptor descriptor, BlockFace face, IDimension world, IRemoteClient user);
        void BlockPlaced(BlockDescriptor descriptor, BlockFace face, IDimension world, IRemoteClient user);
        void BlockMined(BlockDescriptor descriptor, BlockFace face, IDimension world, IRemoteClient user);
        void BlockUpdate(BlockDescriptor descriptor, BlockDescriptor source, IMultiplayerServer server, IDimension world);

        /// <summary>
        /// Called for each Block in a newly loaded Chunk.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="dimension">The Dimension containing the Block</param>
        /// <param name="coordinates">The coordinates of the Block within the Dimension.</param>
        void BlockLoadedFromChunk(IMultiplayerServer server, IDimension dimension, GlobalVoxelCoordinates coordinates);

        void TileEntityLoadedForClient(BlockDescriptor descriptor, IDimension world, NbtCompound compound, IRemoteClient client);
    }
}
