using FishNet.Connection;
using FishNet.Object;
using MuVR;
using UnityEngine;

public class BallSpawner : NetworkBehaviour {
	public NetworkObject ballPrefab;

	public void SpawnBall(Vector3 position, Vector3 forward, float velocity) {
		if (IsServer) SpawnBallServer(position, forward, velocity, LocalConnection);
		else SpawnBallServerRPC(position, forward, velocity, LocalConnection);
	}

	[Server]
	private void SpawnBallServer(Vector3 position, Vector3 forward, float velocity, NetworkConnection owner) {
		var spawned = Instantiate(ballPrefab, position + forward, Quaternion.identity);
		Spawn(spawned.gameObject, owner);

		SetBallSpeedTargetRPC(owner, spawned, forward, velocity);
	}

	[ServerRpc]
	private void SpawnBallServerRPC(Vector3 position, Vector3 forward, float velocity, NetworkConnection owner) =>
		SpawnBallServer(position, forward, velocity, owner);

	[TargetRpc]
	private void SetBallSpeedTargetRPC(NetworkConnection target, NetworkObject ball, Vector3 forward, float velocity) {
		ball.GetComponent<NetworkRigidbody>().velocity = forward * velocity;
	}
}