using UnityEngine;

[RequireComponent(typeof(CharacterTrajectoryAndAnimScript))]
public class CharacterMain : MonoBehaviour {
	protected CharacterTrajectoryAndAnimScript character;
	protected PFNN_CPU network;
	protected Transform mainCamera;

	[Range(0.0f, 150.0f)] 
	public float cameraRotationSensitivity = 90.0f;

	[Range(0.0f, 50.0f)] 
	public float cameraZoomSensitivity = 60.0f;

	private float cameraDistance;
	private const float cameraDistanceMax = -40.0f;
	private const float cameraDistanceMin = -2.0f;

	private float cameraAngleX, cameraAngleY;
	private const float cameraAngleMaxX = 80.0f;
	private const float cameraAngleMinX = 5.0f;

	public Vector3 initialWorldPosition;

	// Use this for initialization
	private void Start() {
		initialWorldPosition = new Vector3(
			transform.position.x,
			transform.position.y,
			transform.position.z);

		network = new PFNN_CPU();

		character = GetComponent<CharacterTrajectoryAndAnimScript>();

		mainCamera = gameObject.transform.GetChild(0);
		mainCamera.LookAt(transform);

		cameraAngleX = mainCamera.eulerAngles.x;
		cameraAngleY = 0.0f;
		cameraDistance = mainCamera.position.z;
		MoveCamera();

		ResetCharacter();
		GamepadMap.Enable();

		// Application.targetFrameRate = 30;
	}

	// Update is called once per frame
	private const float resetTime = 1.0f / 60;
	private float time = resetTime;
	protected virtual void Update() {
		// Only invoke the neural network 60 times per second (if the framerate is low enough, we may invoke the network twice)
		while (time <= 0) {
			character.UpdateNetworkInput(ref network.X);
			network.Compute(character.phase);
			character.BuildLocalTransforms(network.Y);

			// display stuff
			character.DisplayTrajectory();
			character.DisplayJoints();

			character.PostVisualisationCalculation(network.Y);
			character.UpdatePhase(network.Y);
			
			// Reset the timer (While standing don't calculate several frames)
			if (character.IsStanding()) time = resetTime;
			else time += resetTime;
		}

		time -= Time.deltaTime;
	}

	protected void MoveCharacter(float axisX = 0.0f, float axisY = 0.0f, float rightTrigger = 0.0f, float leftTrigger = 0.0f) {
		var newTargetDirection = Vector3.Normalize(
			new Vector3(mainCamera.forward.x, 0.0f, mainCamera.forward.z));

		//Debug.Log(newTargetDirection);
		Debug.DrawRay(mainCamera.transform.position, newTargetDirection, Color.cyan);

		character.UpdateStrafe(leftTrigger);
		character.UpdateTargetDirectionAndVelocity(newTargetDirection, axisX, axisY, rightTrigger);
		character.UpdateGait(rightTrigger);
		character.PredictFutureTrajectory();

		//Character.Jumps();
		character.Walls();

		character.UpdateRotation();
		character.UpdateHeights();
	}

	protected void ResetCharacter() {
		network.Reset();
		character.Reset(initialWorldPosition, network.Y);
	}

	public void UpdateCameraDistance(float value) {
		cameraDistance += value * cameraZoomSensitivity * Time.deltaTime;
		cameraDistance = Mathf.Clamp(cameraDistance, cameraDistanceMax, cameraDistanceMin);

		MoveCamera();
	}

	protected void MoveCamera(float speedY = 0.0f, float speedX = 0.0f) {
		cameraAngleX += speedX * cameraRotationSensitivity * Time.deltaTime;
		cameraAngleY += speedY * cameraRotationSensitivity * Time.deltaTime;
		cameraAngleX = Mathf.Clamp(cameraAngleX, cameraAngleMinX, cameraAngleMaxX);
		//Debug.Log("X: " + CameraAngleX + " Y: " + CameraAngleY);

		var dir = new Vector3(0, 0, cameraDistance);
		var rotation = Quaternion.Euler(cameraAngleX, cameraAngleY, 0.0f);

		mainCamera.position = transform.position + rotation * dir;
		mainCamera.LookAt(transform.position + Vector3.up * 6, Vector3.up);

		FixCameraAngles();
	}

	protected void FixCameraAngles() {
		if (cameraAngleY is >= 360.0f or <= -360.0f) cameraAngleY = mainCamera.eulerAngles.y;
	}

	protected void CharacterCrouch() {
		character.Crouch();
	}
}