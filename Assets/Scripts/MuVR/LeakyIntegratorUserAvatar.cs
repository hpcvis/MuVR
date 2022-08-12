using MuVR.Enhanced;
using UnityEngine;

namespace MuVR {
	
	public class LeakyIntegratorUserAvatar : UserAvatarPostProcessed {
		public float positionAlpha = .9f;
		public float rotationAlpha = .9f;
		public float frequency = 60;

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