using System.Linq;
using uMuVR.Utility;
using UnityEditor;

namespace uMuVR {
	
	// Component that copies the transform from the object it is attached to, to a pose slot on a UserAvatar
	public class SyncPFNNJointPose : SyncPose {
		public PFNN.Controller character;
		public PFNN.Controller.JointType joint;

		private PFNN.Controller.JointsComponents targetJoint;

		// When the object is created make sure to update the target
		public new void Start() => UpdateTarget();

		// Function that finds the target from the target avatar and slot
		public new void UpdateTarget() {
			base.UpdateTarget();
			targetJoint = character.GetJoint(joint);
		}

		// Update is called once per frame, and make sure that our transform is properly synced with the pose according to the pose mode
		public new void LateUpdate() {
			if (mode == ISyncable.SyncMode.Store) {
				UpdatePosition(ref setTarget.pose.position, targetJoint.jointPoint.transform.position);
				UpdateRotation(ref setTarget.pose.rotation, targetJoint.jointPoint.transform.rotation);
			} else {
				targetJoint.position = targetJoint.jointPoint.transform.position = UpdatePosition(transform.position, getTarget.pose.position);
				targetJoint.rotation = targetJoint.jointPoint.transform.rotation = UpdateRotation(transform.rotation, getTarget.pose.rotation);
			}
		}
	}

#if UNITY_EDITOR
	// Editor that makes hooking up a sync pose to slots much easier
	[CustomEditor(typeof(SyncPFNNJointPose))]
	[CanEditMultipleObjects]
	public class SyncPFNNJointPoseEditor : SyncPoseEditor {
		// Properties of the object we wish to show a UI for
		protected SerializedProperty character, joint;

		public new void OnEnable() {
			base.OnEnable();
			character = serializedObject.FindProperty("character");
			joint = serializedObject.FindProperty("joint");
		}

		// Immediate mode GUI used to edit a SyncPose in the inspector
		public override void OnInspectorGUI() {
			var sync = (SyncPFNNJointPose)target;

			serializedObject.Update();

			TargetAvatarField(sync);
			PoseSlotField(sync);
			ModeField(sync);
			
			// Display options for selecting the PFNN character joint to sync with
			EditorGUILayout.PropertyField(character);
			EditorGUILayout.PropertyField(joint);
			
			// Toggle hiding additional settings
			sync.showSettings = EditorGUILayout.Foldout(sync.showSettings, "Additional Settings");
			if (sync.showSettings) {
				PositionSettingsField(sync);
				RotationSettingsField(sync);
				
				// Present a field with the pose offset
				EditorGUILayout.PropertyField(localOffset);
				EditorGUILayout.PropertyField(globalOffset);
			}
			
			PoseDebugField(sync);

			// Apply changes to the fields
			var oldAvatar = sync.targetAvatar;
			serializedObject.ApplyModifiedProperties();
			// If the target avatar has changed, automatically select its first slot
			if (sync.targetAvatar != oldAvatar && sync.targetAvatar is not null)
				sync.slot = sync.targetAvatar.slots.Keys.First();
		}
	}
#endif
}