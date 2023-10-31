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
        public const long ApprovalToken = 123456789;

        public event Action<int> ClientConnectEvent;

        public event Action<int> ClientDisconnectEvent;

        public readonly NetcodeSettings Settings = new NetcodeSettings();
        public readonly NetworkObjectManager ObjectManager = new NetworkObjectManager();
        
        private readonly List<ClientInfo> _clientConnections = new List<ClientInfo>();
        private readonly List<NetworkConnection> _pendingClientConnections = new List<NetworkConnection>();

        private NetworkDriver _driver;
        private float _sendMessageTime;

        public bool IsRunning => _driver.IsCreated;

        public void StopServer()
        {
            if (!IsRunning) return;

            foreach (var connection in _clientConnections)
            {
                connection.Disconnect(_driver);
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

            // Approval pending connections
            for (var i = _pendingClientConnections.Count - 1; i >= 0; i--)
            {
                var connection = _pendingClientConnections[i];
                
                NetworkEvent.Type cmd;
                if ((cmd = _driver.PopEventForConnection(connection, out var stream)) == NetworkEvent.Type.Empty)
                {
                    continue;
                }
                switch (cmd)
                {
                    case NetworkEvent.Type.Data:
                        if (stream.ReadLong() == ApprovalToken)
                        {
                            var client = new ClientInfo(_clientConnections.Count + 1, connection);
                            _clientConnections.Add(client);
                            _driver.BeginSend(NetworkPipeline.Null, connection, out var writer);
                            writer.WriteByte((byte)NetworkAction.ConnectionApproval);
                            writer.WriteInt(client.ClientId);
                            _driver.EndSend(writer);
                            ClientConnectEvent?.Invoke(client.ClientId);
                        }
                        else
                        {
                            _driver.Disconnect(connection);
                        }
                        break;
                    case NetworkEvent.Type.Disconnect:
                        break;
                    case NetworkEvent.Type.Empty:
                        break;
                    case NetworkEvent.Type.Connect:
                        Debug.LogError("Unknown error!");
                        _driver.Disconnect(connection);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                _pendingClientConnections.RemoveAt(i);
            }

            for (var i = _clientConnections.Count - 1; i >= 0; i--)
            {
                HandleNetworkEvent(_clientConnections[i]);
            }

            if (Time.realtimeSinceStartup - _sendMessageTime < Settings.serverSendInterval)
            {
                return;
            }

            _sendMessageTime = Time.realtimeSinceStartup;

            ObjectManager.BroadcastUpdateNetworkObject(ClientId, _driver, _clientConnections);
            ObjectManager.BroadcastSpawnNetworkObject(_driver, _clientConnections);
            ObjectManager.BroadcastDestroyNetworkObject(_driver, _clientConnections);
        }

        private void HandleNetworkEvent(ClientInfo client)
        {
            NetworkEvent.Type cmd;
            while (client.IsConnected && (cmd = _driver.PopEventForConnection(client.Connection, out var stream)) != NetworkEvent.Type.Empty)
            {
                switch (cmd)
                {
                    case NetworkEvent.Type.Data:
                        HandleNetworkData((NetworkAction) stream.ReadByte(), client.Connection, stream);
                        break;
                    case NetworkEvent.Type.Disconnect:
                        client.Disconnect();
                        ClientDisconnectEvent?.Invoke(client.ClientId);
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

        private void HandleNetworkData(NetworkAction action, NetworkConnection connection, DataStreamReader reader)
        {
            switch (action)
            {
                case NetworkAction.SpawnObject:
                    break;
                case NetworkAction.DestroyObject:
                    break;
                case NetworkAction.UpdateObject:
                    ObjectManager.UpdateNetworkObject(ref reader);
                    break;
                case NetworkAction.ConnectionApproval:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }
    }
}