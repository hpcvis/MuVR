using UnityEngine;

// Component that copies the transform from the object it is attached to, to another target transform.
// TODO: Can we create an implementation that modifies the values in proxy poses instead of actual transforms?
public class SyncTransform : MonoBehaviour {
	[Tooltip("Transform that should be kept in sync with our transform")]
	public Transform target;

	// Update is called once per frame
	private void Update() {
		target.position = transform.position;
		target.rotation = transform.rotation;
		target.transform.SetGlobalScale(transform.lossyScale);
	}
}