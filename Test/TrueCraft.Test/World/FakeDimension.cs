using System;
using System.Collections;
using System.Collections.Generic;
using fNbt;
using TrueCraft.Core.Logic;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.Server;
using TrueCraft.Core.World;

namespace TrueCraft.Test.World
{
    public class FakeDimension : IDimensionServer
    {
        private const int BuildHeight = 128;

        private readonly IBlockRepository _blockRepository;

        private readonly IItemRepository _itemRepository;

        private readonly IEntityManager _entityManager;

        private Dictionary<GlobalChunkCoordinates, IChunk> _chunks;

        public FakeDimension(IBlockRepository blockRepository, IItemRepository itemRepository,
            IEntityManager entityManager)
        {
            _blockRepository = blockRepository;
            _itemRepository = itemRepository;
            _entityManager = entityManager;
            _chunks = new Dictionary<GlobalChunkCoordinates, IChunk>(434);

            _setBlockIDCount = 0;

            GlobalChunkCoordinates chunkCoordinates = new GlobalChunkCoordinates(0, 0);
            _chunks[chunkCoordinates] = new FakeChunk(chunkCoordinates);
        }

        public DimensionID ID { get => DimensionID.Overworld; }

        public string Name { get => "Fake"; }

        public IBlockRepository BlockRepository { get => _blockRepository; }

        /// <inheritdoc/>
        public IItemRepository ItemRepository { get => _itemRepository; }

        public long TimeOfDay { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IChunk? GetChunk(GlobalVoxelCoordinates coordinates)
        {
            return GetChunk((GlobalChunkCoordinates)coordinates);
        }

        public IChunk? GetChunk(GlobalChunkCoordinates coordinates)
        {
            if (_chunks.ContainsKey(coordinates))
                return _chunks[coordinates];

            return null;
        }

        public LocalVoxelCoordinates FindBlockPosition(GlobalVoxelCoordinates coordinates, out IChunk? chunk)
        {
            chunk = GetChunk(coordinates);
            return (LocalVoxelCoordinates)coordinates;
        }

        public BlockDescriptor GetBlockData(GlobalVoxelCoordinates coordinates)
        {
            IChunk? chunk = GetChunk(coordinates);
            if (chunk is null)
                return new BlockDescriptor() { ID = (byte)BlockIDs.Air };

            LocalVoxelCoordinates local = (LocalVoxelCoordinates)coordinates;
            return new BlockDescriptor()
            {
                ID = chunk.GetBlockID(local),
                Metadata = chunk.GetMetadata(local),
                BlockLight = chunk.GetBlockLight(local),
                SkyLight = chunk.GetSkyLight(local),
                Coordinates = coordinates,
                Chunk = chunk
            };
        }

        public byte GetBlockID(GlobalVoxelCoordinates coordinates)
        {
            IChunk? chunk = GetChunk(coordinates);
            return chunk?.GetBlockID((LocalVoxelCoordinates)coordinates) ?? 0;
        }

        public byte GetBlockLight(GlobalVoxelCoordinates coordinates)
        {
            IChunk? chunk = GetChunk(coordinates);
            return chunk?.GetBlockLight((LocalVoxelCoordinates)coordinates) ?? 0;
        }

        public byte GetMetadata(GlobalVoxelCoordinates coordinates)
        {
            IChunk? chunk = GetChunk(coordinates);
            return chunk?.GetMetadata((LocalVoxelCoordinates)coordinates) ?? 0;
        }

        public byte GetSkyLight(GlobalVoxelCoordinates coordinates)
        {
            IChunk? chunk = GetChunk(coordinates);
            return chunk?.GetSkyLight((LocalVoxelCoordinates)coordinates) ?? 0;
        }

        public bool IsChunkLoaded(GlobalVoxelCoordinates coordinates)
        {
            return _chunks.ContainsKey((GlobalChunkCoordinates)coordinates);
        }

        public bool IsValidPosition(GlobalVoxelCoordinates position)
        {
            return position.Y >= 0 && position.Y < BuildHeight;
        }

        public void SetBlockData(GlobalVoxelCoordinates coordinates, BlockDescriptor block)
        {
            throw new NotImplementedException();
        }

        public void SetBlockID(GlobalVoxelCoordinates coordinates, byte value)
        {
            IChunk? chunk = GetChunk(coordinates);
            chunk?.SetBlockID((LocalVoxelCoordinates)coordinates, value);
            _setBlockIDCount++;
        }

        public void SetBlockLight(GlobalVoxelCoordinates coordinates, byte value)
        {
            IChunk? chunk = GetChunk(coordinates);
            chunk?.SetBlockLight((LocalVoxelCoordinates)coordinates, value);
        }

        public void SetMetadata(GlobalVoxelCoordinates coordinates, byte value)
        {
            IChunk? chunk = GetChunk(coordinates);
            chunk?.SetMetadata((LocalVoxelCoordinates)coordinates, value);
        }

        public void SetSkyLight(GlobalVoxelCoordinates coordinates, byte value)
        {
            IChunk? chunk = GetChunk(coordinates);
            chunk?.SetSkyLight((LocalVoxelCoordinates)coordinates, value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public IEnumerator<IChunk> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #region IDimensionServer
        public event EventHandler<BlockChangeEventArgs>? BlockChanged;
        public event EventHandler<ChunkLoadedEventArgs>? ChunkGenerated;
        public event EventHandler<ChunkLoadedEventArgs>? ChunkLoaded;

        /// <inheritdoc />
        public void Initialize(GlobalChunkCoordinates spawnChunk, IMultiplayerServer server, IDimensionServer.ProgressNotification? progressNotification)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public IEntityManager EntityManager { get => _entityManager; }

        /// <inheritdoc />
        public NbtCompound? GetTileEntity(GlobalVoxelCoordinates coordinates)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void SetTileEntity(GlobalVoxelCoordinates coordinates, NbtCompound? value)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void Save()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public IChunk? GetChunk(GlobalChunkCoordinates coordinates, LoadEffort loadEffort)
        {
            if (_chunks.ContainsKey(coordinates))
                return _chunks[coordinates];

            if (loadEffort == LoadEffort.InMemory)
                return null;

            // For purposes of this Fake, we'll pretend the chunk has already been
            // saved to disk.
            _chunks[coordinates] = new FakeChunk(coordinates);

            return _chunks[coordinates];
        }

        /// <inheritdoc />
        public byte GetBlockID(GlobalVoxelCoordinates coordinates, LoadEffort loadEffort)
        {
            IChunk? chunk = GetChunk((GlobalChunkCoordinates)coordinates, LoadEffort.Load);
            return chunk?.GetBlockID((LocalVoxelCoordinates)coordinates) ?? (byte)BlockIDs.Air;
        }

        /// <inheritdoc />
        public byte GetMetadata(GlobalVoxelCoordinates coordinates, LoadEffort loadEffort)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public byte GetBlockLight(GlobalVoxelCoordinates coordinates, LoadEffort loadEffort)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public byte GetSkyLight(GlobalVoxelCoordinates coordinates, LoadEffort loadEffort)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public string ChunkProvider { get => throw new NotImplementedException(); }
        #endregion

        #region Stats
        private int _setBlockIDCount;

        /// <summary>
        /// Resets the counts of all calls.
        /// </summary>
        public void ResetCounts()
        {
            _setBlockIDCount = 0;
        }

        /// <summary>
        /// Gets the number of times SetBlockID was called since the last call to ResetCounts.
        /// </summary>
        public int SetBlockIDCount { get => _setBlockIDCount; }
        #endregion
    }
}
