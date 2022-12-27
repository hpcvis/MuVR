using System.Collections;
using FishNet;
using UnityEngine;

namespace uMuVR.Utility {
	/// <summary>
	///     Static class providing a function which starts a coroutine that runs another function after the specified duration
	///     (in seconds)
	/// </summary>
	public static class Timer {
		public delegate void VoidDel();

		/// <summary>
		///     Runs the given function after <paramref name="duration" /> seconds.
		/// </summary>
		/// <param name="toRun">The function to run</param>
		/// <param name="duration">The number of seconds to wait before running the function</param>
		/// <returns>An IEnumerator which expected to be passed to StartCoroutine to start the timer.</returns>
		public static IEnumerator Start(VoidDel toRun, float duration = 3) {
			var start = Time.time;
			while (Time.time - start < duration) yield return null;
			toRun();
		}
	}

	/// <summary>
	///     Static class providing a function which starts a coroutine that runs another function after the specified duration
	///     (in simulation ticks)
	/// </summary>
	public static class TickTimer {
		/// <summary>
		///     Runs the given function after <paramref name="duration" /> simulation ticks.
		/// </summary>
		/// <param name="toRun">The function to run</param>
		/// <param name="duration">The number of ticks to wait before running the function</param>
		/// <returns>An IEnumerator which expected to be passed to StartCoroutine to start the timer.</returns>
		public static IEnumerator Start(Timer.VoidDel toRun, uint tickDuration = 3) {
			var tm = InstanceFinder.TimeManager;
			var start = tm.Tick;
			while (tm.Tick - start < tickDuration) yield return null;
			toRun();
		}
	}
}