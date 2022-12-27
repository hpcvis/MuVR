using UnityEngine;

namespace uMuVR.Utility.Constraints {

	/// <summary>
	/// Synced version of <see cref="JointRotationConstraint"/>
	/// </summary>
	public class SyncJointRotationConstraint : JointRotationConstraint, ISyncable {
		/// <summary>
		/// User avatar to extract the target from
		/// </summary>
		[Header("User Avatar Settings")] public UserAvatar targetAvatar;
		/// <summary>
		/// Pose slot to extract the target from
		/// </summary>
		public string targetJoint = string.Empty;

		/// <summary>
		/// When the game starts find the target in the user avatar
		/// </summary>
		protected new void Awake() {
			if (!string.IsNullOrEmpty(targetJoint))
				target = targetAvatar.FindOrCreatePoseProxy(targetJoint);

			base.Awake();
		}
	}
}