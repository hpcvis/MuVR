using System.Collections;
using FishNet;
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

	public static class TickTimer
	{
		// Runs the given function after <duration> ticks
		public static IEnumerator Start(Timer.VoidDel toRun, uint tickDuration = 3)
		{
			var tm = InstanceFinder.TimeManager;
			var start = tm.Tick;
			while (tm.Tick - start < tickDuration) yield return null;
			toRun();
		}
	}
}
