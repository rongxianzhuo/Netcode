using System;
using Unity.Networking.Transport;
using UnityEngine;

public class ClientBehaviour : MonoBehaviour
{

    private NetworkDriver _driver;
    private NetworkConnection _connection;
    private bool _done;
    
    private void Start () {
        _driver = NetworkDriver.Create();
        var endpoint = NetworkEndpoint.LoopbackIpv4;
        endpoint.Port = 9002;
        _connection = _driver.Connect(endpoint);
    }
    
    private void Update()
    {
        _driver.ScheduleUpdate().Complete();

        if (!_connection.IsCreated)
        {
            if (!_done) Debug.Log("Something went wrong during connect");
            return;
        }

        NetworkEvent.Type cmd;
        while ((cmd = _connection.PopEvent(_driver, out var stream)) != NetworkEvent.Type.Empty)
        {
            switch (cmd)
            {
                case NetworkEvent.Type.Connect:
                {
                    Debug.Log("We are now connected to the server");
                    _driver.BeginSend(_connection, out var writer);
                    writer.WriteUInt(1);
                    _driver.EndSend(writer);
                    break;
                }
                case NetworkEvent.Type.Data:
                {
                    var value = stream.ReadUInt();
                    Debug.Log("Got the value = " + value + " back from the server");
                    _done = true;
                    _connection.Disconnect(_driver);
                    _connection = default(NetworkConnection);
                    break;
                }
                case NetworkEvent.Type.Disconnect:
                    Debug.Log("Client got disconnected from server");
                    _connection = default(NetworkConnection);
                    break;
                case NetworkEvent.Type.Empty:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
    
    private void OnDestroy()
    {
        if (!_driver.IsCreated) return;
        _driver.Dispose();
    }
}
