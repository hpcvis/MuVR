using System;
using MuVR;
using UnityEngine;

public class SyncJointToTargetConstraint : JointToTargetConstraint {
	[Header("User Avatar Settings")]
	public UserAvatar targetAvatar;
	public string targetJoint = string.Empty;

	protected new void Awake() {
		if (!string.IsNullOrEmpty(targetJoint))
			target = targetAvatar.FindOrCreatePoseProxy(targetJoint);
		
		base.Awake();
	}
}