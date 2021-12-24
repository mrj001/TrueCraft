﻿using System;

namespace TrueCraft.Core.World
{
    public class ChunkLoadedEventArgs : EventArgs
    {
        public GlobalChunkCoordinates Coordinates { get => Chunk.Coordinates; }
        public IChunk Chunk { get; }

        public ChunkLoadedEventArgs(IChunk chunk)
        {
            Chunk = chunk;
        }
    }
}
