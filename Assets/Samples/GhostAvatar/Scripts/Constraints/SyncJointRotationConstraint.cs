using MuVR;
using UnityEngine;

public class SyncJointRotationConstraint : JointRotationConstraint {
	[Header("User Avatar Settings")]
	public UserAvatar targetAvatar;
	public string targetJoint = string.Empty;

	protected new void Awake() {
		if (!string.IsNullOrEmpty(targetJoint))
			target = targetAvatar.FindOrCreatePoseProxy(targetJoint);
		
		base.Awake();
	}
}