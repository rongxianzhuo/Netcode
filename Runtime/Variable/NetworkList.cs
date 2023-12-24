using System;
using System.Collections.Generic;
using Netcode.Message;
using Unity.Collections;
using UnityEngine;

namespace Netcode.Variable
{
    public class NetworkList<T>: INetworkVariable where T : IEquatable<T>
    {

        private readonly List<T> _internalList = new List<T>();
        
        static NetworkList()
        {
            NetworkList<bool>.Serializer = new BoolNetworkVariableSerializer();
            NetworkList<int>.Serializer = new IntNetworkVariableSerializer();
            NetworkList<float>.Serializer = new FloatNetworkVariableSerializer();
            NetworkList<Vector3>.Serializer = new Vector3NetworkVariableSerializer();
        }

        private static readonly INetworkVariableSerializer<T> Serializer;

        public bool IsChanged { get; private set; }

        public void ClearChange()
        {
            IsChanged = false;
        }

        public VariablePermission ReadPermission { get; }

        public VariablePermission WritePermission { get; }

        public NetworkDelivery Delivery { get; }

        public NetworkList(IReadOnlyCollection<T> defaultValue
            , VariablePermission readPermission=VariablePermission.All
            , VariablePermission writePermission=VariablePermission.ServerOnly
            , NetworkDelivery delivery=NetworkDelivery.ReliableSequenced)
        {
            ReadPermission = readPermission;
            WritePermission = writePermission;
            Delivery = delivery;
            if (defaultValue != null && defaultValue.Count != 0)
            {
                _internalList.AddRange(defaultValue);
            }
        }

        public T this[int index]
        {
            get => _internalList[index];
            set
            {
                if (_internalList[index].Equals(value)) return;
                IsChanged = true;
                _internalList[index] = value;
            }
        }

        public int Count => _internalList.Count;

        public void Add(T t)
        {
            _internalList.Add(t);
            IsChanged = true;
        }

        public void Serialize(ref DataStreamWriter writer)
        {
            writer.WriteInt(_internalList.Count);
            foreach (var t in _internalList)
            {
                Serializer.Serialize(ref writer, t);
            }
        }

        public void Deserialize(ref DataStreamReader reader)
        {
            var count = reader.ReadInt();
            _internalList.Clear();
            for (var i = 0; i < count; i++)
            {
                _internalList.Add(Serializer.Deserialize(ref reader));
            }
        }
    }
}