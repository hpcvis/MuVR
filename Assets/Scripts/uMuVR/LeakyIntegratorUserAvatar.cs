using uMuVR.Enhanced;
using UnityEngine;

namespace uMuVR {
	/// <summary>
	/// PostProcessed UserAvatar which uses a leaky integrator to smooth severe sharp features in the input data
	/// </summary>
	public class LeakyIntegratorUserAvatar : UserAvatarPostProcessed {
		/// <summary>
		/// How much we should blend between old and new position data
		/// </summary>
		public float positionAlpha = .9f;
		/// <summary>
		/// How much we should blend between old and new rotation data
		/// </summary>
		public float rotationAlpha = .9f;
		/// <summary>
		/// How many times per second data should be blended
		/// </summary>
		public float frequency = 60;

	
		/// <summary>
		/// When post processing should be applied, apply the leaky integrator
		/// </summary>
		/// <param name="slot">The name of the slot which is being processed</param>
		/// <param name="smoothed">Reference to the smoothed pose associated with the slot</param>
		/// <param name="unsmoothed">Reference to the smoothed pose associated with the slot</param>
		/// <param name="dt">Elapsed time since the last post processing on this slot</param>
		/// <returns>Smoothed pose with the unsmoothed data leakily integrated</returns>
		public override Pose OnPostProcess(string slot, Pose smoothed, Pose unsmoothed, float dt) {
			var modified = new Pose {
				position = positionAlpha * smoothed.position + (1 - positionAlpha) * unsmoothed.position,
				rotation = Quaternion.Slerp(smoothed.rotation, unsmoothed.rotation, 1 - rotationAlpha)
			};
			// Preform the blending with respect to time
			return PoseExtensions.Lerp(smoothed, modified, dt * frequency);
		}
	}
}