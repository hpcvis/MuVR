using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Component that transfers ownership of this object to another player
public class OwnershipManager : EnchancedNetworkBehaviour {
	[Tooltip("Enable changing ownership when a player interacts with this object.")]
	public bool enableInteractionTransfer = true;
	[Tooltip("Enable changing ownership when this object enters an ownership volume that belongs to a player.")]
	public bool enableVolumeTransfer = true;

	// XR Interactable that is interacted with to trigger interactions
	public XRBaseInteractable interactable = null;

	// When this object is spawned on the client, add it as a listener to the interaction's interactions
	public override void OnStartClient() {
		base.OnStartClient();	

		// Only register us as a listener if interaction transfers are enabled
		if (enableInteractionTransfer)
			if(interactable is not null) interactable.selectEntered.AddListener(OnInteractableSelected);
	}

	// When this object is destroyed on the client, remove it it as an interaction listener
	public override void OnStopClient() {
		base.OnStopClient();

		// Only unregister us as a listener if interaction transfers are enabled
		if(interactable is not null) interactable.selectEntered.RemoveListener(OnInteractableSelected);
	}

	// When variables are changed in the editor, automatically add the attached GrabInteractable
	protected override void OnValidate() {
		base.OnValidate();

		if (enableInteractionTransfer && interactable is null)
			interactable = GetComponent<XRBaseInteractable>();
	}

	// When this object is interacted with (only called if interaction transfers are enabled), give it to the interaction's owner
	void OnInteractableSelected(SelectEnterEventArgs e) {
		// note: beware of NetworkObjects that may be in the way of the player representation that we are looking for
		// there was a NetworkObject on the XRRig at some point which broke this whole function
		var no = e.interactorObject.transform.GetComponentInParent<NetworkObject>();
		if (no is null) return;

		GiveOwnership(no.Owner);
		
		Debug.Log($"Selected; IsOwner: {IsOwner}");
	}

	// When this object enters an Ownership Volume (only called if volume transfers are enabled), give it to the volume's owner
	void OnTriggerEnter(Collider other) {
		if (!enableVolumeTransfer) return;
		
		var ov = other.GetComponent<OwnershipVolume>();
		if (ov is null) return;
		
		if(ov.volumeOwner is not null) GiveOwnership(ov.volumeOwner);
		
		Debug.Log("Entered Ownership Volume");
	}
}
