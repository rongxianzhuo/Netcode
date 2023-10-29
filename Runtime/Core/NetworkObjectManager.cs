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

        public void SpawnNetworkObject(NetworkObject networkObject)
        {
            var objectId = _allocateNetworkObjectId++;
            networkObject.NetworkObjectId = objectId;
            _newObjects.Add(networkObject);
            networkObject.NetworkStart(false);
        }

        internal void UpdateNetworkObject(ref DataStreamReader reader)
        {
            var networkObject = _existObjects[reader.ReadInt()];
            foreach (var networkBehaviour in networkObject.NetworkBehaviours)
            {
                foreach (var t in networkBehaviour.NetworkVariables)
                {
                    t.Deserialize(ref reader);
                }
            }
        }

        internal void SpawnNetworkObject(ref DataStreamReader reader)
        {
            var networkObject = NetworkPrefabLoader.Instantiate(reader.ReadInt());
            networkObject.name = "Client";
            networkObject.NetworkObjectId = reader.ReadInt();
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

            _existObjects[networkObject.NetworkObjectId] = networkObject;
            networkObject.NetworkStart(true);
        }

        internal void BroadcastUpdateNetworkObject(NetworkDriver driver, IReadOnlyList<NetworkConnection> clientConnections)
        {
            foreach (var networkObject in _existObjects.Values)
            {
                foreach (var connection in clientConnections)
                {
                    driver.BeginSend(NetworkPipeline.Null, connection, out var writer);
                    writer.WriteByte((byte)NetworkAction.UpdateObject);
                    writer.WriteInt(networkObject.NetworkObjectId);
                    foreach (var networkBehaviour in networkObject.NetworkBehaviours)
                    {
                        foreach (var networkVariable in networkBehaviour.NetworkVariables)
                        {
                            networkVariable.Serialize(ref writer);
                        }
                    }
                    driver.EndSend(writer);
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
            _newObjects.Clear();
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