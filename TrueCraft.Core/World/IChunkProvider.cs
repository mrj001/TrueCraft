﻿using System;
using System.Collections.Generic;

namespace TrueCraft.Core.World
{
    /// <summary>
    /// Provides new chunks to worlds. Generally speaking this is a terrain generator.
    /// </summary>
    public interface IChunkProvider
    {
        IList<IChunkDecorator> ChunkDecorators { get; }

        IChunk GenerateChunk(IWorld world, GlobalChunkCoordinates coordinates);

        GlobalVoxelCoordinates GetSpawn(IWorld world);
        void Initialize(IWorld world);
    }
}