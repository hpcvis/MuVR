using UnityEngine;
using UnityEngine.Serialization;

namespace uMuVR.Utility.Constraints {

	/// <summary>
	/// Constraint which uses physics impulses to (attempt to) force the joint to match the position of a target object
	/// </summary>
	[RequireComponent(typeof(Rigidbody))]
	public class JointToTargetConstraint : MonoBehaviour {
		/// <summary>
		/// Object to match the position of
		/// </summary>
		public Transform target;
		/// <summary>
		/// Spring force
		/// </summary>
		[FormerlySerializedAs("K")] public float springConstant = 1;
		/// <summary>
		/// Maximum force that can be applied
		/// </summary>
		public float maxForce = 2000;

		/// <summary>
		/// Reference to the rigidbody attached to this object
		/// </summary>
		protected Rigidbody rb;
		/// <summary>
		/// When the game starts get a reference to that rigidbody
		/// </summary>
		protected void Awake() => rb = GetComponent<Rigidbody>();

		/// <summary>
		/// Every physics tick, apply the constraint
		/// </summary>
		protected void FixedUpdate() {
			// We are applying a force from our current position towards the target
			var force = (target.position - transform.position) * springConstant;
			// Cap the force
			force = force.normalized * Mathf.Min(force.magnitude, maxForce);
			// Apply the force
			rb.AddForce(force);

			// If we are in the editor, visualize the force
#if UNITY_EDITOR
			Debug.DrawRay(transform.position, force, Color.red);
#endif
		}
	}
}
