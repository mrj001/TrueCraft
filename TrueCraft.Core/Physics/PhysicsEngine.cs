using System;
using System.Collections.Generic;
using TrueCraft.Core.Entities;
using TrueCraft.Core.Logic;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.World;

namespace TrueCraft.Core.Physics
{
    public class PhysicsEngine : IPhysicsEngine
    {
        private readonly IDimension _dimension;
        private readonly List<IEntity> _entities;
        private readonly object _entityLock;

        /// <summary>
        /// Any velocity vector components below this amount
        /// will be truncated to zero.
        /// </summary>
        private const double MinimumVelocity = 0.005;

        public PhysicsEngine(IDimension dimension)
        {
            _dimension = dimension;
            _entities = new List<IEntity>();
            _entityLock = new object();
        }

        public void AddEntity(IEntity entity)
        {
            lock (_entityLock)
                if (!_entities.Contains(entity))
                    _entities.Add(entity);
        }

        public void RemoveEntity(IEntity entity)
        {
            lock (_entityLock)
            {
                int index = _entities.IndexOf(entity);
                if (index >= 0)
                    _entities.RemoveAt(index);
            }
        }

        private Vector3 TruncateVelocity(double terminalVelocity, Vector3 velocity)
        {
            if (Math.Abs(velocity.X) < MinimumVelocity)
                velocity.X = 0;
            if (Math.Abs(velocity.Y) < MinimumVelocity)
                velocity.Y = 0;
            if (Math.Abs(velocity.Z) < MinimumVelocity)
                velocity.Z = 0;

            velocity.Clamp(terminalVelocity);
            return velocity;
        }

