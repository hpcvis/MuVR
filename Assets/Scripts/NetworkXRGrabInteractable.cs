using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Class that extends an XR Grab Interactable to properly manage an attached NetworkRigidbody
public class NetworkXRGrabInteractable : XRGrabInteractable {
	private bool wasNetworkKinematic;
	private NetworkRigidbody networkRigidbody;

	protected override void Awake() {
		base.Awake();
		networkRigidbody = GetComponent<NetworkRigidbody>();
	}
    
	protected override void SetupRigidbodyGrab(Rigidbody rigidbody) {
		base.SetupRigidbodyGrab(rigidbody);
        
		if (networkRigidbody is null) return;

		wasNetworkKinematic = networkRigidbody.targetIsKinematic;
		networkRigidbody.targetIsKinematic = movementType == MovementType.Kinematic || movementType == MovementType.Instantaneous;
		networkRigidbody.UpdateOwnershipKinematicState();
	}
    
	protected override void SetupRigidbodyDrop(Rigidbody rigidbody) {
		base.SetupRigidbodyDrop(rigidbody);
        
		if (networkRigidbody is null) return;

		networkRigidbody.targetIsKinematic = wasNetworkKinematic;
		networkRigidbody.UpdateOwnershipKinematicState();
	}
}