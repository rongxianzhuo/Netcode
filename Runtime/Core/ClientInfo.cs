using System.Collections.Generic;
using Unity.Networking.Transport;

namespace Netcode.Core
{
    internal class ClientInfo
    {

        public readonly int ClientId;

        public readonly HashSet<int> VisibleObjects = new HashSet<int>();
        
        public NetworkConnection Connection { get; private set; }

        public bool IsConnected => Connection.IsCreated;

        public ClientInfo(int clientId, NetworkConnection connection)
        {
            ClientId = clientId;
            Connection = connection;
        }

        public void Disconnect(NetworkDriver driver)
        {
            if (!Connection.IsCreated) return;
            Connection.Disconnect(driver);
            Disconnect();
        }

        public void Disconnect()
        {
            Connection = default;
            VisibleObjects.Clear();
        }

    }
}