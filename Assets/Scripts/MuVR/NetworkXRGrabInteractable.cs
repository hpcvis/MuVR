using MuVR.Enhanced;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
#if UNITY_EDITOR
using FishNet.Component.Transforming;
using FishNet.Object;
using UnityEditor;
#endif

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


		// Function that adds an option to convert XRGrab Interacatbles 
#if UNITY_EDITOR
		[MenuItem("CONTEXT/XRGrabInteractable/Make Networked")]
		public static void MakeNetworkXRGrabInteractable(MenuCommand command) {
			var interactable = (XRGrabInteractable)command.context;
			var go = interactable.gameObject;

			var copy = new GameObject().AddComponent<XRGrabInteractable>().CloneFromWithIL(interactable);

			DestroyImmediate(interactable);
			go.AddComponent<NetworkXRGrabInteractable>().CloneFromWithIL(copy);
			DestroyImmediate(copy.gameObject);
		}
		
		[MenuItem("CONTEXT/NetworkXRGrabInteractable/Setup Object Networking")]
		public static void SetupObjectNetworking(MenuCommand command) {
			var interactable = (NetworkXRGrabInteractable)command.context;

			var no = interactable.GetComponent<NetworkObject>();
			var om = interactable.GetComponent<OwnershipManager>();
			var nt = interactable.GetComponent<NetworkTransform>();
			var rb = interactable.GetComponent<Rigidbody>();
			var nrb = interactable.GetComponent<NetworkRigidbody>();

			no ??= interactable.gameObject.AddComponent<NetworkObject>();
			om ??= interactable.gameObject.AddComponent<OwnershipManager>();
			nt ??= interactable.gameObject.AddComponent<NetworkTransform>();
			if (rb is not null) nrb ??= interactable.gameObject.AddComponent<NetworkRigidbody>();
		}
#endif
	}
}