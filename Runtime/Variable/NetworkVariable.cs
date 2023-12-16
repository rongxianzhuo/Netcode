using System;
using Unity.Collections;
using UnityEngine;

namespace Netcode.Variable
{
    public class NetworkVariable<T>: INetworkVariable where T : IEquatable<T>
    {

        public event Action<T, T> ValueChangedEvent;
        
        static NetworkVariable()
        {
            NetworkVariable<bool>.Serializer = new BoolNetworkVariableSerializer();
            NetworkVariable<int>.Serializer = new IntNetworkVariableSerializer();
            NetworkVariable<float>.Serializer = new FloatNetworkVariableSerializer();
            NetworkVariable<Vector3>.Serializer = new Vector3NetworkVariableSerializer();
        }

        private static readonly INetworkVariableSerializer<T> Serializer;

        public bool IsChanged { get; private set; }

        public void AddListenerAndCall(Action<T, T> action)
        {
            ValueChangedEvent += action;
            action(_value, _value);
        }

        public void RemoveListener(Action<T, T> action)
        {
            ValueChangedEvent -= action;
        }

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
                var old = _value;
                _value = value;
                ValueChangedEvent?.Invoke(old, value);
            }
        }

        public NetworkVariable(T defaultValue
            , VariablePermission readPermission=VariablePermission.All
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
            var old = _value;
            _value = Serializer.Deserialize(ref reader);
            ValueChangedEvent?.Invoke(old, _value);
        }
    }
}