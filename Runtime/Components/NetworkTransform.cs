using Netcode.Core;
using Netcode.Variable;
using UnityEngine;

namespace Netcode.Components
{
    public class NetworkTransform : NetworkBehaviour
    {

        public readonly NetworkVariable<Vector3> Position = new NetworkVariable<Vector3>(default
            , writePermission: VariablePermission.ServerOnly);

        public override void OnNetworkStart()
        {
            base.OnNetworkStart();
            transform.position = Position.Value;
        }

        private void Update()
        {
            transform.position = IsClient ? Vector3.Lerp(transform.position, Position.Value, 0.15f) : Position.Value;
        }
    }
}