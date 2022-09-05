using System;
using System.Collections.Generic;
using System.Linq;
using fNbt;
using TrueCraft.Core;
using TrueCraft.Core.Entities;
using TrueCraft.Core.Inventory;
using TrueCraft.Core.Logic;
using TrueCraft.Core.Logic.Blocks;
using TrueCraft.Core.Logic.Items;
using TrueCraft.Core.Networking;
using TrueCraft.Core.Networking.Packets;
using TrueCraft.Core.Server;
using TrueCraft.Core.World;
using TrueCraft.Inventory;

namespace TrueCraft.Handlers
{
    public static class InteractionHandlers
    {
        private static IServerServiceLocator _serviceLocator = null!;

        public static IServerServiceLocator ServiceLocator
        {
            set
            {
                _serviceLocator = value;
            }
        }


        public static void HandlePlayerDiggingPacket(IPacket _packet, IRemoteClient _client, IMultiplayerServer server)
        {
            var packet = (PlayerDiggingPacket)_packet;
            var client = (RemoteClient)_client;
            IDimension dimension = _client.Dimension!;
            var position = new GlobalVoxelCoordinates(packet.X, packet.Y, packet.Z);
            var descriptor = dimension.GetBlockData(position);
            var provider = _serviceLocator.BlockRepository.GetBlockProvider(descriptor.ID);
            short damage;
            int time;
            switch (packet.PlayerAction)
            {
                case PlayerDiggingPacket.Action.DropItem:
                    // Throwing item
                    if (client.SelectedItem.Empty)
                        break;
                    var spawned = client.SelectedItem;
                    spawned.Count = 1;
                    var inventory = client.SelectedItem;
                    inventory.Count--;
                    IEntityManager entityManager = ((IDimensionServer)dimension).EntityManager;
                    var item = new ItemEntity(dimension, entityManager, client.Entity!.Position + new Vector3(0, PlayerEntity.Height, 0), spawned);
                    item.Velocity = MathHelper.RotateY(Vector3.Forwards, MathHelper.DegreesToRadians(client.Entity.Yaw)) * 0.5;
                    client.Hotbar[client.SelectedSlot].Item = inventory;
                    entityManager.SpawnEntity(item);
                    break;

                case PlayerDiggingPacket.Action.StartDigging:
                    foreach (var nearbyClient in server.Clients) // TODO: Send this repeatedly during the course of the digging
                    {
                        var c = (RemoteClient)nearbyClient;
                        if (c.KnownEntities.Contains(client.Entity!))
                            c.QueuePacket(new AnimationPacket(client.Entity!.EntityID, AnimationPacket.PlayerAnimation.SwingArm));
                    }
                    if (provider is null)
                        server.SendMessage(ChatColor.Red + "WARNING: block provider for ID {0} is null (player digging)", descriptor.ID);
                    else
                        provider.BlockLeftClicked(_serviceLocator, descriptor, packet.Face, dimension, client);

                    // "But why on Earth does this behavior change if you use shears on leaves?"
                    // "This is poor seperation of concerns"
                    // "Let me do a git blame and flame whoever wrote the next line"
                    // To answer all of those questions, here:
                    // Minecraft sends a player digging packet when the player starts and stops digging a block (two packets)
                    // However, it only sends ONE packet if the block would be mined immediately - which usually is only the case
                    // for blocks that have a hardness equal to zero.
                    // The exception to this rule is shears on leaves. Leaves normally have a hardness of 0.2, but when you mine them
                    // using shears the client only sends the start digging packet and expects them to be mined immediately.
                    // So if you want to blame anyone, send flames to Notch for the stupid idea of not sending "stop digging" packets
                    // for hardness == 0 blocks.

                    time = BlockProvider.GetHarvestTime(_serviceLocator, descriptor.ID, client.SelectedItem.ID, out damage);
                    if (time <= 20)
                    {
                        provider?.BlockMined(descriptor, packet.Face, dimension, client);
                        break;
                    }
                    client.ExpectedDigComplete = DateTime.UtcNow.AddMilliseconds(time);
                    break;
                case PlayerDiggingPacket.Action.StopDigging:
                    foreach (var nearbyClient in server.Clients)
                    {
                        var c = (RemoteClient)nearbyClient;
                        if (c.KnownEntities.Contains(client.Entity!))
                            c.QueuePacket(new AnimationPacket(client.Entity!.EntityID, AnimationPacket.PlayerAnimation.None));
                    }
                    if (provider != null && descriptor.ID != 0)
                    {
                        time = BlockProvider.GetHarvestTime(_serviceLocator, descriptor.ID, client.SelectedItem.ID, out damage);
                        if (time <= 20)
                            break; // Already handled earlier
                        var diff = (DateTime.UtcNow - client.ExpectedDigComplete).TotalMilliseconds;
                        if (diff > -100) // Allow a small tolerance
                        {
                            provider.BlockMined(descriptor, packet.Face, dimension, client);
                            // Damage the item
                            if (damage != 0)
                            {
                                IDurableItem? tool = _serviceLocator.ItemRepository.GetItemProvider(client.SelectedItem.ID) as IDurableItem;
                                if (tool is not null)
                                {
                                    var slot = client.SelectedItem;
                                    slot.Metadata += damage;
                                    if (slot.Metadata >= tool.Durability)
                                        slot.Count = 0; // Destroy item
                                    client.Hotbar[client.SelectedSlot].Item = slot;
                                }
                            }
                        }
                    }
                    break;
            }
        }

