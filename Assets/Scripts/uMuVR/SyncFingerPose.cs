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

		public GameObject knuckleJoint, connectorJoint, tipJoint;


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
				knuckleJoint.transform.rotation = getTarget.pose.rotation;
				ApplyOpenness(getTarget.pose.position.x);
			} else {
				setTarget.pose.rotation = knuckleJoint.transform.rotation;
				setTarget.pose.position.x = CalculateOpenness();
			}
		}

		private float CalculateOpenness() {
			float knuckle2Con, con2Tip;
			switch (localFingerRotationAxis) {
				case Axis.X:
					knuckle2Con = connectorJoint.transform.localRotation.eulerAngles.x;
					con2Tip = tipJoint.transform.localRotation.eulerAngles.x;
					break;
				case Axis.Y:
					knuckle2Con = connectorJoint.transform.localRotation.eulerAngles.y;
					con2Tip = tipJoint.transform.localRotation.eulerAngles.y;
					break;
				case Axis.Z:
					knuckle2Con = connectorJoint.transform.localRotation.eulerAngles.z;
					con2Tip = tipJoint.transform.localRotation.eulerAngles.z;
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
					connectorJoint.transform.localRotation = Quaternion.Euler(angle, connectorJoint.transform.localRotation.eulerAngles.y, connectorJoint.transform.localRotation.eulerAngles.z);
					tipJoint.transform.localRotation = Quaternion.Euler(angle, tipJoint.transform.localRotation.eulerAngles.y, tipJoint.transform.localRotation.eulerAngles.z);
					break;
				case Axis.Y:
					connectorJoint.transform.localRotation = Quaternion.Euler(connectorJoint.transform.localRotation.eulerAngles.x, angle, connectorJoint.transform.localRotation.eulerAngles.z);
					tipJoint.transform.localRotation = Quaternion.Euler(tipJoint.transform.localRotation.eulerAngles.x, angle, tipJoint.transform.localRotation.eulerAngles.z);
					break;
				case Axis.Z:
					connectorJoint.transform.localRotation = Quaternion.Euler(connectorJoint.transform.localRotation.eulerAngles.x, connectorJoint.transform.localRotation.eulerAngles.y, angle);
					tipJoint.transform.localRotation = Quaternion.Euler(tipJoint.transform.localRotation.eulerAngles.x, tipJoint.transform.localRotation.eulerAngles.y, angle);
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
			var sync = (SyncPose)target;

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
			
			PoseDebugField(sync);

			// Apply changes to the fields
			var oldAvatar = sync.targetAvatar;
			serializedObject.ApplyModifiedProperties();
			// If the target avatar has changed, automatically select its first slot (if the name is no longer valid!)
			if (sync.targetAvatar != oldAvatar && sync.targetAvatar is not null)
				if(!sync.targetAvatar.slots.ContainsKey(sync.slot))
					sync.slot = sync.targetAvatar.slots.Keys.First();
		}

		
	}
#endif
}