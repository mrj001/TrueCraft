using System;
using System.Collections.Generic;
using TrueCraft.Core;
using TrueCraft.Core.World;
using System.Diagnostics;
using TrueCraft.Core.Logic;

namespace TrueCraft.Core.AI
{
    public class AStarPathFinder
    {
        private readonly Vector3i[] Neighbors =
        {
            Vector3i.North,
            Vector3i.East,
            Vector3i.South,
            Vector3i.West
        };

        private readonly Vector3i[][] DiagonalNeighbors =
        {
            new[] { Vector3i.North, Vector3i.East },
            new[] { Vector3i.North, Vector3i.West },
            new[] { Vector3i.South, Vector3i.East },
            new[] { Vector3i.South, Vector3i.West },
        };

        private PathResult TracePath(GlobalVoxelCoordinates start, GlobalVoxelCoordinates goal, Dictionary<GlobalVoxelCoordinates, GlobalVoxelCoordinates> parents)
        {
            List<GlobalVoxelCoordinates> list = new List<GlobalVoxelCoordinates>();
            GlobalVoxelCoordinates current = goal;
            while (current != start)
            {
                current = parents[current];
                list.Insert(0, current);
            }
            list.Add(goal);
            return new PathResult(list);
        }

        // TODO: entity Bounding Box is not taken into account: What if the entity is more than one block high?
        private bool CanOccupyVoxel(IDimension dimension, BoundingBox entity, GlobalVoxelCoordinates voxel)
        {
            byte id = dimension.GetBlockID(voxel);

            IBlockProvider provider = dimension.BlockRepository.GetBlockProvider(id);
            if (provider is null)
                return true;

            // TODO: supply metadata.
            return provider.GetCollisionBox(0) is null;
        }

        private IEnumerable<GlobalVoxelCoordinates> GetNeighbors(IDimension dimension, BoundingBox subject, GlobalVoxelCoordinates current)
        {
            for (int i = 0; i < Neighbors.Length; i++)
            {
                var next = Neighbors[i] + current;
                if (CanOccupyVoxel(dimension, subject, next))
                    yield return next;
            }
            for (int i = 0; i < DiagonalNeighbors.Length; i++)
            {
                var pair = DiagonalNeighbors[i];
                var next = pair[0] + pair[1] + current;

                if (CanOccupyVoxel(dimension, subject, next)
                    && CanOccupyVoxel(dimension, subject, pair[0] + current)
                    && CanOccupyVoxel(dimension, subject, pair[1] + current))
                    yield return next;
            }
        }

        public PathResult? FindPath(IDimension dimension, BoundingBox subject, GlobalVoxelCoordinates start, GlobalVoxelCoordinates goal)
        {
            // Thanks to www.redblobgames.com/pathfinding/a-star/implementation.html
            var parents = new Dictionary<GlobalVoxelCoordinates, GlobalVoxelCoordinates>();
            var costs = new Dictionary<GlobalVoxelCoordinates, double>();
            var openset = new PriorityQueue<GlobalVoxelCoordinates>();
            var closedset = new HashSet<GlobalVoxelCoordinates>();

            openset.Enqueue(start, 0);
            parents[start] = start;
            costs[start] = start.DistanceTo(goal);

            while (openset.Count > 0)
            {
                var current = openset.Dequeue();
                if (current == goal)
                    return TracePath(start, goal, parents);

                closedset.Add(current);

                foreach (var next in GetNeighbors(dimension, subject, current))
                {
                    if (closedset.Contains(next))
                        continue;
                    var cost = (int)(costs[current] + current.DistanceTo(next));
                    if (!costs.ContainsKey(next) || cost < costs[next])
                    {
                        costs[next] = cost;
                        var priority = cost + next.DistanceTo(goal);
                        openset.Enqueue(next, priority);
                        parents[next] = current;
                    }
                }
            }

            return null;
        }
    }
}