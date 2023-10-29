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

        private readonly Dictionary<int, INetworkVariable> _changedNetworkVariable =
            new Dictionary<int, INetworkVariable>();

        public bool IsClient => MyNetworkObject.IsClient;

        public NetworkObject MyNetworkObject { get; private set; }

        public IReadOnlyList<INetworkVariable> NetworkVariables => _networkVariables;

        private static bool CheckPermission(int clientId, VariablePermission permission, bool isOwner)
        {
            if (permission == VariablePermission.All) return true;
            if (clientId == 0) return true;
            return permission == VariablePermission.OwnerOnly && isOwner;
        }

        internal IReadOnlyDictionary<int, INetworkVariable> CalculateChangedVariable(int clientId)
        {
            _changedNetworkVariable.Clear();
            for (var i = 0; i < _networkVariables.Count; i++)
            {
                var variable = _networkVariables[i];
                if (!CheckPermission(clientId, variable.WritePermission, MyNetworkObject.OwnerId == clientId))
                {
                    continue;
                }
                if (variable.IsChanged) _changedNetworkVariable[i] = variable;
            }

            return _changedNetworkVariable;
        }

        internal void NetworkStart(NetworkObject networkObject)
        {
            MyNetworkObject = networkObject;
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