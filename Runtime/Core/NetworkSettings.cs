using UnityEngine;

namespace Netcode.Core
{
    
    [CreateAssetMenu(fileName = "NetworkSettings", menuName = "Netcode/Create NetworkSettings", order = 0)]
    public class NetworkSettings : ScriptableObject
    {

        public NetworkObject objectPrefab;

    }
}