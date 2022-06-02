using System;
using UnityEngine;

// Component that holds pose data. It acts as the glue between the input layer and the networking layer
public class PlayerAvatar : MonoBehaviour {
	// Enum representing linkage
	[Serializable]
	public enum Slot {
		INVALID,
		head,
		leftShoulder, rightShoulder,
		leftElbow, rightElbow,
		leftHand, rightHand,
		pelvis,
		leftKnee, rightKnee,
		leftFoot, rightFoot
	}

	// Class wrapper around unity's Pose to enable reference semantics
	[Serializable]
	public class PoseRef {
		public Pose pose = Pose.identity;
	}
	
	// Poses that can can be read to or from by the input and networking layers respectively
	[Header("Pose Transforms (Read Only)")] 
	[ReadOnly] public PoseRef head;
	[ReadOnly] public PoseRef leftShoulder, rightShoulder,
		leftElbow, rightElbow,
		leftHand, rightHand,
		pelvis,
		leftKnee, rightKnee,
		leftFoot, rightFoot;
}
