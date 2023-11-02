
using Netcode.Components;

namespace Netcode.Core
{
    public interface INetworkPrefabLoader
    {
        NetworkObject Instantiate(int prefabId);
    }
}