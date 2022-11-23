using MuVR;
using MuVR.Utility;
using UnityEngine;

public class SyncJointRotationConstraint : JointRotationConstraint, ISyncable {
	[Header("User Avatar Settings")]
	public UserAvatar targetAvatar;
	public string targetJoint = string.Empty;

	protected new void Awake() {
		if (!string.IsNullOrEmpty(targetJoint))
			target = targetAvatar.FindOrCreatePoseProxy(targetJoint);
		
		base.Awake();
	}
}