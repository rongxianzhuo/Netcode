using System;
using Unity.Collections;

namespace Netcode.Variable
{
    public class NetworkVariable<T>: INetworkVariable where T : IEquatable<T>
    {
        
        static NetworkVariable()
        {
            NetworkVariable<int>.Serializer = new IntNetworkVariableSerializer();
            NetworkVariable<float>.Serializer = new FloatNetworkVariableSerializer();
        }

        private static readonly INetworkVariableSerializer<T> Serializer;

        public bool IsChanged { get; private set; }

        public void ClearChange()
        {
            IsChanged = false;
        }

        public VariablePermission ReadPermission { get; }

        public VariablePermission WritePermission { get; }

        private T _value;

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

        public NetworkVariable(T defaultValue
            , VariablePermission readPermission=VariablePermission.ServerOnly
            , VariablePermission writePermission=VariablePermission.ServerOnly)
        {
            _value = defaultValue;
            ReadPermission = readPermission;
            WritePermission = writePermission;
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