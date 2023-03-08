using System;
using System.Linq;
using TriInspector;
using uMuVR.Utility;
using UnityEditor;
using UnityEngine;

namespace uMuVR {
	
	/// <summary>
	/// Component that copies the transform from the object it is attached to, to a pose slot on a UserAvatar
	/// </summary>
	public class SyncPose : MonoBehaviour, Utility.ISyncable {
		/// <summary>
		/// Enum flag indicating which axis should be synced
		/// </summary>
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
		public ISyncable.SyncMode mode;

		[PropertyTooltip("Offset applied while syncing")]
		public Pose localOffset = Pose.identity;
		public Vector3 globalPositionOffset = Vector3.zero;

		[PropertyTooltip("The weight of position and rotation synchronization, .5 will blend ")]
		public float positionWeight = 1, rotationWeight = 1;

		[PropertyTooltip("The axes that should be synchronized")]
		[Range(0, 1)]
		public SyncedAxis positionAxis = SyncedAxis.Everything, rotationAxis = SyncedAxis.Everything;

		/// <summary>
		/// References to the pose we should store values in and get values from
		/// </summary>
		[SerializeField, ReadOnly] protected UserAvatar.PoseRef setTarget, getTarget;
		
		/// <summary>
		/// When the object is created make sure to update the target
		/// </summary>
		public void Start() => UpdateTarget();
		
		/// <summary>
		/// Function that finds the target from the target avatar and slot
		/// </summary>
		/// <exception cref="ArgumentNullException">Thrown when the target avatar doesn't exist or the requested pose slot can't be found</exception>
		public void UpdateTarget() {
			// If the target avatar is not set or set to a prefab, find the User Avatar on a parent
			if (targetAvatar?.gameObject.scene.name == null)
				targetAvatar = GetComponentInParent<UserAvatar>();
			if (targetAvatar is null)
				throw new ArgumentNullException("No target avatar provided or found");
			if (!targetAvatar.slots.Keys.Contains(slot))
				throw new ArgumentNullException("The requested slot can not be found");

			setTarget = targetAvatar.SetterPoseRef(slot); 
			getTarget = targetAvatar.GetterPoseRef(slot);
		}
		
		/// <summary>
		/// At the end of the frame, make sure that our transform is properly synced with the pose according to the pose mode
		/// </summary>
		public void LateUpdate() {
			if (mode == ISyncable.SyncMode.Store) {
				UpdatePosition(ref setTarget.pose.position, transform.position);
				UpdateRotation(ref setTarget.pose.rotation, transform.rotation);
			} else {
				transform.position = UpdatePosition(transform.position, getTarget.pose.position);
				transform.rotation = UpdateRotation(transform.rotation, getTarget.pose.rotation);
			}
		}
		
		/// <summary>
		/// Updates the position value, taking ignored axes into account (designed to be generalizable so can work for either case)
		/// </summary>
		/// <param name="dest">The new position</param>
		/// <param name="src">The old position</param>
		/// <returns>The position that should be stored once ignored axis and weights are accounted for</returns>
		protected Vector3 UpdatePosition(Vector3 dest, Vector3 src) {
			if (!(positionWeight > 0)) return dest;
			if (positionAxis == SyncedAxis.None) return dest;
			var offset = globalPositionOffset + transform.TransformDirection(localOffset.position);
			if (positionAxis == SyncedAxis.Everything)
				return Vector3.Lerp(dest, src + offset, positionWeight);

			var update = dest;
			if ((positionAxis & SyncedAxis.X) > 0) update.x = src.x + offset.x;
			if ((positionAxis & SyncedAxis.Y) > 0) update.y = src.y + offset.y;
			if ((positionAxis & SyncedAxis.Z) > 0) update.z = src.z + offset.z;
			return Vector3.Lerp(dest, update, positionWeight);
		}
		protected void UpdatePosition(ref Vector3 dest, Vector3 src) => dest = UpdatePosition(dest, src);
		
