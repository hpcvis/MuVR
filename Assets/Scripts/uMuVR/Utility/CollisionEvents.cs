using UnityEngine;
using UnityEngine.Events;

namespace uMuVR.Utility {
	
	/// <summary>
	/// Component that promotes the collision callbacks to events that other objects can subscribe to
	/// </summary>
	public class CollisionEvents : MonoBehaviour {
		/// <summary>
		/// Subscribable onCollisionEnter
		/// </summary>
		public UnityEvent<Collision> onCollisionEnter;
		/// <summary>
		/// Subscribable onCollisionExit
		/// </summary>
		public UnityEvent<Collision> onCollisionExit;
		/// <summary>
		/// Subscribable onCollisionStay
		/// </summary>
		public UnityEvent<Collision> onCollisionStay;
		/// <summary>
		/// Subscribable onTriggerEnter
		/// </summary>
		public UnityEvent<Collider> onTriggerEnter;
		/// <summary>
		/// Subscribable onTriggerExit
		/// </summary>
		public UnityEvent<Collider> onTriggerExit;
		/// <summary>
		/// Subscribable onTriggerStay
		/// </summary>
		public UnityEvent<Collider> onTriggerStay;

		private void OnCollisionEnter(Collision collision) { onCollisionEnter?.Invoke(collision); }
		private void OnCollisionExit(Collision collision) { onCollisionExit?.Invoke(collision); }
		private void OnCollisionStay(Collision collision) { onCollisionStay?.Invoke(collision); }
		private void OnTriggerEnter(Collider other) { onTriggerEnter?.Invoke(other); }
		private void OnTriggerExit(Collider other) { onTriggerExit?.Invoke(other); }
		private void OnTriggerStay(Collider other) { onTriggerStay?.Invoke(other); }
	}
}