using System;
using System.Collections.Generic;
using Netcode.Core;
using Netcode.Variable;
using UnityEngine;
using UnityEngine.AI;

namespace Netcode.Components
{
    public sealed class NetworkObject : MonoBehaviour
    {

        private readonly Dictionary<int, IReadOnlyDictionary<int, INetworkVariable>> _changedNetworkBehaviour =
            new Dictionary<int, IReadOnlyDictionary<int, INetworkVariable>>();

        public Func<int, bool> CheckObjectVisibility = _ => true;

        [SerializeField]
        private int prefabId;

        public bool IsNetworkStarted { get; private set; }

        public int NetworkObjectId { get; private set; }

        public int OwnerId { get; private set; }

        public bool IsOwner { get; private set; }

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

        internal void NetworkInit(bool isClient, NetworkManager objectManager)
        {
            IsClient = isClient;
            foreach (var behaviour in _networkBehaviours)
            {
                behaviour.NetworkInit(objectManager, this);
            }

            if (isClient)
            {
                ForeachComponent(component =>
                {
                    switch (component)
                    {
                        case Collider:
                        case NavMeshAgent:
                            Destroy(component);
                            break;
                    }
                });
            }
            else
            {
                ForeachComponent(component =>
                {
                    switch (component)
                    {
                        case Renderer:
                        case MeshFilter:
                            Destroy(component);
                            break;
                    }
                });
            }
        }

        internal void NetworkStart(int ownerId, int networkId, bool isOwner)
        {
            IsOwner = isOwner;
            OwnerId = ownerId;
            NetworkObjectId = networkId;
            IsNetworkStarted = true;
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

        private void ForeachComponent(Action<Component> action, Transform root=null)
        {
            if (root == null) root = transform;
            foreach (var component in root.GetComponents<Component>())
            {
                action(component);
            }

            for (var i = 0; i < root.childCount; i++)
            {
                ForeachComponent(action, root.GetChild(i));
            }
        }
    }
}