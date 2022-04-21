﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TrueCraft.Core;
using TrueCraft.Core.Entities;
using TrueCraft.Core.Logic;
using TrueCraft.Core.Networking;
using TrueCraft.Core.Networking.Packets;
using TrueCraft.Core.Physics;
using TrueCraft.Core.Server;
using TrueCraft.Core.World;

namespace TrueCraft
{
    public class EntityManager : IEntityManager
    {
        public TimeSpan TimeSinceLastUpdate { get; private set; }
        public IDimension Dimension { get; }

        private readonly IMultiplayerServer _server;

        private readonly PhysicsEngine _physicsEngine;

        private int _nextEntityID;

        private readonly List<IEntity> _entities; // TODO: Persist to disk

        private object _entityLock = new object();

        private readonly ConcurrentBag<IEntity> _pendingDespawns;

        private DateTime _lastUpdate;

        public EntityManager(IMultiplayerServer server, IDimension dimension)
        {
            _server = server;
            Dimension = dimension;
            _physicsEngine = new PhysicsEngine(dimension, (BlockRepository)server.BlockRepository);
            _pendingDespawns = new ConcurrentBag<IEntity>();
            _entities = new List<IEntity>();
            // TODO: Handle loading worlds that already have entities
            // Note: probably not the concern of EntityManager. The server could manually set this?
            _nextEntityID = 1;
            _lastUpdate = DateTime.UtcNow;
            TimeSinceLastUpdate = TimeSpan.Zero;
        }

        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var entity = sender as IEntity;
            if (entity == null)
                throw new InvalidCastException("Attempted to handle property changes for non-entity");
            if (entity is PlayerEntity)
                HandlePlayerPropertyChanged(e.PropertyName, entity as PlayerEntity);
            switch (e.PropertyName)
            {
                case "Position":
                case "Yaw":
                case "Pitch":
                    PropegateEntityPositionUpdates(entity);
                    break;
                case "Metadata":
                    PropegateEntityMetadataUpdates(entity);
                    break;
            }
        }

        private void HandlePlayerPropertyChanged(string property, PlayerEntity entity)
        {
            var client = GetClientForEntity(entity) as RemoteClient;
            if (client == null)
                return; // Note: would an exception be appropriate here?
            switch (property)
            {
                case "Position":
                    if ((int)(entity.Position.X) >> 4 != (int)(entity.OldPosition.X) >> 4 ||
                        (int)(entity.Position.Z) >> 4 != (int)(entity.OldPosition.Z) >> 4)
                    {
                        client.Log("Passed chunk boundary at {0}, {1}", (int)(entity.Position.X) >> 4, (int)(entity.Position.Z) >> 4);
                        _server.Scheduler.ScheduleEvent("client.update-chunks", client,
                            TimeSpan.Zero, s => client.UpdateChunks());
                        UpdateClientEntities(client);
                    }
                    break;
            }
        }
        
        internal void UpdateClientEntities(RemoteClient client)
        {
            var entity = client.Entity;
            // Calculate entities you shouldn't know about anymore
            for (int i = 0; i < client.KnownEntities.Count; i++)
            {
                var knownEntity = client.KnownEntities[i];
                if (knownEntity.Position.DistanceTo(entity.Position) > client.ChunkRadius * WorldConstants.ChunkDepth)
                {
                    client.QueuePacket(new DestroyEntityPacket(knownEntity.EntityID));
                    client.KnownEntities.Remove(knownEntity);
                    i--;
                    // Make sure you're despawned on other clients if you move away from stationary players
                    if (knownEntity is PlayerEntity)
                    {
                        var c = (RemoteClient)GetClientForEntity(knownEntity as PlayerEntity);
                        if (c.KnownEntities.Contains(entity))
                        {
                            c.KnownEntities.Remove(entity);
                            c.QueuePacket(new DestroyEntityPacket(entity.EntityID));
                            c.Log("Destroying entity {0} ({1})", knownEntity.EntityID, knownEntity.GetType().Name);
                        }
                    }
                    client.Log("Destroying entity {0} ({1})", knownEntity.EntityID, knownEntity.GetType().Name);
                }
            }
            // Calculate entities you should now know about
            var toSpawn = GetEntitiesInRange(entity, client.ChunkRadius);
            foreach (var e in toSpawn)
            {
                if (e != entity && !client.KnownEntities.Contains(e))
                {
                    SendEntityToClient(client, e);
                    // Make sure other players know about you since you've moved
                    if (e is PlayerEntity)
                    {
                        var c = (RemoteClient)GetClientForEntity(e as PlayerEntity);
                        if (!c.KnownEntities.Contains(entity))
                            SendEntityToClient(c, entity);
                    }
                }
            }
        }

