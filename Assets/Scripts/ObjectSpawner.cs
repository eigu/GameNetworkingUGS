using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityUtils;

public class ObjectSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject _prefab;
    [SerializeField] private int _numberOfPrefabs = 10;

    private void Start()
    {
        if (!HasAuthority) return;
        if (!NetworkManager.LocalClient.IsSessionOwner) return;

        List<Vector3> randomPoints = new List<Vector3>();

        for (int i = 0; i < _numberOfPrefabs; i++)
        {
            randomPoints.Add(Vector3.zero.RandomPointInAnnulus(5, 10));
        }

        for (int i = 0; i < _numberOfPrefabs; ++i)
        {
            var instance = Instantiate(_prefab);

            var networkObject = instance.GetComponent<NetworkObject>();

            instance.transform.position = randomPoints[i];

            networkObject.Spawn();
        }
    }
}
