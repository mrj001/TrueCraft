using System;
using System.Linq;
using TrueCraft.Core.Logic;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.Server;
using TrueCraft.Core.World;

namespace TrueCraft.Core.Entities
{
    public class FallingGravelEntity : FallingBlockEntity
    {
        public FallingGravelEntity(IDimension dimension, IEntityManager entityManager,
            Vector3 position) : base(dimension, entityManager, position)
        {
        }

        public override byte EntityType { get { return 71; } }

        public override void TerrainCollision(Vector3 collisionPoint, Vector3 collisionDirection)
        {
            if (Despawned)
                return;
            if (collisionDirection == Vector3.Down)
            {
                byte id = (byte)BlockIDs.Gravel;
                EntityManager.DespawnEntity(this);
                Vector3 position = collisionPoint + Vector3i.Up;
                IBlockProvider hit = Dimension.BlockRepository.GetBlockProvider(Dimension.GetBlockID((GlobalVoxelCoordinates)position));
                // TODO: metadata for GetCollisionBox.
                if (hit.GetCollisionBox(0) is null && !BlockProvider.Overwritable.Any(o => o == hit.ID))
                    EntityManager.SpawnEntity(new ItemEntity(Dimension, EntityManager,
                        position + new Vector3(0.5), new ItemStack(id)));
                else
                    Dimension.SetBlockID((GlobalVoxelCoordinates)position, id);
            }
        }
    }
}