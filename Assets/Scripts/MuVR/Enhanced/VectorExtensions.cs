﻿using UnityEngine;

namespace MuVR.Enhanced {
	// Extensions to Unity's Vector Types
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
	}
	
	public static class Vector4Extensions {
		public static Vector4 ToVec4(this Vector2 vec, float z = 0, float w = 0) => new Vector4(vec.x, vec.y, z, w);
		public static Vector4 ToVec4(this Vector3 vec, float w = 0) => new Vector4(vec.x, vec.y, vec.z, w);
	}
	
}