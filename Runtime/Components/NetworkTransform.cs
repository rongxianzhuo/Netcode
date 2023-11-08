using Netcode.Core;
using Netcode.Variable;
using UnityEngine;

namespace Netcode.Components
{
    public class NetworkTransform : NetworkBehaviour
    {

        public readonly NetworkVariable<Vector3> Position = new NetworkVariable<Vector3>(default
            , writePermission: VariablePermission.ServerOnly);

        public readonly NetworkVariable<Vector3> Rotation = new NetworkVariable<Vector3>(default
            , writePermission: VariablePermission.ServerOnly);

        public float interpolateFactor = 0.15f;

        public override void OnNetworkStart()
        {
            base.OnNetworkStart();
            transform.position = Position.Value;
        }

        private void Update()
        {
            transform.position = IsClient ? Vector3.Lerp(transform.position, Position.Value, interpolateFactor) : Position.Value;
            var rotation = Quaternion.Euler(Rotation.Value);
            transform.rotation = IsClient ? Quaternion.Slerp(transform.rotation, rotation, interpolateFactor) : rotation;
        }
    }
}