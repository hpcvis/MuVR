using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using TriInspector;

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

	[PropertyTooltip("UserAvatar we are syncing with.\nNOTE: Drag the prefab with the UserAvatar here when modifying the input prefab.")]
	[Required]
	public UserAvatar targetAvatar;
	[PropertyTooltip("Which pose on the avatar we are syncing with")]
	public string slot;
	[PropertyTooltip("Should we send our transform to the pose, or update our transform to match the pose?")]
	public SyncMode mode;
	
	[PropertyTooltip("Offset applied while syncing")]
	public Pose offset = Pose.identity;
	[PropertyTooltip("Whether or not we should sync positions or rotations")]
	public bool syncPositions = true, syncRotations = true;
	[PropertyTooltip("The axes that should be synchronized")]
	public SyncedAxis positionAxis = SyncedAxis.Everything, rotationAxis = SyncedAxis.Everything; 

	[SerializeField, ReadOnly] private UserAvatar.PoseRef target;

	// When the object is created make sure to update the target
	private void Start() => UpdateTarget();

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
	private void LateUpdate() {
		if (mode == SyncMode.SyncTo) {
			updatePosition(ref target.pose.position, transform.position);
			updateRotaion(ref target.pose.rotation, transform.rotation);
		} else {
			transform.position = updatePosition(transform.position, target.pose.position);
			transform.rotation = updateRotaion(transform.rotation, target.pose.rotation);
		}
	}

	// Updates the position value, taking ignored axes into account (designed to be generalizable so can work for either case)
	private Vector3 updatePosition(Vector3 dest, Vector3 src) {
		if (!syncPositions) return dest;
		if (positionAxis == SyncedAxis.Everything)
			return src + offset.position;

		if ((positionAxis & SyncedAxis.X) > 0) dest.x = src.x + offset.position.x;
		if ((positionAxis & SyncedAxis.Y) > 0) dest.y = src.y + offset.position.y;
		if ((positionAxis & SyncedAxis.Z) > 0) dest.z = src.z + offset.position.z;
		return dest;
	}
	private void updatePosition(ref Vector3 dest, Vector3 src) => dest = updatePosition(dest, src);
	
	// Updates the rotation value, taking ignored axes into account (designed to be generalizable so can work for either case)
	private Quaternion updateRotaion(Quaternion dest, Quaternion src) {
		if (!syncRotations) return dest;
		if (rotationAxis == SyncedAxis.Everything)
			return src * offset.rotation;
		if (rotationAxis == SyncedAxis.None) return dest;

		var update = src * offset.rotation;
		if ((rotationAxis & SyncedAxis.X) == 0) update.eulerAngles = new Vector3(dest.eulerAngles.x, update.eulerAngles.y, update.eulerAngles.z);
		if ((rotationAxis & SyncedAxis.Y) == 0) update.eulerAngles = new Vector3(update.eulerAngles.x, dest.eulerAngles.y, update.eulerAngles.z);
		if ((rotationAxis & SyncedAxis.Z) == 0) update.eulerAngles = new Vector3(update.eulerAngles.x, update.eulerAngles.y, dest.eulerAngles.z);
		return update;
	}
	private void updateRotaion(ref Quaternion dest, Quaternion src) => dest = updateRotaion(dest, src);
}

#if UNITY_EDITOR
// Editor that makes hooking up a sync pose to slots much easier
[CustomEditor(typeof(SyncPose))]
[CanEditMultipleObjects]
public class SyncPoseEditor : Editor {
	// Bool indicating if the target pose debug dropdown should be expanded or not
	private bool showTarget = false;

	// Properties of the object we wish to show a default UI for
	private SerializedProperty targetAvatar, mode, syncPositions, syncRotations, offset;

	private void OnEnable() {
		targetAvatar = serializedObject.FindProperty("targetAvatar");
		mode = serializedObject.FindProperty("mode");
		syncPositions = serializedObject.FindProperty("syncPositions");
		syncRotations = serializedObject.FindProperty("syncRotations");
		offset = serializedObject.FindProperty("offset");
	}

