using System;
using System.Reflection;
using Netcode.Core;
using Netcode.Variable;
using UnityEngine;

namespace Netcode.Components
{
    public class NetworkTransform : NetworkBehaviour
    {

        public readonly NetworkVariable<Vector3> Test = new NetworkVariable<Vector3>(default);
        public readonly NetworkVariable<Vector3> Joystick = new NetworkVariable<Vector3>(default
            , writePermission: VariablePermission.OwnerOnly);

        private void UpdatePosition()
        {
            if (IsClient)
            {
                transform.position = Test.Value;
                Joystick.Value = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            }
            else
            {
                Test.Value += Joystick.Value * Time.deltaTime;
                transform.position = Test.Value;
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