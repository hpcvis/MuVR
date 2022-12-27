using FishNet.Connection;
using uMuVR.Enhanced;

namespace uMuVR {
	/// <summary>
	/// Component that when attached to a network object prefab will rename the object to reference the name of its owner when it is instantiated.
	/// </summary>
	public class RenameAccordingToOwner : NetworkBehaviour {
		/// <summary>
		/// Original name of the object
		/// </summary>
		private string baseName;

		/// <summary>
		/// When the game starts update the base name
		/// </summary>
		private void Awake() => baseName = gameObject.name.Replace("(Clone)", "").Trim();

		/// <summary>
		/// When we connect to the network, update the object's name to contain its owner's client ID
		/// </summary>
		public override void OnStartBoth() {
			base.OnStartBoth();

			RenameObject();
		}

		/// <summary>
		/// When ownership of the object changes, update the object's name to contain its owner's client ID
		/// </summary>
		public override void OnOwnershipBoth(NetworkConnection _) => RenameObject();

		/// <summary>
		/// Function which renames the object to contain its owner's client ID
		/// </summary>
		void RenameObject() {
			// Rename this object after its owner
			gameObject.name = $"{baseName} [{Owner.ClientId}]";
		}
	}
}
