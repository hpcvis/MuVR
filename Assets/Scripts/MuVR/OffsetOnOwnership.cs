using FishNet.Connection;
using MuVR.Enchanced;
using UnityEngine;

namespace MuVR {
	
	// Component that changes a child transform's offset based on its ownership status
	public class OffsetOnOwnership : NetworkBehaviour {
		// The SyncPose to be updated
		public Transform target;

		// The offsets when the object is owned or not
		public Pose ownedOffset = Pose.identity, unownedOffset = Pose.identity;

		// Automatically set target equal to a SyncPose on the same object
		private new void OnValidate() {
			base.OnValidate();
			target ??= GetComponent<Transform>();
		}

		// When ownership changes set the pose appropriately
		public override void OnOwnershipBoth(NetworkConnection prev) {
			base.OnOwnershipBoth(prev);
			var offset = IsOwner ? ownedOffset : unownedOffset;

			target.position = offset.position;
			target.rotation = offset.rotation;
		}
	}
}