		/// <summary>
		/// Updates the rotation value, taking ignored axes into account (designed to be generalizable so can work for either case)
		/// </summary>
		/// <param name="dest">The new rotation</param>
		/// <param name="src">The old rotation</param>
		/// <returns>The rotation which should be stored once ignored axis and weights are accounted for</returns>
		protected Quaternion UpdateRotation(Quaternion dest, Quaternion src) {
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
		protected void UpdateRotation(ref Quaternion dest, Quaternion src) => dest = UpdateRotation(dest, src);
		

#if UNITY_EDITOR
		/// <summary>
		/// Extra property that only exists in the editor which determines if the object's additional settings should be displayed or not
		/// </summary>
		[HideInInspector] public bool showSettings;
#endif
	}

#if UNITY_EDITOR
	/// <summary>
	/// Editor that makes hooking up a sync pose to slots much easier
	/// </summary>
	[CustomEditor(typeof(SyncPose))]
	[CanEditMultipleObjects]
	public class SyncPoseEditor : Editor {
		/// <summary>
		/// Bool indicating if the target pose debug dropdown should be expanded or not
		/// </summary>
		[SerializeField] protected bool showTarget = false;
		
		/// <summary>
		/// Properties of the object we wish to show a default UI for
		/// </summary>
		protected SerializedProperty targetAvatar, mode, positionWeight, rotationWeight, globalOffset, localOffset;

		/// <summary>
		/// When the editor is enabled find references to the object's properties
		/// </summary>
		protected void OnEnable() {
			targetAvatar = serializedObject.FindProperty("targetAvatar");
			mode = serializedObject.FindProperty("mode");
			positionWeight = serializedObject.FindProperty("positionWeight");
			rotationWeight = serializedObject.FindProperty("rotationWeight");
			globalOffset = serializedObject.FindProperty("globalPositionOffset");
			localOffset = serializedObject.FindProperty("localOffset");
		}
 
		/// <summary>
		/// Immediate mode GUI used to edit a SyncPose in the inspector
		/// </summary>
		public override void OnInspectorGUI() {
			var sync = (SyncPose)target;

			serializedObject.Update();

			TargetAvatarField(sync);
			PoseSlotField(sync);
			ModeField(sync);

			// Toggle hiding additional settings
			sync.showSettings = EditorGUILayout.Foldout(sync.showSettings, "Additional Settings", EditorStyles.foldoutHeader);
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
				if(!sync.targetAvatar.slots.ContainsKey(sync.slot))
					sync.slot = sync.targetAvatar.slots.Keys.First();
		}

		/// <summary>
		/// Function which displays the target avatar GUI for the provided SyncPose
		/// </summary>
		/// <param name="sync">The SyncPose to display a GUI for</param>
		public void TargetAvatarField(ISyncable sync) {
			// Field for the TargetAvatar
			EditorGUILayout.PropertyField(targetAvatar, new GUIContent("Target Avatar") {
				tooltip = "UserAvatar we are syncing with.\nNOTE: Drag the prefab with the UserAvatar here when modifying the input prefab."
			});
		}

		/// <summary>
		/// Function which displays the pose slot GUI for the provided SyncPose
		/// </summary>
		/// <param name="sync">The SyncPose to display a GUI for</param>
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

		/// <summary>
		/// Function which displays the mode selection GUI for the provided SyncPose
		/// </summary>
		/// <param name="sync">The SyncPose to display a GUI for</param>
		public void ModeField(SyncPose sync) {
			// Present a dropdown menu listing the possible modes (sync to/from)
			EditorGUILayout.PropertyField(mode, new GUIContent("Mode") {
				tooltip = "Should we send our transform to the pose, or update our transform to match the pose?"
			});
		}

		/// <summary>
		/// Function which displays the position weight and axis ignore settings GUI for the provided SyncPose
		/// </summary>
		/// <param name="sync">The SyncPose to display a GUI for</param>
		public void PositionSettingsField(SyncPose sync) {
			// Display weight slider (and log its change on the object)
			var weight = EditorGUILayout.Slider("Position Weight", sync.positionWeight, 0, 1);
			if (Math.Abs(weight - sync.positionWeight) > Mathf.Epsilon) {
				Undo.RecordObject(target, "Update Position's Weight");
				sync.positionWeight = weight;
			}
			
			// If the weight is non-zero, display the axes selection field
			if (!(positionWeight.floatValue > 0)) return;
			var axis = (SyncPose.SyncedAxis)EditorGUILayout.EnumFlagsField(new GUIContent("Position Axes") {
				tooltip = "The axes that should be synchronized."
			}, sync.positionAxis);
			// Log the change on the object (if a change occurred)
			if (axis == sync.positionAxis) return;
			Undo.RecordObject(target, "Update Position's Synced Axes");
			sync.positionAxis = axis;
		}

