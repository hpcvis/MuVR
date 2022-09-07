using UnityEngine;

// Class that increases the quality of the physics simulations
public class RagdollPrecisionIncreaser : MonoBehaviour {
	// Start is called before the first frame update
	private void Start() {
		foreach (var rb in GetComponentsInChildren<Rigidbody>()) {
			rb.solverIterations = 8;
			rb.solverVelocityIterations = 8;
			rb.maxAngularVelocity = 20;
		}
	}
}