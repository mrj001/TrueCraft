using System;
using TrueCraft.Core.World;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.Logic;
using System.Linq;
using TrueCraft.Core.Server;

namespace TrueCraft.Core.Entities
{
    public class FallingSandEntity : FallingBlockEntity
    {
        public FallingSandEntity(IDimension dimension, IEntityManager entityManager,
            Vector3 position) : base(dimension, entityManager, position)
        {
        }

        public override byte EntityType { get { return 70; } }

        public override void TerrainCollision(Vector3 collisionPoint, Vector3 collisionDirection)
        {
            if (Despawned)
                return;
            if (collisionDirection == Vector3.Down)
            {
                byte id = (byte)BlockIDs.Sand;
                EntityManager.DespawnEntity(this);
                Vector3 position = collisionPoint + Vector3i.Up;
                IBlockProvider hit = Dimension.BlockRepository.GetBlockProvider(Dimension.GetBlockID((GlobalVoxelCoordinates)position));
                // TODO: Is 0 metadata appropriate?
                if (hit.GetCollisionBox(0) is null && !BlockProvider.Overwritable.Any(o => o == hit.ID))
                    EntityManager.SpawnEntity(new ItemEntity(Dimension, EntityManager,
                        position + new Vector3(0.5), new ItemStack(id)));
                else
                    Dimension.SetBlockID((GlobalVoxelCoordinates)position, id);
            }
        }
    }
}