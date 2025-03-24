using Unity.Netcode;
using UnityEngine;


public class OwnershipPickup : NetworkBehaviour
{
    [SerializeField] private float _pickupRadius = 2f;
    [SerializeField] private string _pickupTag = "Pickup";

    private void Update()
    {
        if (!HasAuthority || !IsSpawned) return;

        var nearbyColliders = Physics.OverlapSphere(transform.position, _pickupRadius);

        foreach (Collider collider in nearbyColliders)
        {
            if (!collider.CompareTag(_pickupTag)) continue;

            NetworkObject networkObject = collider.GetComponent<NetworkObject>();

            if (networkObject == null || !networkObject.IsSpawned) continue;

            if (!networkObject.IsOwner)
            {
                Debug.Log($"Change ownership of {networkObject} to {NetworkManager.LocalClientId}");

                networkObject.ChangeOwnership(NetworkManager.LocalClientId);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(transform.position, _pickupRadius);
    }
}