        public static void HandlePlayerBlockPlacementPacket(IPacket _packet, IRemoteClient _client, IMultiplayerServer server)
        {
            var packet = (PlayerBlockPlacementPacket)_packet;
            var client = (RemoteClient)_client;

            var slot = client.SelectedItem;
            var position = new GlobalVoxelCoordinates(packet.X, packet.Y, packet.Z);
            BlockDescriptor? block = null;
            if (position != -GlobalVoxelCoordinates.One)
            {
                if (position.DistanceTo(client.Entity!.Position) > 10 /* TODO: Reach */)
                    return;
                block = client.Dimension!.GetBlockData(position);
            }
            else
            {
                // TODO: Handle situations like firing arrows and such? Is that how it works?
                return;
            }
            bool use = true;
            if (block != null)
            {
                var provider = _serviceLocator.BlockRepository.GetBlockProvider(block.Value.ID);
                if (provider == null)
                {
                    server.SendMessage(ChatColor.Red + "WARNING: block provider for ID {0} is null (player placing)", block.Value.ID);
                    server.SendMessage(ChatColor.Red + "Error occured from client {0} at coordinates {1}", client.Username ?? "?", block.Value.Coordinates);
                    server.SendMessage(ChatColor.Red + "Packet logged at {0}, please report upstream", DateTime.UtcNow);
                    return;
                }
                if (!provider.BlockRightClicked(_serviceLocator, block.Value, packet.Face, client.Dimension, client))
                {
                    position += MathHelper.BlockFaceToCoordinates(packet.Face);
                    var oldID = client.Dimension.GetBlockID(position);
                    var oldMeta = client.Dimension.GetMetadata(position);
                    // TODO: BlockChangePacket should have new ID & metadata, not old; Naming issue?
                    client.QueuePacket(new BlockChangePacket(position.X, (sbyte)position.Y, position.Z, (sbyte)oldID, (sbyte)oldMeta));
                    // TODO: why send SetSlot when there is no change??
                    client.QueuePacket(new SetSlotPacket(0, client.SelectedSlot, client.SelectedItem.ID, client.SelectedItem.Count, client.SelectedItem.Metadata));
                    return;
                }
            }
            if (!slot.Empty)
            {
                if (use)
                {
                    var itemProvider = _serviceLocator.ItemRepository.GetItemProvider(slot.ID);
                    if (itemProvider == null)
                    {
                        server.SendMessage(ChatColor.Red + "WARNING: item provider for ID {0} is null (player placing)", block?.ID.ToString() ?? "(null)");
                        server.SendMessage(ChatColor.Red + "Error occured from client {0} at coordinates {1}", client.Username ?? "?", block?.Coordinates.ToString() ?? "(null)" );
                        server.SendMessage(ChatColor.Red + "Packet logged at {0}, please report upstream", DateTime.UtcNow);
                    }
                    if (block != null)
                    {
                        if (itemProvider != null)
                            itemProvider.ItemUsedOnBlock(position, slot, packet.Face, client.Dimension, client);
                    }
                    else
                    {
                        // TODO: Use item
                    }
                }
            }
        }

