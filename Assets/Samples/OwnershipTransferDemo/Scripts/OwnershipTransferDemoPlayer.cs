using MuVR.Utility;
using UnityEngine;

public class OwnershipTransferDemoPlayer : MonoBehaviour {
	public float speed = 5;
	public float ballVelocity = 10;
	public float spawnDelay = .1f;
	public BallSpawner spawner;

	private bool canShoot = true;

	void Start() => spawner = GetComponentInParent<BallSpawner>();

	void Update() {
		var horizontal = Input.GetAxis("Horizontal");
		var vertical = Input.GetAxis("Vertical");

		var forward = new Vector3(horizontal, 0, vertical).normalized;

		if (forward.magnitude < 2 * Mathf.Epsilon) {
			forward = (-transform.position);
			forward.z = 0;
			forward.y = 0;
			forward = forward.normalized;
		} else 
			transform.position += forward * (Time.deltaTime * speed);

		if (Input.GetKey(KeyCode.Space) && canShoot) {
			spawner.SpawnBall(transform.position, forward, ballVelocity);
			
			canShoot = false;
			StartCoroutine(Timer.Start(() => canShoot = true, spawnDelay));
		}
	}
}