        private void PropegateEntityPositionUpdates(IEntity entity)
        {
            for (int i = 0, ServerClientsCount = _server.Clients.Count; i < ServerClientsCount; i++)
            {
                var client = _server.Clients[i] as RemoteClient;
                if (client.Entity == entity)
                    continue; // Do not send movement updates back to the client that triggered them
                if (client.KnownEntities.Contains(entity))
                {
                    // TODO: Consider using more kinds of entity packets (i.e. EntityRelativeMovePacket) that may be more effecient
                    // In the past I've done this and entity positions quickly got very inaccurate on the client.
                    client.QueuePacket(new EntityTeleportPacket(entity.EntityID,
                        MathHelper.CreateAbsoluteInt(entity.Position.X),
                        MathHelper.CreateAbsoluteInt(entity.Position.Y),
                        MathHelper.CreateAbsoluteInt(entity.Position.Z),
                        MathHelper.CreateRotationByte(entity.Yaw),
                        MathHelper.CreateRotationByte(entity.Pitch)));
                }
            }
        }

        private void PropegateEntityMetadataUpdates(IEntity entity)
        {
            if (!entity.SendMetadataToClients)
                return;
            for (int i = 0, ServerClientsCount = _server.Clients.Count; i < ServerClientsCount; i++)
            {
                var client = _server.Clients[i] as RemoteClient;
                if (client.Entity == entity)
                    continue; // Do not send movement updates back to the client that triggered them
                if (client.KnownEntities.Contains(entity))
                    client.QueuePacket(new EntityMetadataPacket(entity.EntityID, entity.Metadata));
            }
        }

        private bool IsInRange(Vector3 a, Vector3 b, int range)
        {
            return Math.Abs(a.X - b.X) < range * WorldConstants.ChunkWidth &&
                Math.Abs(a.Z - b.Z) < range * WorldConstants.ChunkDepth;
        }

        private IEntity[] GetEntitiesInRange(IEntity entity, int maxChunks)
        {
            return _entities.Where(e => e.EntityID != entity.EntityID && !e.Despawned && IsInRange(e.Position, entity.Position, maxChunks)).ToArray();
        }

        private void SendEntityToClient(RemoteClient client, IEntity entity)
        {
            if (entity.EntityID == -1)
                return; // We haven't finished setting this entity up yet
            client.Log("Spawning entity {0} ({1}) at {2}", entity.EntityID, entity.GetType().Name, (GlobalVoxelCoordinates)entity.Position);
            RemoteClient spawnedClient = null;
            if (entity is PlayerEntity)
                spawnedClient = (RemoteClient)GetClientForEntity(entity as PlayerEntity);
            client.KnownEntities.Add(entity);
            client.QueuePacket(entity.SpawnPacket);
            if (entity is IPhysicsEntity)
            {
                var pentity = entity as IPhysicsEntity;
                client.QueuePacket(new EntityVelocityPacket
                    {
                        EntityID = entity.EntityID,
                        XVelocity = (short)(pentity.Velocity.X * 320),
                        YVelocity = (short)(pentity.Velocity.Y * 320),
                        ZVelocity = (short)(pentity.Velocity.Z * 320),
                    });
            }
            if (entity.SendMetadataToClients)
                client.QueuePacket(new EntityMetadataPacket(entity.EntityID, entity.Metadata));
            if (spawnedClient != null)
            {
                // Send equipment when spawning player entities
                client.QueuePacket(new EntityEquipmentPacket(entity.EntityID,
                        0, spawnedClient.SelectedItem.ID, spawnedClient.SelectedItem.Metadata));
                client.QueuePacket(new EntityEquipmentPacket(entity.EntityID,
                        4, spawnedClient.Armor[0].Item.ID, spawnedClient.Armor[0].Item.Metadata));
                client.QueuePacket(new EntityEquipmentPacket(entity.EntityID,
                        3, spawnedClient.Armor[1].Item.ID, spawnedClient.Armor[1].Item.Metadata));
                client.QueuePacket(new EntityEquipmentPacket(entity.EntityID,
                        2, spawnedClient.Armor[2].Item.ID, spawnedClient.Armor[2].Item.Metadata));
                client.QueuePacket(new EntityEquipmentPacket(entity.EntityID,
                        1, spawnedClient.Armor[3].Item.ID, spawnedClient.Armor[3].Item.Metadata));
            }
        }

