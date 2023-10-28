using System;
using Netcode.Core;
using Unity.Networking.Transport;
using UnityEngine;

namespace Netcode
{

    public class ClientBehaviour : MonoBehaviour
    {

        private readonly NetworkManager _manager = new NetworkManager();

        private void Start()
        {
            _manager.StartClient();
            _manager.ConnectServer();
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