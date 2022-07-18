using FishNet.Connection;
using MuVR.Enhanced;
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

			// If we used to be the owner, undo the owned offset and apply the unowned offset
			if (prev == LocalConnection) {
				target.position -= ownedOffset.position;
				target.rotation *= Quaternion.Inverse(ownedOffset.rotation);

				target.position += unownedOffset.position;
				target.rotation *= unownedOffset.rotation;
			
				// If we are the new owner, undo the unowned offset and apply the owned offset
			} else if (IsOwner) {
				target.position -= unownedOffset.position;
				target.rotation *= Quaternion.Inverse(unownedOffset.rotation);	
			
				target.position += ownedOffset.position;
				target.rotation *= ownedOffset.rotation;
			}
		}
	}
}