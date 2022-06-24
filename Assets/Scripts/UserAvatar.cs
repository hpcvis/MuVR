using System;
using RotaryHeart.Lib.SerializableDictionary;
using UnityEngine;

// Component that holds pose data. It acts as the glue between the input layer and the networking layer
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
}
