using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

namespace Netcode.Core
{
    public sealed class ServerNetworkManager : NetworkManager
    {

        public const int ClientId = 0;
        public const long ApprovalToken = 123456789;

        public event Action<int> ClientConnectEvent;

        public event Action<int> ClientDisconnectEvent;

        public readonly NetcodeSettings Settings = new NetcodeSettings();
        
        private readonly List<ClientInfo> _clientConnections = new List<ClientInfo>();
        private readonly List<NetworkConnection> _pendingClientConnections = new List<NetworkConnection>();

        private float _sendMessageTime;

        public void StopServer()
        {
            if (!IsRunning) return;

            foreach (var connection in _clientConnections)
            {
                connection.Disconnect(Driver);
            }
            _clientConnections.Clear();
                
            foreach (var connection in _pendingClientConnections)
            {
                connection.Disconnect(Driver);
            }
            _pendingClientConnections.Clear();
            
            DestroyAllNetworkObject();
            DestroyNetworkDriver();
            NetworkLoopSystem.RemoveNetworkUpdateLoop(Update);
        }

        public void StartServer()
        {
            if (IsRunning)
            {
                throw new Exception("Already run");
            }
            CreateNetworkDriver();
            var endpoint = NetworkEndpoint.AnyIpv4;
            endpoint.Port = 9002;
            if (Driver.Bind(endpoint) != 0) Debug.Log("Failed to bind to port 9002");
            else Driver.Listen();
            NetworkLoopSystem.AddNetworkUpdateLoop(Update);
        }

        private void Update()
        {
            if (!IsRunning) return;
            
            Driver.ScheduleUpdate().Complete();

            // Accept new connections
            NetworkConnection c;
            while ((c = Driver.Accept()) != default)
            {
                _pendingClientConnections.Add(c);
            }

            // Approval pending connections
            for (var i = _pendingClientConnections.Count - 1; i >= 0; i--)
            {
                var connection = _pendingClientConnections[i];
                
                NetworkEvent.Type cmd;
                if ((cmd = Driver.PopEventForConnection(connection, out var stream)) == NetworkEvent.Type.Empty)
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
                            Driver.BeginSend(NetworkPipeline.Null, connection, out var writer);
                            writer.WriteByte((byte)NetworkAction.ConnectionApproval);
                            writer.WriteInt(client.ClientId);
                            Driver.EndSend(writer);
                            ClientConnectEvent?.Invoke(client.ClientId);
                        }
                        else
                        {
                            Driver.Disconnect(connection);
                        }
                        break;
                    case NetworkEvent.Type.Disconnect:
                        break;
                    case NetworkEvent.Type.Empty:
                        break;
                    case NetworkEvent.Type.Connect:
                        Debug.LogError("Unknown error!");
                        Driver.Disconnect(connection);
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

            BroadcastSpawnNetworkObject(Driver, _clientConnections);
            BroadcastUpdateNetworkObject(ClientId, Driver, _clientConnections);
            BroadcastDestroyNetworkObject(Driver, _clientConnections);
        }

        private void HandleNetworkEvent(ClientInfo client)
        {
            NetworkEvent.Type cmd;
            while (client.IsConnected && (cmd = Driver.PopEventForConnection(client.Connection, out var stream)) != NetworkEvent.Type.Empty)
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
                    UpdateNetworkObject(ref reader);
                    break;
                case NetworkAction.ConnectionApproval:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }
    }
}