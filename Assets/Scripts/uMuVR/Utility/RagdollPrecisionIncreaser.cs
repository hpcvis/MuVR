using UnityEngine;

namespace uMuVR.Utility {

	/// <summary>
	/// Class that increases the quality of the physics simulations on child rigidbodies
	/// </summary>
	public class RagdollPrecisionIncreaser : MonoBehaviour {
		/// <summary>
		/// When the game starts make sure all of the child rigidbodies have their performance increased!
		/// </summary>
		private void Start() {
			foreach (var rb in GetComponentsInChildren<Rigidbody>()) {
				rb.solverIterations = 8;
				rb.solverVelocityIterations = 8;
				rb.maxAngularVelocity = 20;
			}
		}
	}
}