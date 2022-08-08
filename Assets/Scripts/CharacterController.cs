using UnityEngine;

public class CharacterController : PFNN.Controller {

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

	protected void Start() {
		mainCamera.LookAt(transform);
		
		cameraAngleX = mainCamera.eulerAngles.x;
		cameraAngleY = 0.0f;
		cameraDistance = mainCamera.position.z;
		MoveCamera();
		
		GamepadMap.Enable();
	}

	protected override void Update() {
		if (GamepadMap.ButtonB) Crouch();

		if (GamepadMap.ButtonBack) ResetCharacter();

		LeftStickAxisAndRightTrigger();

		RightStickAxis();
		Bumpers();

		base.Update();
	}

    /// <summary>
    ///     Camera rotation.
    /// </summary>
    private void RightStickAxis() {
		if (GamepadMap.RightStickAxisX != 0 || GamepadMap.RightStickAxisY != 0) MoveCamera(GamepadMap.RightStickAxisX, GamepadMap.RightStickAxisY);
	}

    /// <summary>
    ///     Camera zoom.
    /// </summary>
    private void Bumpers() {
		if (GamepadMap.LeftBumper)
			UpdateCameraDistance(-1.0f);
		else if (GamepadMap.RightBumper) UpdateCameraDistance(1.0f);
	}

    /// <summary>
    ///     Player movement.
    /// </summary>
    private void LeftStickAxisAndRightTrigger() {
	    MoveCharacter(new Vector2(GamepadMap.LeftStickAxisX, GamepadMap.LeftStickAxisY), GamepadMap.RightTrigger, GamepadMap.LeftTrigger);
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

	    var dir = new Vector3(0, 0, cameraDistance);
	    var rotation = Quaternion.Euler(cameraAngleX, cameraAngleY, 0.0f);

	    mainCamera.position = transform.position + rotation * dir;
	    mainCamera.LookAt(transform.position + Vector3.up * 6, Vector3.up);

	    FixCameraAngles();
    }

    protected void FixCameraAngles() {
	    if (cameraAngleY is >= 360.0f or <= -360.0f) cameraAngleY = mainCamera.eulerAngles.y;
    }
}