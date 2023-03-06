using UnityEngine;

namespace uMuVR.Enhanced {
	// Extensions to Unity's Math Types
	public static class Mathf {
		// Function which converts an angle in the range [0, 360] to the corresponding angle in the range [-180, 180]
		public static float AngleTo180s(float angle) {
			if (angle > 180)
				return angle - 360;
			return angle;
		}
		
		// Function which converts an angle in the range [-180, 180] to the corresponding angle in the range [0, 360]
		public static float AngleTo360s(float angle) {
			if (angle < 0)
				return angle + 360;
			return angle;
		}

		public static float UnclampedInverseLerp(float a, float b, float v) {
			return (v - a) / (b - a);
		}
	}
	
	public static class Vector2Extensions {
		/// <summary>
		///     Extension function that rotates a Vector2 around the origin <paramref name="degrees" /> degrees
		/// </summary>
		/// <param name="v">The vector to rotate</param>
		/// <param name="degrees">The number of degrees to rotate <paramref name="v" /> by</param>
		/// <returns><paramref name="v" /> rotated</returns>
		public static Vector2 Rotate(this Vector2 v, float degrees) {
			var sin = UnityEngine.Mathf.Sin(degrees * UnityEngine.Mathf.Deg2Rad);
			var cos = UnityEngine.Mathf.Cos(degrees * UnityEngine.Mathf.Deg2Rad);

			var tx = v.x;
			var ty = v.y;
			v.x = cos * tx - sin * ty;
			v.y = sin * tx + cos * ty;
			return v;
		}

		/// <summary>
		///     Extension function which converts a Vector2 to a Vector3 with the specified height
		/// </summary>
		/// <param name="p">Original vector</param>
		/// <param name="y">New height</param>
		/// <returns>
		///     Vector3 with its xz coordinates specified by <paramref name="p" /> and its y coordinate specified by
		///     <paramref name="y" />
		/// </returns>
		public static Vector3 FixedHeight(this Vector2 p, float y) {
			return new(p.x, y, p.y);
		}

		/// <summary>
		///     Extension function which truncates a larger vector down to a Vector2 (only keeping the x and y portions)
		/// </summary>
		/// <param name="vec">Original vector</param>
		public static Vector2 ToVec2(this Vector3 vec) {
			return new(vec.x, vec.y);
		}
		public static Vector2 ToVec2(this Vector4 vec) {
			return new(vec.x, vec.y);
		}

		/// <summary>
		///     Extension function which checks if two vectors are approximately equal
		/// </summary>
		/// <param name="a">The first vector</param>
		/// <param name="b">The second vector</param>
		/// <param name="epsilon">
		///     Optional epsilon value used to determine how much the vectors can diverge and still be
		///     considered equal.
		/// </param>
		/// <returns>
		///     True if the magnitude of the difference between the two vectors is less than <paramref name="epsilon" />,
		///     false otherwise
		/// </returns>
		public static bool Approximately(this Vector2 a, Vector2 b, float epsilon) {
			return (a - b).magnitude < epsilon;
		}
		public static bool Approximately(this Vector2 a, Vector2 b) {
			return Approximately(a, b,UnityEngine.Mathf.Epsilon);
		}
	}

	public static class Vector3Extensions {
		/// <summary>
		///     Extension function which converts a Vector3 to a Vector3 with the specified height
		/// </summary>
		/// <param name="p">Original vector</param>
		/// <param name="y">New height</param>
		/// <returns>
		///     Vector3 with its xz coordinates specified by <paramref name="p" /> and its y coordinate specified by
		///     <paramref name="y" />
		/// </returns>
		public static Vector3 FixedHeight(this Vector3 p, float y) {
			return new Vector3(p.x, y, p.z);
		}

		/// <summary>
		///     Extension function which truncates a larger vector down to a Vector3 (only keeping the x, y, and z portions)
		///     Or that extends a smaller vector to a Vector3 taking the missing coordinates as input
		/// </summary>
		/// <param name="vec">Original vector</param>
		/// <param name="z">The z component to append to a Vector2</param>
		public static Vector3 ToVec3(this Vector2 vec, float z = 0) {
			return new(vec.x, vec.y, z);
		}
		public static Vector3 ToVec3(this Vector4 vec) {
			return new(vec.x, vec.y, vec.z);
		}

		/// <summary>
		///     Extension function which checks if two vectors are approximately equal
		/// </summary>
		/// <param name="a">The first vector</param>
		/// <param name="b">The second vector</param>
		/// <param name="epsilon">
		///     Optional epsilon value used to determine how much the vectors can diverge and still be
		///     considered equal.
		/// </param>
		/// <returns>
		///     True if the magnitude of the difference between the two vectors is less than <paramref name="epsilon" />,
		///     false otherwise
		/// </returns>
		public static bool Approximately(this Vector3 a, Vector3 b, float epsilon) {
			return (a - b).magnitude < epsilon;
		}
		public static bool Approximately(this Vector3 a, Vector3 b) {
			return Approximately(a, b, UnityEngine.Mathf.Epsilon);
		}

