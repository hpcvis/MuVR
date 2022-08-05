using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MuVR {
	
	// Component that copies the transform from the object it is attached to, to a pose slot on a UserAvatar
	public class SyncPose : MonoBehaviour {
		// Enum setting weather we should be sending our transform to the pose, or reading our transform from the pose
		public enum SyncMode {
			SyncTo,
			SyncFrom
		}

		// Enum flag indicating which axis should be synced
		[Flags]
		public enum SyncedAxis {
			None = 0,
			X = 1 << 0,
			Y = 1 << 1,
			Z = 1 << 2,
			Everything = ~0,
		}

		[Tooltip("UserAvatar we are syncing with.\nNOTE: Drag the prefab with the UserAvatar here when modifying the input prefab.")]
		public UserAvatar targetAvatar;

		[Tooltip("Which pose on the avatar we are syncing with")]
		public string slot;

		[Tooltip("Should we send our transform to the pose, or update our transform to match the pose?")]
		public SyncMode mode;

		[Tooltip("Offset applied while syncing")]
		public Pose localOffset = Pose.identity;
		public Vector3 globalOffset = Vector3.zero;

		[Tooltip("The weight of position and rotation synchronization, .5 will blend ")]
		public float positionWeight = 1, rotationWeight = 1;

		[Tooltip("The axes that should be synchronized")]
		[Range(0, 1)]
		public SyncedAxis positionAxis = SyncedAxis.Everything, rotationAxis = SyncedAxis.Everything;

		[SerializeField] protected UserAvatar.PoseRef target;

		// When the object is created make sure to update the target
		public void Start() => UpdateTarget();

		// Function that finds the target from the target avatar and slot
		public void UpdateTarget() {
			// If the target avatar is not set or set to a prefab, find the User Avatar on a parent
			if (targetAvatar?.gameObject.scene.name == null)
				targetAvatar = GetComponentInParent<UserAvatar>();
			if (targetAvatar is null)
				throw new Exception("No target avatar provided or found");
			if (!targetAvatar.slots.Keys.Contains(slot))
				throw new Exception("The requested slot can not be found");

			target = targetAvatar.slots[slot];
		}

		// Update is called once per frame, and make sure that our transform is properly synced with the pose according to the pose mode
		public void LateUpdate() {
			if (mode == SyncMode.SyncTo) {
				updatePosition(ref target.pose.position, transform.position);
				updateRotation(ref target.pose.rotation, transform.rotation);
			} else {
				transform.position = updatePosition(transform.position, target.pose.position);
				transform.rotation = updateRotation(transform.rotation, target.pose.rotation);
			}
		}

		// Updates the position value, taking ignored axes into account (designed to be generalizable so can work for either case)
		protected Vector3 updatePosition(Vector3 dest, Vector3 src) {
			if (!(positionWeight > 0)) return dest;
			if (positionAxis == SyncedAxis.None) return dest;
			var offset = globalOffset + transform.TransformDirection(localOffset.position);
			if (positionAxis == SyncedAxis.Everything)
				return Vector3.Lerp(dest, src + offset, positionWeight);

			var update = dest;
			if ((positionAxis & SyncedAxis.X) > 0) update.x = src.x + offset.x;
			if ((positionAxis & SyncedAxis.Y) > 0) update.y = src.y + offset.y;
			if ((positionAxis & SyncedAxis.Z) > 0) update.z = src.z + offset.z;
			return Vector3.Lerp(dest, update, positionWeight);
		}
		protected void updatePosition(ref Vector3 dest, Vector3 src) => dest = updatePosition(dest, src);

		// Updates the rotation value, taking ignored axes into account (designed to be generalizable so can work for either case)
		protected Quaternion updateRotation(Quaternion dest, Quaternion src) {
			if (!(rotationWeight > 0)) return dest;
			if (rotationAxis == SyncedAxis.None) return dest;
			if (rotationAxis == SyncedAxis.Everything)
				return Quaternion.Slerp(dest, src * localOffset.rotation, rotationWeight);

			var update = src * localOffset.rotation;
			if ((rotationAxis & SyncedAxis.X) == 0)
				update.eulerAngles = new Vector3(dest.eulerAngles.x, update.eulerAngles.y, update.eulerAngles.z);
			if ((rotationAxis & SyncedAxis.Y) == 0)
				update.eulerAngles = new Vector3(update.eulerAngles.x, dest.eulerAngles.y, update.eulerAngles.z);
			if ((rotationAxis & SyncedAxis.Z) == 0)
				update.eulerAngles = new Vector3(update.eulerAngles.x, update.eulerAngles.y, dest.eulerAngles.z);
			return Quaternion.Slerp(dest, update, rotationWeight);
		}
		protected void updateRotation(ref Quaternion dest, Quaternion src) => dest = updateRotation(dest, src);
		



		// Extra property that only exists in the editor which determines if the object's additional settings should be displayed or not
#if UNITY_EDITOR
		[HideInInspector] public bool showSettings;
#endif
	}

#if UNITY_EDITOR
	// Editor that makes hooking up a sync pose to slots much easier
	[CustomEditor(typeof(SyncPose))]
	[CanEditMultipleObjects]
	public class SyncPoseEditor : Editor {
		// Bool indicating if the target pose debug dropdown should be expanded or not
		[SerializeField] protected bool showTarget = false;

		// Properties of the object we wish to show a default UI for
		protected SerializedProperty targetAvatar, mode, positionWeight, rotationWeight, globalOffset, localOffset;

		protected void OnEnable() {
			targetAvatar = serializedObject.FindProperty("targetAvatar");
			mode = serializedObject.FindProperty("mode");
			positionWeight = serializedObject.FindProperty("positionWeight");
			rotationWeight = serializedObject.FindProperty("rotationWeight");
			globalOffset = serializedObject.FindProperty("globalOffset");
			localOffset = serializedObject.FindProperty("localOffset");
		}

		// Immediate mode GUI used to edit a SyncPose in the inspector
		public override void OnInspectorGUI() {
			var sync = (SyncPose)target;

			serializedObject.Update();

			TargetAvatarField(sync);
			PoseSlotField(sync);
			ModeField(sync);

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

		public void TargetAvatarField(SyncPose sync) {
			// Field for the TargetAvatar
			EditorGUILayout.PropertyField(targetAvatar, new GUIContent("Target Avatar") {
				tooltip = "UserAvatar we are syncing with.\nNOTE: Drag the prefab with the UserAvatar here when modifying the input prefab."
			});
		}

		public void PoseSlotField(SyncPose sync) {
			// Present a dropdown menu listing the slots found on the target (no list and disabled if not found)
			EditorGUILayout.BeginHorizontal();
			{
				var cache = GUI.enabled;
				GUI.enabled = sync.targetAvatar is not null;

				EditorGUILayout.PrefixLabel(new GUIContent("Slot") {
					tooltip = "Which pose on the avatar we are syncing with.\nNOTE: If there is not currently a Target Avatar selected, this field will not be editable."
				});
				if (EditorGUILayout.DropdownButton(new GUIContent(ValidateSlot(sync.slot) ? sync.slot : "INVALID"), 
					    FocusType.Keyboard) && sync.targetAvatar is not null)
				{
					var menu = new GenericMenu();

					// if (sync.targetAvatar is not null)
					foreach (var slot in sync.targetAvatar.slots.Keys)
						menu.AddItem(new GUIContent(slot), sync.slot == slot, OnSlotSelect, slot);

					menu.ShowAsContext();
				}

				GUI.enabled = cache;
			}
			EditorGUILayout.EndHorizontal();
		}

		public void ModeField(SyncPose sync) {
			// Present a dropdown menu listing the possible modes (sync to/from)
			EditorGUILayout.PropertyField(mode, new GUIContent("Mode") {
				tooltip = "Should we send our transform to the pose, or update our transform to match the pose?"
			});
		}

		public void PositionSettingsField(SyncPose sync) {
			var weight = EditorGUILayout.Slider("Position Weight", sync.positionWeight, 0, 1);
			if (Math.Abs(weight - sync.positionWeight) > Mathf.Epsilon) {
				Undo.RecordObject(target, "Update Position's Weight");
				sync.positionWeight = weight;
			}
			
			if (!(positionWeight.floatValue > 0)) return;
			var axis = (SyncPose.SyncedAxis)EditorGUILayout.EnumFlagsField(new GUIContent("Position Axes") {
				tooltip = "The axes that should be synchronized."
			}, sync.positionAxis);
			
			if (axis == sync.positionAxis) return;
			Undo.RecordObject(target, "Update Position's Synced Axes");
			sync.positionAxis = axis;
		}

		public void RotationSettingsField(SyncPose sync) {
			var weight = EditorGUILayout.Slider("Rotation Weight", sync.rotationWeight, 0, 1);
			if (Math.Abs(weight - sync.rotationWeight) > Mathf.Epsilon) {
				Undo.RecordObject(target, "Update Rotation's Weight");
				sync.rotationWeight = weight;
			}
			
			if (!(rotationWeight.floatValue > 0)) return;
			var axis = (SyncPose.SyncedAxis)EditorGUILayout.EnumFlagsField(new GUIContent("Rotation Axes") {
				tooltip = "The axes that should be synchronized."
			}, sync.rotationAxis);
			
			if (axis == sync.rotationAxis) return;
			Undo.RecordObject(target, "Update Rotation's Synced Axes");
			sync.rotationAxis = axis;
		}

		public void PoseDebugField(SyncPose sync) {
			// Present a non-editable field with the debug pose (or an empty pose if the target is invalid)
			if (sync.targetAvatar is not null && sync.targetAvatar.slots.ContainsKey(sync.slot))
				PoseField("Pose Debug", sync.targetAvatar.slots[sync.slot].pose, ref showTarget, false);
		}


		// Function that validates weather or not a slot's name is invalid
		protected bool ValidateSlot(string name) {
			var sync = (SyncPose)target;
			return sync?.targetAvatar?.slots?.Keys.Contains(name) ?? false;
		}

		// Function that displays a pose field with dropdown hiding position and rotation
		protected Pose PoseField(string label, Pose pose, ref bool show, bool editable = true,
			params GUILayoutOption[] options) {
			var cache = GUI.enabled;
			if (show = EditorGUILayout.BeginFoldoutHeaderGroup(show, label)) {
				GUI.enabled = editable && cache;
				var position = EditorGUILayout.Vector3Field("Position", pose.position, options);
				var rotation =
					Quaternion.Euler(EditorGUILayout.Vector3Field("Rotation", pose.rotation.eulerAngles, options));

				if (position != pose.position || rotation != pose.rotation) {
					Undo.RecordObject(target, $"Update Pose {label}");
					pose.position = position;
					pose.rotation = rotation;
				}

				GUI.enabled = cache;
			}

			EditorGUILayout.EndFoldoutHeaderGroup();
			return pose;
		}

		// Function called when a new slot is selected
		protected void OnSlotSelect(object s) {
			if (s is not string slot)
				throw new ArgumentException(nameof(String));

			Undo.RecordObject(target, "Slot Select");
			var sync = (SyncPose)target;
			sync.slot = slot;
		}
	}
#endif
}