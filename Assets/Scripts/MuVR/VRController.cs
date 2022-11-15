using CircularBuffer;
using MuVR.Enhanced;
using UltimateXR.Avatar;
using UltimateXR.Core;
using UnityEngine;

public class VRController : PFNN.Controller {
	public Transform Hips, HMD, Foot;
	private readonly float initialHMDHeight = 1.5f;

	// Buffer tracking positions of the headset over time
	private readonly CircularBuffer<Vector3> hipPositionBuffer = new(10);

	private void OnEnable() {
		UxrManager.AvatarMoved += OnAvatarMoved;

		// Teleport the legs under the avatar
		TeleportLegs();
	}
	private void OnDisable() {
		UxrManager.AvatarMoved -= OnAvatarMoved;
	}

	[SerializeField] private float targetDistance = 0;
	private float targetDistanceTarget;

	protected override void Update() {
		// Add the new hip position to the buffer
		hipPositionBuffer.PushBack(Hips.position);
		// Calculate the average speed of the headset
		var speed = 0f;
		for (var i = 1; i < hipPositionBuffer.Size; i++)
			speed += (hipPositionBuffer[i] - hipPositionBuffer[i - 1]).magnitude;
		speed /= hipPositionBuffer.Size;

		// While the feet and HMD are pointing in substantially different directions force the target distance to 0
		var footForward = ((GetJoint(JointType.LeftToeBase).jointPoint.transform.forward + GetJoint(JointType.RightToeBase).jointPoint.transform.forward) / 2).normalized.FixedHeight(0);
		if (Mathf.Abs(Vector3.Angle(footForward, HMD.forward.FixedHeight(0))) > 10) speed = 1;

		// Adjust the target distance based on the speed of the hips 
		targetDistanceTarget = Mathf.Max(-150 * speed + .3f, 0);
		// Debug.Log($"{footForward} - {Hips.forward.FixedHeight(0)} - {Mathf.Abs(Vector3.Angle(footForward, HMD.forward.FixedHeight(0)))} - {targetDistance}");

		// Allow the target distance to immediately drop to match its target, but force it to take 3 seconds to reach its maximum
		if (targetDistance < targetDistanceTarget) targetDistance += .1f * Time.deltaTime;
		if (targetDistance > targetDistanceTarget) targetDistance = targetDistanceTarget;

		// Make sure the body is always under the hips (using strafing)
		MoveCharacterTo(Hips.transform.forward, Hips.transform.position, 0, 1, targetDistance);

		const float Cmax = .95f;
		const float Cmid = .85f;
		crouchedTarget = Mathf.Clamp01(1 - ((HMD.transform.position.y - Foot.transform.position.y) / (Cmax * initialHMDHeight - Cmid * initialHMDHeight) - Cmid / (Cmax - Cmid)));

		base.Update();
	}

	protected void TeleportLegs() {
		initialWorldPosition = Hips.transform.position.FixedHeight(0);
		initialWorldPosition.x += 3f; // Not entirely sure why this is necessary...
		// Add a bunch of large data to the hip position buffer so that we ensure the legs get close to the user
		for(var i = 0; i < 10; i++)
			hipPositionBuffer.PushBack(Vector3.one * i);
		ResetCharacter();
	}

	protected void OnAvatarMoved(object sender, UxrAvatarMoveEventArgs e) {
		// If the magnitude is large, that means we teleported and thus the legs should teleport as well
		if ((e.OldPosition - e.NewPosition).sqrMagnitude > 2 * 2) TeleportLegs();
		// Debug.Log("Teleported");
		//
		// Debug.Log(e.NewPosition);
		// Debug.Log(GetJoint(JointType.Hips).jointPoint.transform.position);
	}
}