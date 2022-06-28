using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Component that transfers ownership of this object to another user
public class OwnershipManager : EnchancedNetworkBehaviour {
	[Tooltip("Enable changing ownership when a user interacts with this object.")]
	public bool enableInteractionTransfer = true;
	[Tooltip("Enable changing ownership when this object enters an ownership volume that belongs to a user.")]
	public bool enableVolumeTransfer = true;
	[Tooltip("Should the owner of this object return it to the scene before leaving the game?")]
	public bool releaseOwnershipOnLeave = true;

	// Counter tracking how many controllers are actively selecting us
	private uint selectionCount = 0;
	// Property indicating if we are actively selected
	private bool isSelected => selectionCount > 0;

	// XR Interactable that is interacted with to trigger interactions
	public XRBaseInteractable interactable = null;

	// When this object is spawned on the client, add it as a listener to the interaction's interactions
	public override void OnStartClient() {
		base.OnStartClient();	

		// Only register us as a listener if interaction transfers are enabled
		if (enableInteractionTransfer)
			if (interactable is not null) {
				interactable.selectEntered.AddListener(OnInteractableSelected);
				interactable.selectExited.AddListener(OnInteractableUnselected);
			}
	}

	// When this object is destroyed on the client, remove it it as an interaction listener
	public override void OnStopClient() {
		base.OnStopClient();

		// Only unregister us as a listener if interaction transfers are enabled
		if (interactable is not null) {
			interactable.selectEntered.RemoveListener(OnInteractableSelected);
			interactable.selectExited.RemoveListener(OnInteractableUnselected);
		}
	}

	// Un/Register the listener which returns control of the object to scene when its owner leaves
	public override void OnStartServer() {
		base.OnStartServer();
		ServerManager.Objects.OnPreDestroyClientObjects += OnPreDestroyClientObjects;
	}
	public override void OnStopServer() {
		base.OnStopServer();
		ServerManager.Objects.OnPreDestroyClientObjects -= OnPreDestroyClientObjects;
	}

	// When the owner of this object leaves, return control of it to the scene
	public void OnPreDestroyClientObjects(NetworkConnection leaving) {
		if (leaving != Owner) return; 
		
		if(releaseOwnershipOnLeave)
			GiveOwnership(null);
	}
	

	// When variables are changed in the editor, automatically add the attached GrabInteractable
	protected override void OnValidate() {
		base.OnValidate();

		if (enableInteractionTransfer && interactable is null)
			interactable = GetComponent<XRBaseInteractable>();
	}

	// When this object is interacted with (only called if interaction transfers are enabled), give it to the interaction's owner
	void OnInteractableSelected(SelectEnterEventArgs e) {
		// note: beware of NetworkObjects that may be in the way of the user representation that we are looking for
		// there was a NetworkObject on the XRRig at some point which broke this whole function
		var no = e.interactorObject.transform.GetComponentInParent<NetworkObject>();
		if (no is null) return;

		GiveOwnershipWithCooldown(no.Owner);
		selectionCount++; // Since we are now selected, volume transfers are temporarily disabled
	}
	
	// When interaction with this object ceases, decrement the number of selections
	void OnInteractableUnselected(SelectExitEventArgs e) {
		selectionCount--; // If this was the last interaction, volume transfers are now enabled again!
		
		// If we are no longer selected by anyone, transfer our ownership to whoever controls the OwnershipVolume we are within
		if(!isSelected)
			if(enableVolumeTransfer && lastVolume is not null) OnTriggerEnter(lastVolume.GetComponent<Collider>());
	}

	// When this object enters an Ownership Volume (only called if volume transfers are enabled), give it to the volume's owner
	private OwnershipVolume lastVolume; // Variable tracking the last OwnershipVolume we were within (used to set owner when interaction stops) NOTE: Overlapping ownership volumes will act strangely
	void OnTriggerEnter(Collider other) {
		if (!enableVolumeTransfer) return;

		var ov = other.GetComponent<OwnershipVolume>();
		if (ov is null) return;
		lastVolume = ov; // Save a reference to the volume for latter
		
		// If we are currently selected don't transfer ownership
		if (isSelected) return;
		
		if (ov.volumeOwner is not null)
			GiveOwnershipWithCooldown(ov.volumeOwner);
		// Be sure to listen for changes in ownership
		ov.RegisterAsListener(this);
	}

	// When this object leaves its Ownership Volume it stops listening to changes in ownership
	void OnTriggerExit(Collider other) {
		if (!enableVolumeTransfer) return;

		var ov = other.GetComponent<OwnershipVolume>();
		if (ov is null) return;
		if (ov == lastVolume) lastVolume = null; // We are no longer colliding with the previous volume
		
		// If we are currently selected don't transfer ownership
		if (isSelected) return;

		// Stop listening to changes in ownership
		ov.UnregisterAsListener(this);
	}
}