        public static void HandleClickWindowPacket(IPacket _packet, IRemoteClient _client, IMultiplayerServer server)
        {
            var packet = (ClickWindowPacket)_packet;
            var client = (RemoteClient)_client;
            IWindow<IServerSlot> window = client.CurrentWindow;

            // Confirm expected Window ID
            if (packet.WindowID != window.WindowID)
            {
                server.Log(Core.Logging.LogCategory.Notice, "Invalid window number received {0}; expected {1}", packet.WindowID, window.WindowID);
                server.DisconnectClient(_client);
                return;
            }

            if (packet.SlotIndex == -999)
            {
                IDimensionServer dimension = ((IDimensionServer)client.Dimension!);
                IEntityManager entityManager = dimension.EntityManager;
                // Throwing item
                ItemEntity item;
                if (packet.RightClick)
                {
                    var spawned = client.ItemStaging;
                    spawned.Count = 1;
                    var inventory = client.ItemStaging;
                    inventory.Count--;
                    item = new ItemEntity(dimension, entityManager,
                        client.Entity!.Position + new Vector3(0, PlayerEntity.Height, 0), spawned);
                    client.ItemStaging = inventory;
                }
                else
                {
                    item = new ItemEntity(dimension, entityManager,
                        client.Entity!.Position + new Vector3(0, PlayerEntity.Height, 0), client.ItemStaging);
                    client.ItemStaging = ItemStack.EmptyStack;
                }
                item.Velocity = MathHelper.FowardVector(client.Entity.Yaw) * 0.3;
                entityManager.SpawnEntity(item);
                return;
            }

            // Confirm reasonable slot index.
            if (packet.SlotIndex >= window.Count || packet.SlotIndex < 0)
            {
                server.Log(Core.Logging.LogCategory.Notice, "Illegal slot number received {0} in not in the set -999, [0, {1})", packet.SlotIndex, window.Count);
                server.DisconnectClient(_client);
                return;
            }

            // TODO confirm prior content of Slot

            ((IServerWindow)window).HandleClick(client, packet);
        }

        public static void HandleCloseWindowPacket(IPacket _packet, IRemoteClient _client, IMultiplayerServer server)
        {
            var packet = (CloseWindowPacket)_packet;
            if (packet.WindowID != 0)
                ((RemoteClient)_client).CloseWindow(true);
        }

        public static void HandleChangeHeldItem(IPacket _packet, IRemoteClient _client, IMultiplayerServer server)
        {
            var packet = (ChangeHeldItemPacket)_packet;
            var client = (RemoteClient)_client;
            client.SelectedSlot = packet.Slot;
            IList<IRemoteClient> notified = ((IDimensionServer)client.Dimension!).EntityManager.ClientsForEntity(client.Entity!);
            foreach (IRemoteClient c in notified)
                c.QueuePacket(new EntityEquipmentPacket(client.Entity!.EntityID, 0, client.SelectedItem.ID, client.SelectedItem.Metadata));
        }

        public static void HandlePlayerAction(IPacket _packet, IRemoteClient _client, IMultiplayerServer server)
        {
            var packet = (PlayerActionPacket)_packet;
            var client = (RemoteClient)_client;
            PlayerEntity entity = (PlayerEntity)client.Entity!;
            switch (packet.Action)
            {
                case PlayerActionPacket.PlayerAction.Crouch:
                    entity.EntityFlags |= EntityFlags.Crouched;
                    break;
                case PlayerActionPacket.PlayerAction.Uncrouch:
                    entity.EntityFlags &= ~EntityFlags.Crouched;
                    break;
            }
        }

        public static void HandleAnimation(IPacket _packet, IRemoteClient _client, IMultiplayerServer server)
        {
            var packet = (AnimationPacket)_packet;
            var client = (RemoteClient)_client;
            if (packet.EntityID == client.Entity!.EntityID)
            {
                IList<IRemoteClient> nearby = ((IDimensionServer)client.Dimension!).EntityManager
                    .ClientsForEntity(client.Entity);
                foreach (IRemoteClient player in nearby)
                    player.QueuePacket(packet);
            }
        }

        public static void HandleUpdateSignPacket(IPacket _packet, IRemoteClient _client, IMultiplayerServer server)
        {
            var packet = (UpdateSignPacket)_packet;
            var client = (RemoteClient)_client;
            var coords = new GlobalVoxelCoordinates(packet.X, packet.Y, packet.Z);
            if (client.Entity!.Position.DistanceTo((Vector3)coords) < 10) // TODO: Reach
            {
                var block = client.Dimension!.GetBlockID(coords);
                if (block == (byte)BlockIDs.Sign || block == (byte)BlockIDs.WallSign)
                {
                    ((IDimensionServer)client.Dimension).SetTileEntity(coords, new NbtCompound(new[]
                    {
                        new NbtString("Text1", packet.Text[0]),
                        new NbtString("Text2", packet.Text[1]),
                        new NbtString("Text3", packet.Text[2]),
                        new NbtString("Text4", packet.Text[3]),
                    }));
                    // TODO: Some utility methods for things like "clients with given chunk loaded"
                    server.Clients.Where(c => ((RemoteClient)c).LoggedIn && c.Dimension == _client.Dimension).ToList().ForEach(c => c.QueuePacket(packet));
                }
            }
        }
    }
}