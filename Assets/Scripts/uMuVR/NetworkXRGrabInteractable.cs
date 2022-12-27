using uMuVR.Enhanced;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
#if UNITY_EDITOR
using FishNet.Component.Transforming;
using FishNet.Object;
using UnityEditor;
#endif

namespace uMuVR {
	
	/// <summary>
	/// Class that extends an XR Grab Interactable to properly manage an attached NetworkRigidbody
	/// </summary>
	public class NetworkXRGrabInteractable : XRGrabInteractable {
		/// <summary>
		/// Variable used to save properties that get changed (Gravity)
		/// </summary>
		private bool usedGravity;
		/// <summary>
		/// Variable used to save properties that get changed (Drag)
		/// </summary>
		private float oldDrag;
		/// <summary>
		/// Variable used to save properties that get changed (Angular Drag)
		/// </summary>
		private float oldAngularDrag;
		/// <summary>
		/// Variable used to save properties that get changed (isKinematic)
		/// </summary>
		private bool wasNetworkKinematic;
		
		/// <summary>
		/// Reference to the network rigidbody attached to this object
		/// </summary>
		private NetworkRigidbody networkRigidbody;

		/// <summary>
		/// 
		/// </summary>
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
		
		
		
		
#if UNITY_EDITOR
		/// <summary>
		/// Function that adds an option to convert XRGrab Interacatbles to the networked version 
		/// </summary>
		[MenuItem("CONTEXT/XRGrabInteractable/Make Networked")]
		public static void MakeNetworkXRGrabInteractable(MenuCommand command) {
			var interactable = (XRGrabInteractable)command.context;
			var go = interactable.gameObject;

			var copy = new GameObject().AddComponent<XRGrabInteractable>().CloneFromWithIL(interactable);

			DestroyImmediate(interactable);
			go.AddComponent<NetworkXRGrabInteractable>().CloneFromWithIL(copy);
			DestroyImmediate(copy.gameObject);
		}
		
		/// <summary>
		/// Function that adds an option to add the whole needed networking stack to a networked interactable
		/// </summary>
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