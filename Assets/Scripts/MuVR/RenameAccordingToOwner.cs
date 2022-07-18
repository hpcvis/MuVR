using FishNet.Connection;
using MuVR.Enhanced;

namespace MuVR {
	
	// Component that when attached to a network object prefab will rename the object to reference the name of its owner when it is instantiated.
	public class RenameAccordingToOwner : NetworkBehaviour {
		private string baseName;

		private void Awake() => baseName = gameObject.name.Replace("(Clone)", "").Trim();

		public override void OnStartBoth() {
			base.OnStartBoth();

			RenameObject();
		}

		public override void OnOwnershipBoth(NetworkConnection _) => RenameObject();

		void RenameObject() {
			// Rename this object after its owner
			gameObject.name = $"{baseName} [{Owner.ClientId}]";
		}
	}
}