	// Immediate mode GUI used to edit a SyncPose in the inspector
	public override void OnInspectorGUI() {
		var sync = (SyncPose)target;

		serializedObject.Update();

		// Field for the TargetAvatar
		EditorGUILayout.PropertyField(targetAvatar, new GUIContent("Target Avatar") {
			tooltip = "UserAvatar we are syncing with.\nNOTE: Drag the prefab with the UserAvatar here when modifying the input prefab."
		});

		// Present a dropdown menu listing the slots found on the target (no list and disabled if not found)
		EditorGUILayout.BeginHorizontal();
		{
			var cache = GUI.enabled;
			GUI.enabled = sync.targetAvatar is not null;

			EditorGUILayout.PrefixLabel(new GUIContent("Slot") {
				tooltip = "Which pose on the avatar we are syncing with.\nNOTE: If there is not currently a Target Avatar selected, this field will not be editable."
			});
			if (EditorGUILayout.DropdownButton(new GUIContent(ValidateSlot(sync.slot) ? sync.slot : "INVALID"),
				    FocusType.Keyboard) && sync.targetAvatar is not null) {
				var menu = new GenericMenu();

				// if (sync.targetAvatar is not null)
					foreach (var slot in sync.targetAvatar.slots.Keys)
						menu.AddItem(new GUIContent(slot), sync.slot == slot, OnSlotSelect, slot);

				menu.ShowAsContext();
			}

			GUI.enabled = cache;
		}
		EditorGUILayout.EndHorizontal();

		// Present a dropdown menu listing the possible modes (sync to/from)
		EditorGUILayout.PropertyField(mode, new GUIContent("Mode") {
			tooltip = "Should we send our transform to the pose, or update our transform to match the pose?"
		});

		// Toggles weather to enable or disable syncing of positions
		EditorGUILayout.PropertyField(syncPositions);
		if (syncPositions.boolValue) {
			var axis = (SyncPose.SyncedAxis) EditorGUILayout.EnumFlagsField(new GUIContent("Position Axes") {
				tooltip = "The axes that should be synchronized."
			}, sync.positionAxis);
			if (axis != sync.positionAxis) {
				Undo.RecordObject(target, "Update Position's Synced Axes");
				sync.positionAxis = axis;
			}
		}
		// Toggles weather to enable or disable syncing of rotations
		EditorGUILayout.PropertyField(syncRotations);
		if (syncRotations.boolValue) {
			var axis = (SyncPose.SyncedAxis) EditorGUILayout.EnumFlagsField(new GUIContent("Rotation Axes") {
				tooltip = "The axes that should be synchronized."
			}, sync.rotationAxis);
			if (axis != sync.rotationAxis) {
				Undo.RecordObject(target, "Update Rotation's Synced Axes");
				sync.rotationAxis = axis;
			}
		}

		// Present a field with the pose offset
		EditorGUILayout.PropertyField(offset);

		// Present a non-editable field with the debug pose (or an empty pose if the target is invalid)
		if(sync.targetAvatar is not null)
			PoseField("Pose Debug", sync.targetAvatar?.slots[sync.slot]?.pose ?? Pose.identity, ref showTarget, false);

		// Apply changes to the fields
		var oldAvatar = sync.targetAvatar;
		serializedObject.ApplyModifiedProperties();
		// If the target avatar has changed, automatically select its first slot
		if (sync.targetAvatar != oldAvatar && sync.targetAvatar is not null)
			sync.slot = sync.targetAvatar.slots.Keys.First();
	}


	// Function that validates weather or not a slot's name is invalid
	private bool ValidateSlot(string name) {
		var sync = (SyncPose)target;
		return sync?.targetAvatar?.slots?.Keys.Contains(name) ?? false;
	}

	// Function that displays a pose field with dropdown hiding position and rotation
	private Pose PoseField(string label, Pose pose, ref bool show, bool editable = true, params GUILayoutOption[] options) {
		var cache = GUI.enabled;
		if ((show = EditorGUILayout.BeginFoldoutHeaderGroup(show, label))) {
			GUI.enabled = editable && cache;
			var position = EditorGUILayout.Vector3Field("Position", pose.position, options);
			var rotation = Quaternion.Euler(EditorGUILayout.Vector3Field("Rotation", pose.rotation.eulerAngles, options));

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
	private void OnSlotSelect(object s) {
		if (s is not string slot)
			throw new ArgumentException(nameof(String));

		Undo.RecordObject(target, "Slot Select");
		var sync = (SyncPose)target;
		sync.slot = slot;
	}

	// Function called when a new mode is selected
	private void OnModeSelect(object m) {
		if (m is not SyncPose.SyncMode mode)
			throw new ArgumentException(nameof(SyncPose.SyncMode));

		Undo.RecordObject(target, "Mode Select");
		var sync = (SyncPose)target;
		sync.mode = mode;
	}
}
#endif