using System;
using Unity.Collections;

namespace Netcode.Variable
{
    public class NetworkVariable<T>: INetworkVariable where T : IEquatable<T>
    {

        private static readonly INetworkVariableSerializer<T> Serializer;

        private T _value;

        public bool IsChanged { get; internal set; }

        public T Value
        {
            get => _value;
            set
            {
                if (_value.Equals(value)) return;
                IsChanged = true;
                _value = value;
            }
        }
        
        static NetworkVariable()
        {
            NetworkVariable<int>.Serializer = new IntNetworkVariableSerializer();
            NetworkVariable<float>.Serializer = new FloatNetworkVariableSerializer();
        }

        public NetworkVariable(T defaultValue)
        {
            _value = defaultValue;
        }

        public void Serialize(ref DataStreamWriter writer)
        {
            Serializer.Serialize(ref writer, _value);
        }

        public void Deserialize(ref DataStreamReader reader)
        {
            _value = Serializer.Deserialize(ref reader);
        }
    }
}