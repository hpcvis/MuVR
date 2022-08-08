using System;
using MuVR;
using UnityEngine;

[RequireComponent(typeof(SyncPose))]
public class OffsetByFeet : MonoBehaviour {

	private SyncPose sync;
	private float initialHeightOffset;

	private void Awake() {
		sync = GetComponent<SyncPose>();
		initialHeightOffset = sync.globalOffset.y;
	}

	// Update is called once per frame
	private void LateUpdate() {
		sync.globalOffset.y = initialHeightOffset - ProjectOnGround.averageHeightDifference / 2; // If we don't divide the average by 2, legs are lifted way too high!
	}
}