using FishNet.Connection;
using uMuVR.Enhanced;
using TriInspector;
using UnityEngine;

namespace uMuVR {
	
	/// <summary>
	/// Component that enables or disables a GameObject or list of Components based on the current ownership status of this object
	/// </summary>
	public class DisableOnOwnership : NetworkBehaviour {
		[PropertyTooltip("Array of components to disable when ownership changes")]
		public Component[] toDisable;
		[PropertyTooltip("When true components will be disabled while the owner, when false the components will be disabled while not the owner")]
		public bool disableWhenOwner = true;
		[PropertyTooltip("When true the attached game object will be disabled instead of individual components")]
		public bool disableGameObject = false;
		
		/// <summary>
		/// When the ownership of the object changes, figure out which elements should be enabled
		/// </summary>
		/// <param name="prev">The previous object owner</param>
		public override void OnOwnershipBoth(NetworkConnection prev) {
			base.OnOwnershipBoth(prev);

			// Determine if we should enable or disabled based on the user's configuration
			bool shouldEnable = !(IsOwner ? disableWhenOwner : !disableWhenOwner);
			// If we are disabling the entire game object, enable or disable the entire game object
			if (disableGameObject) {
				gameObject.SetActive(shouldEnable);
				return;
			}

			// Otherwise... for each of the components we wish to disable, find its enabled property...
			foreach (var component in toDisable) {
				var properties = component.GetType().GetProperties();
				foreach (var property in properties)
					// If the enabled property exists on the component, enable or disable the component
					if (property.Name == "enabled") {
						property.SetValue(component, shouldEnable);
						break;
					}
			}

		}

	}
}
