﻿using System;
using TrueCraft.Core.Networking;
using TrueCraft.Core.Networking.Packets;
using TrueCraft.Core.World;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.Physics;
using TrueCraft.Core.Logic;
using System.Linq;
using TrueCraft.Core.Server;

namespace TrueCraft.Core.Entities
{
    public class FallingSandEntity : ObjectEntity, IAABBEntity
    {
        public FallingSandEntity(IDimension dimension, IEntityManager entityManager,
            Vector3 position) : base(dimension, entityManager)
        {
            _Position = position + new Vector3(0.5);
        }

        public override byte EntityType { get { return 70; } }

        public override Size Size
        {
            get
            {
                return new Size(0.98);
            }
        }

        public override IPacket SpawnPacket
        {
            get
            {
                return new SpawnGenericEntityPacket(EntityID, (sbyte)EntityType,
                    MathHelper.CreateAbsoluteInt(Position.X), MathHelper.CreateAbsoluteInt(Position.Y),
                    MathHelper.CreateAbsoluteInt(Position.Z), 0, null, null, null);
            }
        }

        public override int Data { get { return 1; } }

        public void TerrainCollision(Vector3 collisionPoint, Vector3 collisionDirection)
        {
            if (Despawned)
                return;
            if (collisionDirection == Vector3.Down)
            {
                var id = SandBlock.BlockID;
                if (EntityType == 71)
                    id = GravelBlock.BlockID;
                EntityManager.DespawnEntity(this);
                Vector3 position = collisionPoint + Vector3i.Up;
                var hit = Dimension.BlockRepository.GetBlockProvider(Dimension.GetBlockID((GlobalVoxelCoordinates)position));
                if (hit.BoundingBox == null && !BlockProvider.Overwritable.Any(o => o == hit.ID))
                    EntityManager.SpawnEntity(new ItemEntity(Dimension, EntityManager,
                        position + new Vector3(0.5), new ItemStack(id)));
                else
                    Dimension.SetBlockID((GlobalVoxelCoordinates)position, id);
            }
        }

        public BoundingBox BoundingBox
        {
            get
            {
                return new BoundingBox(Position - (Size / 2), Position + (Size / 2));
            }
        }

        public bool BeginUpdate()
        {
            EnablePropertyChange = false;
            return true;
        }

        public void EndUpdate(Vector3 newPosition)
        {
            EnablePropertyChange = true;
            Position = newPosition;
        }

        public float AccelerationDueToGravity
        {
            get
            {
                return 0.8f;
            }
        }

        public float Drag
        {
            get
            {
                return 0.40f;
            }
        }

        public float TerminalVelocity
        {
            get
            {
                return 39.2f;
            }
        }
    }
}