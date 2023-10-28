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

        protected override void OnNetworkStart()
        {
            base.OnNetworkStart();
            transform.position = new Vector3(Test.Value, 0, 0);
        }

        private void Update()
        {
            transform.position = new Vector3(Test.Value, 0, 0);
        }
    }
}