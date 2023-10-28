using System;
using Unity.Collections;

namespace Netcode.Variable
{
    public class NetworkVariable<T> where T : IEquatable<T>
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
        }

        public NetworkVariable(T defaultValue)
        {
            _value = defaultValue;
        }

        public void Serialize(DataStreamWriter writer)
        {
            Serializer.Serialize(writer, _value);
        }

        public T Deserialize(DataStreamReader reader)
        {
            return Serializer.Deserialize(reader);
        }

    }
}