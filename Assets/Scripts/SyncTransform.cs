using UnityEngine;

// Component that copies the transform from the object it is attached to, to another target transform.
public class SyncTransform : MonoBehaviour {
	[Tooltip("Transform that should be kept in sync with our transform")]
	public Transform target;

	// Update is called once per frame
	private void Update() {
		target.position = transform.position;
		target.rotation = transform.rotation;
		target.transform.localScale = ComputeScale(target.transform, transform.lossyScale);
	}
	
	// Function that converts the global scale of one transform into the corresponding local scale on another transform.
	Vector3 ComputeScale (Transform t, Vector3 globalScale) {
		t.localScale = Vector3.one;
		t.localScale = new Vector3 (globalScale.x/t.lossyScale.x, globalScale.y/t.lossyScale.y, globalScale.z/t.lossyScale.z);
		return t.localScale;
	}
}