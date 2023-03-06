using uMuVR.Enhanced;
using UnityEngine;
using UnityEngine.Serialization;

namespace uMuVR.Utility.Constraints {
	
	/// <summary>
	/// Constraint which uses physics impulses to (attempt to) force a rigidbody to match the rotation of a target object
	/// </summary>
	/// <remarks>This component forces the object's center of mass to be located at (0, 0, 0) in local space</remarks>
	[RequireComponent(typeof(Rigidbody))]
	public class JointRotationConstraint : MonoBehaviour {
		/// <summary>
		/// The object to match the rotation of
		/// </summary>
		public Transform target;
		/// <summary>
		/// Offset applied to the target
		/// </summary>
		public Quaternion offset = Quaternion.identity;
		/// <summary>
		/// Spring force applied
		/// </summary>
		[FormerlySerializedAs("K")] public float springConstant = 100;

		/// <summary>
		/// Reference to the rigidbody attached to this object
		/// </summary>
		protected Rigidbody rb;
		/// <summary>
		/// When the game starts create a connection to the rigidbody
		/// </summary>
		protected void Awake() => rb = GetComponent<Rigidbody>();

		/// <summary>
		/// Every physics update, apply the constraint
		/// </summary>
		protected void FixedUpdate() {
			// If there is no force then don't bother
			if (UnityEngine.Mathf.Approximately(springConstant, 0)) return;

			// Reset the center of mass so that torque is properly applied
			rb.centerOfMass = Vector3.zero;
			rb.inertiaTensor = Vector3.one;

			// Calculate how much we need to rotate to match the object
			var diff = offset * target.rotation.Diff(transform.rotation);
			// Apply a torque to make this change occur
			rb.maxAngularVelocity = springConstant;
			rb.AddTorque(diff.ToAngularVelocity() * springConstant, ForceMode.VelocityChange);

			// If we are in the editor visualize the torque
#if UNITY_EDITOR
			Debug.DrawRay(transform.position, diff.ToAngularVelocity() * springConstant, Color.blue);
#endif
		}
	}
}