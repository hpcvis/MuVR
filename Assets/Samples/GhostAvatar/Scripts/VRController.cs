using MuVR.Enhanced;
using UltimateXR.Avatar;
using UltimateXR.Core;
using UnityEngine;

public class VRController : PFNN.Controller {

	public Transform HMD;

	private void OnEnable() {
		UxrManager.AvatarMoved += OnAvatarMoved;

		// Teleport the legs under the avatar
		TeleportLegs();
	}
	
	private void OnDisable() {
		UxrManager.AvatarMoved -= OnAvatarMoved;
	}

	protected override void Update() {
		// Make sure the body is always under the HMD (using strafing)
		MoveCharacterTo(HMD.transform.forward, HMD.transform.position, 0, 1, .1f);

		base.Update();
	}

	protected void TeleportLegs() {
		initialWorldPosition = HMD.transform.position.FixedHeight(0);
		initialWorldPosition.x += 3f; // Not entirely sure why this is necessary...
		ResetCharacter();
	}
	
	protected void OnAvatarMoved(object sender, UxrAvatarMoveEventArgs e) {
		// If the magnitude is large, that means we teleported and thus the legs should teleport as well
		if ((e.OldPosition - e.NewPosition).sqrMagnitude > 2 * 2) {
			TeleportLegs();
			// Debug.Log("Teleported");
			//
			// Debug.Log(e.NewPosition);
			// Debug.Log(GetJoint(JointType.Hips).jointPoint.transform.position);
		}
	}
	
	
}