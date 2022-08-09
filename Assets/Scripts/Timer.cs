using System.Collections;
using UnityEngine;

namespace MuVR.Utility {
	
	public static class Timer {
		public delegate void VoidDel();

		// Runs the given function after <duration> seconds
		public static IEnumerator Start(VoidDel toRun, float duration = 3) {
			var start = Time.time;
			while (Time.time - start < duration) yield return null;
			toRun();
		}
	}
}