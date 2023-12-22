using System;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

namespace Netcode.Core
{
    public class ClientNetworkManager : NetworkManager
    {

        public event Action<int> ClientConnectEvent;

        public readonly NetcodeSettings Settings = new NetcodeSettings();
        private readonly ClientInfo[] _serverConnection = new []{new ClientInfo(0, default)};

        private float _sendMessageTime;

        private ClientInfo ServerInfo
        {
            get => _serverConnection[0];
            set => _serverConnection[0] = value;
        }
        
        public int ClientId { get; private set; }

        public void Disconnect()
        {
            if (!IsRunning) return;
            
            if (ServerInfo.IsConnected)
            {
                ServerInfo.Disconnect(Driver);
                ServerInfo = default;
            }

            DestroyAllNetworkObject();
        }

        public void StopNetwork()
        {
            Disconnect();
            if (!IsRunning) return;
            DestroyNetworkDriver();
            NetworkLoopSystem.RemoveNetworkUpdateLoop(Update);
        }

        public void StartClient()
        {
            if (IsRunning)
            {
                throw new Exception("Already run");
            }

            CreateNetworkDriver();
            NetworkLoopSystem.AddNetworkUpdateLoop(Update);
        }

        public void ConnectServer(string address, ushort port)
        {
            if (ServerInfo.IsConnected) ServerInfo.Disconnect(Driver);
            var endpoint = NetworkEndpoint.Parse(address, port);
            ServerInfo = new ClientInfo(ServerNetworkManager.ClientId, Driver.Connect(endpoint));
        }

        private void Update()
        {
            if (!IsRunning) return;
            
            Driver.ScheduleUpdate().Complete();

            NetworkEvent.Type cmd;
            while (ServerInfo.IsConnected && (cmd = ServerInfo.Connection.PopEvent(Driver, out var stream)) != NetworkEvent.Type.Empty)
            {
                switch (cmd)
                {
                    case NetworkEvent.Type.Connect:
                    {
                        Driver.BeginSend(ReliableSequencedPipeline, ServerInfo.Connection, out var writer);
                        writer.WriteLong(ServerNetworkManager.ApprovalToken);
                        Driver.EndSend(writer);
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
                BroadcastUpdateNetworkObject(ClientId, Driver, _serverConnection);
            }
        }

        private void HandleNetworkData(NetworkAction action, NetworkConnection connection, ref DataStreamReader reader)
        {
            switch (action)
            {
                case NetworkAction.SpawnObject:
                    SpawnNetworkObject(ClientId, ref reader);
                    break;
                case NetworkAction.DestroyObject:
                    DestroyNetworkObject(ref reader);
                    break;
                case NetworkAction.UpdateObject:
                    UpdateNetworkObject(ref reader);
                    break;
                case NetworkAction.ConnectionApproval:
                    ClientId = reader.ReadInt();
                    ClientConnectEvent?.Invoke(ClientId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }
    }
}