using MuVR;
using MuVR.Utility;
using UnityEngine;

public class SyncJointToTargetConstraint : JointToTargetConstraint, ISyncable {
	[Header("User Avatar Settings")] public UserAvatar targetAvatar;
	public string targetJoint = string.Empty;

	protected new void Awake() {
		if (!string.IsNullOrEmpty(targetJoint))
			target = targetAvatar.FindOrCreatePoseProxy(targetJoint);

		base.Awake();
	}
}