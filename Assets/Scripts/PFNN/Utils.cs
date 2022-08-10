using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Utils {
	public static float extraDirectionSmooth = 0.9f; // 0.9f;
	public static float extraVelocitySmooth = 0.9f;
	public static float extraStrafeSmooth = 0.9f;
	public static float extraCrouchedSmooth = 0.9f;
	public static float extraGaitSmooth = 0.1f;
	public static float extraJointSmooth = 0.5f;

	public struct WallPoints {
		public Vector2 wallStart;
		public Vector2 wallEnd;
	}

	public static Vector3 MixDirections(Vector3 from, Vector3 to, float smoothValue) {
		var xQuat = Quaternion.AngleAxis(Mathf.Atan2(from.x, from.z) * Mathf.Rad2Deg, new Vector3(0.0f, 1.0f, 0.0f));
		var yQuat = Quaternion.AngleAxis(Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, new Vector3(0.0f, 1.0f, 0.0f));
		var zQuat = Quaternion.Slerp(xQuat, yQuat, smoothValue);
		return zQuat * new Vector3(0.0f, 0.0f, 1.0f);
	}

	public static Quaternion QuaternionExponent(Vector3 vec) {
		var w = vec.magnitude;

		var quat = w < 0.01f
			? Quaternion.identity
			: new Quaternion( // Possible error (1016)
				vec.x * (Mathf.Sin(w) / w),
				vec.y * (Mathf.Sin(w) / w),
				vec.z * (Mathf.Sin(w) / w),
				Mathf.Cos(w));

		return Quaternion.Normalize(quat);
	}

	public static Vector2 SegmentNearest(Vector2 v, Vector2 w, Vector2 p) {
		var len = Vector2.Dot(v - w, v - w);

		if (len == 0.0f)
			return v;

		var t = Mathf.Clamp01(Vector2.Dot(p - v, w - v) / len);
		return v + t * (w - v);
	}

	public static Vector3 CloserToPoint(Vector3 a, Vector3 b, Vector3 target) {
		var aDist = (a - target).sqrMagnitude;
		var bDist = (b - target).sqrMagnitude;
		return aDist < bDist ? a : b;
	}

	public static Vector3 ToFixedHeight(Vector2 p, float y) => new Vector3(p.x, y, p.y);

	public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source) {
		return source.Select((item, index) => (item, index));
	}
	
	public static void Concatentate<T>(this IEnumerable<IEnumerable<T>> data, out T[] concatenatedData, out long[] starts) {
		starts = new long[data.LongCount()];
		starts[0] = 0;

		var concat = data.First();
		foreach (var (enumerable, i) in data.WithIndex().Skip(1)) {
			starts[i] = concat.LongCount();
			concat = concat.Concat(enumerable);
		}
		concatenatedData = concat.ToArray();
	}
}