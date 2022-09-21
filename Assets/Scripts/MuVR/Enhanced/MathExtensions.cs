using UnityEngine;

namespace MuVR.Enhanced {
	// Extensions to Unity's Math Types
	public static class Vector2Extensions {
		public static Vector2 Rotate(this Vector2 v, float degrees) {
			var sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
			var cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

			var tx = v.x;
			var ty = v.y;
			v.x = cos * tx - sin * ty;
			v.y = sin * tx + cos * ty;
			return v;
		}
		
		public static Vector3 FixedHeight(this Vector2 p, float y) => new Vector3(p.x, y, p.y);

		public static Vector2 ToVec2(this Vector3 vec) => new Vector2(vec.x, vec.y);
		public static Vector2 ToVec2(this Vector4 vec) => new Vector2(vec.x, vec.y);
	}

	public static class Vector3Extensions {
		public static Vector3 FixedHeight(this Vector3 p, float y) => new Vector3(p.x, y, p.z);
		
		public static Vector3 ToVec3(this Vector2 vec, float z = 0) => new Vector3(vec.x, vec.y, z);
		public static Vector3 ToVec3(this Vector4 vec) => new Vector3(vec.x, vec.y, vec.z);

		public static Quaternion AngularVelocityToQuaternion(this Vector3 v) => Quaternion.AngleAxis(v.magnitude, v.normalized);
	}
	
	public static class Vector4Extensions {
		public static Vector4 ToVec4(this Vector2 vec, float z = 0, float w = 0) => new Vector4(vec.x, vec.y, z, w);
		public static Vector4 ToVec4(this Vector3 vec, float w = 0) => new Vector4(vec.x, vec.y, vec.z, w);
	}

	public static class PoseExtensions {
		public static Pose Lerp(this Pose a, Pose b, float t) {
			Pose ret;
			ret.position = Vector3.Lerp(a.position, b.position, t);
			ret.rotation = Quaternion.Lerp(a.rotation, b.rotation, t);
			return ret;
		}
	}

	public static class QuaternionExtensions {
		public static Quaternion Diff(this Quaternion to, Quaternion from) => to * Quaternion.Inverse(from);

		public static Vector3 ToAngularVelocity(this Quaternion q) {
			q.ToAngleAxis(out var angle, out var axis);
			return axis * (angle * Mathf.Deg2Rad);
		}
	}
	
}