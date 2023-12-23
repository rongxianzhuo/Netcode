using Netcode.Core;
using Netcode.Message;
using Netcode.Variable;
using UnityEngine;

namespace Netcode.Components
{ 
    public class NetworkTransform : NetworkBehaviour
    {

        private readonly NetworkVariable<Vector3> _networkPosition = new NetworkVariable<Vector3>(default
            , writePermission: VariablePermission.ServerOnly
            , delivery: NetworkDelivery.Unreliable);

        private readonly NetworkVariable<Vector3> _networkRotation = new NetworkVariable<Vector3>(default
            , writePermission: VariablePermission.ServerOnly
            , delivery: NetworkDelivery.Unreliable);

        public float interpolateFactor = 0.15f;
        public float blinkDistance = 3;

        public override void OnNetworkStart()
        {
            base.OnNetworkStart();
            if (IsClient)
            {
                transform.position = _networkPosition.Value;
                transform.rotation = Quaternion.Euler(_networkRotation.Value);
            }
            else
            {
                _networkPosition.Value = transform.position;
                _networkRotation.Value = transform.eulerAngles;
            }
        }

        private void Update()
        {
            if (IsClient)
            {
                var distance = Vector3.Distance(transform.position, _networkPosition.Value);
                if (distance > blinkDistance)
                {
                    transform.position = _networkPosition.Value;
                    transform.rotation = Quaternion.Euler(_networkRotation.Value);
                }
                else
                {
                    transform.position = Vector3.Lerp(transform.position, _networkPosition.Value, interpolateFactor);
                    var rotation = Quaternion.Euler(_networkRotation.Value);
                    transform.rotation = Quaternion.Slerp(transform.rotation, rotation, interpolateFactor);
                }
            }
            else
            {
                _networkPosition.Value = transform.position;
                _networkRotation.Value = transform.eulerAngles;
            }
        }
    }
}