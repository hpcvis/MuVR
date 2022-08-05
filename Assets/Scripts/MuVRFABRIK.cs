using DitzelGames.FastIK;
using MuVR;
using UnityEngine;

public class MuVRFABRIK : FastIKFabric {
	[Header("Joint")]
	public UserAvatar avatar;
	public string targetJoint;
	public string poleJoint;

	private void Start() {
		if(targetJoint != string.Empty)
			Target = avatar?.FindOrCreatePoseProxy(targetJoint) ?? Target;
		if (poleJoint != string.Empty)
			Pole = avatar?.FindOrCreatePoseProxy(poleJoint) ?? Pole;
	}
}