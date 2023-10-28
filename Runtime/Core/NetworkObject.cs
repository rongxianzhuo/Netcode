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

        private NetworkBehaviour[] _networkBehaviours;

        public bool IsClient { get; private set; }

        public IReadOnlyList<NetworkBehaviour> NetworkBehaviours => _networkBehaviours;

        internal void NetworkStart(bool isClient)
        {
            IsClient = isClient;
            if (_networkBehaviours == null) return;
            foreach (var behaviour in _networkBehaviours)
            {
                behaviour.NetworkStart(isClient);
            }
        }

        private void Awake()
        {
            _networkBehaviours = GetComponents<NetworkBehaviour>();
        }
    }
}