using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

namespace Netcode.Core
{
    public sealed class ServerNetworkManager
    {

        public const int ClientId = 0;

        public event Action<int> ClientConnectEvent; 
        
        public readonly NetworkObjectManager ObjectManager = new NetworkObjectManager();
        
        private readonly List<NetworkConnection> _clientConnections = new List<NetworkConnection>();
        private readonly List<NetworkConnection> _pendingClientConnections = new List<NetworkConnection>();

        private NetworkDriver _driver;

        public bool IsRunning => _driver.IsCreated;

        public void StopServer()
        {
            if (!IsRunning) return;

            foreach (var connection in _clientConnections)
            {
                if (connection.IsCreated) connection.Disconnect(_driver);
            }
            _clientConnections.Clear();
                
            foreach (var connection in _pendingClientConnections)
            {
                connection.Disconnect(_driver);
            }
            _pendingClientConnections.Clear();
            
            ObjectManager.Clear();
            _driver.Dispose();
            _driver = default;
        }

        public void StartServer()
        {
            if (IsRunning)
            {
                throw new Exception("Already run");
            }
            _driver = NetworkDriver.Create();
            var endpoint = NetworkEndpoint.AnyIpv4;
            endpoint.Port = 9002;
            if (_driver.Bind(endpoint) != 0) Debug.Log("Failed to bind to port 9002");
            else _driver.Listen();
        }

        public void Update()
        {
            if (!IsRunning) return;
            
            _driver.ScheduleUpdate().Complete();

            // Accept new connections
            NetworkConnection c;
            while ((c = _driver.Accept()) != default)
            {
                _pendingClientConnections.Add(c);
            }

            for (var i = _clientConnections.Count - 1; i >= 0; i--)
            {
                var connection = _clientConnections[i];
                
                NetworkEvent.Type cmd;
                while (connection.IsCreated && (cmd = _driver.PopEventForConnection(_clientConnections[i], out var stream)) != NetworkEvent.Type.Empty)
                {
                    switch (cmd)
                    {
                        case NetworkEvent.Type.Data:
                            HandleNetworkData((NetworkAction) stream.ReadByte(), connection, stream);
                            break;
                        case NetworkEvent.Type.Disconnect:
                            _clientConnections[i] = default;
                            break;
                        case NetworkEvent.Type.Empty:
                            break;
                        case NetworkEvent.Type.Connect:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            ObjectManager.BroadcastUpdateNetworkObject(ClientId, _driver, _clientConnections);
            ObjectManager.BroadcastSpawnNetworkObject(_driver, _clientConnections);

            for (var i = _pendingClientConnections.Count - 1; i >= 0; i--)
            {
                var connection = _pendingClientConnections[i];
                
                NetworkEvent.Type cmd;
                while ((cmd = _driver.PopEventForConnection(connection, out var stream)) != NetworkEvent.Type.Empty)
                {
                    switch (cmd)
                    {
                        case NetworkEvent.Type.Data:
                            if (stream.ReadByte() == 5)
                            {
                                _clientConnections.Add(connection);
                                _pendingClientConnections.RemoveAt(i);
                                var clientId = _clientConnections.Count;
                                _driver.BeginSend(NetworkPipeline.Null, connection, out var writer);
                                writer.WriteByte((byte)NetworkAction.ConnectionApproval);
                                writer.WriteInt(clientId);
                                _driver.EndSend(writer);
                                ClientConnectEvent?.Invoke(clientId);
                                ObjectManager.BroadcastAllNetworkObject(_driver, connection);
                            }
                            break;
                        case NetworkEvent.Type.Disconnect:
                            _pendingClientConnections.RemoveAt(i);
                            break;
                        case NetworkEvent.Type.Empty:
                            break;
                        case NetworkEvent.Type.Connect:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        private void HandleNetworkData(NetworkAction action, NetworkConnection connection, DataStreamReader reader)
        {
            switch (action)
            {
                case NetworkAction.SpawnObject:
                    break;
                case NetworkAction.RemoveObject:
                    break;
                case NetworkAction.UpdateObject:
                    ObjectManager.UpdateNetworkObject(ref reader);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }
    }
}