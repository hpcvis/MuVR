
// Component that when attached to a network object prefab will rename the object to reference the name of its owner when it is instantiated.

using FishNet.Connection;

public class RenameAccordingToOwner : EnchancedNetworkBehaviour {
	public override void OnStartBoth() {
		base.OnStartBoth();
		
		RenameObject();
	}

	public override void OnOwnershipBoth(NetworkConnection _) {
		RenameObject();
	}

	void RenameObject() {
		// Rename this object after its owner
		gameObject.name = gameObject.name.Replace("(Clone)", " [" + Owner.ClientId + "]");
	}
}
