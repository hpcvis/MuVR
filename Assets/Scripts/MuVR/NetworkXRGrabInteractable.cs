using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MuVR {
	
	// Class that extends an XR Grab Interactable to properly manage an attached NetworkRigidbody
	public class NetworkXRGrabInteractable : XRGrabInteractable {
		private bool usedGravity;
		private float oldDrag;
		private float oldAngularDrag;

		private bool wasNetworkKinematic;
		private NetworkRigidbody networkRigidbody;

		protected override void Awake() {
			base.Awake();
			networkRigidbody = GetComponent<NetworkRigidbody>();
		}

		protected override void SetupRigidbodyGrab(Rigidbody rigidbody) {
			if (networkRigidbody is null) {
				base.SetupRigidbodyGrab(rigidbody);
				return;
			}

			usedGravity = rigidbody.useGravity;
			oldDrag = rigidbody.drag;
			oldAngularDrag = rigidbody.angularDrag;
			rigidbody.useGravity = false;
			rigidbody.drag = 0;
			rigidbody.angularDrag = 0;

			wasNetworkKinematic = networkRigidbody.targetIsKinematic;
			networkRigidbody.targetIsKinematic = movementType == MovementType.Kinematic || movementType == MovementType.Instantaneous;
			networkRigidbody.UpdateOwnershipKinematicState();
		}

		protected override void SetupRigidbodyDrop(Rigidbody rigidbody) {
			if (networkRigidbody is null) {
				base.SetupRigidbodyDrop(rigidbody);
				return;
			}

			rigidbody.useGravity = usedGravity;
			rigidbody.drag = oldDrag;
			rigidbody.angularDrag = oldAngularDrag;
			networkRigidbody.targetIsKinematic = wasNetworkKinematic;
			networkRigidbody.UpdateOwnershipKinematicState();
		}

		protected override void Detach() {
			base.Detach();
			if (networkRigidbody is null) return;

			networkRigidbody.velocity = networkRigidbody.target.velocity;
			networkRigidbody.angularVelocity = networkRigidbody.target.angularVelocity;
			// networkRigidbody.Tick();
		}
	}
}