using System;
using System.Collections.Generic;
using Netcode.Variable;
using UnityEngine;

namespace Netcode.Components
{
    public sealed class NetworkObject : MonoBehaviour
    {

        private readonly Dictionary<int, IReadOnlyDictionary<int, INetworkVariable>> _changedNetworkBehaviour =
            new Dictionary<int, IReadOnlyDictionary<int, INetworkVariable>>();

        public Func<int, bool> CheckObjectVisibility = _ => true;

        [SerializeField]
        private int prefabId;

        public int NetworkObjectId { get; private set; }

        public int OwnerId { get; private set; }

        public int PrefabId => prefabId;

        private NetworkBehaviour[] _networkBehaviours = Array.Empty<NetworkBehaviour>();

        public bool IsClient { get; private set; }

        public IReadOnlyList<NetworkBehaviour> NetworkBehaviours => _networkBehaviours;

        internal Dictionary<int, IReadOnlyDictionary<int, INetworkVariable>> CalculateSendVariable(int clientId
            , int targetClientId
            , bool excludeNotChangeVariable)
        {
            _changedNetworkBehaviour.Clear();
            for (var i = 0; i < _networkBehaviours.Length; i++)
            {
                var behaviour = _networkBehaviours[i];
                var changeVariable = 
                    behaviour.CalculateSendVariable(clientId, targetClientId, excludeNotChangeVariable);
                if (changeVariable.Count == 0) continue;
                _changedNetworkBehaviour[i] = changeVariable;
            }

            return _changedNetworkBehaviour;
        }

        internal void NetworkInit()
        {
            foreach (var behaviour in _networkBehaviours)
            {
                behaviour.NetworkInit(this);
            }
        }

        internal void NetworkStart(bool isClient, int ownerId, int networkId)
        {
            IsClient = isClient;
            OwnerId = ownerId;
            NetworkObjectId = networkId;
            foreach (var behaviour in _networkBehaviours)
            {
                behaviour.OnNetworkStart();
            }
        }

        private void Awake()
        {
            var networkBehaviours = GetComponents<NetworkBehaviour>();
            if (networkBehaviours == null) return;
            _networkBehaviours = networkBehaviours;
        }
    }
}