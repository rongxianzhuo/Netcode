using Unity.Collections;
using UnityEngine;

namespace Netcode.Variable
{

    public interface INetworkVariableSerializer<T>
    {

        void Serialize(ref DataStreamWriter writer, T value);

        T Deserialize(ref DataStreamReader reader);

    }

    public class IntNetworkVariableSerializer : INetworkVariableSerializer<int>
    {
        public void Serialize(ref DataStreamWriter writer, int value)
        {
            writer.WriteInt(value);
        }

        public int Deserialize(ref DataStreamReader reader)
        {
            return reader.ReadInt();
        }
    }

    public class FloatNetworkVariableSerializer : INetworkVariableSerializer<float>
    {
        public void Serialize(ref DataStreamWriter writer, float value)
        {
            writer.WriteFloat(value);
        }

        public float Deserialize(ref DataStreamReader reader)
        {
            return reader.ReadFloat();
        }
    }
}