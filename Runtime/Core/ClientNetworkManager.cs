using System;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

namespace Netcode.Core
{
    public class ClientNetworkManager
    {

        public readonly NetcodeSettings Settings = new NetcodeSettings();
        public readonly NetworkObjectManager ObjectManager = new NetworkObjectManager();
        private readonly ClientInfo[] _serverConnection = new []{new ClientInfo(0, default)};

        private NetworkDriver _driver;
        private float _sendMessageTime;

        private ClientInfo ServerInfo
        {
            get => _serverConnection[0];
            set => _serverConnection[0] = value;
        }

        public bool IsRunning => _driver.IsCreated;
        
        public int ClientId { get; private set; }

        public void Disconnect()
        {
            if (!IsRunning) return;
            
            if (ServerInfo.IsConnected)
            {
                ServerInfo.Disconnect(_driver);
                ServerInfo = default;
            }

            ObjectManager.Clear();
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
            if (ServerInfo.IsConnected) ServerInfo.Disconnect(_driver);
            var endpoint = NetworkEndpoint.LoopbackIpv4;
            endpoint.Port = 9002;
            ServerInfo = new ClientInfo(ServerNetworkManager.ClientId, _driver.Connect(endpoint));
        }

        public void Update()
        {
            if (!IsRunning) return;
            
            _driver.ScheduleUpdate().Complete();

            NetworkEvent.Type cmd;
            while (ServerInfo.IsConnected && (cmd = ServerInfo.Connection.PopEvent(_driver, out var stream)) != NetworkEvent.Type.Empty)
            {
                switch (cmd)
                {
                    case NetworkEvent.Type.Connect:
                    {
                        _driver.BeginSend(NetworkPipeline.Null, ServerInfo.Connection, out var writer);
                        writer.WriteLong(ServerNetworkManager.ApprovalToken);
                        _driver.EndSend(writer);
                        break;
                    }
                    case NetworkEvent.Type.Data:
                        HandleNetworkData((NetworkAction) stream.ReadByte(), ServerInfo.Connection, ref stream);
                        break;
                    case NetworkEvent.Type.Disconnect:
                        ServerInfo.Disconnect();
                        StopNetwork();
                        break;
                    case NetworkEvent.Type.Empty:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (Time.realtimeSinceStartup - _sendMessageTime < Settings.clientSendInternal)
            {
                return;
            }

            _sendMessageTime = Time.realtimeSinceStartup;

            if (ClientId > ServerNetworkManager.ClientId)
            {
                ObjectManager.BroadcastUpdateNetworkObject(ClientId, _driver, _serverConnection);
            }
        }

        private void HandleNetworkData(NetworkAction action, NetworkConnection connection, ref DataStreamReader reader)
        {
            switch (action)
            {
                case NetworkAction.SpawnObject:
                    ObjectManager.SpawnNetworkObject(ref reader);
                    break;
                case NetworkAction.DestroyObject:
                    ObjectManager.DestroyNetworkObject(ref reader);
                    break;
                case NetworkAction.UpdateObject:
                    ObjectManager.UpdateNetworkObject(ref reader);
                    break;
                case NetworkAction.ConnectionApproval:
                    ClientId = reader.ReadInt();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }
    }
}