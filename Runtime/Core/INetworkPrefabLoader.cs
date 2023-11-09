using Netcode.Components;
using UnityEngine;

namespace Netcode.Core
{
    public interface INetworkPrefabLoader
    {
        NetworkObject Instantiate(int prefabId, Vector3 position, Quaternion rotation);
    }
}