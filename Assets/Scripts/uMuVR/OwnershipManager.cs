using FishNet.Component.Transforming;
using FishNet.Connection;
using FishNet.Object;
using TriInspector;
using UltimateXR.Manipulation;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace uMuVR {
	
	/// <summary>
	/// Component that transfers ownership of this object to another user
	/// </summary>
	public class OwnershipManager : uMuVR.Enhanced.NetworkBehaviour {
		[PropertyTooltip("Enable changing ownership when a user interacts with this object.")]
		public bool enableInteractionTransfer = true;
		[PropertyTooltip("Enable changing ownership when this object enters an ownership volume that belongs to a user.")]
		public bool enableVolumeTransfer = true;
		[PropertyTooltip("Should the owner of this object return it to the scene before leaving the game?")]
		public bool releaseOwnershipOnLeave = true;


		[PropertyTooltip("XR Interactable that is interacted with to trigger interactions")]
		[ShowIf(nameof(enableInteractionTransfer)), PropertyOrder(1)]
		public XRBaseInteractable XRIinteractable = null;
		[ShowIf(nameof(enableInteractionTransfer)), PropertyOrder(2)]
		public UxrGrabbableObject UXRinteractable = null;
		[PropertyTooltip("Number of ticks to wait before an ownership transfer can occur again")]
		public uint ownershipTransferCooldown = 10;
		
		/// <summary>
		/// Counter tracking how many controllers are actively selecting us
		/// </summary>
		private uint selectionCount = 0;
		/// <summary>
		/// Property indicating if we are actively selected
		/// </summary>
		private bool isSelected => selectionCount > 0;
		
		/// <summary>
		/// When this object is spawned on the client, add it as a listener to the interaction's interactions
		/// </summary>
		public override void OnStartClient() {
			base.OnStartClient();

			// Only register us as a listener if interaction transfers are enabled
			if (enableInteractionTransfer) {
				if (XRIinteractable is not null) {
					XRIinteractable.selectEntered.AddListener(OnXRIInteractableSelected);
					XRIinteractable.selectExited.AddListener(OnXRIInteractableUnselected);
				}

				if (UXRinteractable is not null) {
					UXRinteractable.Grabbing += OnUxrInteractableSelected;
					UXRinteractable.Released += OnUxrInteractableUnselected;
					UXRinteractable.Placed += OnUxrInteractableUnselected;
				}
			}
			
				
		}
		
		/// <summary>
		/// When this object is destroyed on the client, remove it it as an interaction listener
		/// </summary>
		public override void OnStopClient() {
			base.OnStopClient();

			// Only unregister us as a listener if interaction transfers are enabled
			if (XRIinteractable is not null) {
				XRIinteractable.selectEntered.RemoveListener(OnXRIInteractableSelected);
				XRIinteractable.selectExited.RemoveListener(OnXRIInteractableUnselected);
			}
			
			if (UXRinteractable is not null) {
				UXRinteractable.Grabbing -= OnUxrInteractableSelected;
				UXRinteractable.Released -= OnUxrInteractableUnselected;
				UXRinteractable.Placed -= OnUxrInteractableUnselected;
			}
		}
		
		/// <summary>
		/// Un/Register the listener which returns control of the object to scene when its owner leaves
		/// </summary>
		public override void OnStartServer() {
			base.OnStartServer();
			ServerManager.Objects.OnPreDestroyClientObjects += OnPreDestroyClientObjects;
		}
		public override void OnStopServer() {
			base.OnStopServer();
			ServerManager.Objects.OnPreDestroyClientObjects -= OnPreDestroyClientObjects;
		}
		
		/// <summary>
		/// When the owner of this object leaves, return control of it to the scene
		/// </summary>
		public void OnPreDestroyClientObjects(NetworkConnection leaving) {
			if (leaving != Owner) return;

			if (releaseOwnershipOnLeave)
				GiveOwnership(null);
		}

		
		/// <summary>
		/// Automatically add the attached GrabInteractable
		/// </summary>
		protected override void OnValidate() {
			base.OnValidate();

			if (enableInteractionTransfer && XRIinteractable is null)
				XRIinteractable = GetComponent<XRBaseInteractable>();
			if (enableInteractionTransfer && UXRinteractable is null)
				UXRinteractable = GetComponent<UxrGrabbableObject>();
		}

		
		/// <summary>
		/// When this object is interacted with (only called if interaction transfers are enabled), give it to the interaction's owner
		/// </summary>
		/// <param name="e"></param>
		protected void OnXRIInteractableSelected(SelectEnterEventArgs e) {
			// NOTE: beware of NetworkObjects that may be in the way of the user representation that we are looking for
			// there was a NetworkObject on the XRRig at some point which broke this whole function
			var no = e.interactorObject.transform.GetComponentInParent<NetworkObject>();
			if (no is null) return;

			GiveOwnershipWithCooldown(no.Owner, ownershipTransferCooldown, true);
			selectionCount++; // Since we are now selected, volume transfers are temporarily disabled
		}

		protected void OnUxrInteractableSelected(object sender, UxrManipulationEventArgs args) {
			// note: beware of NetworkObjects that may be in the way of the user representation that we are looking for
			// there was a NetworkObject on the XRRig at some point which broke this whole function
			var no = args.Grabber.transform.GetComponentInParent<NetworkObject>();
			if (no is null) return;
			
			selectionCount++; // Since we are now selected, volume transfers are temporarily disabled
			GiveOwnershipWithCooldown(no.Owner, ownershipTransferCooldown, true);
		}

		/// <summary>
		/// When interaction with this object ceases, decrement the number of selections
		/// </summary>
		/// <param name="e"></param>
		protected void OnXRIInteractableUnselected(SelectExitEventArgs e) {
			selectionCount--; // If this was the last interaction, volume transfers are now enabled again!
		}

		protected void OnUxrInteractableUnselected(object sender, UxrManipulationEventArgs args) {
			selectionCount--; // If this was the last interaction, volume transfers are now enabled again!
		}

		/// <summary>
		/// When this object enters an Ownership Volume (only called if volume transfers are enabled), give it to the volume's owner
		/// </summary>
		/// <param name="other"></param>
		protected void OnTriggerStay(Collider other) {
			if (!enableVolumeTransfer) return;

			var ov = other.GetComponent<OwnershipVolume>();
			if (ov is null) return;
			if (ov.volumeOwner == Owner) return; // No need to transfer if the volume has the same owner

			// If we are currently selected don't transfer ownership
			if (isSelected) return;

			// Debug.Log($"{this} - {ov}");

			if (ov.volumeOwner is not null)
				GiveOwnershipWithCooldown(ov.volumeOwner, ownershipTransferCooldown, true);

			// Be sure to listen for changes in ownership
			ov.RegisterAsListener(this);
		}
		
		
		
#if UNITY_EDITOR
		// Function to add all of the necessary components for an object to be networked (visible in the UxrGrabbableObject and OwnershipManager) 
		[MenuItem("CONTEXT/UxrGrabbableObject/Make Networked")]
		[MenuItem("CONTEXT/OwnershipManager/Setup Object Networking")]
        public static void SetupObjectNetworking(MenuCommand command) {
        	var go = (Component)command.context;
    
        	var no = go.GetComponent<NetworkObject>();
        	var om = go.GetComponent<OwnershipManager>();
        	var nt = go.GetComponent<NetworkTransform>();
        	var rb = go.GetComponent<Rigidbody>();
        	var nrb = go.GetComponent<NetworkRigidbody>();
    
        	no ??= go.gameObject.AddComponent<NetworkObject>();
        	om ??= go.gameObject.AddComponent<OwnershipManager>();
        	nt ??= go.gameObject.AddComponent<NetworkTransform>();
        	if (rb is not null) nrb ??= go.gameObject.AddComponent<NetworkRigidbody>();
        }
#endif
	}
}
