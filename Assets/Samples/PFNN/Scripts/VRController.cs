using UnityEngine;

public class VRController : PFNN.Controller {

	public Transform HMD;

	protected override void Update() {
		// Make sure the body is always under the HMD (using strafing)
		MoveCharacterTo(HMD.transform.forward, HMD.transform.position, 0, 1, .1f);

		base.Update();
	}
}