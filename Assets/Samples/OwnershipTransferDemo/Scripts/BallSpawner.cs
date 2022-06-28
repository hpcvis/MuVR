using FishNet.Object;
using UnityEngine;

public class BallSpawner : NetworkBehaviour {
	public NetworkObject ballPrefab;
	
	public void SpawnBall(Vector3 position, Vector3 forward, float velocity) {
		if(IsServer) SpawnBallServer(position, forward, velocity);
		else SpawnBallServerRPC(position, forward, velocity);
	}

	[Server]
	void SpawnBallServer(Vector3 position, Vector3 forward, float velocity) {
		var spawned = Instantiate(ballPrefab.gameObject, position + forward, Quaternion.identity);
		Spawn(spawned, LocalConnection);

		spawned.GetComponent<NetworkRigidbody>().velocity = forward * velocity;
	}

	[ServerRpc]
	void SpawnBallServerRPC(Vector3 position, Vector3 forward, float velocity) =>
		SpawnBallServer(position, forward, velocity);
}