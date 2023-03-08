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
	public class SyncFingerPose : MonoBehaviour, Utility.ISyncable {
		/// <summary>
		/// Enum setting weather we should be sending our transform to the pose, or reading our transform from the pose
		/// </summary>
		// public enum SyncMode {
		// 	SyncTo,
		// 	SyncFrom
		// }
		public enum Axis {
			X,
			Y,
			Z
		}


		[PropertyTooltip("UserAvatar we are syncing with.\nNOTE: Drag the prefab with the UserAvatar here when modifying the input prefab.")]
		[Required]
		public UserAvatar targetAvatar;

		[PropertyTooltip("Which pose on the avatar we are syncing with")]
		public string slot;

		[PropertyTooltip("Should we send our transform to the pose, or update our transform to match the pose?")]
		public ISyncable.SyncMode mode;

		public Axis localFingerRotationAxis = Axis.Z;

		public Transform knuckleJoint, connectorJoint, tipJoint;


		/// <summary>
		/// References to the pose we should store values in and get values from
		/// </summary>
		[SerializeField] protected UserAvatar.PoseRef setTarget, getTarget;
		
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
			if (mode == ISyncable.SyncMode.SyncTo) {
				setTarget.pose.rotation = knuckleJoint.rotation;
				setTarget.pose.position.x = CalculateOpenness();
			} else {
				knuckleJoint.rotation = getTarget.pose.rotation;
				ApplyOpenness(getTarget.pose.position.x);
			}
		}

		private float CalculateOpenness() {
			float knuckle2Con, con2Tip;
			switch (localFingerRotationAxis) {
				case Axis.X:
					knuckle2Con = connectorJoint.localRotation.eulerAngles.x;
					con2Tip = tipJoint.localRotation.eulerAngles.x;
					break;
				case Axis.Y:
					knuckle2Con = connectorJoint.localRotation.eulerAngles.y;
					con2Tip = tipJoint.localRotation.eulerAngles.y;
					break;
				case Axis.Z:
					knuckle2Con = connectorJoint.localRotation.eulerAngles.z;
					con2Tip = tipJoint.localRotation.eulerAngles.z;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			var connectorOpenness = Enhanced.Mathf.UnclampedInverseLerp(-90, 0, Enhanced.Mathf.AngleTo180s(knuckle2Con));
			var tipOpenness = Enhanced.Mathf.UnclampedInverseLerp(-90, 0, Enhanced.Mathf.AngleTo180s(con2Tip));

			return (connectorOpenness + tipOpenness) / 2;
		}

		private void ApplyOpenness(float openness) {
			var angle = Enhanced.Mathf.AngleTo360s(UnityEngine.Mathf.LerpUnclamped(-90, 0, openness));

			switch (localFingerRotationAxis) {
				case Axis.X:
					connectorJoint.localRotation = Quaternion.Euler(angle, connectorJoint.localRotation.eulerAngles.y, connectorJoint.localRotation.eulerAngles.z);
					tipJoint.localRotation = Quaternion.Euler(angle, tipJoint.localRotation.eulerAngles.y, tipJoint.localRotation.eulerAngles.z);
					break;
				case Axis.Y:
					connectorJoint.localRotation = Quaternion.Euler(connectorJoint.localRotation.eulerAngles.x, angle, connectorJoint.localRotation.eulerAngles.z);
					tipJoint.localRotation = Quaternion.Euler(tipJoint.localRotation.eulerAngles.x, angle, tipJoint.localRotation.eulerAngles.z);
					break;
				case Axis.Z:
					connectorJoint.localRotation = Quaternion.Euler(connectorJoint.localRotation.eulerAngles.x, connectorJoint.localRotation.eulerAngles.y, angle);
					tipJoint.localRotation = Quaternion.Euler(tipJoint.localRotation.eulerAngles.x, tipJoint.localRotation.eulerAngles.y, angle);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
		
		
		

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
	[CustomEditor(typeof(SyncFingerPose))]
	[CanEditMultipleObjects]
	public class SyncFingerPoseEditor : SyncPoseEditor {
		/// <summary>
		/// Properties of the object we wish to show a default UI for
		/// </summary>
		protected SerializedProperty /*targetAvatar, mode,*/ localFingerRotationAxis, knuckleJoint, connectorJoint, tipJoint;

		/// <summary>
		/// When the editor is enabled find references to the object's properties
		/// </summary>
		protected new void OnEnable() {
			targetAvatar = serializedObject.FindProperty("targetAvatar");
			mode = serializedObject.FindProperty("mode");
			localFingerRotationAxis = serializedObject.FindProperty("localFingerRotationAxis");
			knuckleJoint = serializedObject.FindProperty("knuckleJoint");
			connectorJoint = serializedObject.FindProperty("connectorJoint");
			tipJoint = serializedObject.FindProperty("tipJoint");
		}
 
		/// <summary>
		/// Immediate mode GUI used to edit a SyncPose in the inspector
		/// </summary>
		public override void OnInspectorGUI() {
			var sync = (SyncFingerPose)target;

			serializedObject.Update();

			TargetAvatarField(sync);
			PoseSlotField(sync);
			ModeField(sync);
			
			EditorGUILayout.PropertyField(knuckleJoint);
			EditorGUILayout.PropertyField(connectorJoint);
			EditorGUILayout.PropertyField(tipJoint);

			// Toggle hiding additional settings
			sync.showSettings = EditorGUILayout.Foldout(sync.showSettings, "Additional Settings", EditorStyles.foldoutHeader);
			if (sync.showSettings) {
				// Present a field with rotation settings
				EditorGUILayout.PropertyField(localFingerRotationAxis);
			}
			
			OpennessDebugField(sync);

			// Apply changes to the fields
			var oldAvatar = sync.targetAvatar;
			serializedObject.ApplyModifiedProperties();
			// If the target avatar has changed, automatically select its first slot (if the name is no longer valid!)
			if (sync.targetAvatar != oldAvatar && sync.targetAvatar is not null)
				if(!sync.targetAvatar.slots.ContainsKey(sync.slot))
					sync.slot = sync.targetAvatar.slots.Keys.First();
		}

		/// <summary>
		/// Function which displays the pose slot GUI for the provided SyncPose
		/// </summary>
		/// <param name="sync">The SyncPose to display a GUI for</param>
		public void PoseSlotField(SyncFingerPose sync) {
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
		public void ModeField(SyncFingerPose sync) {
			// Present a dropdown menu listing the possible modes (sync to/from)
			EditorGUILayout.PropertyField(mode, new GUIContent("Mode") {
				tooltip = "Should we send our transform to the pose, or update our transform to match the pose?"
			});
		}
		
		/// <summary>
		/// Function which displays a GUI showing an uneditable display of the pose stored in the user avatar
		/// </summary>
		/// <param name="sync">The SyncPose to display a GUI for</param>
		public void OpennessDebugField(SyncFingerPose sync) {
			try {
				// Present a non-editable field with the debug pose (or an empty pose if the target is invalid)
				if (sync.targetAvatar is null || !sync.targetAvatar.slots.ContainsKey(sync.slot)) return;
				if(sync.mode == ISyncable.SyncMode.SyncTo) 
					OpennessField("Pose Debug", sync.targetAvatar.SetterPoseRef(sync.slot).pose, ref showTarget, false);
				else OpennessField("Pose Debug", sync.targetAvatar.GetterPoseRef(sync.slot).pose, ref showTarget, false);
			} catch(NullReferenceException) {}
		}
		
		/// <summary>
		/// Function that validates weather or not a slot's name is invalid
		/// </summary>
		/// <param name="name">The name of the slot to check for</param>
		/// <returns>True if the slot exists in the target avatar, false otherwise</returns>
		protected bool ValidateSlot(string name) {
			var sync = (SyncFingerPose)target;
			return sync?.targetAvatar?.slots?.Keys.Contains(name) ?? false;
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
			var sync = (SyncFingerPose)target;
			sync.slot = slot;
		}
		
		/// <summary>
		/// Function that displays a pose field with dropdown hiding openness and rotation
		/// </summary>
		/// <param name="label">Label that goes before the field</param>
		/// <param name="pose">Pose to edit in the field</param>
		/// <param name="show">Variable indicating if the field should be expanded or not</param>
		/// <param name="editable">Variable indicating if the field should be editable or read only</param>
		/// <param name="options">Additional options to control the layout</param>
		/// <returns></returns>
		public Pose OpennessField(string label, Pose pose, ref bool show, bool editable = true, 
			params GUILayoutOption[] options) {
			// Save if the GUI is currently enabled
			var cache = GUI.enabled;
			
			// If we should expand the field
			if (show = EditorGUILayout.BeginFoldoutHeaderGroup(show, label)) {
				// Make the GUI editable if requested
				GUI.enabled = editable && cache;
				// Position and rotation GUIs
				var openness = EditorGUILayout.FloatField("Openness", pose.position.x, options);
				var rotation = Quaternion.Euler(EditorGUILayout.Vector3Field("Rotation", pose.rotation.eulerAngles, options));

				// If the value of the position or rotation has changed... record it
				if (Mathf.Abs(openness - pose.position.x) > Mathf.Epsilon || rotation != pose.rotation) {
					Undo.RecordObject(target, $"Update Pose {label}");
					pose.position.x = openness;
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
	}
#endif
}