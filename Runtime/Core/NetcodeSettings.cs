using System;
using UnityEngine;

namespace Netcode.Core
{
    
    [Serializable]
    public class NetcodeSettings
    {

        public float serverSendInterval = 0.1f;
        public float clientSendInternal = 0.03f;

    }
}