using System;
using System.Linq;
using MuVR;
using UltimateXR.Avatar;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(UxrAvatar))]
public class SyncUxrAvatar : SyncPose {
	[Flags]
	public enum JointSyncs {
		None = 0,
		Head = 1 << 0,
		LeftShoulder = 1 << 1,
		RightShoulder = 1 << 2,
		LeftElbow = 1 << 3,
		RightElbow = 1 << 4,
		LeftWrist = 1 << 5,
		RightWrist = 1 << 6,
		Pelvis = 1 << 7,
		AllExceptPelvis = ~Pelvis,
		All = ~0
	}
	
	public JointSyncs toSync;

	private UxrAvatar source;
	private void Awake() => source = GetComponent<UxrAvatar>();

	protected ref Pose GetPose(string slot) => ref targetAvatar.SetterPoseRef(slot).pose;

	private new void LateUpdate() {
		// ReSharper disable Unity.NoNullPropagation
		if (toSync.HasFlag(JointSyncs.Head)) {
			UpdatePosition(ref GetPose("Head").position, source?.AvatarRig?.Head?.Head?.position ?? Vector3.zero);
			UpdateRotation(ref GetPose("Head").rotation, source?.AvatarRig?.Head?.Head?.rotation ?? Quaternion.identity);
		}
		
		if (toSync.HasFlag(JointSyncs.LeftShoulder)) { 
			UpdatePosition(ref GetPose("Left Shoulder").position, source?.AvatarRig?.LeftArm?.UpperArm?.position ?? Vector3.zero);
			UpdateRotation(ref GetPose("Left Shoulder").rotation, source?.AvatarRig?.LeftArm?.UpperArm?.rotation ?? Quaternion.identity);
		}
		if (toSync.HasFlag(JointSyncs.RightShoulder)) { 
			UpdatePosition(ref GetPose("Right Shoulder").position, source?.AvatarRig?.RightArm?.UpperArm?.position ?? Vector3.zero);
			UpdateRotation(ref GetPose("Right Shoulder").rotation, source?.AvatarRig?.RightArm?.UpperArm?.rotation ?? Quaternion.identity);
		}
		
		if (toSync.HasFlag(JointSyncs.LeftElbow)) { 
			UpdatePosition(ref GetPose("Left Elbow").position, source?.AvatarRig?.LeftArm?.Forearm?.position ?? Vector3.zero);
			UpdateRotation(ref GetPose("Left Elbow").rotation, source?.AvatarRig?.LeftArm?.Forearm?.rotation ?? Quaternion.identity);
		}
		if (toSync.HasFlag(JointSyncs.RightElbow)) { 
			UpdatePosition(ref GetPose("Right Elbow").position, source?.AvatarRig?.RightArm?.Forearm?.position ?? Vector3.zero);
			UpdateRotation(ref GetPose("Right Elbow").rotation, source?.AvatarRig?.RightArm?.Forearm?.rotation ?? Quaternion.identity);
		}
		
		if (toSync.HasFlag(JointSyncs.LeftWrist)) { 
			UpdatePosition(ref GetPose("Left Wrist").position, source?.AvatarRig?.LeftArm?.Hand?.Wrist?.position ?? Vector3.zero);
			UpdateRotation(ref GetPose("Left Wrist").rotation, source?.AvatarRig?.LeftArm?.Hand?.Wrist?.rotation ?? Quaternion.identity);
		}
		if (toSync.HasFlag(JointSyncs.RightWrist)) { 
			UpdatePosition(ref GetPose("Right Wrist").position, source?.AvatarRig?.RightArm?.Hand?.Wrist?.position ?? Vector3.zero);
			UpdateRotation(ref GetPose("Right Wrist").rotation, source?.AvatarRig?.RightArm?.Hand?.Wrist?.rotation ?? Quaternion.identity);
		}
		
		if (toSync.HasFlag(JointSyncs.Pelvis)) { 
			UpdatePosition(ref GetPose("Pelvis").position, source?.AvatarRig?.Hips?.position ?? Vector3.zero);
			UpdateRotation(ref GetPose("Pelvis").rotation, source?.AvatarRig?.Hips?.rotation ?? Quaternion.identity);
		}
		// ReSharper enable Unity.NoNullPropagation
	}
}

#if UNITY_EDITOR
	// Editor that makes hooking up a sync pose to slots much easier
	[CustomEditor(typeof(SyncUxrAvatar))]
	[CanEditMultipleObjects]
	public class SyncUxrAvatarEditor : SyncPoseEditor {
		// Properties of the object we wish to show a default UI for
		protected SerializedProperty toSync;

		protected new void OnEnable() {
			toSync = serializedObject.FindProperty("toSync");
			base.OnEnable();
		}

		// Immediate mode GUI used to edit a SyncPose in the inspector
		public override void OnInspectorGUI() {
			var sync = (SyncPose)target;

			serializedObject.Update();

			TargetAvatarField(sync);
			EditorGUILayout.PropertyField(toSync);

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


