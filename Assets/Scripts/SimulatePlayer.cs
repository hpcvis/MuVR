using FishNet.Object;
using UnityEngine;

public class SimulatePlayer : NetworkBehaviour {
	private float nextMoveUpdate;
	private float nextRpc;
	private Vector3 posGoal;
	private Quaternion rotGoal;
	
	void Start() {
		// Turn off fullscreen and set the default resolution
		Screen.SetResolution(800, 600, false);
	}

	private void Update() {
		if (!IsOwner)
			return;

		transform.position = Vector3.MoveTowards(transform.position, posGoal, Time.deltaTime * 3f);
		transform.rotation = Quaternion.RotateTowards(transform.rotation, rotGoal, Time.deltaTime * 20f);

		if (Time.time > nextRpc) {
			nextRpc = Time.time + 0.5f;
			ServerRpc();
		}

		if (Time.time > nextMoveUpdate) {
			var rotate = Random.Range(0f, 1f) <= 0.5f;
			var x = Random.Range(0f, 1f) <= 0.5f;
			var y = Random.Range(0f, 1f) <= 0.5f;
			var z = Random.Range(0f, 1f) <= 0.5f;

			if (!x && !y && !z)
				x = true;

			transform.position = posGoal;
			transform.rotation = rotGoal;

			var xPos = x ? Random.Range(-30f, 30f) : transform.position.x;
			var yPos = y ? Random.Range(-30f, 30f) : transform.position.y;
			var zPos = z ? Random.Range(-30f, 30f) : transform.position.z;

			nextMoveUpdate = Time.time + 2f;
			posGoal = new Vector3(xPos, yPos, zPos);
			if (rotate)
				rotGoal = Quaternion.Euler(Random.Range(-80f, 80f),
					Random.Range(-80f, 80f),
					Random.Range(-80f, 80f)
				);
		}
	}

	[ServerRpc]
	private void ServerRpc() {
		ObserversRpc();
	}

	[ObserversRpc]
	private void ObserversRpc() { }
}