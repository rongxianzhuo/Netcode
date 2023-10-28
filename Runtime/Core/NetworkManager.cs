using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;

namespace Netcode.Core
{
    public sealed class NetworkManager
    {

        private NetworkDriver _driver;
        private int _nextAllocateObjectId;
        private NetworkSettings _networkSettings;
        private NetworkConnection _serverConnection;
        private readonly List<NetworkConnection> _clientConnections = new List<NetworkConnection>();
        private readonly List<NetworkConnection> _pendingClientConnections = new List<NetworkConnection>();

        public bool IsClient { get; private set; }

        public bool IsRunning => _driver.IsCreated;

        public void Initialize(NetworkSettings settings)
        {
            _networkSettings = settings;
        }

        public void Disconnect()
        {
            if (!IsRunning) return;
            
            if (_serverConnection.IsCreated)
            {
                _serverConnection.Disconnect(_driver);
                _serverConnection = default;
            }

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

            IsClient = true;
            _driver = NetworkDriver.Create();
        }

        public void ConnectServer()
        {
            if (_serverConnection.IsCreated) _serverConnection.Disconnect(_driver);
            var endpoint = NetworkEndpoint.LoopbackIpv4;
            endpoint.Port = 9002;
            _serverConnection = _driver.Connect(endpoint);
        }

        public void StartServer()
        {
            if (IsRunning)
            {
                throw new Exception("Already run");
            }
            IsClient = false;
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

            if (IsClient)
            {
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
                            HandleNetworkData((NetworkAction) stream.ReadByte(), _serverConnection, stream);
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
            else
            {
                // Accept new connections
                NetworkConnection c;
                while ((c = _driver.Accept()) != default)
                {
                    _pendingClientConnections.Add(c);
                }

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
                                    _driver.BeginSend(NetworkPipeline.Null, connection, out var writer);
                                    writer.WriteByte((byte)NetworkAction.SpawnObject);
                                    _driver.EndSend(writer);
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

                for (var i = _clientConnections.Count - 1; i >= 0; i--)
                {
                    var connection = _clientConnections[i];
                    
                    NetworkEvent.Type cmd;
                    while ((cmd = _driver.PopEventForConnection(_clientConnections[i], out var stream)) != NetworkEvent.Type.Empty)
                    {
                        switch (cmd)
                        {
                            case NetworkEvent.Type.Data:
                                HandleNetworkData((NetworkAction) stream.ReadByte(), connection, stream);
                                break;
                            case NetworkEvent.Type.Disconnect:
                                _clientConnections.RemoveAt(i);
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
        }

        private void HandleNetworkData(NetworkAction action, NetworkConnection connection, DataStreamReader stream)
        {
            if (IsClient)
            {
                switch (action)
                {
                    case NetworkAction.SpawnObject:
                        Debug.Log("Spawn object");
                        break;
                    case NetworkAction.RemoveObject:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(action), action, null);
                }
            }
            else
            {
                switch (action)
                {
                    case NetworkAction.SpawnObject:
                        break;
                    case NetworkAction.RemoveObject:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(action), action, null);
                }
            }
        }
    }
}