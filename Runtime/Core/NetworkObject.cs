using System;
using System.Collections.Generic;
using UnityEngine;

namespace Netcode.Core
{
    public sealed class NetworkObject : MonoBehaviour
    {

        [SerializeField]
        private int prefabId;

        public int NetworkObjectId { get; internal set; }

        public int PrefabId => prefabId;

        private NetworkBehaviour[] _networkBehaviours = Array.Empty<NetworkBehaviour>();

        public bool IsClient { get; private set; }

        public IReadOnlyList<NetworkBehaviour> NetworkBehaviours => _networkBehaviours;

        internal void NetworkStart(bool isClient)
        {
            IsClient = isClient;
            foreach (var behaviour in _networkBehaviours)
            {
                behaviour.NetworkStart(isClient);
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