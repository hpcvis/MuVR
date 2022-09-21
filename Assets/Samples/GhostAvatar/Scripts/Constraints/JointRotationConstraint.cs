using MuVR.Enhanced;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class JointRotationConstraint : MonoBehaviour {
	public Transform target;
	public Quaternion offset = Quaternion.identity;
	public float K = 100;
	
	protected Rigidbody rb;

	protected void Awake() {
		rb = GetComponent<Rigidbody>();
	}

	protected void FixedUpdate() {
		if (Mathf.Approximately(K, 0)) return;

		// Reset the center of mass so that torque is properly applied
		rb.centerOfMass = Vector3.zero;
		rb.inertiaTensor = Vector3.one;
		
		var diff = offset * target.rotation.Diff(transform.rotation);
		rb.maxAngularVelocity = K;
		rb.AddTorque(diff.ToAngularVelocity() * K, ForceMode.VelocityChange);
	
#if UNITY_EDITOR
		Debug.DrawRay(transform.position, diff.ToAngularVelocity() * K, Color.blue);
#endif
	}
}