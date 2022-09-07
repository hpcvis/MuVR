using MuVR.Enhanced;
using UnityEngine;

[RequireComponent(typeof(ConfigurableJoint))]
public class JointRotationConstraint : MonoBehaviour {
	public Transform target;
	
	protected Quaternion ourInitialRotation;
	protected ConfigurableJoint joint;

	protected void Awake() {
		joint = GetComponent<ConfigurableJoint>();
		ourInitialRotation = transform.localRotation;
	}

	protected void FixedUpdate() {
		var targetLocalSpaceRotation = Quaternion.Inverse(transform.parent.rotation) * target.rotation;
		joint.SetTargetRotationLocal(targetLocalSpaceRotation, ourInitialRotation);
	}
}