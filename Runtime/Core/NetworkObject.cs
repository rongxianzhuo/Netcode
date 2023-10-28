using UnityEngine;

namespace Netcode.Core
{
    public class NetworkObject : MonoBehaviour
    {

        [SerializeField]
        private int prefabId;

        public int NetworkObjectId { get; internal set; }

        public int PrefabId => prefabId;

    }
}