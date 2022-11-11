using MuVR.Enhanced;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
public class JointRotationConstraint : MonoBehaviour {
	public Transform target;
	public Quaternion offset = Quaternion.identity;
	[FormerlySerializedAs("K")] public float springConstant = 100;
	
	protected Rigidbody rb;

	protected void Awake() {
		rb = GetComponent<Rigidbody>();
	}

	protected void FixedUpdate() {
		if (Mathf.Approximately(springConstant, 0)) return;

		// Reset the center of mass so that torque is properly applied
		rb.centerOfMass = Vector3.zero;
		rb.inertiaTensor = Vector3.one;
		
		var diff = offset * target.rotation.Diff(transform.rotation);
		rb.maxAngularVelocity = springConstant;
		rb.AddTorque(diff.ToAngularVelocity() * springConstant, ForceMode.VelocityChange);
	
#if UNITY_EDITOR
		Debug.DrawRay(transform.position, diff.ToAngularVelocity() * springConstant, Color.blue);
#endif
	}
}