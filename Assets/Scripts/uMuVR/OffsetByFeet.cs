using uMuVR;
using UnityEngine;

/// <summary>
/// Offsets a SyncPose based on the average offset of the character's feet
/// </summary>
[RequireComponent(typeof(SyncPose))]
public class OffsetByFeet : MonoBehaviour {
	/// <summary>
	/// The target sync pose
	/// </summary>
	private SyncPose sync;
	/// <summary>
	/// The sync pose's initial offset
	/// </summary>
	private float initialHeightOffset;

	/// <summary>
	/// When the game starts get a reference to the connected sync pose and figure out its configured offset
	/// </summary>
	private void Awake() {
		sync = GetComponent<SyncPose>();
		initialHeightOffset = sync.globalPositionOffset.y;
	}

	/// <summary>
	/// Every frame update the sync pose's offset based on the feet position
	/// </summary>
	private void LateUpdate() {
		sync.globalPositionOffset.y = initialHeightOffset - ProjectFootOnGround.averageHeightDifference / 2; // If we don't divide the average by 2, legs are lifted way too high!
	}
}