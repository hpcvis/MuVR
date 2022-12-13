﻿using UnityEngine;

public class GhostAvatarInputLinker : MonoBehaviour {
	public ProximityHandFade[] leftHand, rightHand;

	public void Start() {
		var pair = transform.parent.GetComponentInChildren<GhostAvatarVisualsLinker>();
		foreach(var b in leftHand) b.physicsBone = pair.leftHand;
		foreach(var b in rightHand) b.physicsBone = pair.rightHand;
	}
}