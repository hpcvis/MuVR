using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAvatar : MonoBehaviour {
	// Enum representing linkage
	[Serializable]
	public enum Linkage {
		INVALID,
		head,
		leftShoulder, rightShoulder,
		leftElbow, rightElbow,
		leftHand, rightHand,
		pelvis,
		leftKnee, rightKnee,
		leftFoot, rightFoot
	}
	
	[Header("Pose Transforms")] 
	public Transform head;
	public Transform leftShoulder, rightShoulder,
		leftElbow, rightElbow,
		leftHand, rightHand,
		pelvis,
		leftKnee, rightKnee,
		leftFoot, rightFoot;
}
