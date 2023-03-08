using System;
using FishNet.Connection;
using FishNet.Object;
using uMuVR.Utility;
using RotaryHeart.Lib.SerializableDictionary;
using TriInspector;
using UnityEngine;
using UnityEngine.Events;

namespace uMuVR {
	
	/// <summary>
	/// Component that holds pose data. It acts as the glue between the input layer and the networking layer.
	/// Additionally, it provides a convenient method for spawning input controls for the object's owner
	/// </summary>
	public class UserAvatar : NetworkBehaviour {
		#region Pose Slots
		
		/// <summary>
		/// Class wrapper around unity's Pose struct to enable reference semantics
		/// </summary>
		[Serializable]
		public class PoseRef {
			public Pose pose = Pose.identity;
		}
		
		/// <summary>
		/// Implementation of the particular type of serialized dictionary used by this object
		/// </summary>
		[Serializable]
		public class StringToPoseRefDictionary : SerializableDictionaryBase<string, PoseRef> { }
		
		/// <summary>
		/// Poses that can can be read to or from by the input and networking layers respectively
		/// </summary>
		[Title("Pose Transforms")] 
		public StringToPoseRefDictionary slots = new();
		
		/// <summary>
		/// Reference to a pose slot for storage purposes
		/// </summary>
		/// <remarks>NOTE: Provides support for the PostProcessed Avatar</remarks>
		/// <param name="slot">Name of the slot to reference</param>
		/// <returns>Reference to the slot where data can be stored</returns>
		public virtual PoseRef SetterPoseRef(string slot) => slots[slot];
		/// <summary>
		/// Reference to a pose slot for reading purposes
		/// </summary>
		/// <remarks>NOTE: Provides support for the PostProcessed Avatar</remarks>
		/// <param name="slot">Name of the slot to reference</param>
		/// <returns>Reference to the slot where data can be read</returns>
		public virtual PoseRef GetterPoseRef(string slot) => slots[slot];
		
		/// <summary>
		/// Creates (or finds if it already exists) a game object that synchronizes its transform with this slot, and return its transform
		/// </summary>
		/// <param name="slot">The name of the slot to reference</param>
		/// <returns>Transform of the new proxy object</returns>
		/// <exception cref="ArgumentException">Argument exception if the slot can't be found in the dictionary</exception>
		public Transform FindOrCreatePoseProxy(string slot) {
			Transform proxy, cached;
			// If the slot doesn't exist in the dictionary... error
			if (!slots.ContainsKey(slot)) throw new ArgumentException("The given slot " + slot + " is not stored within this avatar");
			// If the proxy holder object doesn't already exist... create it
			if ((proxy = transform.Find("Proxies")) is null) proxy = new GameObject { transform = { parent = this.transform }, name = "Proxies" }.transform;
			// If there is a proxy for this slot which already exists... return it instead
			if ((cached = proxy.Find(slot)) is not null) return cached;

			// Create a new proxy game object and attach a sync pose to it
			var sp = new GameObject { transform = { parent = proxy }, name = slot }.AddComponent<SyncPose>();
			// Configure the sync pose to reference the appropriate slot
			sp.targetAvatar = this;
			sp.slot = slot;
			sp.mode = ISyncable.SyncMode.Load;

			// Return the transform of that newly created proxy object
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
		
		/// <summary>
		/// Event invoked when input controls are spawned
		/// </summary>
		public UnityEvent<GameObject> onInputSpawned;
		
		/// <summary>
		/// Function called when input controls are spawned, allows very easy access to the event on inherited objects
		/// </summary>
		/// <param name="g">Reference to the spawned controls</param>
		protected virtual void OnInputSpawned(GameObject g) { }

		/// <summary>
		/// When the client starts, if we are the avatar's owner spawn us controls, otherwise disable Avatar syncs and rely on data from the network
		/// </summary>
		public override void OnStartClient() {
			base.OnStartClient();

			// If we have input authority, spawn the input controls
			if (IsOwner)
				SpawnInputControls();
			else
				DisableSyncs();
		}

		 
		/// <summary>
		/// When we become the input authority spawn the input controls, when we lose input authority remove the input controls
		/// </summary>
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

		
		/// <summary>
		/// Function that spawns the input controls
		/// </summary>
		/// <exception cref="IndexOutOfRangeException">Throws an exception if the index of the input controls which should be spawned is out of range</exception>
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
		
		/// <summary>
		/// (client only) If we aren't the owner disable all of the SyncPoses... just rely on the network transforms
		/// </summary>
		[Client]
		private void DisableSyncs() {
			var syncs = GetComponentsInChildren<ISyncable>();
			foreach (var sync in syncs)
				sync.enabled = false;
		}
		
		#endregion
	}
}