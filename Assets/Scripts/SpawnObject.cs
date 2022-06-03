using System;
using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using UnityEngine;

/// <summary>
/// Spawns a set of prefabs owned by the server.
/// </summary>
public class SpawnObject : MonoBehaviour
{
    [SerializeField] private GameObject[] spawnables = new GameObject[0];

    private void OnEnable()
    {
        InstanceFinder.NetworkManager.SceneManager.OnClientLoadedStartScenes += OnClientLoadedStartScenes;
    }

    private void OnDisable()
    {
        InstanceFinder.NetworkManager.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedStartScenes;
    }

    private void OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
    {
        foreach (var obj in spawnables)
        {
            var spawnedObj = Instantiate(obj, new Vector3(0, 3, 0), Quaternion.identity);
            InstanceFinder.NetworkManager.ServerManager.Spawn(spawnedObj, conn);
        }
    }
}
