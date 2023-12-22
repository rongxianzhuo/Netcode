using System;
using System.Collections.Generic;
using Netcode.Components;
using Netcode.Message;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Netcode.Core
{
    public abstract class NetworkManager
    {

        public object AttachObject;
        public INetworkPrefabLoader NetworkPrefabLoader;
        
        private int _allocateNetworkObjectId;
        private readonly List<int> _toDestroyNetworkObjectIds = new List<int>();
        private readonly Dictionary<int, NetworkObject> _networkObjects = new Dictionary<int, NetworkObject>();

        internal NetworkDriver Driver { get; private set; }

        internal NetworkPipeline UnreliablePipeline { get; private set; }

        internal NetworkPipeline ReliableSequencedPipeline { get; private set; }

        internal NetworkPipeline UnreliableSequencedPipeline { get; private set; }

        public bool IsRunning => Driver.IsCreated;

        protected void CreateNetworkDriver()
        {
            var settings = new NetworkSettings();
            settings.WithSimulatorStageParameters(
                maxPacketCount: 1000,
                packetDropPercentage: 10,
                mode: ApplyMode.SentPacketsOnly,
                packetDelayMs: 50);
            Driver = NetworkDriver.Create(settings);
            UnreliablePipeline = Driver.CreatePipeline(typeof(SimulatorPipelineStage));
            ReliableSequencedPipeline = Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage), typeof(SimulatorPipelineStage));
            UnreliableSequencedPipeline = Driver.CreatePipeline(typeof(UnreliableSequencedPipelineStage), typeof(SimulatorPipelineStage));
        }

        protected void DestroyNetworkDriver()
        {
            Driver.Dispose();
            Driver = default;
        }

        public void SpawnNetworkObject(NetworkObject networkObject, int ownerId)
        {
            var networkObjectId = _allocateNetworkObjectId++;
            _networkObjects[networkObjectId] = networkObject;
            networkObject.NetworkInit(false, this);
            networkObject.NetworkStart(ownerId, networkObjectId, ServerNetworkManager.ClientId == ownerId);
        }

        public void DestroyNetworkObject(ref DataStreamReader reader)
        {
            if (!_networkObjects.TryGetValue(reader.ReadInt(), out var networkObject))
            {
                return;
            }
            _networkObjects.Remove(networkObject.NetworkObjectId);
            if (networkObject != null) Object.Destroy(networkObject.gameObject);
        }

        protected void UpdateNetworkObject(ref DataStreamReader reader)
        {
            var networkObjectId = reader.ReadInt();
            if (!_networkObjects.TryGetValue(networkObjectId, out var networkObject)) return;
            var networkBehaviour = networkObject.NetworkBehaviours[reader.ReadInt()];
            var networkVariable = networkBehaviour.NetworkVariables[reader.ReadInt()];
            networkVariable.Deserialize(ref reader);
        }

        protected void SpawnNetworkObject(int myClientId, ref DataStreamReader reader)
        {
            var prefabId = reader.ReadInt();
            var ownerId = reader.ReadInt();
            var networkObjectId = reader.ReadInt();
            var alreadySpawn = _networkObjects.TryGetValue(networkObjectId, out var networkObject);
            if (alreadySpawn)
            {
                Debug.LogError($"NetworkObject already spawn: {networkObjectId}");
            }
            else
            {
                networkObject = NetworkPrefabLoader.Instantiate(prefabId, Vector3.zero, Quaternion.identity);
                networkObject.name = "Client";
                networkObject.NetworkInit(true, this);
            }
            
            var changeBehaviourCount = reader.ReadInt();
            while (changeBehaviourCount-- > 0)
            {
                var networkBehaviour = networkObject.NetworkBehaviours[reader.ReadInt()];
                var changeVariableCount = reader.ReadInt();
                while (changeVariableCount-- > 0)
                {
                    var networkVariable = networkBehaviour.NetworkVariables[reader.ReadInt()];
                    networkVariable.Deserialize(ref reader);
                }
            }
            if (alreadySpawn) return;
            _networkObjects[networkObjectId] = networkObject;
            networkObject.NetworkStart(ownerId, networkObjectId, myClientId == ownerId);
        }

        internal void BroadcastDestroyNetworkObject(NetworkDriver driver, IEnumerable<ClientInfo> clientConnections)
        {
            _toDestroyNetworkObjectIds.Clear();
            foreach (var pair in _networkObjects)
            {
                if (pair.Value == null) _toDestroyNetworkObjectIds.Add(pair.Key);
            }

            foreach (var id in _toDestroyNetworkObjectIds)
            {
                _networkObjects.Remove(id);
            }
            _toDestroyNetworkObjectIds.Clear();
            foreach (var client in clientConnections)
            {
                if (!client.IsConnected) continue;
                foreach (var networkObjectId in client.VisibleObjects)
                {
                    if (_networkObjects.TryGetValue(networkObjectId, out var networkObject)
                        && networkObject.CheckObjectVisibility(client.ClientId)) continue;
                    _toDestroyNetworkObjectIds.Add(networkObjectId);
                }

                foreach (var networkObjectId in _toDestroyNetworkObjectIds)
                {
                    client.VisibleObjects.Remove(networkObjectId);
                    driver.BeginSend(ReliableSequencedPipeline, client.Connection, out var writer);
                    writer.WriteByte((byte)NetworkAction.DestroyObject);
                    writer.WriteInt(networkObjectId);
                    driver.EndSend(writer);
                }
            }
        }

        internal void BroadcastUpdateNetworkObject(int clientId, NetworkDriver driver, IReadOnlyList<ClientInfo> clientConnections)
        {
            foreach (var networkObject in _networkObjects.Values)
            {
                foreach (var client in clientConnections)
                {
                    if (!client.IsConnected) continue;
                    if (!networkObject.CheckObjectVisibility(client.ClientId)) continue;
                    if (client.IsClient && !client.VisibleObjects.Contains(networkObject.NetworkObjectId)) continue;
                    var changedVariable = 
                        networkObject.CalculateSendVariable(clientId
                            , client.ClientId
                            , true);
                    if (changedVariable.Count == 0) continue;
                    foreach (var behaviour in changedVariable)
                    {
                        foreach (var variable in behaviour.Value)
                        {
                            var pipeline = variable.Value.Delivery switch
                            {
                                NetworkDelivery.UnreliableSequenced => UnreliableSequencedPipeline,
                                NetworkDelivery.ReliableSequenced => ReliableSequencedPipeline,
                                _ => UnreliablePipeline
                            };
                            driver.BeginSend(pipeline, client.Connection, out var writer);
                            writer.WriteByte((byte)NetworkAction.UpdateObject);
                            writer.WriteInt(networkObject.NetworkObjectId);
                            writer.WriteInt(behaviour.Key);
                            writer.WriteInt(variable.Key);
                            variable.Value.Serialize(ref writer);
                            driver.EndSend(writer);
                        }
                    }
                }
            }

            foreach (var networkObject in _networkObjects.Values)
            {
                foreach (var behaviour in networkObject.NetworkBehaviours)
                {
                    foreach (var variable in behaviour.NetworkVariables)
                    {
                        variable.ClearChange();
                    }
                }
            }
        }

        internal void BroadcastSpawnNetworkObject(NetworkDriver driver, IEnumerable<ClientInfo> clients)
        {
            foreach (var client in clients)
            {
                if (!client.IsConnected) continue;
                foreach (var networkObject in _networkObjects.Values)
                {
                    if (!networkObject.CheckObjectVisibility(client.ClientId)) continue;
                    if (client.VisibleObjects.Contains(networkObject.NetworkObjectId)) continue;
                    client.VisibleObjects.Add(networkObject.NetworkObjectId);
                    driver.BeginSend(ReliableSequencedPipeline, client.Connection, out var writer);
                    writer.WriteByte((byte)NetworkAction.SpawnObject);
                    writer.WriteInt(networkObject.PrefabId);
                    writer.WriteInt(networkObject.OwnerId);
                    writer.WriteInt(networkObject.NetworkObjectId);
                    var changedVariable = 
                        networkObject.CalculateSendVariable(ServerNetworkManager.ClientId
                            , client.ClientId
                            , false);
                    writer.WriteInt(changedVariable.Count);
                    if (changedVariable.Count > 0)
                    {
                        foreach (var behaviour in changedVariable)
                        {
                            writer.WriteInt(behaviour.Key);
                            writer.WriteInt(behaviour.Value.Count);
                            foreach (var variable in behaviour.Value)
                            {
                                writer.WriteInt(variable.Key);
                                variable.Value.Serialize(ref writer);
                            }
                        }
                    }
                    driver.EndSend(writer);
                }
            }
        }

        public void DestroyAllNetworkObject()
        {
            foreach (var networkObject in _networkObjects.Values)
            {
                if (networkObject == null) continue;
                Object.Destroy(networkObject.gameObject);
            }
            _networkObjects.Clear();
            _allocateNetworkObjectId = 0;
        }

    }
}