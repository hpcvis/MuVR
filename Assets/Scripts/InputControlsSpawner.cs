using System;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

// Component that spawns backend input controls when we acquire input authority
public class InputControlsSpawner : EnchancedNetworkBehaviour {
    [Tooltip("List of input controls that may be spawned as appropriate")]
    [SerializeField] private GameObject[] inputPrefabs;
    
    [Tooltip("Index indicating which of the input controls should be spawned")]
    public int spawnIndex = 0;
    [ReadOnly] public GameObject input = null;

    public override void OnStartClient() {
        base.OnStartClient();
        
        // If we have input authority, spawn the input controls
        if (IsOwner)
            SpawnInputControls();
        else
            DisableSyncs();
    }

    // When we become the input authority spawn the input controls, when we lose input authority remove the input controls
    public override void OnOwnershipClient(NetworkConnection oldOwner) {
        base.OnOwnershipClient(oldOwner);
        
        if (IsOwner && input is not null)
            Debug.LogWarning("For some reason authority changed but we still have it...");
        else if (IsOwner)
            SpawnInputControls();
        else if (input is not null) {
            Debug.Log("We are no longer the input authority and thus should get rid of our input controls");
            Destroy(input);
            input = null;

            DisableSyncs();
        }
    }
    

    // Function that spawns the input controls
    [Client]
    void SpawnInputControls() {
        if (spawnIndex > inputPrefabs.Length)
            throw new IndexOutOfRangeException("Spawn Index is not associated with a valid prefab");

        // TODO: Add functionality to spawn VR or non VR input
        Debug.Log("Spawning input controls!");
        input = Instantiate(inputPrefabs[spawnIndex], transform.position, transform.rotation, transform);
    }

    // If we aren't the owner disable all of the pose syncs... just rely on the network transforms
    [Client]
    void DisableSyncs() {
        SyncPose[] syncs = GetComponentsInChildren<SyncPose>();
        foreach (var sync in syncs)
            sync.enabled = false;
    }
}
