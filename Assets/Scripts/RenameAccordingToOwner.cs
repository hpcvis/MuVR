
// Component that when attached to a network object prefab will rename the object to reference the name of its owner when it is instantiated.

using System;
using FishNet.Connection;

public class RenameAccordingToOwner : EnchancedNetworkBehaviour
{
	private string baseName;

	private void Awake() => baseName = gameObject.name.Replace("(Clone)", "");

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
