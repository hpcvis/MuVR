using System;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace uMuVR.Utility.Constraints {
	public class SyncFingerPoseConstraint : SyncFingerPose {
		public JointRotationConstraint _knuckleJoint, _connectorJoint, _tipJoint;

		/// <summary>
		/// When the game starts create a proxy finger, and set each joint of the proxy finger as a target for a joint!
		/// </summary>
		protected new void Start() {
			mode = ISyncable.SyncMode.Load;
			base.Start();

			if (string.IsNullOrEmpty(slot)) return;
			base.knuckleJoint = targetAvatar.FindOrCreatePoseProxy(slot);
			DestroyImmediate(base.knuckleJoint.gameObject.GetComponent<SyncPose>());
			base.knuckleJoint.transform.position = _knuckleJoint.transform.position;
			base.knuckleJoint.transform.rotation = base.knuckleJoint.transform.rotation;
			
			base.connectorJoint = new GameObject { name = "Connector Joint Proxy", transform = { parent = base.knuckleJoint, position = _connectorJoint.transform.position, rotation = _connectorJoint.transform.rotation} }.transform;
			base.tipJoint = new GameObject { name = "Tip Joint Proxy", transform = { parent = base.connectorJoint, position = _tipJoint.transform.position, rotation = _tipJoint.transform.rotation } }.transform;

			_knuckleJoint.target = base.knuckleJoint;
			_connectorJoint.target = base.connectorJoint;
			_tipJoint.target = base.tipJoint;
		}

		public void OnValidate() {
			_knuckleJoint ??= GetComponent<JointRotationConstraint>();
			if (_knuckleJoint is null) return;

			JointRotationConstraint FindConnectorJoint() {
				var children = _knuckleJoint.GetComponentsInChildren<JointRotationConstraint>();
				return children.FirstOrDefault(child => child != _knuckleJoint);
			}
			_connectorJoint ??= FindConnectorJoint();
			if (_connectorJoint is null) return;
			
			JointRotationConstraint FindTipJoint() {
				var children = _connectorJoint.GetComponentsInChildren<JointRotationConstraint>();
				return children.FirstOrDefault(child => child != _connectorJoint);
			}
			_tipJoint ??= FindTipJoint();
		}
	}
	
#if UNITY_EDITOR
	/// <summary>
	/// Editor that makes hooking up a sync pose to slots much easier
	/// </summary>
	[CustomEditor(typeof(SyncFingerPoseConstraint))]
	[CanEditMultipleObjects]
	public class SyncFingerPoseConstraintEditor : SyncFingerPoseEditor {
		/// <summary>
		/// When the editor is enabled find references to the object's properties
		/// </summary>
		protected new void OnEnable() {
			targetAvatar = serializedObject.FindProperty("targetAvatar");
			mode = serializedObject.FindProperty("mode");
			localFingerRotationAxis = serializedObject.FindProperty("localFingerRotationAxis");
			knuckleJoint = serializedObject.FindProperty("_knuckleJoint");
			connectorJoint = serializedObject.FindProperty("_connectorJoint");
			tipJoint = serializedObject.FindProperty("_tipJoint");
		}
 
		/// <summary>
		/// Immediate mode GUI used to edit a SyncPose in the inspector
		/// </summary>
		public override void OnInspectorGUI() {
			var sync = (SyncFingerPoseConstraint)target;

			serializedObject.Update();

			TargetAvatarField(sync);
			PoseSlotField(sync);
			
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
	}
#endif
}