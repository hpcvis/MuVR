using FishNet.Object;
using FishNet.Object.Synchronizing;
using MuVR;
using TMPro;
using UnityEngine;
using NetworkBehaviour = MuVR.Enchanced.NetworkBehaviour;

public class PingPongGameManager : NetworkBehaviour {
	[SyncVar(OnChange = nameof(UpdateScores))]
	public int plusScore = 0;
	[SyncVar(OnChange = nameof(UpdateScores))]
	public int minusScore = 0;
	
	[SerializeField] private NetworkObject ballPrefab;
	[SerializeField] private Transform plusSpawn, minusSpawn;
	[SerializeField] private OwnershipVolume plusVolume, minusVolume;
	[SerializeField] private TextMeshPro text;

	private bool spawnPlus = false;

	// Randomly chose a spawn location for the ball on the server
	public override void OnStartServer() {
		base.OnStartServer();

		spawnPlus = (Random.Range(0f, 1f) > .5f);
		RespawnBall();
	}

	// Update the scores on the client when they join
	public override void OnStartClient() {
		base.OnStartClient();
		UpdateScores(0, 0, false);
	}

	// Function that respawns the ball at the position specified by <spawnPlus>
	[Server]
	public void RespawnBall() {
		var ball = Instantiate(ballPrefab, spawnPlus ? plusSpawn.position : minusSpawn.position, Quaternion.identity);
		Spawn(ball.gameObject, spawnPlus ? plusVolume.volumeOwner : minusVolume.volumeOwner);
	}

	// Function called when an object goes out of bounds on the +x side
	public void OnObjectOutOfBoundsPlus(Collider ball) {
		if (ball.CompareTag("PingPongBall")) {
			if(IsServer) OnBallOutOfBounds(ball.GetComponent<NetworkObject>(), true);
		}
	}
	
	// Function called when an object goes out of bounds on the -x side
	public void OnObjectOutOfBoundsMinus(Collider ball) {
		if (ball.CompareTag("PingPongBall")) {
			if(IsServer) OnBallOutOfBounds(ball.GetComponent<NetworkObject>(), false);
		}
	}

	// Function called on the server when the ball goes out of bounds
	[Server]
	private void OnBallOutOfBounds(NetworkObject ball, bool plusOut) {
		// Increment score 
		if (plusOut) minusScore++;
		else plusScore++;

		// Respawn ball on the losing side
		spawnPlus = plusOut;
		
		// Destroy then respawn the ball
		Destroy(ball.gameObject);
		RespawnBall();
	}
	
	// Function called when one of the score variables is changed, updates the score text
	private void UpdateScores(int old, int @new, bool asServer) {
		// Bold the local player's score (the position of the camera will either be positive or negative)
		text.text = (Camera.current?.transform.position.x ?? 0) > 0 ?
			$"<b>Plus's Score: {plusScore}</b>\nMinus's Score: {minusScore}" : $"Plus's Score: {plusScore}\n<b>Minus's Score: {minusScore}</b>";
	}
}