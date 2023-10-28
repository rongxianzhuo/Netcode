using System;
using System.Collections.Generic;
using System.Reflection;
using Netcode.Variable;
using UnityEngine;

namespace Netcode.Core
{
    public class NetworkBehaviour : MonoBehaviour
    {

        private readonly List<INetworkVariable> _networkVariables = new List<INetworkVariable>();

        public bool IsClient { get; private set; }

        public IReadOnlyList<INetworkVariable> NetworkVariables => _networkVariables;

        internal void NetworkStart(bool isClient)
        {
            IsClient = isClient;
            OnNetworkStart();
        }

        protected virtual void OnNetworkStart()
        {
            
        }

        protected virtual void Awake()
        {
            var fields = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            Array.Sort(fields, (info1, info2) => string.Compare(info1.Name, info2.Name, StringComparison.Ordinal));
            var typeOfINetworkVariable = typeof(INetworkVariable);
            foreach (var fieldInfo in fields)
            {
                if (!typeOfINetworkVariable.IsAssignableFrom(fieldInfo.FieldType)) continue;
                _networkVariables.Add((INetworkVariable)fieldInfo.GetValue(this));
            }
        }
        
    }
}