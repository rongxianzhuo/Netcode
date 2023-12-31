using Netcode.Message;
using Unity.Collections;

namespace Netcode.Variable
{
    public interface INetworkVariable
    {
        
        bool IsChanged { get; }

        void ClearChange();
        
        VariablePermission ReadPermission { get; }

        VariablePermission WritePermission { get; }

        NetworkDelivery Delivery { get; }

        void Serialize(ref DataStreamWriter writer);
        
        void Deserialize(ref DataStreamReader reader);
        
    }
}