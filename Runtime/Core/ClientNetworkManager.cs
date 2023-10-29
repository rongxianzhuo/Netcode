using System;
using Unity.Collections;
using Unity.Networking.Transport;

namespace Netcode.Core
{
    public class ClientNetworkManager
    {

        public readonly NetworkObjectManager ObjectManager = new NetworkObjectManager();

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
                    ObjectManager.SpawnNetworkObject(ref stream);
                    break;
                case NetworkAction.RemoveObject:
                    break;
                case NetworkAction.UpdateObject:
                    ObjectManager.UpdateNetworkObject(ref stream);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }
    }
}