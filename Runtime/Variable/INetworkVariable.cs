using Unity.Collections;

namespace Netcode.Variable
{
    public interface INetworkVariable
    {
        
        bool IsChanged { get; }
        
        VariablePermission ReadPermission { get; }

        VariablePermission WritePermission { get; }

        void Serialize(ref DataStreamWriter writer);
        
        void Deserialize(ref DataStreamReader reader);
        
    }
}