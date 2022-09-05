using System;
using System.Xml;
using TrueCraft.Core.Logic.Items;
using TrueCraft.Core.Networking;
using TrueCraft.Core.Server;
using TrueCraft.Core.World;

namespace TrueCraft.Core.Logic.Blocks
{
    public class CropsBlock : BlockProvider
    {
        public CropsBlock(XmlNode node) : base(node)
        {

        }

        // TODO: check bounding box & interactive bounding box in TrueCraft.xml

        protected override ItemStack[] GetDrop(BlockDescriptor descriptor, ItemStack item)
        {
            if (descriptor.Metadata >= 7)
                return new[] { new ItemStack((short)ItemIDs.Wheat), new ItemStack(SeedsItem.ItemID, (sbyte)MathHelper.Random.Next(3)) };
            else
                return new[] { new ItemStack(SeedsItem.ItemID) };
        }

        private void GrowBlock(IMultiplayerServer server, IChunk chunk, LocalVoxelCoordinates coords)
        {
            if ((BlockIDs)chunk.GetBlockID(coords) != BlockIDs.Crops)
                return;

            byte meta = chunk.GetMetadata(coords);
            meta++;
            chunk.SetMetadata(coords, meta);
            if (meta < 7)
            {
                server.Scheduler.ScheduleEvent("crops",
                    chunk, TimeSpan.FromSeconds(MathHelper.Random.Next(30, 60)),
                   (_server) => GrowBlock(_server, chunk, coords));
            }
        }

        public override void BlockUpdate(BlockDescriptor descriptor, BlockDescriptor source, IMultiplayerServer server, IDimension dimension)
        {
            if (dimension.GetBlockID(descriptor.Coordinates + Vector3i.Down) != (byte)BlockIDs.Farmland)
            {
                GenerateDropEntity(descriptor, dimension, server, ItemStack.EmptyStack);
                dimension.SetBlockID(descriptor.Coordinates, 0);
            }
        }

        public override void BlockPlaced(BlockDescriptor descriptor, BlockFace face, IDimension dimension, IRemoteClient user)
        {
            GlobalVoxelCoordinates coordinates = descriptor.Coordinates + MathHelper.BlockFaceToCoordinates(face);
            IChunk chunk = dimension.GetChunk(coordinates)!;
            user.Server.Scheduler.ScheduleEvent("crops", chunk,
                TimeSpan.FromSeconds(MathHelper.Random.Next(30, 60)),
                (server) => GrowBlock(server, chunk, (LocalVoxelCoordinates)coordinates));
        }

        public override void BlockLoadedFromChunk(IMultiplayerServer server, IDimension dimension, GlobalVoxelCoordinates coordinates)
        {
            IChunk chunk = dimension.GetChunk(coordinates)!;
            server.Scheduler.ScheduleEvent("crops", chunk,
                TimeSpan.FromSeconds(MathHelper.Random.Next(30, 60)),
                (s) => GrowBlock(s, chunk, (LocalVoxelCoordinates)coordinates));
        }
    }
}
