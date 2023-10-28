using System;
using Netcode.Core;
using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;

namespace Netcode
{

    public class ServerBehaviour : MonoBehaviour
    {

        private readonly NetworkManager _manager = new NetworkManager();

        private void Start()
        {
            _manager.StartServer();
        }

        private void Update()
        {
            _manager.Update();
        }

        private void OnDestroy()
        {
            _manager.StopNetwork();
        }
    }

}