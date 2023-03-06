using System.Linq;
using TriInspector;
using UnityEditor;
using UnityEngine;

namespace uMuVR.Utility {
	/// <summary>
	/// Component that visualizes a pose in a pose slot
	/// </summary>
	public class PoseVisualizer : SyncPose {
		/// <summary>
		/// We only care about the get target and don't need to worry about setting values
		/// </summary>
		private UserAvatar.PoseRef target => getTarget;
		
		[PropertyTooltip("The prefab that should be spawned to visualize this pose")]
		public GameObject visualizationPrefab;
		/// <summary>
		/// Reference to the spawned prefab
		/// </summary>
		private GameObject spawnedPrefab;
		
		/// <summary>
		/// When the object is created make sure to update the target
		/// </summary>
		public new void Start() {
			UpdateTarget();
			RespawnVisualization();
		}
		
		/// <summary>
		/// Function that gets rid of the old visualization and spawns a new one in its place
		/// </summary>
		public void RespawnVisualization() {
			if (spawnedPrefab is not null) Destroy(spawnedPrefab);
			spawnedPrefab ??= Instantiate(visualizationPrefab, target?.pose.position ?? Vector3.zero, target?.pose.rotation ?? Quaternion.identity, transform);
			spawnedPrefab.name = slot;
		}
		
		/// <summary>
		/// At the end of each frame, make sure that the visualization is properly synced with the pose
		/// </summary>
		public new void LateUpdate() {
			spawnedPrefab.transform.position = UpdatePosition(spawnedPrefab.transform.position, target.pose.position);
			spawnedPrefab.transform.rotation = UpdateRotation(spawnedPrefab.transform.rotation, target.pose.rotation);
		}
	}

#if UNITY_EDITOR
	/// <summary>
	/// Custom editor that makes hooking up a sync pose to slots much easier
	/// </summary>
	[CustomEditor(typeof(PoseVisualizer))]
	[CanEditMultipleObjects]
	public class PoseVisualizerEditor : SyncPoseEditor {
		/// <summary>
		/// Properties of the object we wish to show a default UI for
		/// </summary>
		protected SerializedProperty visualizationPrefab;

		/// <summary>
		/// When the editor is enabled find the properties
		/// </summary>
		protected new void OnEnable() {
			base.OnEnable();
			visualizationPrefab = serializedObject.FindProperty("visualizationPrefab");
		}

		/// <summary>
		/// Display the editor GUI to the user
		/// </summary>
		public override void OnInspectorGUI() {
			var sync = (PoseVisualizer)target;

			serializedObject.Update();

			TargetAvatarField(sync);
			PoseSlotField(sync);

			// Allow selection of the prefab spawned to visualize the pose
			EditorGUILayout.PropertyField(visualizationPrefab);

			// Toggle hiding additional settings
			sync.showSettings = EditorGUILayout.Foldout(sync.showSettings, "Additional Settings");
			if (sync.showSettings) {
				PositionSettingsField(sync);
				RotationSettingsField(sync);
				
				// Present a field with the pose offset
				EditorGUILayout.PropertyField(localOffset);
				EditorGUILayout.PropertyField(globalOffset);
			}

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