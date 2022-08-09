using UnityEngine;

namespace MuVR {
	
	public class WeightedUserAvatar : UserAvatarPostProcessed {
		public float positionAlpha = .9f;
		public float rotationAlpha = .9f;

		public override Pose OnPostProcess(string slot, Pose smoothed, Pose unsmoothed) {
			smoothed.position = positionAlpha * smoothed.position + (1 - positionAlpha) * unsmoothed.position;
			smoothed.rotation = Quaternion.Slerp(smoothed.rotation, unsmoothed.rotation, 1 - rotationAlpha);
			return smoothed;
		}
	}
}