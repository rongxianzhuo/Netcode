using Unity.Networking.Transport;

namespace Netcode.Core
{
    internal class ClientInfo
    {

        public readonly int ClientId;
        
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
            Connection = default;
        }

        public void Disconnect()
        {
            if (!Connection.IsCreated) return;
            Connection = default;
        }

    }
}