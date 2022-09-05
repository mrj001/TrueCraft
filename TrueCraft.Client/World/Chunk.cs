using System;
using System.IO;
using fNbt;
using MonoGame.Framework.Utilities.Deflate;
using TrueCraft.Core;
using TrueCraft.Core.Networking.Packets;
using TrueCraft.Core.World;

namespace TrueCraft.Client.World
{
    public class Chunk : IChunk
    {
        private readonly GlobalChunkCoordinates _coordinates;
        private readonly byte[] _blockIDs;
        private readonly NybbleArray _metaData;
        private readonly NybbleArray _blockLight;
        private readonly NybbleArray _skyLight;

        private readonly byte[] _heightMap;
        private int _maxHeight;

        public Chunk(ChunkDataPacket packet)
        {
            _coordinates = new GlobalChunkCoordinates(packet.X / WorldConstants.ChunkWidth, packet.Z / WorldConstants.ChunkDepth);
            int blockCount = WorldConstants.ChunkDepth * WorldConstants.ChunkWidth * WorldConstants.Height * 5 / 2;
            _blockIDs = new byte[blockCount];

            using (MemoryStream memoryStream = new MemoryStream(packet.CompressedData))
            using (ZlibStream stream = new ZlibStream(memoryStream, CompressionMode.Decompress))
            {
                stream.Read(_blockIDs, 0, blockCount);
                _metaData = new NybbleArray(stream, blockCount);
                _blockLight = new NybbleArray(stream, blockCount);
                _skyLight = new NybbleArray(stream, blockCount);
            }

            _heightMap = new byte[WorldConstants.ChunkDepth * WorldConstants.ChunkWidth];
            UpdateHeightMap();
        }

        /// <inheritdoc />
        public int X { get => _coordinates.X; }

        /// <inheritdoc />
        public int Z { get => _coordinates.Z; }

        /// <inheritdoc />
        public GlobalChunkCoordinates Coordinates { get => _coordinates; }

        // TODO: this should be server-side only
        public bool IsModified => throw new NotImplementedException();

        public DateTime LastAccessed { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public byte[] Data => throw new NotImplementedException();

        public bool TerrainPopulated { get => true; set => throw new NotImplementedException(); }

        /// <inheritdoc />
        public NybbleArray Metadata => throw new NotImplementedException();

        /// <inheritdoc />
        public NybbleArray BlockLight => throw new NotImplementedException();

        /// <inheritdoc />
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

        /// <summary>
        /// Converts Local Voxel Coordinates into an index into the internal arrays.
        /// </summary>
        /// <param name="coordinates">The Coordinates to convert</param>
        /// <returns>The index into the internal arrays.</returns>
        private int CoordinatesToIndex(LocalVoxelCoordinates coordinates)
        {
            return (coordinates.X * WorldConstants.ChunkWidth + coordinates.Z) * WorldConstants.Height + coordinates.Y;
        }

        /// <inheritdoc />
        public byte GetBlockID(LocalVoxelCoordinates coordinates)
        {
            if (coordinates.Y < 0 || coordinates.Y >= WorldConstants.Height)
                return (byte)Core.Logic.Blocks.BlockIDs.Air;
            return _blockIDs[CoordinatesToIndex(coordinates)];
        }

        /// <inheritdoc />
        public void SetBlockID(LocalVoxelCoordinates coordinates, byte value)
        {
            if (coordinates.Y < 0 || coordinates.Y >= WorldConstants.Height)
                return;
            _blockIDs[CoordinatesToIndex(coordinates)] = value;
        }

        /// <inheritdoc />
        public byte GetMetadata(LocalVoxelCoordinates coordinates)
        {
            if (coordinates.Y < 0 || coordinates.Y >= WorldConstants.Height)
                return 0;
            return _metaData[CoordinatesToIndex(coordinates)];
        }

        /// <inheritdoc />
        public void SetMetadata(LocalVoxelCoordinates coordinates, byte value)
        {
            if (coordinates.Y < 0 || coordinates.Y >= WorldConstants.Height)
                return;
            _metaData[CoordinatesToIndex(coordinates)] = value;
        }

        /// <inheritdoc />
        public byte GetBlockLight(LocalVoxelCoordinates coordinates)
        {
            if (coordinates.Y < 0 || coordinates.Y >= WorldConstants.Height)
                return 0;
            // TODO: fix return of light values
            //return _blockLight[CoordinatesToIndex(coordinates)];
            return 15;
        }

        /// <inheritdoc />
        public void SetBlockLight(LocalVoxelCoordinates coordinates, byte value)
        {
            if (coordinates.Y < 0 || coordinates.Y >= WorldConstants.Height)
                return;
            _blockLight[CoordinatesToIndex(coordinates)] = value;
        }

        /// <inheritdoc />
        public byte GetSkyLight(LocalVoxelCoordinates coordinates)
        {
            if (coordinates.Y < 0 || coordinates.Y >= WorldConstants.Height)
                return 15;
            // TODO: fix return of light values
            //return _skyLight[CoordinatesToIndex(coordinates)];
            return 15;
        }

        /// <inheritdoc />
        public void SetSkyLight(LocalVoxelCoordinates coordinates, byte value)
        {
            if (coordinates.Y < 0 || coordinates.Y >= WorldConstants.Height)
                return;
            _skyLight[CoordinatesToIndex(coordinates)] = value;
        }

        /// <inheritdoc />
        public NbtCompound? GetTileEntity(LocalVoxelCoordinates coordinates)
        {
            // TODO: this should be server-side only
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void SetTileEntity(LocalVoxelCoordinates coordinates, NbtCompound? value)
        {
            // TODO: this should be server-side only
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void UpdateHeightMap()
        {
            int blockIndex;
            int heightMapIndex = 0;
            int maxHeight = 0;
            for (int x = 0; x < WorldConstants.ChunkWidth; x ++)
                for (int z = 0; z < WorldConstants.ChunkDepth; z ++)
                {
                    int y = WorldConstants.Height - 1;
                    blockIndex = (x * WorldConstants.ChunkWidth + z) * WorldConstants.ChunkDepth + y;
                    while (y > 0 && _blockIDs[blockIndex] == 0)
                    {
                        y--;
                        blockIndex--;
                    }
                    _heightMap[heightMapIndex] = (byte)y;
                    heightMapIndex++;
                    if (y > maxHeight)
                        maxHeight = y;
                }
            _maxHeight = maxHeight;
        }

        /// <inheritdoc />
        public int GetHeight(int x, int z)
        {
            return _heightMap[x * WorldConstants.ChunkWidth + z];
        }

        /// <inheritdoc />
        public int MaxHeight { get => _maxHeight; }
    }
}
