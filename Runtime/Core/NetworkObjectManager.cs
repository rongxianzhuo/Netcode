using System.Collections.Generic;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

namespace Netcode.Core
{
    public class NetworkObjectManager
    {

        public INetworkPrefabLoader NetworkPrefabLoader;
        
        private int _allocateNetworkObjectId;
        private readonly Dictionary<int, NetworkObject> _networkObjects = new Dictionary<int, NetworkObject>();

        public void SpawnNetworkObject(NetworkObject networkObject, int ownerId)
        {
            var networkObjectId = _allocateNetworkObjectId++;
            _networkObjects[networkObjectId] = networkObject;
            networkObject.NetworkStart(false, ownerId, networkObjectId);
        }

        internal void UpdateNetworkObject(ref DataStreamReader reader)
        {
            var networkObjectId = reader.ReadInt();
            var networkObject = _networkObjects[networkObjectId];
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
        }

        internal void SpawnNetworkObject(ref DataStreamReader reader)
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
                networkObject = NetworkPrefabLoader.Instantiate(prefabId);
            }
            networkObject.name = "Client";
            
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
            networkObject.NetworkStart(true, ownerId, networkObjectId);
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
                    driver.BeginSend(NetworkPipeline.Null, client.Connection, out var writer);
                    writer.WriteByte((byte)NetworkAction.UpdateObject);
                    writer.WriteInt(networkObject.NetworkObjectId);
                    writer.WriteInt(changedVariable.Count);
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
                    driver.EndSend(writer);
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
                    driver.BeginSend(NetworkPipeline.Null, client.Connection, out var writer);
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

        internal void Clear()
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