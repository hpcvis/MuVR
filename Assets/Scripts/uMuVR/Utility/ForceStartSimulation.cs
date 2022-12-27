using UnityEngine;

namespace uMuVR.Utility {

	/// <summary>
	/// Component which enables the physics simulation in non-networked environments
	/// </summary>
	public class ForceStartSimulation : MonoBehaviour {
		private void Awake() => Physics.autoSimulation = true;
	}
}