using System;
using System.Collections.Generic;
using fNbt;
using TrueCraft.Core.Networking;
using TrueCraft.Core.Server;
using TrueCraft.Core.World;

namespace TrueCraft.Core.Logic.Blocks
{
    public class AirBlock : IBlockProvider
    {
        
        public byte ID { get => (byte)BlockIDs.Air; }

        public double BlastResistance { get { return 0; } }

        public double Hardness { get { return 0; } }

        public bool Opaque { get { return false; } }

        public byte Luminance { get { return 0; } }

        public string GetDisplayName(short metadata)
        {
            return "Air";
        }

        /// <inheritdoc />
        public IEnumerable<BoundingBox>? GetCollisionBoxes(byte metadata)
        {
            return null;
        }

        /// <inheritdoc />
        public BoundingBox? GetCollisionBox(byte metadata)
        {
            return null;
        }

        public SoundEffectClass SoundEffect
        {
            get
            {
                return SoundEffectClass.None;
            }
        }

        public bool RenderOpaque => false;

        public byte LightOpacity => 0;

        public bool Flammable => false;

        public ToolMaterial EffectiveToolMaterials => throw new NotImplementedException();

        public ToolType EffectiveTools => throw new NotImplementedException();

        public BoundingBox? InteractiveBoundingBox => null;

        public IEnumerable<short> VisibleMetadata
        {
            get
            {
                yield break;
            }
        }

        public Tuple<int, int> GetTextureMap(byte metadata)
        {
            return new Tuple<int, int>(0, 0);
        }

        protected ItemStack[] GetDrop(BlockDescriptor descriptor, ItemStack item)
        {
            return new ItemStack[0];
        }

        public void GenerateDropEntity(BlockDescriptor descriptor, IDimension world, IMultiplayerServer server, ItemStack heldItem)
        {
            throw new NotImplementedException();
        }

        public void BlockLeftClicked(IServiceLocator serviceLocator, BlockDescriptor descriptor, BlockFace face, IDimension world, IRemoteClient user)
        {
            throw new NotImplementedException();
        }

        public bool BlockRightClicked(IServiceLocator serviceLocator, BlockDescriptor descriptor, BlockFace face, IDimension world, IRemoteClient user)
        {
            throw new NotImplementedException();
        }

        public void BlockPlaced(BlockDescriptor descriptor, BlockFace face, IDimension world, IRemoteClient user)
        {
            throw new NotImplementedException();
        }

        public void BlockMined(BlockDescriptor descriptor, BlockFace face, IDimension world, IRemoteClient user)
        {
            throw new NotImplementedException();
        }

        public void BlockUpdate(BlockDescriptor descriptor, BlockDescriptor source, IMultiplayerServer server, IDimension world)
        {
            throw new NotImplementedException();
        }

        public void BlockLoadedFromChunk(IMultiplayerServer server, IDimension dimension, GlobalVoxelCoordinates coordinates)
        {
            throw new NotImplementedException();
        }

        public void TileEntityLoadedForClient(BlockDescriptor descriptor, IDimension world, NbtCompound compound, IRemoteClient client)
        {
            throw new NotImplementedException();
        }
    }
}
