using UnityEngine;

// Component that copies the transform from the object it is attached to, to another target transform.
public class SyncTransform : MonoBehaviour {
	[Tooltip("Transform that should be kept in sync with our transform")]
	public Transform target;
	
	[Tooltip("Offset applied while syncing")]
	public Pose offset;

	// Update is called once per frame
	private void Update() {
		target.position = transform.position + offset.position;
		target.rotation = transform.rotation * offset.rotation;
		target.transform.SetGlobalScale(transform.lossyScale);
	}
}