		/// <summary>
		/// Function which displays the rotation weight and axis ignore settings GUI for the provided SyncPose
		/// </summary>
		/// <param name="sync">The SyncPose to display a GUI for</param>
		public void RotationSettingsField(SyncPose sync) {
			// Display weight slider (and log its change on the object)
			var weight = EditorGUILayout.Slider("Rotation Weight", sync.rotationWeight, 0, 1);
			if (Math.Abs(weight - sync.rotationWeight) > Mathf.Epsilon) {
				Undo.RecordObject(target, "Update Rotation's Weight");
				sync.rotationWeight = weight;
			}
			
			// If the weight is non-zero, display the axes selection field
			if (!(rotationWeight.floatValue > 0)) return;
			var axis = (SyncPose.SyncedAxis)EditorGUILayout.EnumFlagsField(new GUIContent("Rotation Axes") {
				tooltip = "The axes that should be synchronized."
			}, sync.rotationAxis);
			// Log the change on the object (if a change occurred)
			if (axis == sync.rotationAxis) return;
			Undo.RecordObject(target, "Update Rotation's Synced Axes");
			sync.rotationAxis = axis;
		}

		/// <summary>
		/// Function which displays a GUI showing an uneditable display of the pose stored in the user avatar
		/// </summary>
		/// <param name="sync">The SyncPose to display a GUI for</param>
		public void PoseDebugField(SyncPose sync) {
			try {
				// Present a non-editable field with the debug pose (or an empty pose if the target is invalid)
				if (sync.targetAvatar is null || !sync.targetAvatar.slots.ContainsKey(sync.slot)) return;
				if(sync.mode == ISyncable.SyncMode.Store) 
					PoseField("Pose Debug", sync.targetAvatar.SetterPoseRef(sync.slot).pose, ref showTarget, false);
				else PoseField("Pose Debug", sync.targetAvatar.GetterPoseRef(sync.slot).pose, ref showTarget, false);
			} catch(NullReferenceException) {}
		}

		
		/// <summary>
		/// Function that validates weather or not a slot's name is invalid
		/// </summary>
		/// <param name="name">The name of the slot to check for</param>
		/// <returns>True if the slot exists in the target avatar, false otherwise</returns>
		protected bool ValidateSlot(string name) {
			var sync = (SyncPose)target;
			return sync?.targetAvatar?.slots?.Keys.Contains(name) ?? false;
		}
		
		/// <summary>
		/// Function that displays a pose field with dropdown hiding position and rotation
		/// </summary>
		/// <param name="label">Label that goes before the field</param>
		/// <param name="pose">Pose to edit in the field</param>
		/// <param name="show">Variable indicating if the field should be expanded or not</param>
		/// <param name="editable">Variable indicating if the field should be editable or read only</param>
		/// <param name="options">Additional options to control the layout</param>
		/// <returns></returns>
		protected Pose PoseField(string label, Pose pose, ref bool show, bool editable = true, 
		  params GUILayoutOption[] options) {
			// Save if the GUI is currently enabled
			var cache = GUI.enabled;
			
			// If we should expand the field
			if (show = EditorGUILayout.BeginFoldoutHeaderGroup(show, label)) {
				// Make the GUI editable if requested
				GUI.enabled = editable && cache;
				// Position and rotation GUIs
				var position = EditorGUILayout.Vector3Field("Position", pose.position, options);
				var rotation = Quaternion.Euler(EditorGUILayout.Vector3Field("Rotation", pose.rotation.eulerAngles, options));

				// If the value of the position or rotation has changed... record it
				if (position != pose.position || rotation != pose.rotation) {
					Undo.RecordObject(target, $"Update Pose {label}");
					pose.position = position;
					pose.rotation = rotation;
				}

				// Reset the editablity state
				GUI.enabled = cache;
			}

			// End the foldout group
			EditorGUILayout.EndFoldoutHeaderGroup();
			// Return the pose currently in the field
			return pose;
		}
		
		/// <summary>
		/// Callback function called when a new slot is selected. Updates the value of slot on the object and records the change
		/// </summary>
		/// <param name="s">The name of the selected slot</param>
		/// <exception cref="ArgumentException">Exception thrown if the slot is not a string!</exception>
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