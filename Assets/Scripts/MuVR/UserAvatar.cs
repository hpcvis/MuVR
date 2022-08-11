using System;
using FishNet.Connection;
using FishNet.Object;
using RotaryHeart.Lib.SerializableDictionary;
using TriInspector;
using UnityEngine;
using UnityEngine.Events;

namespace MuVR {
	
	// Component that holds pose data. It acts as the glue between the input layer and the networking layer.
	// Additionally, it provides a convenient method for spawning 
	public class UserAvatar : NetworkBehaviour {
		#region Pose Slots

		// Class wrapper around unity's Pose to enable reference semantics
		[Serializable]
		public class PoseRef {
			public Pose pose = Pose.identity;
		}

		// Implementation of the particular type of serialized dictionary used by this object
		[Serializable]
		public class StringToPoseRefDictionary : SerializableDictionaryBase<string, PoseRef> { }

		// Poses that can can be read to or from by the input and networking layers respectively
		[Title("Pose Transforms")] 
		public StringToPoseRefDictionary slots = new();
		
		// Provide separate functions that return a reference to the PoseRef used for setting and getting
		// NOTE: Provides support for the PostProcessed Avatar
		public virtual PoseRef SetterPoseRef(string slot) => slots[slot];
		public virtual PoseRef GetterPoseRef(string slot) => slots[slot];

		// Creates a game object that synchronizes its transform with this slot, and return its transform
		public Transform FindOrCreatePoseProxy(string slot) {
			Transform proxy, cached;
			if (!slots.ContainsKey(slot)) throw new ArgumentException("The given slot " + slot + " is not stored within this avatar");
			if ((proxy = transform.Find("Proxies")) is null) proxy = new GameObject { transform = { parent = this.transform }, name = "Proxies" }.transform;
			if ((cached = proxy.Find(slot)) is not null) return cached;

			var sp = new GameObject { transform = { parent = proxy }, name = slot }.AddComponent<SyncPose>();
			sp.targetAvatar = this;
			sp.slot = slot;
			sp.mode = SyncPose.SyncMode.SyncFrom;

			return sp.transform;
		}

		#endregion
		

		// -- Input Spawning --


		#region Input Spawning

		[Title("Input Configuration")]
		[PropertyTooltip("List of input controls that may be spawned as appropriate")]
		[SerializeField] private GameObject[] inputPrefabs;

		[PropertyTooltip("Index indicating which of the input controls should be spawned")]
		public int spawnIndex;

		[PropertyTooltip("The input object that gets spawned")]
		[ReadOnly] public GameObject input;

		// Event invoked when input controls are spawned
		public UnityEvent<GameObject> onInputSpawned;

		// Function called when input controls are spawned, allows very easy access to the event on inherited objects
		protected virtual void OnInputSpawned(GameObject g) { }

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
		
		#endregion
	}
}