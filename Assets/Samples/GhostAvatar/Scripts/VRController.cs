using MuVR.Enhanced;
using UltimateXR.Avatar;
using UltimateXR.Core;
using UnityEngine;

public class VRController : PFNN.Controller {

	public Transform Hips, HMD, Foot;
	public float targetDistance = .13f;
	private float initialHMDHeight = 1.5f;

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
		MoveCharacterTo(Hips.transform.forward, Hips.transform.position, 0, 1, targetDistance);

		const float Cmax = .95f;
		const float Cmid = .85f;
		crouchedTarget = Mathf.Clamp01( 1 - ((HMD.transform.position.y - Foot.transform.position.y) / (Cmax * initialHMDHeight - Cmid * initialHMDHeight) - Cmid / (Cmax - Cmid)) );

		base.Update();
	}

	protected void TeleportLegs() {
		initialWorldPosition = Hips.transform.position.FixedHeight(0);
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