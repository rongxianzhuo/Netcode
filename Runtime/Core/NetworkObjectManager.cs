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
        private readonly HashSet<NetworkObject> _newObjects = new HashSet<NetworkObject>();
        private readonly Dictionary<int, NetworkObject> _existObjects = new Dictionary<int, NetworkObject>();

        public void SpawnNetworkObject(NetworkObject networkObject, int ownerId)
        {
            _newObjects.Add(networkObject);
            networkObject.NetworkStart(false, ownerId, _allocateNetworkObjectId++);
        }

        internal void UpdateNetworkObject(ref DataStreamReader reader)
        {
            var networkId = reader.ReadInt();
            var networkObject = _existObjects[networkId];
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
            var networkObject = NetworkPrefabLoader.Instantiate(reader.ReadInt());
            networkObject.name = "Client";
            var ownerId = reader.ReadInt();
            var networkObjectId = reader.ReadInt();
            if (networkObject.NetworkBehaviours != null)
            {
                foreach (var networkBehaviour in networkObject.NetworkBehaviours)
                {
                    foreach (var t in networkBehaviour.NetworkVariables)
                    {
                        t.Deserialize(ref reader);
                    }
                }
            }

            _existObjects[networkObjectId] = networkObject;
            networkObject.NetworkStart(true, ownerId, networkObjectId);
        }

        internal void BroadcastUpdateNetworkObject(int clientId, NetworkDriver driver, IReadOnlyList<NetworkConnection> clientConnections)
        {
            foreach (var networkObject in _existObjects.Values)
            {
                var changedVariable = networkObject.CalculateChangedVariable(clientId);
                if (changedVariable.Count == 0) continue;
                foreach (var connection in clientConnections)
                {
                    if (!connection.IsCreated) continue;
                    driver.BeginSend(NetworkPipeline.Null, connection, out var writer);
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

            foreach (var networkObject in _existObjects.Values)
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

        internal void BroadcastSpawnNetworkObject(NetworkDriver driver, IReadOnlyList<NetworkConnection> clientConnections)
        {
            foreach (var networkObject in _newObjects)
            {
                foreach (var connection in clientConnections)
                {
                    if (!connection.IsCreated) continue;
                    driver.BeginSend(NetworkPipeline.Null, connection, out var writer);
                    writer.WriteByte((byte)NetworkAction.SpawnObject);
                    writer.WriteInt(networkObject.PrefabId);
                    writer.WriteInt(networkObject.OwnerId);
                    writer.WriteInt(networkObject.NetworkObjectId);
                    if (networkObject.NetworkBehaviours != null)
                    {
                        foreach (var networkBehaviour in networkObject.NetworkBehaviours)
                        {
                            foreach (var t in networkBehaviour.NetworkVariables)
                            {
                                t.Serialize(ref writer);
                            }
                        }
                    }
                    driver.EndSend(writer);
                }

                _existObjects[networkObject.NetworkObjectId] = networkObject;
            }

            foreach (var networkObject in _newObjects)
            {
                foreach (var behaviour in networkObject.NetworkBehaviours)
                {
                    foreach (var variable in behaviour.NetworkVariables)
                    {
                        variable.ClearChange();
                    }
                }
            }
            _newObjects.Clear();
        }

        internal void BroadcastAllNetworkObject(NetworkDriver driver, NetworkConnection connection)
        {
            foreach (var networkObject in _existObjects.Values)
            {
                if (!connection.IsCreated) continue;
                driver.BeginSend(NetworkPipeline.Null, connection, out var writer);
                writer.WriteByte((byte)NetworkAction.SpawnObject);
                writer.WriteInt(networkObject.PrefabId);
                writer.WriteInt(networkObject.OwnerId);
                writer.WriteInt(networkObject.NetworkObjectId);
                if (networkObject.NetworkBehaviours != null)
                {
                    foreach (var networkBehaviour in networkObject.NetworkBehaviours)
                    {
                        foreach (var t in networkBehaviour.NetworkVariables)
                        {
                            t.Serialize(ref writer);
                        }
                    }
                }
                driver.EndSend(writer);
            }
        }

        internal void Clear()
        {
            foreach (var networkObject in _newObjects)
            {
                if (networkObject == null) continue;
                Object.Destroy(networkObject.gameObject);
            }
            _newObjects.Clear();
            foreach (var networkObject in _existObjects.Values)
            {
                if (networkObject == null) continue;
                Object.Destroy(networkObject.gameObject);
            }
            _existObjects.Clear();
            _allocateNetworkObjectId = 0;
        }
        
    }
}