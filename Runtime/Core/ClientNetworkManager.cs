using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Netcode.Core
{
    public class ClientNetworkManager
    {

        public INetworkPrefabLoader NetworkPrefabLoader;
        
        private readonly List<NetworkObject> _networkObjects = new List<NetworkObject>();

        private NetworkDriver _driver;
        private NetworkConnection _serverConnection;

        public bool IsRunning => _driver.IsCreated;

        public void Disconnect()
        {
            if (!IsRunning) return;
            
            if (_serverConnection.IsCreated)
            {
                _serverConnection.Disconnect(_driver);
                _serverConnection = default;
            }

            foreach (var networkObject in _networkObjects)
            {
                if (networkObject == null) continue;
                Object.Destroy(networkObject.gameObject);
            }
            _networkObjects.Clear();
        }

        public void StopNetwork()
        {
            Disconnect();
            if (_driver.IsCreated)
            {
                _driver.Dispose();
                _driver = default;
            }
        }

        public void StartClient()
        {
            if (IsRunning)
            {
                throw new Exception("Already run");
            }

            _driver = NetworkDriver.Create();
        }

        public void ConnectServer()
        {
            if (_serverConnection.IsCreated) _serverConnection.Disconnect(_driver);
            var endpoint = NetworkEndpoint.LoopbackIpv4;
            endpoint.Port = 9002;
            _serverConnection = _driver.Connect(endpoint);
        }

        public void Update()
        {
            if (!IsRunning) return;
            
            _driver.ScheduleUpdate().Complete();

            NetworkEvent.Type cmd;
            while (_serverConnection.IsCreated && (cmd = _serverConnection.PopEvent(_driver, out var stream)) != NetworkEvent.Type.Empty)
            {
                switch (cmd)
                {
                    case NetworkEvent.Type.Connect:
                    {
                        _driver.BeginSend(NetworkPipeline.Null, _serverConnection, out var writer);
                        writer.WriteByte(5);
                        _driver.EndSend(writer);
                        break;
                    }
                    case NetworkEvent.Type.Data:
                        HandleNetworkData((NetworkAction) stream.ReadByte(), _serverConnection, ref stream);
                        break;
                    case NetworkEvent.Type.Disconnect:
                        _serverConnection = default;
                        StopNetwork();
                        break;
                    case NetworkEvent.Type.Empty:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void HandleNetworkData(NetworkAction action, NetworkConnection connection, ref DataStreamReader stream)
        {
            switch (action)
            {
                case NetworkAction.SpawnObject:
                    SpawnNetworkObject(ref stream);
                    break;
                case NetworkAction.RemoveObject:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }

        private void SpawnNetworkObject(ref DataStreamReader reader)
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
            _networkObjects.Add(networkObject);
            networkObject.NetworkStart(true);
        }
    }
}