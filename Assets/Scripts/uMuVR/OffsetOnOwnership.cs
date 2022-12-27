using FishNet.Connection;
using uMuVR.Enhanced;
using UnityEngine;

namespace uMuVR {
	
	/// <summary>
	/// Component that changes a child transform's offset based on its ownership status
	/// </summary>
	public class OffsetOnOwnership : NetworkBehaviour {
		/// <summary>
		/// Target to be updated
		/// </summary>
		public Transform target;
		/// <summary>
		/// The offsets when the object is owned or not
		/// </summary>
		public Pose ownedOffset = Pose.identity, unownedOffset = Pose.identity;
		
		/// <summary>
		/// Automatically set target equal to this object's transform if not already set
		/// </summary>
		private new void OnValidate() {
			base.OnValidate();
			target ??= GetComponent<Transform>();
		}
		
		/// <summary>
		/// When ownership changes set the pose appropriately
		/// </summary>
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