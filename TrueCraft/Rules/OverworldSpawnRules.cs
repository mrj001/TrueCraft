﻿using System;
using TrueCraft.Core.AI;
using TrueCraft.Core.Server;
using TrueCraft.Core.World;

namespace TrueCraft.Rules
{
    /// <summary>
    /// Default rules for spawning mobs in the overworld.
    /// </summary>
    public class OverworldSpawnRules : ISpawnRule
    {
        public void GenerateMobs(IChunk chunk, IEntityManager entityManager)
        {
            // TODO
        }

        public void SpawnMobs(IChunk chunk, IEntityManager entityManager)
        {
            // TODO
        }

        public int ChunkSpawnChance
        {
            get
            {
                return 10;
            }
        }
    }
}