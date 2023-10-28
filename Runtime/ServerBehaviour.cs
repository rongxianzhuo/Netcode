using System;
using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;

namespace Netcode
{

    public class ServerBehaviour : MonoBehaviour
    {

        private NetworkDriver _driver;
        private NativeList<NetworkConnection> _connections;

        private void Start()
        {
            _driver = NetworkDriver.Create();
            var endpoint = NetworkEndpoint.AnyIpv4;
            endpoint.Port = 9002;
            if (_driver.Bind(endpoint) != 0) Debug.Log("Failed to bind to port 9002");
            else _driver.Listen();
            _connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
        }

        private void Update()
        {
            _driver.ScheduleUpdate().Complete();
            
            // Clean up connections
            for (var i = 0; i < _connections.Length; i++)
            {
                if (_connections[i].IsCreated) continue;
                _connections.RemoveAtSwapBack(i);
                --i;
            }
            
            // Accept new connections
            NetworkConnection c;
            while ((c = _driver.Accept()) != default)
            {
                _connections.Add(c);
                Debug.Log("Accepted a connection");
            }

            for (var i = 0; i < _connections.Length; i++)
            {
                if (!_connections[i].IsCreated) continue;
                NetworkEvent.Type cmd;
                while ((cmd = _driver.PopEventForConnection(_connections[i], out var stream)) != NetworkEvent.Type.Empty)
                {
                    switch (cmd)
                    {
                        case NetworkEvent.Type.Data:
                        {
                            var number = stream.ReadUInt();
                            Debug.Log("Got " + number + " from the Client adding + 2 to it.");
                            number +=2;

                            _driver.BeginSend(NetworkPipeline.Null, _connections[i], out var writer);
                            writer.WriteUInt(number);
                            _driver.EndSend(writer);
                            break;
                        }
                        case NetworkEvent.Type.Disconnect:
                            Debug.Log("Client disconnected from server");
                            _connections[i] = default;
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

        private void OnDestroy()
        {
            if (!_driver.IsCreated) return;
            _driver.Dispose();
            _connections.Dispose();
        }
    }

}