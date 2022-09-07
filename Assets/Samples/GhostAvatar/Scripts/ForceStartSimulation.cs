using UnityEngine;

public class ForceStartSimulation : MonoBehaviour {
	private void Awake() => Physics.autoSimulation = true;
}