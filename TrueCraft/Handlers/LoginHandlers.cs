using System;
using System.Linq;
using TrueCraft.Core;
using TrueCraft.Core.Entities;
using TrueCraft.Core.Networking;
using TrueCraft.Core.Networking.Packets;
using TrueCraft.Core.Server;
using TrueCraft.Core.World;
using TrueCraft.Inventory;
using TrueCraft.World;

namespace TrueCraft.Handlers
{
    internal static class LoginHandlers
    {
        public static void HandleHandshakePacket(IPacket packet, IRemoteClient client, IMultiplayerServer server)
        {
            var handshakePacket = (HandshakePacket) packet;
            var remoteClient = (RemoteClient)client;
            remoteClient.Username = handshakePacket.Username;
            remoteClient.QueuePacket(new HandshakeResponsePacket("-")); // TODO: Implement some form of authentication
        }

        public static void HandleLoginRequestPacket(IPacket packet, IRemoteClient client, IMultiplayerServer server)
        {
            var loginRequestPacket = (LoginRequestPacket)packet;
            var remoteClient = (RemoteClient)client;
            if (loginRequestPacket.ProtocolVersion < server.PacketReader.ProtocolVersion)
                remoteClient.QueuePacket(new DisconnectPacket("Client outdated! Use beta 1.7.3."));
            else if (loginRequestPacket.ProtocolVersion > server.PacketReader.ProtocolVersion)
                remoteClient.QueuePacket(new DisconnectPacket("Server outdated! Use beta 1.7.3."));
            else if (((IWorld)server.World!).Count == 0)
                remoteClient.QueuePacket(new DisconnectPacket("Server has no worlds configured."));
            else if (!server.PlayerIsWhitelisted(remoteClient.Username!) && server.PlayerIsBlacklisted(remoteClient.Username!))
                remoteClient.QueuePacket(new DisconnectPacket("You're banned from this server"));
            else if (server.Clients.Count(c => c.Username == client.Username) > 1)
                remoteClient.QueuePacket(new DisconnectPacket("The player with this username is already logged in"));
            else
            {
                remoteClient.LoggedIn = true;
                IDimensionServer dimension = (IDimensionServer)((IWorld)server.World)[DimensionID.Overworld];  // TODO read dimension from saved player data.
                remoteClient.Entity = new PlayerEntity(dimension, dimension.EntityManager, remoteClient.Username!);
                remoteClient.Dimension = dimension;
                remoteClient.ChunkRadius = 2;

                if (!remoteClient.Load())
                    remoteClient.Entity.Position = new Vector3(0, 0, 0);  // TODO read default Spawn Point from World object.
                // Make sure they don't spawn in the ground
                var collision = new Func<bool>(() =>
                {
                    byte feet = client.Dimension!.GetBlockID((GlobalVoxelCoordinates)client.Entity!.Position);
                    byte head = client.Dimension.GetBlockID((GlobalVoxelCoordinates)(client.Entity.Position + Vector3.Up));
                    BoundingBox? feetBox = server.BlockRepository.GetBlockProvider(feet).GetCollisionBox(0);
                    BoundingBox? headBox = server.BlockRepository.GetBlockProvider(head).GetCollisionBox(0);
                    return feetBox.HasValue || headBox.HasValue;
                });
                while (collision())
                    client.Entity!.Position += Vector3.Up;

                IEntityManager entityManager = ((IDimensionServer)remoteClient.Dimension).EntityManager;
                entityManager.SpawnEntity(remoteClient.Entity);

                // Send setup packets
                remoteClient.QueuePacket(new LoginResponsePacket(client.Entity!.EntityID, 0, DimensionID.Overworld));
                remoteClient.UpdateChunks(block: true);
                remoteClient.QueuePacket(((IServerWindow)remoteClient.InventoryWindowContent).GetWindowItemsPacket());
                remoteClient.QueuePacket(new UpdateHealthPacket(((PlayerEntity)remoteClient.Entity).Health));
                remoteClient.QueuePacket(new SpawnPositionPacket((int)remoteClient.Entity.Position.X,
                        (int)remoteClient.Entity.Position.Y, (int)remoteClient.Entity.Position.Z));
                remoteClient.QueuePacket(new SetPlayerPositionPacket(remoteClient.Entity.Position.X,
                        remoteClient.Entity.Position.Y + 1,
                        remoteClient.Entity.Position.Y + remoteClient.Entity.Size.Height + 1,
                        remoteClient.Entity.Position.Z, remoteClient.Entity.Yaw, remoteClient.Entity.Pitch, true));
                remoteClient.QueuePacket(new TimeUpdatePacket(remoteClient.Dimension.TimeOfDay));

                // Start housekeeping for this client
                entityManager.SendEntitiesToClient(remoteClient);
                server.Scheduler.ScheduleEvent("remote.keepalive", remoteClient, TimeSpan.FromSeconds(10), remoteClient.SendKeepAlive);
                server.Scheduler.ScheduleEvent("remote.chunks", remoteClient, TimeSpan.FromSeconds(1), remoteClient.ExpandChunkRadius);

                if (!string.IsNullOrEmpty(Program.ServerConfiguration?.MOTD))
                    remoteClient.SendMessage(Program.ServerConfiguration.MOTD);
                if (!(Program.ServerConfiguration?.Singleplayer ?? ServerConfiguration.SinglePlayerDefault))
                    server.SendMessage(ChatColor.Yellow + "{0} joined the server.", remoteClient.Username!);
            }
        }
    }
}