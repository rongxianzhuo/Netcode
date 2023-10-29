using System;
using System.Reflection;
using Netcode.Core;
using Netcode.Variable;
using UnityEngine;

namespace Netcode.Components
{
    public class NetworkTransform : NetworkBehaviour
    {

        public readonly NetworkVariable<float> Test = new NetworkVariable<float>(default);
        public readonly NetworkVariable<float> Joystick = new NetworkVariable<float>(default
            , writePermission: VariablePermission.OwnerOnly);

        private void UpdatePosition()
        {
            if (IsClient)
            {
                transform.position = new Vector3(Test.Value, 0, 0);
                Joystick.Value = Input.GetAxis("Horizontal");
            }
            else
            {
                Test.Value += Joystick.Value * Time.deltaTime;
                transform.position = new Vector3(Test.Value, 0, 0);
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