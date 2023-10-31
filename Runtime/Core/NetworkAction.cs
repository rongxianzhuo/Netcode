namespace Netcode.Core
{
    public enum NetworkAction : byte
    {
        ConnectionApproval,
        SpawnObject,
        UpdateObject,
        DestroyObject
    }
}