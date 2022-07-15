using UnityEngine;

// Component that causes this object to rotate so it is always facing the camera
public class LookAtCamera : MonoBehaviour {
	[Tooltip("Camera to look at (will default to Camera.current if not set)")]
	public Camera target = null;
	[Tooltip("Prevent rotation along one of the Euler axis")]
	public bool lockX = false, lockY = false, lockZ = false;

	// Update is called once per frame
	private void Update() {
		var position = Camera.main?.transform.position ?? transform.position;
		try {
			position = target.transform.position;
		} catch (UnassignedReferenceException) {}

		// Don't update rotation if it would be null
		if (position == transform.position) return;
		
		// Invert the position so that text remains upright
		var rot = Quaternion.LookRotation(transform.position - position, Vector3.up);
		
		// Make sure that any locked axis maintain their old values
		var oldRot = transform.rotation;
		if (lockX) rot.eulerAngles = new Vector3(oldRot.eulerAngles.x, rot.eulerAngles.y, rot.eulerAngles.z);
		if (lockY) rot.eulerAngles = new Vector3(rot.eulerAngles.x, oldRot.eulerAngles.y, rot.eulerAngles.z);
		if (lockZ) rot.eulerAngles = new Vector3(rot.eulerAngles.x, rot.eulerAngles.y, oldRot.eulerAngles.z);

		// Update the rotation
		transform.rotation = rot;
	}
}