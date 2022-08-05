using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MuVR {
	
	// Component that copies the transform from the object it is attached to, to a pose slot on a UserAvatar
	public class SyncPFNNJointPose : SyncPose {
		public CharacterTrajectoryAndAnimScript character;
		public CharacterTrajectoryAndAnimScript.JointType joint;

		private CharacterTrajectoryAndAnimScript.JointsComponents targetJoint;

		// When the object is created make sure to update the target
		public new void Start() => UpdateTarget();

		// Function that finds the target from the target avatar and slot
		public new void UpdateTarget() {
			base.UpdateTarget();
			targetJoint = character.GetJoint(joint);
		}

		// Update is called once per frame, and make sure that our transform is properly synced with the pose according to the pose mode
		public new void LateUpdate() {
			if (mode == SyncPose.SyncMode.SyncTo) {
				updatePosition(ref target.pose.position, targetJoint.jointPoint.transform.position);
				updateRotation(ref target.pose.rotation, targetJoint.jointPoint.transform.rotation);
			} else {
				targetJoint.position = targetJoint.jointPoint.transform.position = updatePosition(transform.position, target.pose.position);
				targetJoint.rotation = targetJoint.jointPoint.transform.rotation = updateRotation(transform.rotation, target.pose.rotation);
			}
		}
	}

#if UNITY_EDITOR
	// Editor that makes hooking up a sync pose to slots much easier
	[CustomEditor(typeof(SyncPFNNJointPose))]
	[CanEditMultipleObjects]
	public class SyncPFNNJointPoseEditor : SyncPoseEditor {
		// Properties of the object we wish to show a default UI for
		protected SerializedProperty character, joint;

		public void OnEnable() {
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