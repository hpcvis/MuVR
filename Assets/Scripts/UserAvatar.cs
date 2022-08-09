using System;
using RotaryHeart.Lib.SerializableDictionary;
using UnityEngine;

namespace MuVR {
	
	// Component that holds pose data. It acts as the glue between the input layer and the networking layer.
	// Additionally, it provides a convenient method for spawning 
	public class UserAvatar : MonoBehaviour {
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
	}
}