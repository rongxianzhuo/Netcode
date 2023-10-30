using System;
using System.Reflection;
using Netcode.Core;
using Netcode.Variable;
using UnityEngine;

namespace Netcode.Components
{
    public class NetworkTransform : NetworkBehaviour
    {

        public readonly NetworkVariable<Vector3> Position = new NetworkVariable<Vector3>(default
            , writePermission: VariablePermission.ServerOnly);
        
        public readonly NetworkVariable<Vector3> Joystick = new NetworkVariable<Vector3>(default
            , writePermission: VariablePermission.OwnerOnly);

        private void UpdatePosition()
        {
            if (IsClient)
            {
                transform.position = Vector3.Lerp(transform.position, Position.Value, 0.15f);
                Joystick.Value = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            }
            else
            {
                Position.Value += Joystick.Value * Time.deltaTime;
                transform.position = Position.Value;
            }
        }

        protected override void OnNetworkStart()
        {
            base.OnNetworkStart();
            UpdatePosition();
        }

        private void Update()
        {
            UpdatePosition();
        }
    }
}