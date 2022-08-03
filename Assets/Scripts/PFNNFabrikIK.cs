using DitzelGames.FastIK;
using UnityEngine;

public class PFNNFabrikIK : FastIKFabric {
	[Header("Joint")]
	public CharacterTrajectoryAndAnimScript character;
	public CharacterTrajectoryAndAnimScript.JointType targetJoint;
	public CharacterTrajectoryAndAnimScript.JointType poleJoint;

	private void Start() {
		if(targetJoint != CharacterTrajectoryAndAnimScript.JointType.None)
			Target = character?.GetJoint(targetJoint).jointPoint.transform ?? Target;
		if (poleJoint != CharacterTrajectoryAndAnimScript.JointType.None)
			Pole = character?.GetJoint(poleJoint).jointPoint.transform ?? Pole;
	}
}