        /// <summary>
        /// Updates all the Entities managed by this Physics Engine.
        /// </summary>
        /// <param name="time">The amount of time since the last call to this Update.</param>
        public void Update(TimeSpan time)
        {
            double seconds = time.TotalSeconds;
            if (seconds == 0)
                return;

            lock(_entityLock)
            {
                foreach (IEntity entity in _entities)
                {
                    if (entity.BeginUpdate())
                    {
                        Vector3 velocity = entity.Velocity;
                        if (!IsGrounded(entity))
                            velocity -= new Vector3(0, entity.AccelerationDueToGravity * seconds, 0);
                        else
                            velocity.Y = Math.Max(0, velocity.Y);
                        velocity *= 1 - entity.Drag * seconds;
                        velocity = TruncateVelocity(entity.TerminalVelocity, velocity);

                        // This Ray specifies the Entity's move during this update.
                        Ray move = new Ray(entity.Position, velocity * seconds);

                        double nearestCollision;
                        GlobalVoxelCoordinates? collisionBlock;
                        BlockFace collisionFace = BlockFace.NegativeX;
                        BlockFace nearestCollisionFace = collisionFace;
                        BoundingBox? collisionTarget = null;
                        int collisionCount = 0;
                        bool hadCollision = true;

                        BoundingBox testBox = GetAABMoveBox(entity.BoundingBox, move.Direction);
                        int xmin = (int)(Math.Floor(testBox.Min.X));
                        int xmax = (int)(Math.Ceiling(testBox.Max.X - 1));
                        int ymin = (int)(Math.Floor(testBox.Min.Y));
                        int ymax = (int)(Math.Ceiling(testBox.Max.Y - 1));
                        int zmin = (int)(Math.Floor(testBox.Min.Z));
                        int zmax = (int)(Math.Ceiling(testBox.Max.Z - 1));

                        // An entity moving diagonally could conceivably collide
                        // with an x-, y-, and z-face all within a single tick.
                        // Thus, we must check for up to three collisions.
                        while (collisionCount < 3 && hadCollision)
                        {
                            nearestCollision = double.MaxValue;
                            collisionBlock = null;
                            hadCollision = false;

                            for (int x = xmin; x <= xmax; x++)
                                for (int z = zmin; z <= zmax; z++)
                                    for (int y = ymin; y <= ymax; y++)
                                    {
                                        GlobalVoxelCoordinates coords = new(x, y, z);
                                        BoundingBox? target = GetBoundingBox(_dimension, coords);
                                        if (!target.HasValue)
                                            continue;

                                        target = target.Value.OffsetBy((Vector3)coords);
                                        if (target.Value.Contains(entity.Position))
                                            continue;

                                        BoundingBox expandedTarget = target.Value.Expand(entity.Size);

                                        double collisionDistance = double.MaxValue;
                                        if (move.Intersects(expandedTarget, ref collisionDistance, ref collisionFace) && collisionDistance < nearestCollision)
                                        {
                                            hadCollision = true;
                                            nearestCollision = collisionDistance;
                                            nearestCollisionFace = collisionFace;
                                            collisionBlock = coords;
                                            collisionTarget = target.Value;
                                        }
                                    }

                            if (collisionBlock is not null)
                            {
                                collisionCount++;

                                entity.TerrainCollision((Vector3)collisionBlock, move.Direction.Unit());

                                // Adjust the move direction per the collision.
                                // Tighten up the box in which we need to search for additional collissions.
                                Vector3 distance = move.Direction;
                                switch (nearestCollisionFace)
                                {
                                    case BlockFace.NegativeX:
                                        distance.X = collisionTarget!.Value.Min.X - move.Position.X - entity.Size.Width * 0.5;
                                        xmax = collisionBlock.X;
                                        break;

                                    case BlockFace.PositiveX:
                                        distance.X = collisionTarget!.Value.Max.X - move.Position.X + entity.Size.Width * 0.5;
                                        xmin = collisionBlock.X;
                                        break;

                                    case BlockFace.NegativeY:
                                        distance.Y = collisionTarget!.Value.Min.Y - move.Position.Y - entity.Size.Height;
                                        ymax = collisionBlock.Y;
                                        break;

                                    case BlockFace.PositiveY:
                                        distance.Y = collisionTarget!.Value.Max.Y - move.Position.Y;
                                        ymin = collisionBlock.Y;
                                        break;

                                    case BlockFace.NegativeZ:
                                        distance.Z = collisionTarget!.Value.Min.Z - move.Position.Z - entity.Size.Depth * 0.5;
                                        zmax = collisionBlock.Z;
                                        break;

                                    case BlockFace.PositiveZ:
                                        distance.Z = collisionTarget!.Value.Max.Z - move.Position.Z + entity.Size.Depth * 0.5;
                                        zmin = collisionBlock.Z;
                                        break;
                                }
                                move = new Ray(move.Position, distance);
                            }
                        }

                        entity.EndUpdate(entity.Position + move.Direction, move.Direction / seconds);
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether or not the Entity is vertically in contact with terrain.
        /// </summary>
        /// <param name="entity">The entity to check</param>
        /// <returns>True if the Entity is vertically in contact with terrain; false otherwise.</returns>
        public bool IsGrounded(IEntity entity)
        {
            BoundingBox bb = entity.BoundingBox;
            int xmin = (int)Math.Floor(bb.Min.X);
            int xmax = (int)Math.Ceiling(bb.Max.X - 1);
            int zmin = (int)Math.Floor(bb.Min.Z);
            int zmax = (int)Math.Ceiling(bb.Max.Z - 1);
            double y = bb.Min.Y;
            int ySupport = (int)(y == (int)Math.Floor(y) ? y - 1: Math.Floor(y));

            for (int x = xmin; x <= xmax; x ++)
                for (int z = zmin; z <= zmax; z ++)
                {
                    GlobalVoxelCoordinates coords = new(x, ySupport, z);
                    BoundingBox? support = GetBoundingBox(_dimension, coords);
                    if (!support.HasValue)
                        continue;

                        double supportTop = support.Value.Max.Y + ySupport;
                        double supportBottom = support.Value.Min.Y + ySupport;
                        // If the Entity's "feet" are inside the block or close enough
                        // to top to be considered in contact, then it is grounded.
                        if ((y <= supportTop && y > supportBottom) || Math.Abs(y - supportTop) < GameConstants.Epsilon)
                            return true;
                    }

            return false;
        }

        /// <summary>
        /// Calculates a Bounding Box encompassing the original Bounding Box and the
        /// Bounding Box at the end of the given move.
        /// </summary>
        /// <param name="bb">The Bounding Box at the start of the Move.</param>
        /// <param name="move">A Vector3 specifying the direction and the
        /// distance (in metres) of the Move.</param>
        /// <returns>A Bounding Box encompassing the original Bounding Box and the
        /// Bounding Box at the end of the given move.</returns>
        private BoundingBox GetAABMoveBox(BoundingBox bb, Vector3 move)
        {
            Vector3 curMin = bb.Min;
            Vector3 curMax = bb.Max;

            Vector3 min = new Vector3(
                Math.Min(curMin.X, curMin.X + move.X),
                Math.Min(curMin.Y, curMin.Y + move.Y),
                Math.Min(curMin.Z, curMin.Z + move.Z)
            );
            Vector3 max = new Vector3(
                Math.Max(curMax.X, curMax.X + move.X),
                Math.Max(curMax.Y, curMax.Y + move.Y),
                Math.Max(curMax.Z, curMax.Z + move.Z)
            );
            return new BoundingBox(min, max);
        }

        private BoundingBox? GetBoundingBox(IDimension dimension, GlobalVoxelCoordinates coordinates)
        {
            byte id = dimension.GetBlockID(coordinates);
            if (id == (byte)BlockIDs.Air) return null;
            byte metadata = dimension.GetMetadata(coordinates);

            IBlockProvider? provider = dimension.BlockRepository.GetBlockProvider(id);
            return provider?.GetCollisionBox(metadata);
        }
    }
}
