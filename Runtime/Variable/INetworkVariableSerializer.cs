using Unity.Collections;

namespace Netcode.Variable
{

    public interface INetworkVariableSerializer<T>
    {

        void Serialize(DataStreamWriter writer, T value);

        T Deserialize(DataStreamReader reader);

    }

    public class IntNetworkVariableSerializer : INetworkVariableSerializer<int>
    {
        public void Serialize(DataStreamWriter writer, int value)
        {
            writer.WriteInt(value);
        }

        public int Deserialize(DataStreamReader reader)
        {
            return reader.ReadInt();
        }
    }
}