		/// <summary>
		///     Extension function which converts a Vector3 representing an AngularVelocity in the physics simulation to its
		///     corresponding quaternion
		/// </summary>
		/// <param name="v">An angular velocity vector</param>
		/// <returns>The quaternion corresponding to <paramref name="v" /></returns>
		public static Quaternion AngularVelocityToQuaternion(this Vector3 v) {
			return Quaternion.AngleAxis(v.magnitude, v.normalized);
		}
	}

	public static class Vector4Extensions {
		/// <summary>
		///     Extension function that extends a smaller vector to a Vector4 taking the missing coordinates as input
		/// </summary>
		/// <param name="vec">Original vector</param>
		/// <param name="z">The z component to append to a Vector2</param>
		/// <param name="w">The w component to append to a Vector2/3</param>
		public static Vector4 ToVec4(this Vector2 vec, float z = 0, float w = 0) {
			return new(vec.x, vec.y, z, w);
		}
		public static Vector4 ToVec4(this Vector3 vec, float w = 0) {
			return new(vec.x, vec.y, vec.z, w);
		}

		/// <summary>
		///     Extension function which checks if two vectors are approximately equal
		/// </summary>
		/// <param name="a">The first vector</param>
		/// <param name="b">The second vector</param>
		/// <param name="epsilon">
		///     Optional epsilon value used to determine how much the vectors can diverge and still be
		///     considered equal.
		/// </param>
		/// <returns>
		///     True if the magnitude of the difference between the two vectors is less than <paramref name="epsilon" />,
		///     false otherwise
		/// </returns>
		public static bool Approximately(this Vector4 a, Vector4 b, float epsilon) {
			return (a - b).magnitude < epsilon;
		}
		public static bool Approximately(this Vector4 a, Vector4 b) {
			return Approximately(a, b, UnityEngine.Mathf.Epsilon);
		}
	}

	public static class PoseExtensions {
		/// <summary>
		///     Extension function which linearly interpolates between two poses
		/// </summary>
		/// <param name="a">The first pose</param>
		/// <param name="b">The second pose</param>
		/// <param name="t">The ratio of how much of the first pose should be blended with the second pose.</param>
		/// <returns><paramref name="a" /> * <paramref name="t" /> + <paramref name="b" /> * (1 - <paramref name="t" />)</returns>
		public static Pose Lerp(this Pose a, Pose b, float t) {
			Pose ret;
			ret.position = Vector3.Lerp(a.position, b.position, t);
			ret.rotation = Quaternion.Lerp(a.rotation, b.rotation, t);
			return ret;
		}
	}

	public static class QuaternionExtensions {
		/// <summary>
		///     Extension function that calculates the rotation to a given quaternion from another quaternion
		/// </summary>
		/// <param name="to">When the resulting rotation is applied to <paramref name="from" />, this rotation is achieved.</param>
		/// <param name="from">The rotation to "subtract" from <paramref name="to" /></param>
		/// <returns>The rotation which when applied to <paramref name="from" /> produced <paramref name="to" /></returns>
		public static Quaternion Diff(this Quaternion to, Quaternion from) {
			return to * Quaternion.Inverse(from);
		}

		/// <summary>
		///     Extension function which converts a quaternion to its corresponding angular velocity vector
		/// </summary>
		/// <param name="q">The quaternion to convert</param>
		/// <returns>The angular velocity vector corresponding to <paramref name="q" /></returns>
		public static Vector3 ToAngularVelocity(this Quaternion q) {
			q.ToAngleAxis(out var angle, out var axis);
			return axis * (angle * UnityEngine.Mathf.Deg2Rad);
		}
	}


	public static class TransformExtensions {
		/// <summary>
		///     Extension function which sets the scale of the transform (in global space)
		/// </summary>
		/// <param name="t">Transform to update</param>
		/// <param name="globalScale">Global space scale</param>
		public static void SetGlobalScale(this Transform t, Vector3 globalScale) {
			t.localScale = Vector3.one;
			t.localScale = new Vector3(globalScale.x / t.lossyScale.x, globalScale.y / t.lossyScale.y,
				globalScale.z / t.lossyScale.z);
		}

		/// <summary>
		///     Extension function that converts a transform into a pose
		/// </summary>
		/// <param name="t">The pose to convert</param>
		/// <param name="p">(optional) Output: The converted pose</param>
		/// <returns>The converted pose</returns>
		public static void GetPose(this Transform t, out Pose p) {
			p.position = t.position;
			p.rotation = t.rotation;
		}
		public static Pose GetPose(this Transform t) {
			t.GetPose(out var p);
			return p;
		}

		/// <summary>
		///     Extension function that updates a transform to match a pose
		/// </summary>
		/// <param name="t">The transform to update</param>
		/// <param name="p">The pose to be matched</param>
		public static void CopyFrom(this Transform t, Pose p) {
			t.position = p.position;
			t.rotation = p.rotation;
		}

		/// <summary>
		///     Extension function that calculates the total bounds of all colliders on (children of) this transform
		/// </summary>
		/// <param name="transform">The root transform to begin searching in</param>
		/// <returns>Bounds encompassing all of the colliders in <paramref name="transform" /> and its children</returns>
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