using UnityEngine;

namespace MuVR.Enhanced {
	
	// Class that adds some extension methods to the Transform class
	public static class TransformExtensions {
		// Allows setting the global scale of the Transform
		public static void SetGlobalScale(this Transform t, Vector3 globalScale) {
			t.localScale = Vector3.one;
			t.localScale = new Vector3(globalScale.x / t.lossyScale.x, globalScale.y / t.lossyScale.y,
				globalScale.z / t.lossyScale.z);
		}

		// Converts a Transform into a Pose
		public static void GetPose(this Transform t, out Pose p) {
			p.position = t.position;
			p.rotation = t.rotation;
		}

		public static Pose GetPose(this Transform t) {
			t.GetPose(out var p);
			return p;
		}

		// Converts a Pose into a Transform
		public static void CopyFrom(this Transform t, Pose p) {
			t.position = p.position;
			t.rotation = p.rotation;
		}

		// Calculates the total bounds of all colliders on/on children of this transform
		public static Bounds CalculateColliderBounds(this Transform transform) {
			var currentRotation = transform.rotation;
			transform.rotation = Quaternion.identity;

			var cs = transform.GetComponentsInChildren<Collider>();
			var bounds = cs[0].bounds;
			foreach (var c in cs) bounds.Encapsulate(c.bounds);

			transform.rotation = currentRotation;
			return bounds;
		}
	}
}