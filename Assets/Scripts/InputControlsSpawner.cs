using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

// Component that spawns backend input controls when we acquire input authority
public class InputControlsSpawner : NetworkBehaviour {
    [Tooltip("List of input controls that may be spawned as appropriate")]
    [SerializeField] private GameObject[] inputPrefabs;
    
    [Tooltip("Index indicating which of the input controls should be spawned")]
    public int spawnIndex = 0;
    [ReadOnly] public GameObject input = null;

    // Variable tracking which player had input authority last frame
    [SerializeField, ReadOnly] private PlayerRef oldInputAuthority = PlayerRef.None;
    
    public void Update() {
        // Check if the input authority has changed since last frame
        if (oldInputAuthority == Object.InputAuthority) return;
        
        OnInputAuthorityChanged(Object.InputAuthority == Runner.LocalPlayer);
        oldInputAuthority = Object.InputAuthority;
    }

    public override void Spawned() {
        // If we have input authority, spawn the input controls
        if (Object.InputAuthority == Runner.LocalPlayer)
            SpawnInputControls();
        // Make sure the old input authority is properly initialized
        oldInputAuthority = Object.InputAuthority;
    }

    // When we become the input authority spawn the input controls, when we lose input authority remove the input controls
    public void OnInputAuthorityChanged(bool haveAuthority) {
        if (haveAuthority && input is not null)
            Debug.LogWarning("For some reason authority changed but we still have it...");
        else if (haveAuthority)
            SpawnInputControls();
        else if(input is not null) {
            Debug.Log("We are no longer the input authority and thus should get rid of our input controls");
            Destroy(input);
            input = null;
        }
    }

    // Function that spawns the input controls
    void SpawnInputControls() {
        if (spawnIndex > inputPrefabs.Length)
            throw new IndexOutOfRangeException("Spawn Index is not associated with a valid prefab");
        
        // TODO: Add functionality to spawn VR or non VR input
        Debug.Log("Spawning input controls!");
        input = Instantiate(inputPrefabs[spawnIndex], Vector3.zero, Quaternion.identity, transform);
        
        // TODO: We need a way to hide the HMD that isn't hard coded...
        transform.GetChild(0).gameObject.SetActive(false);
    }
}
