using UnityEngine;
using UnityEngine.Events;

namespace MuVR {
	
	// Component that promotes the collision callbacks to events that other objects can subscribe to
	public class CollisionEvents : MonoBehaviour {
		public UnityEvent<Collision> onCollisionEnter;
		public UnityEvent<Collision> onCollisionExit;
		public UnityEvent<Collision> onCollisionStay;
		public UnityEvent<Collider> onTriggerEnter;
		public UnityEvent<Collider> onTriggerExit;
		public UnityEvent<Collider> onTriggerStay;

		private void OnCollisionEnter(Collision collision) { onCollisionEnter?.Invoke(collision); }
		private void OnCollisionExit(Collision collision) { onCollisionExit?.Invoke(collision); }
		private void OnCollisionStay(Collision collision) { onCollisionStay?.Invoke(collision); }
		private void OnTriggerEnter(Collider other) { onTriggerEnter?.Invoke(other); }
		private void OnTriggerExit(Collider other) { onTriggerExit?.Invoke(other); }
		private void OnTriggerStay(Collider other) { onTriggerStay?.Invoke(other); }
	}
}