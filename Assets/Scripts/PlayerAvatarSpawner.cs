using System;
using UnityEngine;
using Fusion;
using RotaryHeart.Lib.SerializableDictionary;

// Component responsible for spawning 
public class PlayerAvatarSpawner : NetworkRunnerCallbacksBehaviour {
    
    [Tooltip("The avatar prefab to spawn")]
    public NetworkPrefabRef prefab;

    // Map of player references to the avatar we spawned for that player
    [Serializable] public class PlayerNODictionary : SerializableDictionaryBase<PlayerRef, NetworkObject> { }
    [SerializeField] private PlayerNODictionary spawnedAvatars = new PlayerNODictionary();

    // When a player joins the game spawn their avatar, and save a reference in the dictionary
    public override void OnPlayerJoined(NetworkRunner runner, PlayerRef player) {
        Debug.Log("Player has connected!");
        spawnedAvatars.Add(player, runner.Spawn(prefab, Vector3.zero, Quaternion.identity, player));
    }

    // When the player leaves the game, despawn their avatar and remove its reference from the dictionary
    public override void OnPlayerLeft(NetworkRunner runner, PlayerRef player) {
        if (spawnedAvatars.TryGetValue(player, out NetworkObject networkObject)) {
            runner.Despawn(networkObject);
            spawnedAvatars.Remove(player);
        }
        Debug.Log("Player has disconnected!");
    }
}    