using FishNet.Connection;
using UnityEngine;

// Component that changes a sync pose's offset based on its ownership status
public class OffsetSyncPoseOnOwnership : EnchancedNetworkBehaviour {
	// The SyncPose to be updated
	public SyncPose target;
	// The offsets when the object is owned or not
	public Pose ownedOffset = Pose.identity, unownedOffset = Pose.identity;

	// Automatically set target equal to a SyncPose on the same object
	new void OnValidate() {
		base.OnValidate();
		target ??= GetComponent<SyncPose>();
	}

	// When ownership changes set the pose appropriately
	public override void OnOwnershipBoth(NetworkConnection prev) {
		base.OnOwnershipBoth(prev);
		target.offset = IsOwner ? ownedOffset : unownedOffset;
	}
}