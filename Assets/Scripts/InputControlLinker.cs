using UnityEngine;

// Class that links transforms found in the avatar to child SyncTransforms
public class InputControlLinker : MonoBehaviour {
	// List of SyncPoses that need to have their avatar set
	public SyncPose[] syncs;

	private void Start() {
		// Get a reference to the avatar (should be attached to the parent object)
		var avatar = transform.parent.GetComponent<PlayerAvatar>();

		// For each of the managed SyncPoses, update its avatar and rebind its links
		foreach (var sync in syncs) {
			sync.targetAvatar = avatar;
			sync.UpdateTarget();
		}
	}
}