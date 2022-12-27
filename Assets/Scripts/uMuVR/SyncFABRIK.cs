using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace uMuVR {
	
	/// <summary>
	/// FABRIK inverse kinematics implementation with target points extracted from the user avatar
	/// </summary>
	/// <remarks>From: https://github.com/ditzel/SimpleIK</remarks>
	public class SyncFABRIK : MonoBehaviour, Utility.ISyncable {
		/// <summary>
		///     Chain length of bones
		/// </summary>
		[FormerlySerializedAs("ChainLength")] 
		public int chainLength = 2;
    
		[FormerlySerializedAs("avatar")] 
		public UserAvatar targetAvatar;

		/// <summary>
		///     Targets the chain should bend toward
		/// </summary>
		[FormerlySerializedAs("Target")] 
		public Transform targetTransform;
		public string targetJoint;
		[FormerlySerializedAs("Pole")] 
		public Transform poleTransform;
		public string poleJoint;

		/// <summary>
		///     Solver iterations per update
		/// </summary>
		[Header("Solver Parameters")] 
		[FormerlySerializedAs("Iterations")]
		public int iterations = 10;

		/// <summary>
		///     Distance when the solver stops
		/// </summary>
		[FormerlySerializedAs("Delta")] 
		public float acceptableError = 0.001f;

		/// <summary>
		///     Strength of going back to the start position.
		/// </summary>
		[FormerlySerializedAs("SnapBackStrength"), Range(0, 1)] 
		public float snapbackStrength = 1f;

		protected float[] boneLengths; //Target to Origin
		// protected float completeLength;
		protected Transform[] bones;
		protected Vector3[] positions;
		protected Vector3[] initialSuccessorDirections;
		protected Quaternion[] initialBoneRotations;
		protected Quaternion initialRotationTarget;
		protected Transform root;


		// Start is called before the first frame update
		protected void Awake() => Init();

		private void Start() {
			try {
				if (!string.IsNullOrEmpty(targetJoint))
					targetTransform = targetAvatar?.FindOrCreatePoseProxy(targetJoint) ?? targetTransform;
			} catch (ArgumentException){}
			
			try {
				if (!string.IsNullOrEmpty(poleJoint))
					poleTransform = targetAvatar?.FindOrCreatePoseProxy(poleJoint) ?? poleTransform;
			} catch (ArgumentException){}
		}

		private void Init() {
			//initial array
			bones = new Transform[chainLength + 1];
			positions = new Vector3[chainLength + 1];
			boneLengths = new float[chainLength];
			initialSuccessorDirections = new Vector3[chainLength + 1];
			initialBoneRotations = new Quaternion[chainLength + 1];

			//find root
			root = transform;
			for (var i = 0; i <= chainLength; i++) {
				if (root == null) throw new UnityException("The chain value is longer than the ancestor chain!");
				root = root.parent;
			}

			//init target
			if (targetTransform == null) {
				targetTransform = new GameObject(gameObject.name + " Target").transform;
				SetPositionRootSpace(targetTransform, GetPositionRootSpace(transform));
			}

			initialRotationTarget = GetRotationRootSpace(targetTransform);


			//init data
			var current = transform;
			// completeLength = 0;
			for (var i = bones.Length - 1; i >= 0; i--) {
				bones[i] = current;
				initialBoneRotations[i] = GetRotationRootSpace(current);

				if (i == bones.Length - 1) 
					//leaf
					initialSuccessorDirections[i] = GetPositionRootSpace(targetTransform) - GetPositionRootSpace(current);
				else {
					//mid bone
					initialSuccessorDirections[i] = GetPositionRootSpace(bones[i + 1]) - GetPositionRootSpace(current);
					boneLengths[i] = initialSuccessorDirections[i].magnitude;
					// completeLength += bonesLength[i];
				}

				current = current.parent;
			}
		}

		// Update is called once per frame
		private void LateUpdate() => ResolveIK();

		private void ResolveIK() {
			if (targetTransform == null) return;
			if (boneLengths.Length != chainLength) Init();

			//Fabric

			//  root
			//  (bone0) (bonelen 0) (bone1) (bonelen 1) (bone2)...
			//   x--------------------x--------------------x---...

			//get position
			for (var i = 0; i < bones.Length; i++)
				positions[i] = GetPositionRootSpace(bones[i]);

			var targetPosition = GetPositionRootSpace(targetTransform);
			var targetRotation = GetRotationRootSpace(targetTransform);

			// //1st is possible to reach?
			// if ((targetPosition - GetPositionRootSpace(Bones[0])).sqrMagnitude >= CompleteLength * CompleteLength)
			// {
			//     //just stretch it
			//     var direction = (targetPosition - Positions[0]).normalized;
			//     //set everything after root
			//     for (int i = 1; i < Positions.Length; i++)
			//         Positions[i] = Positions[i - 1] + direction * BonesLength[i - 1];
			// }
			// else
			// {
			for (var i = 0; i < positions.Length - 1; i++)
				positions[i + 1] = Vector3.Lerp(positions[i + 1], positions[i] + initialSuccessorDirections[i], snapbackStrength);

			for (var iteration = 0; iteration < iterations; iteration++) {
				//https://www.youtube.com/watch?v=UNoX65PRehA
				//back
				for (var i = positions.Length - 1; i > 0; i--)
					if (i == positions.Length - 1)
						positions[i] = targetPosition; //set it to target
					else positions[i] = positions[i + 1] + (positions[i] - positions[i + 1]).normalized * boneLengths[i]; //set in line on distance

				//forward
				for (var i = 1; i < positions.Length; i++)
					positions[i] = positions[i - 1] + (positions[i] - positions[i - 1]).normalized * boneLengths[i - 1];

				//close enough?
				if ((positions[^1] - targetPosition).sqrMagnitude < acceptableError * acceptableError)
					break;
			}
			// }

			//move towards pole
			if (poleTransform != null) {
				var polePosition = GetPositionRootSpace(poleTransform);
				for (var i = 1; i < positions.Length - 1; i++) {
					var plane = new Plane(positions[i + 1] - positions[i - 1], positions[i - 1]);
					var projectedPole = plane.ClosestPointOnPlane(polePosition);
					var projectedBone = plane.ClosestPointOnPlane(positions[i]);
					var angle = Vector3.SignedAngle(projectedBone - positions[i - 1], projectedPole - positions[i - 1], plane.normal);
					positions[i] = Quaternion.AngleAxis(angle, plane.normal) * (positions[i] - positions[i - 1]) + positions[i - 1];
				}
			}

			//set position & rotation
			for (var i = 0; i < positions.Length; i++) {
				if (i == positions.Length - 1)
					SetRotationRootSpace(bones[i], Quaternion.Inverse(targetRotation) * initialRotationTarget * Quaternion.Inverse(initialBoneRotations[i]));
				else SetRotationRootSpace(bones[i], Quaternion.FromToRotation(initialSuccessorDirections[i], positions[i + 1] - positions[i]) * Quaternion.Inverse(initialBoneRotations[i]));
				SetPositionRootSpace(bones[i], positions[i]);
			}
		}

		private Vector3 GetPositionRootSpace(Transform current) {
			if (root == null) return current.position;
			return Quaternion.Inverse(root.rotation) * (current.position - root.position);
		}

		private void SetPositionRootSpace(Transform current, Vector3 position) {
			if (root == null)
				current.position = position;
			else current.position = root.rotation * position + root.position;
		}

		private Quaternion GetRotationRootSpace(Transform current) {
			//inverse(after) * before => rot: before -> after
			if (root == null) return current.rotation;
			return Quaternion.Inverse(current.rotation) * root.rotation;
		}

		private void SetRotationRootSpace(Transform current, Quaternion rotation) {
			if (root == null)
				current.rotation = rotation;
			else current.rotation = root.rotation * rotation;
		}

		private void OnDrawGizmos() {
#if UNITY_EDITOR
			var current = transform;
			for (var i = 0; i < chainLength && current != null && current.parent != null; i++) {
				var position = current.position;
				var parentPosition = current.parent.position;
				var scale = Vector3.Distance(position, parentPosition) * 0.1f;
				Handles.matrix = Matrix4x4.TRS(position, Quaternion.FromToRotation(Vector3.up, parentPosition - position), new Vector3(scale, Vector3.Distance(parentPosition, position), scale));
				Handles.color = Color.green;
				Handles.DrawWireCube(Vector3.up * 0.5f, Vector3.one);
				current = current.parent;
			}
#endif
		}
	
#if UNITY_EDITOR
		// Editor that makes hooking up a sync pose to slots much easier
		[CustomEditor(typeof(SyncFABRIK))]
		[CanEditMultipleObjects]
		public class SyncFABRIKEditor : SyncPoseEditor {
			const string CUSTOM_TRANSFORM = "Custom Transform";
			
			public enum TargetSlot {
				Target,
				Pole,
			}
			
			// Functions that return references to slots
			protected ref string ReferenceTargetSlot(TargetSlot targetSlot) => ref (targetSlot == TargetSlot.Target ? ref ((SyncFABRIK)target).targetJoint : ref ((SyncFABRIK)target).poleJoint); 
			protected ref Transform ReferenceTargetTransform(TargetSlot targetSlot) => ref (targetSlot == TargetSlot.Target ? ref ((SyncFABRIK)target).targetTransform : ref ((SyncFABRIK)target).poleTransform); 
			
			// Properties of the object we wish to show a default UI for
			protected SerializedProperty chainLength, targetTransform, poleTransform, iterations, acceptableError, snapbackStrength;
			protected ref SerializedProperty ReferenceTargetTransformProperty(TargetSlot targetSlot) => ref (targetSlot == TargetSlot.Target ? ref targetTransform : ref poleTransform); 

			protected new void OnEnable() {
				targetAvatar = serializedObject.FindProperty("targetAvatar");
				chainLength  = serializedObject.FindProperty("chainLength");
				targetTransform = serializedObject.FindProperty("targetTransform");
				poleTransform = serializedObject.FindProperty("poleTransform");
				iterations = serializedObject.FindProperty("iterations");
				acceptableError = serializedObject.FindProperty("acceptableError");
				snapbackStrength = serializedObject.FindProperty("snapbackStrength");
			}

			// Immediate mode GUI used to edit a SyncPose in the inspector
			public override void OnInspectorGUI() {
				var sync = (SyncFABRIK)target;

				serializedObject.Update();

				EditorGUILayout.PropertyField(chainLength);
				TargetAvatarField(null);

				PoseSlotField(sync, TargetSlot.Target);
				PoseSlotField(sync, TargetSlot.Pole);
				
				EditorGUILayout.PropertyField(iterations);
				EditorGUILayout.PropertyField(acceptableError);
				EditorGUILayout.PropertyField(snapbackStrength);
				
				// Apply changes to the fields
				serializedObject.ApplyModifiedProperties();
			}
			
			
			
			public void PoseSlotField(SyncFABRIK sync, TargetSlot targetSlot) {
				ref var syncSlot = ref ReferenceTargetSlot(targetSlot);

				if (sync.targetAvatar is not null) {
					// Present a dropdown menu listing the slots found on the target (no list and disabled if not found)
					EditorGUILayout.BeginHorizontal();
					{
						EditorGUILayout.PrefixLabel(new GUIContent(targetSlot.ToString()) {
							tooltip = "Which pose on the avatar we are syncing with.\nNOTE: If there is not currently a Target Avatar selected, this field will not be editable."
						});
						if (EditorGUILayout.DropdownButton(new GUIContent(ValidateSlot(syncSlot) ? syncSlot : "INVALID"),
							    FocusType.Keyboard)) {
							var menu = new GenericMenu();

							foreach (var slot in sync.targetAvatar.slots.Keys)
								menu.AddItem(new GUIContent(slot), syncSlot == slot, (s) => OnSlotSelect(s, targetSlot), slot);
							menu.AddItem(new GUIContent(CUSTOM_TRANSFORM), syncSlot == CUSTOM_TRANSFORM, (s) => OnSlotSelect(s, targetSlot), CUSTOM_TRANSFORM);

							menu.ShowAsContext();
						}
					}
					EditorGUILayout.EndHorizontal();

				} else
					syncSlot = string.Empty; // Make sure the slot is cleared out and not overriding our custom transform!

				// If we are setting the transform manually, then display the transform property
				if (syncSlot != CUSTOM_TRANSFORM && sync.targetAvatar is not null) return;
				EditorGUILayout.PropertyField(ReferenceTargetTransformProperty(targetSlot));
			}
			
			protected new bool ValidateSlot(string name) {
				var sync = (SyncFABRIK)target;
				return name == CUSTOM_TRANSFORM || (sync?.targetAvatar?.slots?.Keys.Contains(name) ?? false);
			}
			
			// Function called when a new slot is selected
			protected void OnSlotSelect(object s, TargetSlot targetSlot) {
				if (s is not string slot)
					throw new ArgumentException(nameof(String));

				var sync = (SyncFABRIK)target;

				Undo.RecordObject(target, "Slot Select");
				ReferenceTargetSlot(targetSlot) = slot;
				if (slot != CUSTOM_TRANSFORM)
					ReferenceTargetTransform(targetSlot) = sync.targetAvatar.FindOrCreatePoseProxy(slot);

			}
		}
#endif
	}
}