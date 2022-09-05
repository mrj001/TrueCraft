using System;
using System.Xml;
using TrueCraft.Core.Entities;
using TrueCraft.Core.Server;
using TrueCraft.Core.Networking;
using TrueCraft.Core.World;

namespace TrueCraft.Core.Logic.Blocks
{
    public class SandBlock : BlockProvider
    {
        public SandBlock(XmlNode node) : base(node)
        {
        }

        public override void BlockPlaced(BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            BlockUpdate(descriptor, descriptor, user.Server, dimension);
        }

        public override void BlockUpdate(BlockDescriptor descriptor, BlockDescriptor source, IMultiplayerServer server, IDimension dimension)
        {
            ServerOnly.Assert();

            if (dimension.GetBlockID(descriptor.Coordinates + Vector3i.Down) == (byte)BlockIDs.Air)
            {
                dimension.SetBlockID(descriptor.Coordinates, (byte)BlockIDs.Air);
                IEntityManager entityManager = ((IDimensionServer)server).EntityManager;
                entityManager.SpawnEntity(
                    new FallingSandEntity(dimension, entityManager, (Vector3)descriptor.Coordinates));
            }
        }
    }
}