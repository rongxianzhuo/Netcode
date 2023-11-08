using System;
using System.Collections.Generic;
using System.Reflection;
using Netcode.Core;
using Netcode.Variable;
using UnityEngine;

namespace Netcode.Components
{
    public class NetworkBehaviour : MonoBehaviour
    {

        private readonly List<INetworkVariable> _networkVariables = new List<INetworkVariable>();

        private readonly Dictionary<int, INetworkVariable> _sendNetworkVariable =
            new Dictionary<int, INetworkVariable>();

        public bool IsClient => MyNetworkObject.IsClient;

        public int OwnerId => MyNetworkObject.OwnerId;

        public bool IsOwner => MyNetworkObject.IsOwner;

        public NetworkObject MyNetworkObject { get; private set; }

        public IReadOnlyList<INetworkVariable> NetworkVariables => _networkVariables;

        private bool CheckSendPermission(INetworkVariable variable, int myClientId, int targetClientId)
        {
            if (myClientId == ServerNetworkManager.ClientId)
            {
                return variable.ReadPermission == VariablePermission.All ||
                       (variable.ReadPermission == VariablePermission.OwnerOnly && OwnerId == targetClientId);
            }
            if (variable.WritePermission == VariablePermission.All) return true;
            if (variable.WritePermission == VariablePermission.OwnerOnly && myClientId == OwnerId) return true;
            return false;
        }

        internal IReadOnlyDictionary<int, INetworkVariable> CalculateSendVariable(int myClientId
            , int targetClientId
            , bool excludeNotChangeVariable)
        {
            _sendNetworkVariable.Clear();
            for (var i = 0; i < _networkVariables.Count; i++)
            {
                var variable = _networkVariables[i];
                if (excludeNotChangeVariable && !variable.IsChanged) continue;
                if (!CheckSendPermission(variable, myClientId, targetClientId)) continue;
                _sendNetworkVariable[i] = variable;
            }
            return _sendNetworkVariable;
        }

        internal void NetworkInit(NetworkObject networkObject)
        {
            MyNetworkObject = networkObject;
            var fields = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            Array.Sort(fields, (info1, info2) => string.Compare(info1.Name, info2.Name, StringComparison.Ordinal));
            var typeOfINetworkVariable = typeof(INetworkVariable);
            foreach (var fieldInfo in fields)
            {
                if (!typeOfINetworkVariable.IsAssignableFrom(fieldInfo.FieldType)) continue;
                _networkVariables.Add((INetworkVariable)fieldInfo.GetValue(this));
            }
            OnNetworkInit(_networkVariables);
        }

        protected virtual void OnNetworkInit(List<INetworkVariable> networkVariables)
        {
            
        }

        public virtual void OnNetworkStart()
        {
            
        }
        
    }
}