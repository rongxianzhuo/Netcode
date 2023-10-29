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

        private void UpdatePosition()
        {
            if (MyNetworkObject.IsClient)
            {
                transform.position = new Vector3(Test.Value, 0, 0);
            }
            else
            {
                Test.Value = Mathf.Sin(Time.time);
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