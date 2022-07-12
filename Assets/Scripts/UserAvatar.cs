using System;
using FishNet.Connection;
using FishNet.Object;
using RotaryHeart.Lib.SerializableDictionary;
using UnityEngine;
using UnityEngine.Events;
using TriInspector;

// Component that holds pose data. It acts as the glue between the input layer and the networking layer.
// Additionally, it provides a convenient method for spawning 
public class UserAvatar : NetworkBehaviour {
	// Class wrapper around unity's Pose to enable reference semantics
	[Serializable]
	public class PoseRef {
		public Pose pose = Pose.identity;
	}
	
	// Implementation of the particular type of serialized dictionary used by this object
	[Serializable]
	public class StringToPoseRefDictionary : SerializableDictionaryBase<string, PoseRef> { }

	// Poses that can can be read to or from by the input and networking layers respectively
	[Header("Pose Transforms")]
	public StringToPoseRefDictionary slots = new();



    
    [Header("Input Configuration")]
    [PropertyTooltip("List of input controls that may be spawned as appropriate")]
    [SerializeField] private GameObject[] inputPrefabs;
    
    [PropertyTooltip("Index indicating which of the input controls should be spawned")]
    public int spawnIndex = 0;
    [ReadOnly] public GameObject input = null;
    
    // Event invoked when input controls are spawned
    public UnityEvent<GameObject> onInputSpawned;
    // Function called when input controls are spawned, allows very easy access to the event on inherited objects
    protected virtual void OnInputSpawned(GameObject g) {}

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
            DisableSyncs();
            input = null;
        }
    }
    

    // Function that spawns the input controls
    [Client]
    private void SpawnInputControls() {
        if (spawnIndex > inputPrefabs.Length)
            throw new IndexOutOfRangeException("Spawn Index is not associated with a valid prefab");

        // TODO: Add functionality to spawn VR or non VR input
        Debug.Log("Spawning input controls!");
        input = Instantiate(inputPrefabs[spawnIndex], transform.position, transform.rotation, transform);
        
        // Notify the outside world that input controls have been spawned
        onInputSpawned?.Invoke(input);
        OnInputSpawned(input);
    }
    
    // If we aren't the owner disable all of the pose syncs... just rely on the network transforms
    [Client]
    private void DisableSyncs() {
        var syncs = GetComponentsInChildren<SyncPose>();
        foreach (var sync in syncs)
            sync.enabled = false;
    }
}
