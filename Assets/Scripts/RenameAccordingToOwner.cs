
// Component that when attached to a network object prefab will rename the object to reference the name of its owner when it is instantiated.
public class RenameAccordingToOwner : EnchancedNetworkBehaviour {
	public override void OnStartBoth() {
		base.OnStartBoth();
		
		// Rename this object after its owner
		gameObject.name = gameObject.name.Replace("(Clone)", " [" + Owner.ClientId + "]");
	}
}
