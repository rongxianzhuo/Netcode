using Unity.Collections;

namespace Netcode.Variable
{
    public interface INetworkVariable
    {
        
        public void Serialize(ref DataStreamWriter writer);
        public void Deserialize(ref DataStreamReader reader);
        
    }
}