        private IRemoteClient GetClientForEntity(PlayerEntity entity)
        {
            return _server.Clients.SingleOrDefault(c => c.Entity != null && c.Entity.EntityID == entity.EntityID);
        }

        public IList<IEntity> EntitiesInRange(Vector3 center, float radius)
        {
            return _entities.Where(e => !e.Despawned && e.Position.DistanceTo(center) < radius).ToList();
        }

        public IList<IRemoteClient> ClientsForEntity(IEntity entity)
        {
            return _server.Clients.Where(c => (c as RemoteClient).KnownEntities.Contains(entity)).ToList();
        }

        public void SpawnEntity(IEntity entity)
        {
            if (entity.Despawned)
                return;
            entity.SpawnTime = DateTime.UtcNow;
            entity.EntityManager = this;
            entity.Dimension = Dimension;
            entity.EntityID = _nextEntityID++;
            entity.PropertyChanged -= HandlePropertyChanged;
            entity.PropertyChanged += HandlePropertyChanged;
            lock (_entityLock)
            {
                _entities.Add(entity);
            }
            foreach (var clientEntity in GetEntitiesInRange(entity, 8)) // Note: 8 is pretty arbitrary here
            {
                if (clientEntity != entity && clientEntity is PlayerEntity)
                {
                    var client = (RemoteClient)GetClientForEntity((PlayerEntity)clientEntity);
                    SendEntityToClient(client, entity);
                }
            }
            if (entity is IPhysicsEntity)
                _physicsEngine.AddEntity(entity as IPhysicsEntity);
        }

        public void DespawnEntity(IEntity entity)
        {
            entity.Despawned = true;
            _pendingDespawns.Add(entity);
        }

        public void FlushDespawns()
        {
            IEntity entity;
            while (_pendingDespawns.Count != 0)
            {
                while (!_pendingDespawns.TryTake(out entity))
                    ;
                if (entity is IPhysicsEntity)
                    _physicsEngine.RemoveEntity((IPhysicsEntity)entity);
                lock ((_server as MultiplayerServer).ClientLock) // TODO: Thread safe way to iterate over client collection
                {
                    for (int i = 0, ServerClientsCount = _server.Clients.Count; i < ServerClientsCount; i++)
                    {
                        var client = (RemoteClient)_server.Clients[i];
                        if (client.KnownEntities.Contains(entity) && !client.Disconnected)
                        {
                            client.QueuePacket(new DestroyEntityPacket(entity.EntityID));
                            client.KnownEntities.Remove(entity);
                            client.Log("Destroying entity {0} ({1})", entity.EntityID, entity.GetType().Name);
                        }
                    }
                }
                lock (_entityLock)
                    _entities.Remove(entity);
            }
        }

        public IEntity GetEntityByID(int id)
        {
            return _entities.SingleOrDefault(e => e.EntityID == id);
        }

        public void Update()
        {
            TimeSinceLastUpdate = DateTime.UtcNow - _lastUpdate;
            _lastUpdate = DateTime.UtcNow;
            _physicsEngine.Update(TimeSinceLastUpdate);
            try
            {
                lock (_entities)
                {
                    foreach (var e in _entities)
                    {
                        if (!e.Despawned)
                            e.Update(this);
                    }
                }
            }
            catch
            {
                // Do nothing
            }
            FlushDespawns();
        }

        /// <summary>
        /// Performs the initial population of client entities.
        /// </summary>
        public void SendEntitiesToClient(IRemoteClient _client)
        {
            var client = _client as RemoteClient;
            foreach (var entity in GetEntitiesInRange(client.Entity, client.ChunkRadius))
            {
                if (entity != client.Entity)
                    SendEntityToClient(client, entity);
            }
        }
    }
}