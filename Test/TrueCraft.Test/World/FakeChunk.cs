using System;
using fNbt;
using TrueCraft.Core;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.World;

namespace TrueCraft.Test.World
{
    // NOTE: This Fake depends upon various BlockProvider subclasses for Block ID values.
    public class FakeChunk : IChunk
    {
        private byte[] _blocks;
        private byte[] _blockLight;
        private byte[] _skyLight;
        private byte[] _metadata;
        private byte[,] _heightMap;

        public FakeChunk(GlobalChunkCoordinates coordinates)
        {
            Coordinates = coordinates;

            _blocks = new byte[WorldConstants.ChunkDepth * WorldConstants.ChunkWidth * WorldConstants.Height];
            _blockLight = new byte[WorldConstants.ChunkDepth * WorldConstants.ChunkWidth * WorldConstants.Height];
            _skyLight = new byte[WorldConstants.ChunkDepth * WorldConstants.ChunkWidth * WorldConstants.Height];
            _metadata = new byte[WorldConstants.ChunkDepth * WorldConstants.ChunkWidth * WorldConstants.Height];
            _heightMap = new byte[WorldConstants.ChunkWidth, WorldConstants.ChunkDepth];
        }

        public int X { get => Coordinates.X; }

        public int Z { get => Coordinates.Z; }

        public GlobalChunkCoordinates Coordinates { get; }

        private int CoordinatesToIndex(LocalVoxelCoordinates coordinates)
        {
            return (coordinates.X * WorldConstants.ChunkWidth + coordinates.Z) * WorldConstants.Height + coordinates.Y;
        }

        public int MaxHeight => throw new NotImplementedException();

        public bool IsModified => throw new NotImplementedException();

        public DateTime LastAccessed { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public byte[] Data => throw new NotImplementedException();

        public bool TerrainPopulated { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public NybbleArray Metadata => throw new NotImplementedException();

        public NybbleArray BlockLight => throw new NotImplementedException();

        public NybbleArray SkyLight => throw new NotImplementedException();

        public event EventHandler? Disposed;

        public void Dispose()
        {
            Disposed?.Invoke(this, EventArgs.Empty);
        }

        public Biome GetBiome(int x, int z)
        {
            throw new NotImplementedException();
        }

        public byte GetBlockID(LocalVoxelCoordinates coordinates)
        {
            int index = CoordinatesToIndex(coordinates);
            if (index < 0 || index >= _blocks.Length)
                return (byte)BlockIDs.Air;
            return _blocks[index];
        }

        public byte GetBlockLight(LocalVoxelCoordinates coordinates)
        {
            int index = CoordinatesToIndex(coordinates);
            if (index < 0 || index >= _blocks.Length)
                return 0;
            return _blockLight[index];
        }

        public int GetHeight(int x, int z)
        {
            throw new NotImplementedException();
        }

        public byte GetMetadata(LocalVoxelCoordinates coordinates)
        {
            int index = CoordinatesToIndex(coordinates);
            if (index < 0 || index >= _blocks.Length)
                return 0;
            return _metadata[index];
        }

        public byte GetSkyLight(LocalVoxelCoordinates coordinates)
        {
            int index = CoordinatesToIndex(coordinates);
            if (index < 0 || index >= _blocks.Length)
                return 15;
            return _skyLight[index];
        }

        public NbtCompound GetTileEntity(LocalVoxelCoordinates coordinates)
        {
            throw new NotImplementedException();
        }

        public void SetBlockID(LocalVoxelCoordinates coordinates, byte value)
        {
            _blocks[CoordinatesToIndex(coordinates)] = value;

            if (coordinates.Y == _heightMap[coordinates.X, coordinates.Z] && value == (byte)BlockIDs.Air)
            {
                int y = coordinates.Y;
                while (y >= 0 && (byte)BlockIDs.Air == GetBlockID(new LocalVoxelCoordinates(coordinates.X, y, coordinates.Z)))
                    y--;
                if (y < 0) y = 0;
                _heightMap[coordinates.X, coordinates.Z] = (byte)y;
            }
            if (coordinates.Y > _heightMap[coordinates.X, coordinates.Z] && value != (byte)BlockIDs.Air)
                _heightMap[coordinates.X, coordinates.Z] = (byte)coordinates.Y;
        }

        public void SetBlockLight(LocalVoxelCoordinates coordinates, byte value)
        {
            _blockLight[CoordinatesToIndex(coordinates)] = value;
        }

        public void SetMetadata(LocalVoxelCoordinates coordinates, byte value)
        {
            _metadata[CoordinatesToIndex(coordinates)] = value;
        }

        public void SetSkyLight(LocalVoxelCoordinates coordinates, byte value)
        {
            _skyLight[CoordinatesToIndex(coordinates)] = value;
        }

        public void SetTileEntity(LocalVoxelCoordinates coordinates, NbtCompound? value)
        {
            throw new NotImplementedException();
        }

        public void UpdateHeightMap()
        {
            throw new NotImplementedException("Fix call to obsolete method.");
        }
    }
}
