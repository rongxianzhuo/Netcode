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

    public class Vector3NetworkVariableSerializer : INetworkVariableSerializer<Vector3>
    {
        public void Serialize(ref DataStreamWriter writer, Vector3 value)
        {
            writer.WriteFloat(value.x);
            writer.WriteFloat(value.y);
            writer.WriteFloat(value.z);
        }

        public Vector3 Deserialize(ref DataStreamReader reader)
        {
            var vector3 = new Vector3
            {
                x = reader.ReadFloat(),
                y = reader.ReadFloat(),
                z = reader.ReadFloat()
            };
            return vector3;
        }
    }
}